using ABACore.Models;
using ABACore.Parser;
using ABACore.Visitor;
using Antlr4.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for ALFA policy parsing functionality.
/// </summary>
public class ParserTests
{
    #region Helper Methods

    private static Policy ParsePolicy(string policyText)
    {
        AntlrInputStream inputStream = new(policyText);
        AlfaLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        AlfaParser parser = new(tokenStream);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());

        AlfaParser.PolicyContext policyContext = parser.policy();
        AlfaAstBuilder visitor = new();
        AlfaNode? node = visitor.Visit(policyContext);

        return node is not Policy policy ? throw new InvalidOperationException("Expected Policy node") : policy;
    }

    private class ThrowingErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new InvalidOperationException($"Parse error at {line}:{charPositionInLine}: {msg}");
        }
    }

    #endregion

    #region Basic Policy Parsing Tests

    [Fact]
    public void ParseSimplePolicy_WithPermitRule_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy SimplePolicy {
                rule AllowAll {
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal("SimplePolicy", policy.Id);
        Assert.Single(policy.Rules);
        Assert.Equal(Effect.Permit, policy.Rules[0].Effect);
    }

    [Fact]
    public void ParseSimplePolicy_WithDenyRule_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy SimpleDenyPolicy {
                rule DenyAll {
                    deny
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal("SimpleDenyPolicy", policy.Id);
        Assert.Single(policy.Rules);
        Assert.Equal(Effect.Deny, policy.Rules[0].Effect);
    }

    [Fact]
    public void ParsePolicy_WithMultipleRules_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy MultiRulePolicy {
                rule Rule1 {
                    permit
                }
                rule Rule2 {
                    deny
                }
                rule Rule3 {
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(3, policy.Rules.Count);
        Assert.Equal(Effect.Permit, policy.Rules[0].Effect);
        Assert.Equal(Effect.Deny, policy.Rules[1].Effect);
        Assert.Equal(Effect.Permit, policy.Rules[2].Effect);
    }

    [Fact]
    public void ParsePolicy_WithNamedRules_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy NamedRulesPolicy {
                rule AllowAccess { permit }
                rule DenyAccess { deny }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(2, policy.Rules.Count);
        Assert.Equal("AllowAccess", policy.Rules[0].Id);
        Assert.Equal("DenyAccess", policy.Rules[1].Id);
    }

    #endregion

    #region Combining Algorithm Tests

    [Fact]
    public void ParsePolicy_WithDenyOverrides_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy DenyOverridesPolicy {
                apply denyOverrides
                rule PermitRule {
                    permit
                }
                rule DenyRule {
                    deny
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.DenyOverrides, policy.Combinator);
    }

    [Fact]
    public void ParsePolicy_WithPermitOverrides_ShouldSucceed()
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

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.PermitOverrides, policy.Combinator);
    }

    [Fact]
    public void ParsePolicy_WithFirstApplicable_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy FirstApplicablePolicy {
                apply firstApplicable
                rule PermitRule {
                    permit
                }
                rule DenyRule {
                    deny
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.FirstApplicable, policy.Combinator);
    }

    [Fact]
    public void ParsePolicy_WithOnlyOne_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy OnlyOnePolicy {
                apply onlyOne
                rule PermitRule {
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.OnlyOne, policy.Combinator);
    }

    [Fact]
    public void ParsePolicy_WithDenyUnlessPermit_ShouldSucceed()
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

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.DenyUnlessPermit, policy.Combinator);
    }

    [Fact]
    public void ParsePolicy_WithPermitUnlessDeny_ShouldSucceed()
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

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(CombiningAlgorithm.PermitUnlessDeny, policy.Combinator);
    }

    #endregion

    #region Target and Clause Tests

    [Fact]
    public void ParsePolicy_WithSimpleTarget_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy TargetPolicy {
                rule AllowJohn {
                    target clause userName == ""john""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Assert.Single(rule.Target.Clauses);
    }

    [Fact]
    public void ParseRule_WithTarget_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy RuleWithTarget {
                rule AccessRule {
                    target clause resourceType == ""document""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Assert.Single(rule.Target.Clauses);
    }

    [Fact]
    public void ParsePolicy_WithMultipleClauses_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy MultiClausePolicy {
                rule AdminDatabaseAccess {
                    target
                        clause userName == ""admin""
                        clause resourceType == ""database""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Assert.Equal(2, rule.Target.Clauses.Count);
    }

    #endregion

    #region Condition Tests

    [Fact]
    public void ParseRule_WithSimpleCondition_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ConditionPolicy {
                rule ConditionalAccess {
                    permit
                    condition age > 18
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        Assert.NotNull(rule.Condition.Expression);
    }

    [Fact]
    public void ParseRule_WithComplexCondition_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ComplexConditionPolicy {
                rule ComplexAccess {
                    permit
                    condition (age > 18 && department == ""IT"") || isAdmin == true
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        Assert.IsType<LogicalBinaryExpression>(rule.Condition.Expression);
    }

    #endregion

    #region Expression Tests

    [Fact]
    public void ParseExpression_StringLiteral_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy StringLiteralPolicy {
                rule AllowAlice {
                    target clause userName == ""alice""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Clause clause = rule.Target.Clauses[0];
        Assert.IsType<ComparisonExpression>(clause.Expression);

        var comparison = (ComparisonExpression)clause.Expression;
        Assert.Equal(ComparisonOperator.Equal, comparison.Operator);
        Assert.IsType<LiteralStringExpression>(comparison.Right);
    }

    [Fact]
    public void ParseExpression_IntegerLiteral_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy IntegerLiteralPolicy {
                rule AgeCheck {
                    permit
                    condition age >= 21
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        Assert.IsType<LiteralIntegerExpression>(comparison.Right);
        Assert.Equal(21, ((LiteralIntegerExpression)comparison.Right).Value);
    }

    [Fact]
    public void ParseExpression_DoubleLiteral_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy DoubleLiteralPolicy {
                rule PriceCheck {
                    permit
                    condition price <= 99.99
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        Assert.IsType<LiteralDoubleExpression>(comparison.Right);
    }

    [Fact]
    public void ParseExpression_BooleanLiteral_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy BooleanLiteralPolicy {
                rule AdminCheck {
                    permit
                    condition isActive == true
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        Assert.IsType<LiteralBooleanExpression>(comparison.Right);
    }

    #endregion

    #region Arithmetic Expression Tests

    [Fact]
    public void ParseExpression_Addition_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticPolicy {
                rule AdditionCheck {
                    permit
                    condition (salary + bonus) > 100000
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        Assert.IsType<BinaryExpression>(comparison.Left);

        var binary = (BinaryExpression)comparison.Left;
        Assert.Equal(BinaryOperator.Add, binary.Operator);
    }

    [Fact]
    public void ParseExpression_Subtraction_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy SubtractionPolicy {
                rule SubtractionCheck {
                    permit
                    condition (total - discount) >= 50
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        var binary = (BinaryExpression)comparison.Left;
        Assert.Equal(BinaryOperator.Subtract, binary.Operator);
    }

    [Fact]
    public void ParseExpression_Multiplication_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy MultiplicationPolicy {
                rule MultiplyCheck {
                    permit
                    condition quantity * price > 1000
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        var binary = (BinaryExpression)comparison.Left;
        Assert.Equal(BinaryOperator.Multiply, binary.Operator);
    }

    [Fact]
    public void ParseExpression_Division_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy DivisionPolicy {
                rule DivisionCheck {
                    permit
                    condition total / count < 100
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var comparison = (ComparisonExpression)rule.Condition.Expression;
        var binary = (BinaryExpression)comparison.Left;
        Assert.Equal(BinaryOperator.Divide, binary.Operator);
    }

    #endregion

    #region Comparison Operator Tests

    [Fact]
    public void ParseExpression_ComparisonOperators_ShouldSucceed()
    {
        // Arrange
        (string, ComparisonOperator)[] testCases =
        [
            ("age == 30", ComparisonOperator.Equal),
            ("age != 30", ComparisonOperator.NotEqual),
            ("age > 30", ComparisonOperator.GreaterThan),
            ("age < 30", ComparisonOperator.LessThan),
            ("age >= 30", ComparisonOperator.GreaterThanOrEqual),
            ("age <= 30", ComparisonOperator.LessThanOrEqual)
        ];

        foreach ((string condition, ComparisonOperator expectedOperator) in testCases)
        {
            string policyText = $@"
                policy ComparisonPolicy {{
                    rule ComparisonRule {{
                        permit
                        condition {condition}
                    }}
                }}
            ";

            // Act
            Policy policy = ParsePolicy(policyText);

            // Assert
            Rule rule = policy.Rules[0];
            Assert.NotNull(rule.Condition);
            var comparison = (ComparisonExpression)rule.Condition.Expression;
            Assert.Equal(expectedOperator, comparison.Operator);
        }
    }

    #endregion

    #region Boolean Expression Tests

    [Fact]
    public void ParseExpression_LogicalAnd_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy LogicalAndPolicy {
                rule AndCheck {
                    permit
                    condition age > 18 && isActive == true
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var logical = (LogicalBinaryExpression)rule.Condition.Expression;
        Assert.Equal(LogicalOperator.And, logical.Operator);
    }

    [Fact]
    public void ParseExpression_LogicalOr_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy LogicalOrPolicy {
                rule OrCheck {
                    permit
                    condition isAdmin == true || isSupervisor == true
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        var logical = (LogicalBinaryExpression)rule.Condition.Expression;
        Assert.Equal(LogicalOperator.Or, logical.Operator);
    }

    [Fact]
    public void ParseExpression_LogicalNot_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy LogicalNotPolicy {
                rule NotCheck {
                    permit
                    condition not (isBlocked == true)
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        Assert.IsType<NotExpression>(rule.Condition.Expression);
    }

    #endregion

    #region Attribute Designator Tests

    [Fact]
    public void ParseExpression_AttributeDesignator_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy AttributeDesignatorPolicy {
                rule AllowBob {
                    target clause userName == ""bob""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Clause clause = rule.Target.Clauses[0];
        var comparison = (ComparisonExpression)clause.Expression;
        Assert.IsType<AttributeDesignator>(comparison.Left);

        var attrDesignator = (AttributeDesignator)comparison.Left;
        Assert.Equal("userName", attrDesignator.AttributeName);
        // In the new system, simple attributes have no namespace prefix (resolved later based on imports)
        Assert.Equal("", attrDesignator.Namespace);
    }

    [Fact]
    public void ParseExpression_NamespaceQualifiedAttributeDesignator_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy NamespaceAttributePolicy {
                rule AllowAdmin {
                    target clause Subject.Role == ""admin""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Clause clause = rule.Target.Clauses[0];
        var comparison = (ComparisonExpression)clause.Expression;
        Assert.IsType<AttributeDesignator>(comparison.Left);

        var attrDesignator = (AttributeDesignator)comparison.Left;
        Assert.Equal("Subject", attrDesignator.Namespace);
        Assert.Equal("Role", attrDesignator.AttributeName);
    }

    [Fact]
    public void ParseExpression_FullyQualifiedAttributeDesignator_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy FullyQualifiedAttributePolicy {
                rule CheckResourceType {
                    target clause Oasis.Attributes.Resource.Type == ""document""
                    permit
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Assert.NotNull(policy);
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Target);
        Clause clause = rule.Target.Clauses[0];
        var comparison = (ComparisonExpression)clause.Expression;
        Assert.IsType<AttributeDesignator>(comparison.Left);

        var attrDesignator = (AttributeDesignator)comparison.Left;
        Assert.Equal("Oasis.Attributes.Resource", attrDesignator.Namespace);
        Assert.Equal("Type", attrDesignator.AttributeName);
    }

    #endregion

    #region Function Call Tests

    [Fact]
    public void ParseExpression_FunctionCall_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy FunctionCallPolicy {
                rule StringFunction {
                    permit
                    condition stringEqual(userName, ""admin"")
                }
            }
        ";

        // Act
        Policy policy = ParsePolicy(policyText);

        // Assert
        Rule rule = policy.Rules[0];
        Assert.NotNull(rule.Condition);
        Assert.IsType<BooleanFunctionCall>(rule.Condition.Expression);

        var funcCall = (BooleanFunctionCall)rule.Condition.Expression;
        Assert.Equal("stringEqual", funcCall.FunctionName);
        Assert.Equal(2, funcCall.Parameters.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ParsePolicy_WithInvalidSyntax_ShouldThrowException()
    {
        // Arrange
        string policyText = @"
            policy InvalidPolicy {
                permit deny
            }
        ";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ParsePolicy(policyText));
    }

    [Fact]
    public void ParsePolicy_WithMissingBrace_ShouldThrowException()
    {
        // Arrange
        string policyText = @"
            policy MissingBracePolicy {
                permit
        ";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ParsePolicy(policyText));
    }

    [Fact]
    public void ParsePolicy_EmptyString_ShouldThrowException()
    {
        // Arrange
        string policyText = "";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ParsePolicy(policyText));
    }

    #endregion
}
