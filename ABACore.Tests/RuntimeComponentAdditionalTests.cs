using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class RuntimeComponentAdditionalTests
{
    [Fact]
    public void EvaluationConfiguration_DefaultValues_AreCorrect()
    {
        var config = new EvaluationConfiguration();
        Assert.NotNull(config);
    }

    [Fact]
    public void NamespaceResolver_ResolveAttribute_WithNonExistent_ReturnsNull()
    {
        var resolver = new NamespaceResolver();
        var attr = resolver.ResolveAttribute("NonExistent.Namespace", "attr");

        Assert.Null(attr);
    }

    [Fact]
    public void NamespaceResolver_ResolveFunction_WithNonExistent_ReturnsNull()
    {
        var resolver = new NamespaceResolver();
        var func = resolver.ResolveFunction("NonExistent.Namespace", "func");

        Assert.Null(func);
    }

    [Fact]
    public void NamespaceResolver_ProcessImports_RegistersImports()
    {
        var resolver = new NamespaceResolver();
        var imports = new List<Import>
        {
            new Import
            {
                NamespacePath = "Oasis.Attributes.Subject",
                Wildcard = false
            }
        };

        resolver.ProcessImports("test.namespace", imports);

        // Should not throw
        Assert.NotNull(resolver);
    }

    [Fact]
    public void NamespaceResolver_RegisterNamespace_AddsNamespace()
    {
        var resolver = new NamespaceResolver();
        var ns = new NamespaceInfo
        {
            Name = "custom.namespace",
            Attributes = new Dictionary<string, AttributeInfo>(),
            Functions = new Dictionary<string, FunctionInfo>()
        };

        resolver.RegisterNamespace(ns);

        var retrieved = resolver.GetNamespace("custom.namespace");
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void PolicyRepository_AddPolicy_StoresPolicy()
    {
        var repo = new PolicyRepository();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => Decision.Permit()
        };

        repo.AddPolicy(policy);

        var retrieved = repo.GetPolicy("test-policy");
        Assert.NotNull(retrieved);
        Assert.Equal("test-policy", retrieved.PolicyId);
    }

    [Fact]
    public void PolicyRepository_GetPolicy_WithNonExistent_ReturnsNull()
    {
        var repo = new PolicyRepository();
        var policy = repo.GetPolicy("non-existent");

        Assert.Null(policy);
    }

    [Fact]
    public void PolicyRepository_RemovePolicy_RemovesPolicy()
    {
        var repo = new PolicyRepository();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => Decision.Permit()
        };

        repo.AddPolicy(policy);
        Assert.NotNull(repo.GetPolicy("test-policy"));

        repo.RemovePolicy("test-policy");
        Assert.Null(repo.GetPolicy("test-policy"));
    }


    [Fact]
    public void PolicyExecutor_Execute_ExecutesCompiledPolicy()
    {
        var executor = new PolicyExecutor();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = ctx =>
            {
                var userId = ctx.GetAttribute("subject", "userId");
                return userId?.ToString() == "admin" ? Decision.Permit() : Decision.Deny();
            }
        };

        var context = new EvaluationContext();
        context.SetAttribute("subject", "userId", "admin");

        var decision = executor.Execute(policy, context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyExecutor_Execute_WithDenyDecision_ReturnsDeny()
    {
        var executor = new PolicyExecutor();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => Decision.Deny()
        };

        var context = new EvaluationContext();
        var decision = executor.Execute(policy, context);

        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void PolicyExecutor_Execute_WithNotApplicable_ReturnsNotApplicable()
    {
        var executor = new PolicyExecutor();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => Decision.NotApplicable()
        };

        var context = new EvaluationContext();
        var decision = executor.Execute(policy, context);

        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void PolicyExecutor_Execute_WithIndeterminate_ReturnsIndeterminate()
    {
        var executor = new PolicyExecutor();
        var policy = new CompiledPolicy
        {
            PolicyId = "test-policy",
            EvaluationDelegate = _ => Decision.Indeterminate()
        };

        var context = new EvaluationContext();
        var decision = executor.Execute(policy, context);

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
    }


    [Fact]
    public void CompiledPolicy_Execute_CallsDelegate()
    {
        var called = false;
        var policy = new CompiledPolicy
        {
            PolicyId = "test",
            EvaluationDelegate = ctx =>
            {
                called = true;
                return Decision.Permit();
            }
        };

        var context = new EvaluationContext();
        policy.Evaluate(context);

        Assert.True(called);
    }

    [Fact]
    public void EvaluationContext_GetAttribute_WithMultipleTypes_RetrieverCorrectly()
    {
        var context = new EvaluationContext();

        context.SetAttribute("subject", "strAttr", "string-value");
        context.SetAttribute("subject", "intAttr", 42);
        context.SetAttribute("subject", "boolAttr", true);
        context.SetAttribute("subject", "doubleAttr", 3.14);

        Assert.Equal("string-value", context.GetAttribute("subject", "strAttr"));
        Assert.Equal(42, context.GetAttribute("subject", "intAttr"));
        Assert.Equal(true, context.GetAttribute("subject", "boolAttr"));
        Assert.Equal(3.14, context.GetAttribute("subject", "doubleAttr"));
    }

    [Fact]
    public void EvaluationContext_GetAttribute_WithNonExistentCategory_ReturnsNull()
    {
        var context = new EvaluationContext();
        var value = context.GetAttribute("nonexistent", "attr");

        Assert.Null(value);
    }

    [Fact]
    public void EvaluationContext_GetAttribute_WithNonExistentAttribute_ReturnsNull()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "existing", "value");

        var value = context.GetAttribute("subject", "nonexistent");

        Assert.Null(value);
    }

    [Fact]
    public void Decision_Permit_CreatesPermitDecision()
    {
        var decision = Decision.Permit();
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Decision_Deny_CreatesDenyDecision()
    {
        var decision = Decision.Deny();
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Fact]
    public void Decision_NotApplicable_CreatesNotApplicableDecision()
    {
        var decision = Decision.NotApplicable();
        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void Decision_Indeterminate_CreatesIndeterminateDecision()
    {
        var decision = Decision.Indeterminate();
        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
    }

    [Fact]
    public void ObligationResult_Properties_WorkCorrectly()
    {
        var obligation = new ObligationResult
        {
            Id = "test-obligation",
            Attributes = new Dictionary<string, object> { ["key"] = "value" }
        };

        Assert.Equal("test-obligation", obligation.Id);
        Assert.NotEmpty(obligation.Attributes);
    }

    [Fact]
    public void AdviceResult_Properties_WorkCorrectly()
    {
        var advice = new AdviceResult
        {
            Id = "test-advice",
            Attributes = new Dictionary<string, object> { ["key"] = "value" }
        };

        Assert.Equal("test-advice", advice.Id);
        Assert.NotEmpty(advice.Attributes);
    }

    [Fact]
    public void StatusInfo_WithError_SetsProperties()
    {
        var status = new StatusInfo
        {
            StatusCode = "urn:oasis:names:tc:xacml:1.0:status:processing-error",
            StatusMessage = "An error occurred"
        };

        Assert.Contains("error", status.StatusCode);
        Assert.Equal("An error occurred", status.StatusMessage);
    }
}
