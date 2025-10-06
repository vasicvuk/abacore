using ABACore.Models;

namespace ABACore.Tests;

public class ModelTests
{
    [Fact]
    public void StatusInfo_ProcessingError_CreatesStatusWithMessage()
    {
        var status = StatusInfo.ProcessingError("Test error");

        Assert.Equal("Test error", status.StatusMessage);
        Assert.NotNull(status.StatusCode);
    }

    [Fact]
    public void StatusInfo_MissingAttribute_CreatesStatusWithMessage()
    {
        var status = StatusInfo.MissingAttribute("subject.id");

        Assert.Contains("subject.id", status.StatusMessage);
        Assert.NotNull(status.StatusCode);
    }

    [Fact]
    public void StatusInfo_SyntaxError_CreatesStatusWithMessage()
    {
        var status = StatusInfo.SyntaxError("Invalid syntax");

        Assert.Contains("Invalid syntax", status.StatusMessage);
        Assert.NotNull(status.StatusCode);
    }

    [Fact]
    public void AllExpression_WithInnerExpression_StoresExpression()
    {
        var inner = new LiteralStringExpression { Value = "test" };
        var allExpr = new AllExpression { InnerExpression = inner };

        Assert.Same(inner, allExpr.InnerExpression);
    }

    [Fact]
    public void BooleanAllExpression_WithInnerExpression_StoresExpression()
    {
        var inner = new LiteralStringExpression { Value = "test" };
        var allExpr = new BooleanAllExpression { InnerExpression = inner };

        Assert.Same(inner, allExpr.InnerExpression);
    }

    [Fact]
    public void BagTypeArgument_WithType_StoresType()
    {
        var bagType = new BagTypeArgument { ElementType = AttributeType.String };

        Assert.Equal(AttributeType.String, bagType.ElementType);
    }

    [Fact]
    public void FunctionDeclaration_WithNameAndSignatures_StoresValues()
    {
        var signature = new FunctionSignature
        {
            Inputs = new List<FunctionArgument>
            {
                new SimpleTypeArgument { Type = AttributeType.String }
            },
            Output = new SimpleTypeArgument { Type = AttributeType.Boolean }
        };

        var func = new FunctionDeclaration
        {
            Name = "testFunc",
            Id = "urn:test:func",
            Signatures = new List<FunctionSignature> { signature }
        };

        Assert.Equal("testFunc", func.Name);
        Assert.Equal("urn:test:func", func.Id);
        Assert.Single(func.Signatures);
        Assert.Same(signature, func.Signatures[0]);
    }

    [Fact]
    public void FunctionSignature_WithInputsAndOutput_StoresValues()
    {
        var input = new SimpleTypeArgument { Type = AttributeType.String };
        var output = new SimpleTypeArgument { Type = AttributeType.Boolean };

        var signature = new FunctionSignature
        {
            Inputs = new List<FunctionArgument> { input },
            Output = output
        };

        Assert.Single(signature.Inputs);
        Assert.Same(input, signature.Inputs[0]);
        Assert.Same(output, signature.Output);
    }

    [Fact]
    public void PolicyMetadata_WithProperties_StoresValues()
    {
        var compiledAt = DateTime.UtcNow;
        var metadata = new ABACore.Runtime.PolicyMetadata
        {
            PolicyId = "policy123",
            Version = 5,
            IsLatestVersion = true,
            CompiledAt = compiledAt
        };

        Assert.Equal("policy123", metadata.PolicyId);
        Assert.Equal(5, metadata.Version);
        Assert.True(metadata.IsLatestVersion);
        Assert.Equal(compiledAt, metadata.CompiledAt);
    }

    [Fact]
    public void EvaluationContext_GetAttribute_ReturnsAttributeValue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");

        var value = context.GetAttribute("subject", "id");

        Assert.Equal("user123", value);
    }

    [Fact]
    public void EvaluationContext_GetAttribute_WhenNotSet_ReturnsNull()
    {
        var context = new EvaluationContext();

        var value = context.GetAttribute("subject", "id");

        Assert.Null(value);
    }

    [Fact]
    public void EvaluationContext_SetAttribute_StoresValue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("resource", "type", "document");

        var value = context.GetAttribute("resource", "type");

        Assert.Equal("document", value);
    }

    [Fact]
    public void EvaluationContext_SetAttribute_OverwritesExistingValue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("action", "id", "read");
        context.SetAttribute("action", "id", "write");

        var value = context.GetAttribute("action", "id");

        Assert.Equal("write", value);
    }

    [Fact]
    public void EvaluationContext_GetAttributeGeneric_ConvertsToType()
    {
        var context = new EvaluationContext();
        context.SetAttribute("environment", "hour", 14);

        var value = context.GetAttribute<int>("environment", "hour");

        Assert.Equal(14, value);
    }

    [Fact]
    public void EvaluationContext_GetAttributeGeneric_WhenNotSet_ReturnsDefault()
    {
        var context = new EvaluationContext();

        var value = context.GetAttribute<int>("environment", "hour");

        Assert.Equal(0, value);
    }

    [Fact]
    public void EvaluationContext_HasAttribute_ReturnsTrueWhenSet()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        var has = context.HasAttribute("subject", "role");

        Assert.True(has);
    }

    [Fact]
    public void EvaluationContext_HasAttribute_ReturnsFalseWhenNotSet()
    {
        var context = new EvaluationContext();

        var has = context.HasAttribute("subject", "role");

        Assert.False(has);
    }

    [Fact]
    public void EvaluationContext_GetCategoryAttributes_ReturnsAllCategoryAttributes()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("resource", "type", "document");

        var subjectAttrs = context.GetCategoryAttributes("subject");

        Assert.NotNull(subjectAttrs);
        Assert.Equal(2, subjectAttrs.Count);
        Assert.True(subjectAttrs.ContainsKey("id"));
        Assert.True(subjectAttrs.ContainsKey("role"));
    }

    [Fact]
    public void EvaluationContext_GetCategoryAttributes_WhenCategoryNotSet_ReturnsNull()
    {
        var context = new EvaluationContext();

        var attrs = context.GetCategoryAttributes("subject");

        Assert.Null(attrs);
    }

    [Fact]
    public void EvaluationContext_TryGetAttribute_WhenExists_ReturnsTrueAndValue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");

        var found = context.TryGetAttribute("subject", "id", out var value);

        Assert.True(found);
        Assert.Equal("user123", value);
    }

    [Fact]
    public void EvaluationContext_TryGetAttribute_WhenNotExists_ReturnsFalse()
    {
        var context = new EvaluationContext();

        var found = context.TryGetAttribute("subject", "id", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void EvaluationContext_TryGetAttributeGeneric_WhenExistsAndCorrectType_ReturnsTrueAndValue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("environment", "hour", 14);

        var found = context.TryGetAttribute<int>("environment", "hour", out var value);

        Assert.True(found);
        Assert.Equal(14, value);
    }

    [Fact]
    public void EvaluationContext_TryGetAttributeGeneric_WhenExistsButWrongType_ReturnsFalse()
    {
        var context = new EvaluationContext();
        context.SetAttribute("environment", "hour", "fourteen");

        var found = context.TryGetAttribute<int>("environment", "hour", out var value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void EvaluationContext_RemoveAttribute_WhenExists_RemovesAndReturnsTrue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");

        var removed = context.RemoveAttribute("subject", "id");

        Assert.True(removed);
        Assert.False(context.HasAttribute("subject", "id"));
    }

    [Fact]
    public void EvaluationContext_RemoveAttribute_WhenNotExists_ReturnsFalse()
    {
        var context = new EvaluationContext();

        var removed = context.RemoveAttribute("subject", "id");

        Assert.False(removed);
    }

    [Fact]
    public void EvaluationContext_Clear_RemovesAllAttributes()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");
        context.SetAttribute("resource", "type", "document");
        context.SetAttribute("action", "id", "read");

        context.Clear();

        Assert.False(context.HasAttribute("subject", "id"));
        Assert.False(context.HasAttribute("resource", "type"));
        Assert.False(context.HasAttribute("action", "id"));
    }

    [Fact]
    public void EvaluationContext_GetAllAttributes_ReturnsCopyOfAllAttributes()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "id", "user123");
        context.SetAttribute("resource", "type", "document");

        var allAttrs = context.GetAllAttributes();

        Assert.Equal(2, allAttrs.Count);
        Assert.Contains(allAttrs.Keys, k => k.Contains("subject"));
        Assert.Contains(allAttrs.Keys, k => k.Contains("resource"));
    }

    [Fact]
    public void EvaluationContext_GetAttributeByNamespace_FindsAttribute()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "userName", "alice");

        var value = context.GetAttributeByNamespace("some.namespace", "userName");

        Assert.Equal("alice", value);
    }

    [Fact]
    public void EvaluationContext_GetAttributeByNamespaceGeneric_ConvertsToType()
    {
        var context = new EvaluationContext();
        context.SetAttribute("environment", "temperature", 72);

        var value = context.GetAttributeByNamespace<int>("some.namespace", "temperature");

        Assert.Equal(72, value);
    }

    [Fact]
    public void EvaluationContext_ConstructorWithDictionary_InitializesAttributes()
    {
        var initialAttrs = new Dictionary<string, Dictionary<string, object>>
        {
            [ABACore.Runtime.WellKnownCategories.Subject] = new Dictionary<string, object> { ["id"] = "user123" },
            [ABACore.Runtime.WellKnownCategories.Resource] = new Dictionary<string, object> { ["type"] = "document" }
        };

        var context = new EvaluationContext(initialAttrs);

        Assert.Equal("user123", context.GetAttribute("subject", "id"));
        Assert.Equal("document", context.GetAttribute("resource", "type"));
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
        var status = StatusInfo.ProcessingError("Error");
        var decision = Decision.Indeterminate(status);

        Assert.Equal(DecisionEffect.Indeterminate, decision.Effect);
        Assert.Same(status, decision.Status);
    }

    [Fact]
    public void Decision_WithObligations_StoresObligations()
    {
        var obligation = new ObligationResult { Id = "obl1" };
        var decision = Decision.Permit(new List<ObligationResult> { obligation }, null);

        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal("obl1", decision.Obligations[0].Id);
    }

    [Fact]
    public void Decision_WithAdvice_StoresAdvice()
    {
        var advice = new AdviceResult { Id = "adv1" };
        var decision = Decision.Deny(null, new List<AdviceResult> { advice });

        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
        Assert.Equal("adv1", decision.Advice[0].Id);
    }

    [Fact]
    public void Decision_Permit_HasCorrectEffect()
    {
        var decision = Decision.Permit();

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Decision_WithObligations_StoresObligationsCorrectly()
    {
        var obligation = new ObligationResult { Id = "obl1" };
        var decision = Decision.Permit(new List<ObligationResult> { obligation }, null);

        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }
}
