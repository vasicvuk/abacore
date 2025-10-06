using ABACore.Compilation;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class ExpressionTreeCompilerTests
{
    private readonly ExpressionTreeCompiler _compiler = new();

    #region CompilePolicy Tests

    [Fact]
    public void CompilePolicy_WithNullPolicy_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _compiler.CompilePolicy(null!));
    }

    [Fact]
    public void CompilePolicy_WithSimplePolicy_ReturnsCompiledPolicy()
    {
        var policy = new Policy
        {
            Id = "test-policy",
            Rules = []
        };

        var compiled = _compiler.CompilePolicy(policy);

        Assert.NotNull(compiled);
        Assert.Equal("test-policy", compiled.PolicyId);
        Assert.NotNull(compiled.EvaluationDelegate);
    }

    [Fact]
    public void CompilePolicy_WithCachingEnabled_ReturnsCachedInstance()
    {
        var policy = new Policy { Id = "cached-policy", Rules = [] };

        var first = _compiler.CompilePolicy(policy, enableCaching: true);
        var second = _compiler.CompilePolicy(policy, enableCaching: true);

        Assert.Same(first, second);
    }

    [Fact]
    public void CompilePolicy_WithCachingDisabled_ReturnsNewInstance()
    {
        var policy = new Policy { Id = "uncached-policy", Rules = [] };

        var first = _compiler.CompilePolicy(policy, enableCaching: false);
        var second = _compiler.CompilePolicy(policy, enableCaching: false);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CompilePolicy_WithTarget_EvaluatesTargetCorrectly()
    {
        var policy = new Policy
        {
            Id = "target-policy",
            Target = new Target
            {
                Clauses =
                [
                    new Clause { Expression = new BooleanLiteralExpression { Value = true } }
                ]
            },
            Rules =
            [
                new Rule { Id = "rule1", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var context = new EvaluationContext();
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithNonMatchingTarget_ReturnsNotApplicable()
    {
        var policy = new Policy
        {
            Id = "target-policy",
            Target = new Target
            {
                Clauses =
                [
                    new Clause { Expression = new BooleanLiteralExpression { Value = false } }
                ]
            },
            Rules =
            [
                new Rule { Id = "rule1", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var context = new EvaluationContext();
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithDenyOverrides_ReturnsDenyOnFirstDeny()
    {
        var policy = new Policy
        {
            Id = "deny-overrides",
            Combinator = CombiningAlgorithm.DenyOverrides,
            Rules =
            [
                new Rule { Id = "permit", Effect = Effect.Permit },
                new Rule { Id = "deny", Effect = Effect.Deny }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithPermitOverrides_ReturnsPermitOnFirstPermit()
    {
        var policy = new Policy
        {
            Id = "permit-overrides",
            Combinator = CombiningAlgorithm.PermitOverrides,
            Rules =
            [
                new Rule { Id = "deny", Effect = Effect.Deny },
                new Rule { Id = "permit", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithFirstApplicable_ReturnsFirstApplicableRule()
    {
        var policy = new Policy
        {
            Id = "first-applicable",
            Combinator = CombiningAlgorithm.FirstApplicable,
            Rules =
            [
                new Rule
                {
                    Id = "deny",
                    Effect = Effect.Deny,
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = true } }]
                    }
                },
                new Rule { Id = "permit", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithOnlyOne_WhenOneApplicable_ReturnsThatDecision()
    {
        var policy = new Policy
        {
            Id = "only-one",
            Combinator = CombiningAlgorithm.OnlyOne,
            Rules =
            [
                new Rule
                {
                    Id = "not-applicable",
                    Effect = Effect.Permit,
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    }
                },
                new Rule { Id = "applicable", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithOnlyOne_WhenMultipleApplicable_ReturnsIndeterminate()
    {
        var policy = new Policy
        {
            Id = "only-one-multiple",
            Combinator = CombiningAlgorithm.OnlyOne,
            Rules =
            [
                new Rule { Id = "permit1", Effect = Effect.Permit },
                new Rule { Id = "permit2", Effect = Effect.Permit }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithDenyUnlessPermit_WhenNoPermit_ReturnsDeny()
    {
        var policy = new Policy
        {
            Id = "deny-unless-permit",
            Combinator = CombiningAlgorithm.DenyUnlessPermit,
            Rules =
            [
                new Rule
                {
                    Id = "not-applicable",
                    Effect = Effect.Permit,
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithPermitUnlessDeny_WhenNoDeny_ReturnsPermit()
    {
        var policy = new Policy
        {
            Id = "permit-unless-deny",
            Combinator = CombiningAlgorithm.PermitUnlessDeny,
            Rules =
            [
                new Rule
                {
                    Id = "not-applicable",
                    Effect = Effect.Deny,
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompilePolicy_WithObligations_IncludesObligationsInDecision()
    {
        var policy = new Policy
        {
            Id = "obligations-policy",
            Rules =
            [
                new Rule
                {
                    Id = "rule1",
                    Effect = Effect.Permit,
                    OnPermit =
                    [
                        new Obligation
                        {
                            Id = "log-access",
                            Attributes = new Dictionary<string, Expression>
                            {
                                ["message"] = new LiteralStringExpression { Value = "Access granted" }
                            }
                        }
                    ]
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
    }

    [Fact]
    public void CompilePolicy_WithAdvice_IncludesAdviceInDecision()
    {
        var policy = new Policy
        {
            Id = "advice-policy",
            Rules =
            [
                new Rule
                {
                    Id = "rule1",
                    Effect = Effect.Deny,
                    OnDeny =
                    [
                        new Advice
                        {
                            Id = "notify-admin",
                            Attributes = new Dictionary<string, Expression>
                            {
                                ["reason"] = new LiteralStringExpression { Value = "Access denied" }
                            }
                        }
                    ]
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
    }

    #endregion

    #region CompilePolicySet Tests

    [Fact]
    public void CompilePolicySet_WithNullPolicySet_ThrowsArgumentNullException()
    {
        var repository = new PolicyRepository();
        Assert.Throws<ArgumentNullException>(() => _compiler.CompilePolicySet(null!, repository));
    }

    [Fact]
    public void CompilePolicySet_WithNullRepository_ThrowsArgumentNullException()
    {
        var policySet = new PolicySet { Id = "ps1", Elements = [] };
        Assert.Throws<ArgumentNullException>(() => _compiler.CompilePolicySet(policySet, null!));
    }

    [Fact]
    public void CompilePolicySet_WithInlinePolicy_CompilesSuccessfully()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Elements =
            [
                new Policy
                {
                    Id = "child-policy",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var repository = new PolicyRepository();
        var compiled = _compiler.CompilePolicySet(policySet, repository);

        Assert.NotNull(compiled);
        Assert.Equal("ps1", compiled.PolicyId);
    }

    [Fact]
    public void CompilePolicySet_WithPolicyReference_ResolvesFromRepository()
    {
        var repository = new PolicyRepository();
        var childPolicy = new Policy
        {
            Id = "referenced-policy",
            Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
        };
        repository.AddPolicy(_compiler.CompilePolicy(childPolicy));

        var policySet = new PolicySet
        {
            Id = "ps1",
            Elements = [new PolicyReference { PolicyId = "referenced-policy" }]
        };

        var compiled = _compiler.CompilePolicySet(policySet, repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void CompilePolicySet_WithMissingReference_ThrowsInvalidOperationException()
    {
        var repository = new PolicyRepository();
        var policySet = new PolicySet
        {
            Id = "ps1",
            Elements = [new PolicyReference { PolicyId = "non-existent" }]
        };

        Assert.Throws<InvalidOperationException>(() => _compiler.CompilePolicySet(policySet, repository));
    }

    [Fact]
    public void CompilePolicySet_WithNestedPolicySet_CompilesRecursively()
    {
        var repository = new PolicyRepository();
        var policySet = new PolicySet
        {
            Id = "parent-ps",
            Elements =
            [
                new PolicySet
                {
                    Id = "child-ps",
                    Elements =
                    [
                        new Policy
                        {
                            Id = "nested-policy",
                            Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                        }
                    ]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompilePolicySet_WithCaching_ReturnsCachedInstance()
    {
        var repository = new PolicyRepository();
        var policySet = new PolicySet { Id = "cached-ps", Elements = [] };

        var first = _compiler.CompilePolicySet(policySet, repository, enableCaching: true);
        var second = _compiler.CompilePolicySet(policySet, repository, enableCaching: true);

        Assert.Same(first, second);
    }

    [Fact]
    public void CompilePolicySet_WithTarget_EvaluatesTargetCorrectly()
    {
        var repository = new PolicyRepository();
        var policySet = new PolicySet
        {
            Id = "target-ps",
            Target = new Target
            {
                Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
            },
            Elements =
            [
                new Policy
                {
                    Id = "child",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void CompilePolicySet_WithAllCombiningAlgorithms_EvaluatesCorrectly()
    {
        var repository = new PolicyRepository();

        var algorithms = new[]
        {
            CombiningAlgorithm.DenyOverrides,
            CombiningAlgorithm.PermitOverrides,
            CombiningAlgorithm.FirstApplicable,
            CombiningAlgorithm.OnlyOne,
            CombiningAlgorithm.DenyUnlessPermit,
            CombiningAlgorithm.PermitUnlessDeny
        };

        foreach (var algorithm in algorithms)
        {
            var policySet = new PolicySet
            {
                Id = $"ps-{algorithm}",
                Combinator = algorithm,
                Elements =
                [
                    new Policy
                    {
                        Id = "p1",
                        Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                    }
                ]
            };

            var compiled = _compiler.CompilePolicySet(policySet, repository, enableCaching: false);
            var decision = compiled.EvaluationDelegate(new EvaluationContext());

            Assert.NotEqual(DecisionEffect.Indeterminate, decision.Effect);
        }
    }

    #endregion

    #region Cache Management Tests

    [Fact]
    public void ClearCache_RemovesAllCachedPolicies()
    {
        var policy = new Policy { Id = "cached", Rules = [] };
        var first = _compiler.CompilePolicy(policy, enableCaching: true);

        _compiler.ClearCache();

        var second = _compiler.CompilePolicy(policy, enableCaching: true);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void RemoveFromCache_RemovesSpecificPolicy()
    {
        var policy = new Policy { Id = "to-remove", Rules = [] };
        var first = _compiler.CompilePolicy(policy, enableCaching: true);

        var removed = _compiler.RemoveFromCache("to-remove");

        Assert.True(removed);
        var second = _compiler.CompilePolicy(policy, enableCaching: true);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void RemoveFromCache_WithNonExistentPolicy_ReturnsFalse()
    {
        var removed = _compiler.RemoveFromCache("non-existent");
        Assert.False(removed);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void CompilePolicy_WithRuleConditionThrowingException_ReturnsIndeterminate()
    {
        var policy = new Policy
        {
            Id = "error-policy",
            Combinator = CombiningAlgorithm.FirstApplicable,
            Rules =
            [
                new Rule
                {
                    Id = "rule1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAttributeDesignator
                        {
                            Namespace = "test",
                            AttributeName = "missing",
                            MustBePresent = true
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
    }

    [Fact]
    public void CompilePolicySet_WithExceptionInTarget_ReturnsIndeterminate()
    {
        var repository = new PolicyRepository();
        var policySet = new PolicySet
        {
            Id = "error-ps",
            Target = new Target
            {
                Clauses =
                [
                    new Clause
                    {
                        Expression = new BooleanAttributeDesignator
                        {
                            Namespace = "test",
                            AttributeName = "missing",
                            MustBePresent = true
                        }
                    }
                ]
            },
            Elements =
            [
                new Policy
                {
                    Id = "child",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
    }

    #endregion
}
