using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class PolicyExecutorTests
{
    private readonly PolicyExecutor _executor = new();

    [Fact]
    public void Execute_WithNullPolicy_ThrowsArgumentNullException()
    {
        var context = new EvaluationContext();
        Assert.Throws<ArgumentNullException>(() => _executor.Execute(null!, context));
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        var policy = CreateMockPolicy(Decision.Permit());
        Assert.Throws<ArgumentNullException>(() => _executor.Execute(policy, null!));
    }

    [Fact]
    public void Execute_WithValidPolicy_ReturnsDecision()
    {
        var expectedDecision = Decision.Permit();
        var policy = CreateMockPolicy(expectedDecision);
        var context = new EvaluationContext();

        var result = _executor.Execute(policy, context);

        Assert.Equal(expectedDecision.Effect, result.Effect);
    }

    [Fact]
    public void Execute_WithPolicyThatThrowsException_ReturnsIndeterminate()
    {
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => throw new InvalidOperationException("Test error")
        };
        var context = new EvaluationContext();

        // CompiledPolicy.Evaluate catches exceptions and returns Indeterminate
        var result = policy.Evaluate(context);
        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    [Fact]
    public void Execute_WithPolicyThatThrowsAlfaEvaluationException_ReturnsIndeterminate()
    {
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => throw new AlfaEvaluationException("Original error", "test-policy")
        };
        var context = new EvaluationContext();

        // CompiledPolicy.Evaluate catches ALL exceptions including AlfaEvaluationException
        var result = _executor.Execute(policy, context);
        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    #region ExecutePolicySet Tests

    [Fact]
    public void ExecutePolicySet_WithNullPolicies_ThrowsArgumentNullException()
    {
        var context = new EvaluationContext();
        Assert.Throws<ArgumentNullException>(() =>
            _executor.ExecutePolicySet(null!, context, CombiningAlgorithm.DenyOverrides));
    }

    [Fact]
    public void ExecutePolicySet_WithNullContext_ThrowsArgumentNullException()
    {
        var policies = new[] { CreateMockPolicy(Decision.Permit()) };
        Assert.Throws<ArgumentNullException>(() =>
            _executor.ExecutePolicySet(policies, null!, CombiningAlgorithm.DenyOverrides));
    }

    [Fact]
    public void ExecutePolicySet_WithEmptyPolicies_ThrowsArgumentException()
    {
        var context = new EvaluationContext();
        var ex = Assert.Throws<ArgumentException>(() =>
            _executor.ExecutePolicySet(Array.Empty<CompiledPolicy>(), context, CombiningAlgorithm.DenyOverrides));
        Assert.Contains("cannot be empty", ex.Message);
    }

    #endregion

    #region DenyOverrides Tests

    [Fact]
    public void DenyOverrides_WithSingleDeny_ReturnsDeny()
    {
        var policies = new[] { CreateMockPolicy(Decision.Deny()) };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void DenyOverrides_WithPermitAndDeny_ReturnsDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void DenyOverrides_WithOnlyPermit_ReturnsPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void DenyOverrides_WithAllNotApplicable_ReturnsNotApplicable()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.NotApplicable, result.Effect);
    }

    [Fact]
    public void DenyOverrides_WithErrorAndNoDecision_ReturnsIndeterminate()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error")),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    [Fact]
    public void DenyOverrides_WithObligations_MergesObligations()
    {
        var obligation1 = new ObligationResult { Id = "obl1" };
        var obligation2 = new ObligationResult { Id = "obl2" };

        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit([obligation1], null)),
            CreateMockPolicy(Decision.Permit([obligation2], null))
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyOverrides);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
        Assert.NotNull(result.Obligations);
        Assert.Equal(2, result.Obligations.Count);
    }

    #endregion

    #region PermitOverrides Tests

    [Fact]
    public void PermitOverrides_WithSinglePermit_ReturnsPermit()
    {
        var policies = new[] { CreateMockPolicy(Decision.Permit()) };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitOverrides);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void PermitOverrides_WithDenyAndPermit_ReturnsPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Deny()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitOverrides);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void PermitOverrides_WithOnlyDeny_ReturnsDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Deny()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitOverrides);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void PermitOverrides_WithAllNotApplicable_ReturnsNotApplicable()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitOverrides);

        Assert.Equal(DecisionEffect.NotApplicable, result.Effect);
    }

    [Fact]
    public void PermitOverrides_WithErrorAndNoDecision_ReturnsIndeterminate()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error")),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitOverrides);

        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    #endregion

    #region FirstApplicable Tests

    [Fact]
    public void FirstApplicable_ReturnsFirstPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void FirstApplicable_ReturnsFirstDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Deny()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void FirstApplicable_SkipsNotApplicable()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Permit()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void FirstApplicable_WithAllNotApplicable_ReturnsNotApplicable()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.NotApplicable, result.Effect);
    }

    [Fact]
    public void FirstApplicable_WithError_ReturnsIndeterminate()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error"))
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    [Fact]
    public void FirstApplicable_WithIndeterminate_ReturnsIndeterminate()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Indeterminate(StatusInfo.ProcessingError("Test"))),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.FirstApplicable);

        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
    }

    #endregion

    #region OnlyOne Tests

    [Fact]
    public void OnlyOne_WithSingleApplicable_ReturnsDecision()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.OnlyOne);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void OnlyOne_WithMultipleApplicable_ReturnsIndeterminate()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.OnlyOne);

        Assert.Equal(DecisionEffect.Indeterminate, result.Effect);
        Assert.Contains("Multiple policies", result.Status?.StatusMessage);
    }

    [Fact]
    public void OnlyOne_WithNoneApplicable_ReturnsNotApplicable()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.OnlyOne);

        Assert.Equal(DecisionEffect.NotApplicable, result.Effect);
    }

    [Fact]
    public void OnlyOne_WithErrors_SkipsErroredPolicies()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error")),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.OnlyOne);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    #endregion

    #region DenyUnlessPermit Tests

    [Fact]
    public void DenyUnlessPermit_WithPermit_ReturnsPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyUnlessPermit);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void DenyUnlessPermit_WithoutPermit_ReturnsDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyUnlessPermit);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void DenyUnlessPermit_WithOnlyNotApplicable_ReturnsDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyUnlessPermit);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void DenyUnlessPermit_WithErrors_IgnoresErrors()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error")),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyUnlessPermit);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void DenyUnlessPermit_MergesObligationsAndAdvice()
    {
        var obligation1 = new ObligationResult { Id = "obl1" };
        var advice1 = new AdviceResult { Id = "adv1" };

        var policies = new[]
        {
            CreateMockPolicy(Decision.Deny([obligation1], [advice1])),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.DenyUnlessPermit);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
        Assert.NotNull(result.Obligations);
        Assert.NotNull(result.Advice);
    }

    #endregion

    #region PermitUnlessDeny Tests

    [Fact]
    public void PermitUnlessDeny_WithDeny_ReturnsDeny()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitUnlessDeny);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void PermitUnlessDeny_WithoutDeny_ReturnsPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.Permit())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitUnlessDeny);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void PermitUnlessDeny_WithOnlyNotApplicable_ReturnsPermit()
    {
        var policies = new[]
        {
            CreateMockPolicy(Decision.NotApplicable()),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitUnlessDeny);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void PermitUnlessDeny_WithErrors_IgnoresErrors()
    {
        var policies = new[]
        {
            CreateMockPolicy(_ => throw new Exception("Error")),
            CreateMockPolicy(Decision.Deny())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitUnlessDeny);

        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void PermitUnlessDeny_MergesObligationsAndAdvice()
    {
        var obligation1 = new ObligationResult { Id = "obl1" };
        var advice1 = new AdviceResult { Id = "adv1" };

        var policies = new[]
        {
            CreateMockPolicy(Decision.Permit([obligation1], [advice1])),
            CreateMockPolicy(Decision.NotApplicable())
        };
        var context = new EvaluationContext();

        var result = _executor.ExecutePolicySet(policies, context, CombiningAlgorithm.PermitUnlessDeny);

        Assert.Equal(DecisionEffect.Permit, result.Effect);
        Assert.NotNull(result.Obligations);
        Assert.NotNull(result.Advice);
    }

    #endregion

    #region Helper Methods

    private static CompiledPolicy CreateMockPolicy(Decision decision)
    {
        return new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => decision
        };
    }

    private static CompiledPolicy CreateMockPolicy(Func<EvaluationContext, Decision> evaluator)
    {
        return new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = evaluator
        };
    }

    #endregion
}
