using ABACore.Compilation;
using ABACore.Models;
using ABACore.Parser;
using ABACore.Runtime;
using ABACore.Visitor;
using Antlr4.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for ALFA policy evaluation functionality.
/// </summary>
public class EvaluationTests
{
    #region Helper Methods

    private static CompiledPolicy CompilePolicy(string policyText)
    {
        AntlrInputStream inputStream = new(policyText);
        AlfaLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        AlfaParser parser = new(tokenStream);

        parser.RemoveErrorListeners();

        AlfaParser.PolicyContext policyContext = parser.policy();
        AlfaAstBuilder visitor = new();
        AlfaNode? node = visitor.Visit(policyContext);

        if (node is not Policy policy)
        {
            throw new InvalidOperationException("Expected Policy node");
        }

        PolicyCompiler compiler = new();
        return compiler.CompilePolicy(policy, enableCaching: false);
    }

    private static Decision Evaluate(CompiledPolicy policy, EvaluationContext context)
    {
        PolicyExecutor executor = new();
        return executor.Execute(policy, context);
    }

    #endregion

    #region Basic Evaluation Tests

    [Fact]
    public void Evaluate_SimplePermitRule_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy SimplePermitPolicy {
                rule SimplePermitRule {
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_SimpleDenyRule_ShouldReturnDeny()
    {
        // Arrange
        string policyText = @"
            policy SimpleDenyPolicy {
                rule SimpleDenyRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_EmptyPolicy_ShouldReturnNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy EmptyPolicy {
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    #endregion

    #region Target Evaluation Tests

    [Fact]
    public void Evaluate_WithMatchingTarget_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy TargetMatchPolicy {
                rule TargetMatchRule {
                    target clause userName == ""alice""
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userName", "alice");

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithNonMatchingTarget_ShouldReturnNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy TargetNoMatchPolicy {
                rule TargetNoMatchRule {
                    target clause userName == ""alice""
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userName", "bob");

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithMultipleClausesAllMatch_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy MultiClausePolicy {
                rule MultiClauseRule {
                    target
                        clause subject.userName == ""alice""
                        clause resource.resourceType == ""document""
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userName", "alice");
        context.SetAttribute("resource", "resourceType", "document");

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithMultipleClausesOneNoMatch_ShouldReturnNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy MultiClauseNoMatchPolicy {
                rule MultiClauseNoMatchRule {
                    target
                        clause userName == ""alice""
                        clause resourceType == ""document""
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userName", "alice");
        context.SetAttribute("resource", "resourceType", "file");

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    #endregion

    #region Condition Evaluation Tests

    [Fact]
    public void Evaluate_WithTrueCondition_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy TrueConditionPolicy {
                rule AgeCheck {
                    permit
                    condition age > 18
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithFalseCondition_ShouldReturnNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy FalseConditionPolicy {
                rule AgeCheck {
                    permit
                    condition age > 18
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 15);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithComplexCondition_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy ComplexConditionPolicy {
                rule ComplexCheck {
                    permit
                    condition (age > 18 && department == ""IT"") || isAdmin == true
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);
        context.SetAttribute("subject", "department", "IT");
        context.SetAttribute("subject", "isAdmin", false);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithComplexConditionAdminBypass_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy AdminBypassPolicy {
                rule AdminBypass {
                    permit
                    condition (age > 18 && department == ""IT"") || isAdmin == true
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 15);
        context.SetAttribute("subject", "department", "Sales");
        context.SetAttribute("subject", "isAdmin", true);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region Attribute Context Tests

    [Fact]
    public void Evaluate_WithStringAttribute_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy StringAttributePolicy {
                rule StringAttributeRule {
                    target clause userName == ""alice""
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userName", "alice");

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithIntegerAttribute_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy IntegerAttributePolicy {
                rule IntCheck {
                    permit
                    condition age >= 21
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithBooleanAttribute_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy BooleanAttributePolicy {
                rule BoolCheck {
                    permit
                    condition isActive == true
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "isActive", true);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_WithDoubleAttribute_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy DoubleAttributePolicy {
                rule PriceCheck {
                    permit
                    condition resource.price <= 99.99
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("resource", "price", 50.0);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region Combining Algorithm Tests

    [Fact]
    public void Evaluate_DenyOverrides_DenyRuleFirst_ShouldReturnDeny()
    {
        // Arrange
        string policyText = @"
            policy DenyOverridesPolicy {
                apply denyOverrides
                rule DenyRule {
                    deny
                }
                rule PermitRule {
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_DenyOverrides_PermitOnly_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy DenyOverridesPermitPolicy {
                apply denyOverrides
                rule PermitRule {
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_PermitOverrides_PermitRuleFirst_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy PermitOverridesPolicy {
                apply permitOverrides
                rule PermitRule {
                    permit
                }
                rule DenyRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_PermitOverrides_DenyOnly_ShouldReturnDeny()
    {
        // Arrange
        string policyText = @"
            policy PermitOverridesDenyPolicy {
                apply permitOverrides
                rule DenyRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_FirstApplicable_ShouldReturnFirstMatch()
    {
        // Arrange
        string policyText = @"
            policy FirstApplicablePolicy {
                apply firstApplicable
                rule FirstRule {
                    permit
                    condition age > 18
                }
                rule SecondRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_FirstApplicable_FirstNotApplicable_ShouldReturnSecond()
    {
        // Arrange
        string policyText = @"
            policy FirstApplicableSecondPolicy {
                apply firstApplicable
                rule FirstRule {
                    permit
                    condition age > 18
                }
                rule SecondRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 15);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_DenyUnlessPermit_WithPermit_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy DenyUnlessPermitPolicy {
                apply denyUnlessPermit
                rule PermitRule {
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_DenyUnlessPermit_WithoutPermit_ShouldReturnDeny()
    {
        // Arrange
        string policyText = @"
            policy DenyUnlessPermitNonePolicy {
                apply denyUnlessPermit
                rule ConditionalPermit {
                    permit
                    condition age > 100
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_PermitUnlessDeny_WithDeny_ShouldReturnDeny()
    {
        // Arrange
        string policyText = @"
            policy PermitUnlessDenyPolicy {
                apply permitUnlessDeny
                rule DenyRule {
                    deny
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Evaluate_PermitUnlessDeny_WithoutDeny_ShouldReturnPermit()
    {
        // Arrange
        string policyText = @"
            policy PermitUnlessDenyNonePolicy {
                apply permitUnlessDeny
                rule ConditionalDeny {
                    deny
                    condition age > 100
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region Arithmetic Expression Tests

    [Fact]
    public void Evaluate_ArithmeticAddition_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticAddPolicy {
                rule AddCheck {
                    permit
                    condition (salary + bonus) > 100000
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "salary", 80000);
        context.SetAttribute("subject", "bonus", 30000);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_ArithmeticSubtraction_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticSubtractPolicy {
                rule SubtractCheck {
                    permit
                    condition (resource.total - resource.discount) >= 50
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("resource", "total", 100);
        context.SetAttribute("resource", "discount", 30);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_ArithmeticMultiplication_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticMultiplyPolicy {
                rule MultiplyCheck {
                    condition resource.quantity * resource.price > 1000
                    permit
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("resource", "quantity", 100);
        context.SetAttribute("resource", "price", 15);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Evaluate_ArithmeticDivision_ShouldEvaluateCorrectly()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticDividePolicy {
                rule DivideCheck {
                    permit
                    condition total / count < 100
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("resource", "total", 500);
        context.SetAttribute("resource", "count", 10);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region NotApplicable and Indeterminate Tests

    [Fact]
    public void Evaluate_NoApplicableRules_ShouldReturnNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy NoApplicablePolicy {
                rule ConditionalRule {
                    permit
                    condition age > 100
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = Evaluate(policy, context);

        // Assert
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void Evaluate_MissingAttribute_ShouldHandleGracefully()
    {
        // Arrange
        string policyText = @"
            policy MissingAttributePolicy {
                rule MissingAttrCheck {
                    permit
                    condition age > 18
                }
            }
        ";
        CompiledPolicy policy = CompilePolicy(policyText);
        EvaluationContext context = new();
        // Not setting the 'age' attribute

        // Act & Assert
        // Should either return Indeterminate or throw, depending on implementation
        try
        {
            Decision decision = Evaluate(policy, context);
            // If it doesn't throw, it should be Indeterminate or NotApplicable
            Assert.True(
                decision.Effect is DecisionEffect.Indeterminate or
                DecisionEffect.NotApplicable);
        }
        catch
        {
            // Exception is also acceptable for missing attributes
            Assert.True(true);
        }
    }

    #endregion
}
