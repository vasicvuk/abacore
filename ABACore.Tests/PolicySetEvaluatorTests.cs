using ABACore.Compilation;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class PolicySetEvaluatorTests
{
    private readonly InterpreterCompiler _compiler = new();
    private readonly PolicyRepository _repository = new();

    #region DenyOverrides Tests

    [Fact]
    public void PolicySetDenyOverrides_WithDenyPolicy_ReturnsDeny()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyOverrides,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicySetDenyOverrides_WithOnlyPermit_ReturnsPermit()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyOverrides,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicySetDenyOverrides_WithPolicySetObligations_IncludesObligations()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyOverrides,
            OnPermit =
            [
                new Obligation
                {
                    Id = "ps-obligation",
                    Attributes = new Dictionary<string, Expression>
                    {
                        ["level"] = new LiteralStringExpression { Value = "high" }
                    }
                }
            ],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Contains(decision.Obligations, o => o.Id == "ps-obligation");
    }

    [Fact]
    public void PolicySetDenyOverrides_WithPolicySetAdvice_IncludesAdvice()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyOverrides,
            OnDeny =
            [
                new Advice
                {
                    Id = "ps-advice",
                    Attributes = new Dictionary<string, Expression>
                    {
                        ["reason"] = new LiteralStringExpression { Value = "denied by policy" }
                    }
                }
            ],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Contains(decision.Advice, a => a.Id == "ps-advice");
    }

    #endregion

    #region PermitOverrides Tests

    [Fact]
    public void PolicySetPermitOverrides_WithPermitPolicy_ReturnsPermit()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.PermitOverrides,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicySetPermitOverrides_WithOnlyDeny_ReturnsDeny()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.PermitOverrides,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    #endregion

    #region FirstApplicable Tests

    [Fact]
    public void PolicySetFirstApplicable_ReturnsFirstApplicableDecision()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.FirstApplicable,
            Elements =
            [
                new Policy
                {
                    Id = "not-applicable",
                    Rules =
                    [
                        new Rule
                        {
                            Id = "r1",
                            Effect = Effect.Permit,
                            Target = new Target
                            {
                                Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                            }
                        }
                    ]
                },
                new Policy
                {
                    Id = "applicable",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicySetFirstApplicable_WithIndeterminate_ReturnsIndeterminate()
    {
        // Create a policy that will throw an exception during evaluation
        var throwingPolicy = new CompiledPolicy
        {
            PolicyId = "throwing-policy",
            EvaluationDelegate = _ => throw new InvalidOperationException("Test exception")
        };
        _repository.AddPolicy(throwingPolicy);

        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.FirstApplicable,
            Elements = [new PolicyReference { PolicyId = "throwing-policy" }]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
        Assert.Contains("Test exception", decision.Status?.StatusMessage);
    }

    #endregion

    #region OnlyOne Tests

    [Fact]
    public void PolicySetOnlyOne_WithOneApplicable_ReturnsThatDecision()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.OnlyOne,
            Elements =
            [
                new Policy
                {
                    Id = "not-applicable",
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    },
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                },
                new Policy
                {
                    Id = "applicable",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicySetOnlyOne_WithMultipleApplicable_ReturnsIndeterminate()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.OnlyOne,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
        Assert.Contains("Multiple policies", decision.Status?.StatusMessage);
    }

    #endregion

    #region DenyUnlessPermit Tests

    [Fact]
    public void PolicySetDenyUnlessPermit_WithPermit_ReturnsPermit()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyUnlessPermit,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicySetDenyUnlessPermit_WithoutPermit_ReturnsDeny()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyUnlessPermit,
            Elements =
            [
                new Policy
                {
                    Id = "not-applicable",
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    },
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    #endregion

    #region PermitUnlessDeny Tests

    [Fact]
    public void PolicySetPermitUnlessDeny_WithDeny_ReturnsDeny()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.PermitUnlessDeny,
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicySetPermitUnlessDeny_WithoutDeny_ReturnsPermit()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.PermitUnlessDeny,
            Elements =
            [
                new Policy
                {
                    Id = "not-applicable",
                    Target = new Target
                    {
                        Clauses = [new Clause { Expression = new BooleanLiteralExpression { Value = false } }]
                    },
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region Obligation and Advice Attribute Tests

    [Fact]
    public void PolicySet_WithIntegerAttributeInObligation_EvaluatesCorrectly()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            OnPermit =
            [
                new Obligation
                {
                    Id = "test-obligation",
                    Attributes = new Dictionary<string, Expression>
                    {
                        ["count"] = new LiteralIntegerExpression { Value = 42 }
                    }
                }
            ],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        var obligation = decision.Obligations.FirstOrDefault(o => o.Id == "test-obligation");
        Assert.NotNull(obligation);
        Assert.Equal(42, obligation.Attributes?["count"]);
    }

    [Fact]
    public void PolicySet_WithBooleanAttributeInAdvice_EvaluatesCorrectly()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            OnDeny =
            [
                new Advice
                {
                    Id = "test-advice",
                    Attributes = new Dictionary<string, Expression>
                    {
                        ["urgent"] = new LiteralBooleanExpression { Value = true }
                    }
                }
            ],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        var advice = decision.Advice.FirstOrDefault(a => a.Id == "test-advice");
        Assert.NotNull(advice);
        Assert.Equal(true, advice.Attributes?["urgent"]);
    }

    [Fact]
    public void PolicySet_WithEmptyObligationAttributes_AddsObligationWithoutAttributes()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            OnPermit = [new Obligation { Id = "empty-obligation" }],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Contains(decision.Obligations, o => o.Id == "empty-obligation");
    }

    [Fact]
    public void PolicySet_WithEmptyAdviceAttributes_AddsAdviceWithoutAttributes()
    {
        var policySet = new PolicySet
        {
            Id = "ps1",
            OnDeny = [new Advice { Id = "empty-advice" }],
            Elements =
            [
                new Policy
                {
                    Id = "p1",
                    Rules = [new Rule { Id = "r1", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Contains(decision.Advice, a => a.Id == "empty-advice");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void PolicySetDenyOverrides_WithErrorInPolicy_ContinuesEvaluation()
    {
        var throwingPolicy = new CompiledPolicy
        {
            PolicyId = "throwing",
            EvaluationDelegate = _ => throw new InvalidOperationException("Error")
        };
        _repository.AddPolicy(throwingPolicy);

        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyOverrides,
            Elements =
            [
                new PolicyReference { PolicyId = "throwing" },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicySetPermitOverrides_WithErrorInPolicy_ContinuesEvaluation()
    {
        var throwingPolicy = new CompiledPolicy
        {
            PolicyId = "throwing",
            EvaluationDelegate = _ => throw new InvalidOperationException("Error")
        };
        _repository.AddPolicy(throwingPolicy);

        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.PermitOverrides,
            Elements =
            [
                new PolicyReference { PolicyId = "throwing" },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Deny }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicySetDenyUnlessPermit_WithErrorInPolicy_IgnoresError()
    {
        var throwingPolicy = new CompiledPolicy
        {
            PolicyId = "throwing",
            EvaluationDelegate = _ => throw new InvalidOperationException("Error")
        };
        _repository.AddPolicy(throwingPolicy);

        var policySet = new PolicySet
        {
            Id = "ps1",
            Combinator = CombiningAlgorithm.DenyUnlessPermit,
            Elements =
            [
                new PolicyReference { PolicyId = "throwing" },
                new Policy
                {
                    Id = "p2",
                    Rules = [new Rule { Id = "r2", Effect = Effect.Permit }]
                }
            ]
        };

        var compiled = _compiler.CompilePolicySet(policySet, _repository);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion
}
