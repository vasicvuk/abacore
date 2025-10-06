using ABACore.Compilation;
using ABACore.Models;
using ABACore.Parser;
using ABACore.Runtime;
using ABACore.Visitor;
using Antlr4.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for ALFA policy compilation functionality.
/// </summary>
public class CompilationTests
{
    #region Helper Methods

    private static Policy ParsePolicy(string policyText)
    {
        AntlrInputStream inputStream = new(policyText);
        AlfaLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        AlfaParser parser = new(tokenStream);

        parser.RemoveErrorListeners();

        AlfaParser.PolicyContext policyContext = parser.policy();
        AlfaAstBuilder visitor = new();
        AlfaNode? node = visitor.Visit(policyContext);

        return node is not Policy policy ? throw new InvalidOperationException("Expected Policy node") : policy;
    }

    #endregion

    #region Basic Compilation Tests

    [Fact]
    public void CompilePolicy_SimplePermit_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy SimplePermitPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
        Assert.Equal("SimplePermitPolicy", compiledPolicy.PolicyId);
        Assert.NotNull(compiledPolicy.GeneratedCode);
        Assert.Contains("Decision Evaluate", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_SimpleDeny_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy SimpleDenyPolicy {
                deny
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
        Assert.Equal("SimpleDenyPolicy", compiledPolicy.PolicyId);
    }

    [Fact]
    public void CompilePolicy_WithMultipleRules_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy MultiRulePolicy {
                apply denyOverrides
                rule Rule1 {
                    permit
                }
                rule Rule2 {
                    deny
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
        Assert.Contains("Deny-overrides", compiledPolicy.GeneratedCode);
    }

    #endregion

    #region Compilation with Expressions Tests

    [Fact]
    public void CompilePolicy_WithSimpleCondition_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ConditionPolicy {
                rule AgeCheck {
                    permit
                    condition age > 18
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
        Assert.Contains("context.GetAttribute", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_WithComplexExpression_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ComplexExpressionPolicy {
                rule ComplexCheck {
                    permit
                    condition (age > 18 && isActive == true) || isAdmin == true
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
    }

    [Fact]
    public void CompilePolicy_WithArithmeticExpression_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy ArithmeticPolicy {
                rule SalaryCheck {
                    permit
                    condition (salary + bonus) > 100000
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
    }

    #endregion

    #region Compilation with Functions Tests

    [Fact]
    public void CompilePolicy_WithFunctionCall_ShouldSucceed()
    {
        // Arrange
        string policyText = @"
            policy FunctionCallPolicy {
                rule StringCheck {
                    permit
                    condition stringEqual(userName, ""admin"")
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.NotNull(compiledPolicy.EvaluationDelegate);
        Assert.Contains("Functions.StringEqual", compiledPolicy.GeneratedCode);
    }

    #endregion

    #region Compilation Cache Tests

    [Fact]
    public void CompilePolicy_WithCachingEnabled_ShouldCacheResult()
    {
        // Arrange
        string policyText = @"
            policy CacheTestPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy1 = compiler.CompilePolicy(policy, enableCaching: true);
        CompiledPolicy compiledPolicy2 = compiler.CompilePolicy(policy, enableCaching: true);

        // Assert
        Assert.NotNull(compiledPolicy1);
        Assert.NotNull(compiledPolicy2);
        Assert.Same(compiledPolicy1, compiledPolicy2);
    }

    [Fact]
    public void CompilePolicy_WithCachingDisabled_ShouldNotCacheResult()
    {
        // Arrange
        string policyText = @"
            policy NoCachePolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy1 = compiler.CompilePolicy(policy, enableCaching: false);
        CompiledPolicy compiledPolicy2 = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy1);
        Assert.NotNull(compiledPolicy2);
        Assert.NotSame(compiledPolicy1, compiledPolicy2);
    }

    [Fact]
    public void ClearCache_ShouldRemoveAllCachedPolicies()
    {
        // Arrange
        string policyText = @"
            policy ClearCachePolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy1 = compiler.CompilePolicy(policy, enableCaching: true);
        compiler.ClearCache();
        CompiledPolicy compiledPolicy2 = compiler.CompilePolicy(policy, enableCaching: true);

        // Assert
        Assert.NotNull(compiledPolicy1);
        Assert.NotNull(compiledPolicy2);
        Assert.NotSame(compiledPolicy1, compiledPolicy2);
    }

    [Fact]
    public void RemoveFromCache_ShouldRemoveSpecificPolicy()
    {
        // Arrange
        string policyText = @"
            policy RemoveCachePolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy1 = compiler.CompilePolicy(policy, enableCaching: true);
        bool removed = compiler.RemoveFromCache("RemoveCachePolicy");
        CompiledPolicy compiledPolicy2 = compiler.CompilePolicy(policy, enableCaching: true);

        // Assert
        Assert.True(removed);
        Assert.NotSame(compiledPolicy1, compiledPolicy2);
    }

    #endregion

    #region Combining Algorithm Compilation Tests

    [Fact]
    public void CompilePolicy_DenyOverrides_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy DenyOverridesPolicy {
                apply denyOverrides
                rule Rule1 {
                  deny
                }
                rule Rule2 {
                  permit
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("Deny-overrides", compiledPolicy.GeneratedCode);
        Assert.Contains("atLeastOnePermit", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_PermitOverrides_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy PermitOverridesPolicy {
                apply permitOverrides
                rule Rule1 {
                  permit
                }
                rule Rule2 {
                  deny
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("Permit-overrides", compiledPolicy.GeneratedCode);
        Assert.Contains("atLeastOneDeny", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_FirstApplicable_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy FirstApplicablePolicy {
                apply firstApplicable
                rule Rule1 {
                  permit
                }
                rule Rule2 {
                  deny
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("First-applicable", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_OnlyOne_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy OnlyOnePolicy {
                apply onlyOne
                rule Rule1 {
                  permit
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("Only-one", compiledPolicy.GeneratedCode);
        Assert.Contains("applicableCount", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_DenyUnlessPermit_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy DenyUnlessPermitPolicy {
                apply denyUnlessPermit
                rule Rule1 {
                  permit
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("Deny-unless-permit", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_PermitUnlessDeny_ShouldGenerateCorrectCode()
    {
        // Arrange
        string policyText = @"
            policy PermitUnlessDenyPolicy {
                apply permitUnlessDeny
                rule Rule1 {
                  deny
                }
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("Permit-unless-deny", compiledPolicy.GeneratedCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void CompilePolicy_WithNullPolicy_ShouldThrowException()
    {
        // Arrange
        PolicyCompiler compiler = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => compiler.CompilePolicy(null!, enableCaching: false));
    }

    [Fact]
    public void CompilePolicy_EmptyPolicy_ShouldCompileToNotApplicable()
    {
        // Arrange
        string policyText = @"
            policy EmptyPolicy {
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.NotNull(compiledPolicy);
        Assert.Contains("NotApplicable", compiledPolicy.GeneratedCode);
    }

    #endregion

    #region Generated Code Quality Tests

    [Fact]
    public void CompilePolicy_GeneratedCode_ShouldContainRequiredUsings()
    {
        // Arrange
        string policyText = @"
            policy TestPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.Contains("using System;", compiledPolicy.GeneratedCode);
        Assert.Contains("using ABACore.Models;", compiledPolicy.GeneratedCode);
        Assert.Contains("using ABACore.Runtime;", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_GeneratedCode_ShouldContainNamespace()
    {
        // Arrange
        string policyText = @"
            policy TestPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.Contains("namespace ABACore.Generated", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_GeneratedCode_ShouldContainEvaluateMethod()
    {
        // Arrange
        string policyText = @"
            policy TestPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.Contains("public static Decision Evaluate(EvaluationContext context)", compiledPolicy.GeneratedCode);
    }

    [Fact]
    public void CompilePolicy_GeneratedCode_ShouldContainErrorHandling()
    {
        // Arrange
        string policyText = @"
            policy TestPolicy {
                permit
            }
        ";
        Policy policy = ParsePolicy(policyText);
        PolicyCompiler compiler = new();

        // Act
        CompiledPolicy compiledPolicy = compiler.CompilePolicy(policy, enableCaching: false);

        // Assert
        Assert.Contains("try", compiledPolicy.GeneratedCode);
        Assert.Contains("catch", compiledPolicy.GeneratedCode);
        Assert.Contains("Decision.Indeterminate", compiledPolicy.GeneratedCode);
    }

    #endregion
}
