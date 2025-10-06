using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class ComponentTests
{
    [Fact]
    public void NamespaceResolver_GetNamespace_ReturnsNamespaceInfo()
    {
        var resolver = new NamespaceResolver();
        var ns = resolver.GetNamespace("Oasis.Attributes.Subject");

        Assert.NotNull(ns);
    }

    [Fact]
    public void NamespaceResolver_GetNamespace_WithNonExistent_ReturnsNull()
    {
        var resolver = new NamespaceResolver();
        var ns = resolver.GetNamespace("NonExistent.Namespace");

        Assert.Null(ns);
    }

    [Fact]
    public void NamespaceResolver_CategoryRegistry_IsAccessible()
    {
        var resolver = new NamespaceResolver();
        Assert.NotNull(resolver.CategoryRegistry);
    }

    [Fact]
    public void EvaluationConfiguration_DefaultConstructor_CreatesConfiguration()
    {
        var config = new EvaluationConfiguration();
        Assert.NotNull(config);
    }


    [Fact]
    public void StatusInfo_StatusCode_Property_WorksCorrectly()
    {
        var status = new StatusInfo
        {
            StatusCode = "urn:oasis:names:tc:xacml:1.0:status:ok",
            StatusMessage = "Success"
        };

        Assert.Equal("urn:oasis:names:tc:xacml:1.0:status:ok", status.StatusCode);
        Assert.Equal("Success", status.StatusMessage);
    }

    [Fact]
    public void EvaluationContext_SetAttribute_WithDifferentTypes_StoresCorrectly()
    {
        var context = new EvaluationContext();

        context.SetAttribute("test", "string", "value");
        context.SetAttribute("test", "int", 42);
        context.SetAttribute("test", "bool", true);
        context.SetAttribute("test", "double", 3.14);

        Assert.Equal("value", context.GetAttribute("test", "string"));
        Assert.Equal(42, context.GetAttribute("test", "int"));
        Assert.Equal(true, context.GetAttribute("test", "bool"));
        Assert.Equal(3.14, context.GetAttribute("test", "double"));
    }

    [Fact]
    public void PolicyEngine_WithNonExistentPolicy_ThrowsException()
    {
        var engine = new PolicyEngine();
        var context = new EvaluationContext();

        Assert.Throws<ABACore.Exceptions.PolicyNotFoundException>(() => engine.Evaluate("non-existent", context));
    }

    [Fact]
    public void CompiledPolicy_Evaluate_ExecutesDelegate()
    {
        var policy = new CompiledPolicy
        {
            PolicyId = "test",
            EvaluationDelegate = _ => Decision.Permit()
        };

        var decision = policy.Evaluate(new EvaluationContext());
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }
}
