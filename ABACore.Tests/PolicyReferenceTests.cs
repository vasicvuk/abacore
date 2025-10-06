using System.Text.Json;
using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Runtime;
using ABACore.Serialization;
using Xunit;

namespace ABACore.Tests;

/// <summary>
/// Tests for policy reference functionality in ALFA.
/// One policy/policyset can reference another policyset by name.
/// </summary>
public class PolicyReferenceTests
{
    [Fact]
    public void PolicyReference_JsonRoundTrip_PreservesReference()
    {
        // Arrange
        string jsonWithReference = @"{
  ""namespace"": ""test"",
  ""policySet"": {
    ""id"": ""parentSet"",
    ""apply"": ""deny-overrides"",
    ""elements"": [
      {
        ""kind"": ""reference"",
        ""policyId"": ""externalPolicy""
      },
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""inlinePolicy"",
          ""rules"": [
            {
              ""effect"": ""permit"",
              ""condition"": {
                ""expression"": {
                  ""kind"": ""booleanLiteral"",
                  ""value"": true
                }
              }
            }
          ]
        }
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithReference);
        string convertedJson = PolicyJsonConverter.ToJson(document);

        // Assert
        Assert.NotNull(convertedJson);
        using var doc = JsonDocument.Parse(convertedJson);
        JsonElement elements = doc.RootElement.GetProperty("policySet").GetProperty("elements");
        Assert.Equal(2, elements.GetArrayLength());

        // First element should be a reference
        JsonElement firstElement = elements[0];
        Assert.Equal("reference", firstElement.GetProperty("kind").GetString());
        Assert.Equal("externalPolicy", firstElement.GetProperty("policyId").GetString());

        // Second element should be an inline policy
        JsonElement secondElement = elements[1];
        Assert.Equal("policy", secondElement.GetProperty("kind").GetString());
    }

    [Fact]
    public void PolicyReference_MultipleReferences_AllPreserved()
    {
        // Arrange
        string jsonWithMultipleReferences = @"{
  ""namespace"": ""test"",
  ""policySet"": {
    ""id"": ""rootSet"",
    ""apply"": ""first-applicable"",
    ""elements"": [
      {
        ""kind"": ""reference"",
        ""policyId"": ""authPolicy""
      },
      {
        ""kind"": ""reference"",
        ""policyId"": ""dataAccessPolicy""
      },
      {
        ""kind"": ""reference"",
        ""policyId"": ""auditPolicy""
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithMultipleReferences);

        // Assert
        Assert.NotNull(document.PolicySet);
        Assert.Equal(3, document.PolicySet.Elements.Count);

        var reference1 = document.PolicySet.Elements[0] as PolicyReference;
        var reference2 = document.PolicySet.Elements[1] as PolicyReference;
        var reference3 = document.PolicySet.Elements[2] as PolicyReference;

        Assert.NotNull(reference1);
        Assert.NotNull(reference2);
        Assert.NotNull(reference3);

        Assert.Equal("authPolicy", reference1!.PolicyId);
        Assert.Equal("dataAccessPolicy", reference2!.PolicyId);
        Assert.Equal("auditPolicy", reference3!.PolicyId);
    }

    [Fact]
    public void PolicyReference_MixedWithPoliciesAndPolicySets_CorrectTypes()
    {
        // Arrange
        string complexJson = @"{
  ""namespace"": ""test"",
  ""policySet"": {
    ""id"": ""complexSet"",
    ""apply"": ""permit-overrides"",
    ""elements"": [
      {
        ""kind"": ""reference"",
        ""policyId"": ""externalPolicy1""
      },
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""inlinePolicy1"",
          ""rules"": [
            {
              ""effect"": ""deny""
            }
          ]
        }
      },
      {
        ""kind"": ""policySet"",
        ""policySet"": {
          ""id"": ""nestedSet"",
          ""apply"": ""deny-overrides"",
          ""elements"": []
        }
      },
      {
        ""kind"": ""reference"",
        ""policyId"": ""externalPolicy2""
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(complexJson);

        // Assert
        Assert.NotNull(document.PolicySet);
        Assert.Equal(4, document.PolicySet.Elements.Count);

        Assert.IsType<PolicyReference>(document.PolicySet.Elements[0]);
        Assert.IsType<Policy>(document.PolicySet.Elements[1]);
        Assert.IsType<PolicySet>(document.PolicySet.Elements[2]);
        Assert.IsType<PolicyReference>(document.PolicySet.Elements[3]);

        Assert.Equal("externalPolicy1", ((PolicyReference)document.PolicySet.Elements[0]).PolicyId);
        Assert.Equal("inlinePolicy1", ((Policy)document.PolicySet.Elements[1]).Id);
        Assert.Equal("nestedSet", ((PolicySet)document.PolicySet.Elements[2]).Id);
        Assert.Equal("externalPolicy2", ((PolicyReference)document.PolicySet.Elements[3]).PolicyId);
    }

    [Fact]
    public void PolicyReference_AlfaFormatter_OutputsCorrectly()
    {
        // Arrange
        var policySet = new PolicySet
        {
            Id = "testSet",
            Combinator = CombiningAlgorithm.DenyOverrides,
            Elements =
            [
                new PolicyReference { PolicyId = "referencedPolicy" },
                new Policy
                {
                    Id = "inlinePolicy",
                    Rules =
                    [
                        new Rule { Effect = Effect.Permit }
                    ]
                }
            ]
        };

        var document = new PolicyDocument
        {
            Namespace = new Namespace { Name = "test", Statements = [] },
            PolicySet = policySet
        };

        // Act
        string alfa = AlfaFormatter.Format(document);

        // Assert
        Assert.NotNull(alfa);
        Assert.Contains("policyset testSet", alfa);
        Assert.Contains("reference referencedPolicy", alfa);
        Assert.Contains("policy inlinePolicy", alfa);
    }

    [Fact]
    public void PolicyReference_Execution_ResolvesAndEvaluatesReferencedPolicy()
    {
        // Arrange - Create a referenced policy in ALFA
        string authPolicyAlfa = @"
namespace test {
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policy authPolicy {
        apply denyOverrides
        rule adminRule {
            permit
            condition role == ""admin""
        }
    }
}";

        string rootSetAlfa = @"
namespace test {
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policyset rootSet {
        apply firstApplicable
        policy authPolicy
    }
}";

        // Load the referenced policy first, then the policy set
        var engine = new PolicyEngine();
        engine.LoadPolicy(authPolicyAlfa);
        engine.LoadPolicy(rootSetAlfa);

        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        // Act
        var decision = engine.Evaluate("rootSet", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyReference_Execution_MultipleReferences_EvaluatedInOrder()
    {
        // Arrange - Create multiple referenced policies in ALFA
        string authPolicyAlfa = @"
namespace test {
    attribute authenticated {
        category = subject
        id = ""authenticated""
        type = boolean
    }

    policy authPolicy {
        apply denyOverrides
        rule authRule {
            permit
            condition authenticated == true
        }
    }
}";

        string rolePolicyAlfa = @"
namespace test {
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policy rolePolicy {
        apply denyOverrides
        rule roleRule {
            permit
            condition role == ""user""
        }
    }
}";

        string accessControlAlfa = @"
namespace test {
    attribute authenticated {
        category = subject
        id = ""authenticated""
        type = boolean
    }
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policyset accessControl {
        apply denyOverrides
        policy authPolicy
        policy rolePolicy
    }
}";

        var engine = new PolicyEngine();
        engine.LoadPolicy(authPolicyAlfa);
        engine.LoadPolicy(rolePolicyAlfa);
        engine.LoadPolicy(accessControlAlfa);

        var context = new EvaluationContext();
        context.SetAttribute("subject", "authenticated", true);
        context.SetAttribute("subject", "role", "user");

        // Act
        var decision = engine.Evaluate("accessControl", context);

        // Assert - Both policies should permit
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyReference_Execution_MixedWithInlinePolicies_AllEvaluated()
    {
        // Arrange - Create a referenced policy and a policy set with both reference and inline policy
        string externalAuthAlfa = @"
namespace test {
    attribute blocked {
        category = subject
        id = ""blocked""
        type = boolean
    }

    policy externalAuth {
        apply denyOverrides
        rule blockRule {
            deny
            condition blocked == true
        }
    }
}";

        string mixedSetAlfa = @"
namespace test {
    attribute blocked {
        category = subject
        id = ""blocked""
        type = boolean
    }

    policyset mixedSet {
        apply denyOverrides
        policy externalAuth
        policy inlinePolicy {
            apply denyOverrides
            rule permitRule {
                permit
                condition true
            }
        }
    }
}";

        var engine = new PolicyEngine();
        engine.LoadPolicy(externalAuthAlfa);
        engine.LoadPolicy(mixedSetAlfa);

        // Test 1: Blocked user should be denied
        var blockedContext = new EvaluationContext();
        blockedContext.SetAttribute("subject", "blocked", true);

        var blockedDecision = engine.Evaluate("mixedSet", blockedContext);
        Assert.Equal(DecisionEffect.Deny, blockedDecision.Effect);

        // Test 2: Normal user should be permitted by inline policy
        var normalContext = new EvaluationContext();
        normalContext.SetAttribute("subject", "blocked", false);

        var normalDecision = engine.Evaluate("mixedSet", normalContext);
        Assert.Equal(DecisionEffect.Permit, normalDecision.Effect);
    }

    [Fact]
    public void PolicyReference_Execution_NotFoundReference_ThrowsException()
    {
        // Arrange - Create a policy set that references a non-existent policy
        string policySetAlfa = @"
namespace test {
    policyset brokenSet {
        apply denyOverrides
        policy nonExistentPolicy
    }
}";

        var engine = new PolicyEngine();

        // Act & Assert - Should throw when trying to load policy set with missing reference
        Assert.Throws<InvalidOperationException>(() => engine.LoadPolicy(policySetAlfa));
    }

    [Fact]
    public void PolicyReference_Execution_NestedPolicySetWithReferences_EvaluatesCorrectly()
    {
        // Arrange - Create nested policy sets with references (Root → Middle → Leaf)
        string leafPolicyAlfa = @"
namespace test {
    attribute resourceType {
        category = resource
        id = ""resourceType""
        type = string
    }

    policy leafPolicy {
        apply denyOverrides
        rule typeRule {
            permit
            condition resourceType == ""document""
        }
    }
}";

        string middleSetAlfa = @"
namespace test {
    attribute resourceType {
        category = resource
        id = ""resourceType""
        type = string
    }

    policyset middleSet {
        apply denyOverrides
        policy leafPolicy
    }
}";

        string rootSetAlfa = @"
namespace test {
    attribute resourceType {
        category = resource
        id = ""resourceType""
        type = string
    }

    policyset rootSet {
        apply denyOverrides
        policy middleSet
    }
}";

        var engine = new PolicyEngine();
        engine.LoadPolicy(leafPolicyAlfa);
        engine.LoadPolicy(middleSetAlfa);
        engine.LoadPolicy(rootSetAlfa);

        var context = new EvaluationContext();
        context.SetAttribute("resource", "resourceType", "document");

        // Act
        var decision = engine.Evaluate("rootSet", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyReference_Execution_WithFirstApplicable_StopsAtFirstMatch()
    {
        // Arrange - Test FirstApplicable combining algorithm with references
        string permitPolicyAlfa = @"
namespace test {
    policy permitPolicy {
        apply denyOverrides
        rule permitAll {
            permit
            condition true
        }
    }
}";

        string denyPolicyAlfa = @"
namespace test {
    policy denyPolicy {
        apply denyOverrides
        rule denyAll {
            deny
            condition true
        }
    }
}";

        string policySetAlfa = @"
namespace test {
    policyset firstApplicableSet {
        apply firstApplicable
        policy permitPolicy
        policy denyPolicy
    }
}";

        var engine = new PolicyEngine();
        engine.LoadPolicy(permitPolicyAlfa);
        engine.LoadPolicy(denyPolicyAlfa);
        engine.LoadPolicy(policySetAlfa);

        var context = new EvaluationContext();

        // Act
        var decision = engine.Evaluate("firstApplicableSet", context);

        // Assert - Should permit because permitPolicy is first
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyReference_Execution_WithPermitOverrides_PermitWins()
    {
        // Arrange - Test PermitOverrides combining algorithm with references
        string denyPolicyAlfa = @"
namespace test {
    policy denyPolicy {
        apply denyOverrides
        rule denyAll {
            deny
            condition true
        }
    }
}";

        string permitPolicyAlfa = @"
namespace test {
    policy permitPolicy {
        apply denyOverrides
        rule permitAll {
            permit
            condition true
        }
    }
}";

        string policySetAlfa = @"
namespace test {
    policyset permitOverridesSet {
        apply permitOverrides
        policy denyPolicy
        policy permitPolicy
    }
}";

        var engine = new PolicyEngine();
        engine.LoadPolicy(denyPolicyAlfa);
        engine.LoadPolicy(permitPolicyAlfa);
        engine.LoadPolicy(policySetAlfa);

        var context = new EvaluationContext();

        // Act
        var decision = engine.Evaluate("permitOverridesSet", context);

        // Assert - Permit should override deny
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void PolicyReference_LoadPolicy_WithoutPolicyId_LoadsAllPoliciesFromFile()
    {
        // Arrange - Single ALFA file with multiple policies
        string multiPolicyAlfa = @"
namespace test {
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policy authPolicy {
        apply denyOverrides
        rule adminRule {
            permit
            condition role == ""admin""
        }
    }

    policy userPolicy {
        apply denyOverrides
        rule userRule {
            permit
            condition role == ""user""
        }
    }

    policyset accessControl {
        apply firstApplicable
        policy authPolicy
        policy userPolicy
    }
}";

        var engine = new PolicyEngine();

        // Act - Load all policies from the file (no policyId specified)
        engine.LoadPolicy(multiPolicyAlfa);

        // Assert - All policies should be loaded
        Assert.True(engine.ContainsPolicy("authPolicy"));
        Assert.True(engine.ContainsPolicy("userPolicy"));
        Assert.True(engine.ContainsPolicy("accessControl"));

        // Verify policies can be evaluated
        var adminContext = new EvaluationContext();
        adminContext.SetAttribute("subject", "role", "admin");
        var adminDecision = engine.Evaluate("accessControl", adminContext);
        Assert.Equal(DecisionEffect.Permit, adminDecision.Effect);

        var userContext = new EvaluationContext();
        userContext.SetAttribute("subject", "role", "user");
        var userDecision = engine.Evaluate("accessControl", userContext);
        Assert.Equal(DecisionEffect.Permit, userDecision.Effect);
    }

    [Fact]
    public void PolicyReference_LoadPolicy_PoliciesCanReferenceEachOther()
    {
        // Arrange - Multiple policies in one file that reference each other
        string multiPolicyAlfa = @"
namespace test {
    attribute authenticated {
        category = subject
        id = ""authenticated""
        type = boolean
    }
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policy authenticationPolicy {
        apply denyOverrides
        rule requireAuth {
            permit
            condition authenticated == true
        }
    }

    policy authorizationPolicy {
        apply denyOverrides
        rule adminAccess {
            permit
            condition role == ""admin""
        }
    }

    policyset securityPolicies {
        apply denyOverrides
        policy authenticationPolicy
        policy authorizationPolicy
    }
}";

        var engine = new PolicyEngine();

        // Act - Load all policies
        engine.LoadPolicy(multiPolicyAlfa);

        // Assert - PolicySet should be able to reference both policies
        var context = new EvaluationContext();
        context.SetAttribute("subject", "authenticated", true);
        context.SetAttribute("subject", "role", "admin");

        var decision = engine.Evaluate("securityPolicies", context);
        Assert.Equal(DecisionEffect.Permit, decision.Effect);

        // Test with only authentication, no authorization
        // With DenyOverrides: if authenticationPolicy permits and authorizationPolicy is NotApplicable, result is Permit
        var authOnlyContext = new EvaluationContext();
        authOnlyContext.SetAttribute("subject", "authenticated", true);
        authOnlyContext.SetAttribute("subject", "role", "guest");

        var authOnlyDecision = engine.Evaluate("securityPolicies", authOnlyContext);
        Assert.Equal(DecisionEffect.Permit, authOnlyDecision.Effect);
    }

    [Fact]
    public void PolicyReference_LoadPolicy_WithPolicyId_OverridesIdForMultiTenantScenario()
    {
        // Arrange - Same policy ALFA text for different tenants
        string policyTemplate = @"
namespace tenant {
    attribute role {
        category = subject
        id = ""role""
        type = string
    }

    policy accessPolicy {
        apply denyOverrides
        rule adminRule {
            permit
            condition role == ""admin""
        }
    }
}";

        var engine = new PolicyEngine();

        // Act - Load the same ALFA text multiple times with different IDs (multi-tenant isolation)
        var tenant1Reg = engine.LoadPolicy(policyTemplate, "tenant1");
        var tenant2Reg = engine.LoadPolicy(policyTemplate, "tenant2");
        var tenant3Reg = engine.LoadPolicy(policyTemplate, "tenant3");

        // Assert - Each tenant has their own isolated policy instance
        Assert.Equal("accessPolicy", tenant1Reg.PolicyId);
        Assert.Equal("accessPolicy", tenant2Reg.PolicyId);
        Assert.Equal("accessPolicy", tenant3Reg.PolicyId);

        Assert.True(engine.ContainsPolicy("accessPolicy", storeId: "tenant1"));
        Assert.True(engine.ContainsPolicy("accessPolicy", storeId: "tenant2"));
        Assert.True(engine.ContainsPolicy("accessPolicy", storeId: "tenant3"));

        // Verify each tenant policy works independently
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        var tenant1Decision = engine.Evaluate("accessPolicy", context, storeId: "tenant1");
        var tenant2Decision = engine.Evaluate("accessPolicy", context, storeId: "tenant2");
        var tenant3Decision = engine.Evaluate("accessPolicy", context, storeId: "tenant3");

        Assert.Equal(DecisionEffect.Permit, tenant1Decision.Effect);
        Assert.Equal(DecisionEffect.Permit, tenant2Decision.Effect);
        Assert.Equal(DecisionEffect.Permit, tenant3Decision.Effect);
    }

    [Fact]
    public void PolicyReference_LoadPolicy_NestedReferences_EvaluatesCorrectly()
    {
        // Arrange - Complex nested structure all in one file
        string complexAlfa = @"
namespace test {
    attribute resourceType {
        category = resource
        id = ""resourceType""
        type = string
    }
    attribute action {
        category = action
        id = ""action""
        type = string
    }

    policy readPolicy {
        apply denyOverrides
        rule allowRead {
            permit
            condition action == ""read""
        }
    }

    policy documentPolicy {
        apply denyOverrides
        rule allowDocuments {
            permit
            condition resourceType == ""document""
        }
    }

    policyset documentReadPolicies {
        apply denyOverrides
        policy documentPolicy
        policy readPolicy
    }

    policyset rootPolicies {
        apply firstApplicable
        policy documentReadPolicies
    }
}";

        var engine = new PolicyEngine();

        // Act - Load all policies
        engine.LoadPolicy(complexAlfa);

        // Assert - All 4 policies should be loaded
        Assert.True(engine.ContainsPolicy("readPolicy"));
        Assert.True(engine.ContainsPolicy("documentPolicy"));
        Assert.True(engine.ContainsPolicy("documentReadPolicies"));
        Assert.True(engine.ContainsPolicy("rootPolicies"));

        // Test evaluation through the nested structure
        var context = new EvaluationContext();
        context.SetAttribute("resource", "resourceType", "document");
        context.SetAttribute("action", "action", "read");

        var decision = engine.Evaluate("rootPolicies", context);
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }
}
