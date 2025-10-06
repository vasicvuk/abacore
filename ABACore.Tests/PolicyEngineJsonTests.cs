using System;
using System.IO;
using System.Linq;
using ABACore.Models;
using ABACore.Runtime;
using ABACore.Serialization;
using Xunit;

namespace ABACore.Tests;

/// <summary>
/// Tests for the PolicyEngine JSON loading and conversion functionality.
/// </summary>
public class PolicyEngineJsonTests
{
    [Fact]
    public void LoadPolicyFromJson_ValidJson_LoadsSuccessfully()
    {
        // Arrange
        var engine = new PolicyEngine();
        string jsonPolicy = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""testPolicy"",
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
}";

        // Act
        PolicyRegistration registration = engine.LoadPolicyFromJson(jsonPolicy);

        // Assert
        Assert.NotNull(registration);
        Assert.Equal("testPolicy", registration.PolicyId);
        Assert.Equal(1, registration.Version);
    }

    [Fact]
    public void LoadPolicyFromJsonFile_ValidFile_LoadsSuccessfully()
    {
        // Arrange
        var engine = new PolicyEngine();
        string tempFile = Path.GetTempFileName();
        string jsonPolicy = @"{
  ""namespace"": ""filetest"",
  ""policy"": {
    ""id"": ""filePolicy"",
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
}";

        try
        {
            File.WriteAllText(tempFile, jsonPolicy);

            // Act
            PolicyRegistration registration = engine.LoadPolicyFromJsonFile(tempFile);

            // Assert
            Assert.NotNull(registration);
            Assert.Equal("filePolicy", registration.PolicyId);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadPolicyFromJsonFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var engine = new PolicyEngine();
        string nonExistentFile = Path.Combine(Path.GetTempPath(), "non_existent_file.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => engine.LoadPolicyFromJsonFile(nonExistentFile));
    }

    [Fact]
    public void LoadPoliciesFromJson_MultiplePolicies_LoadsAllSuccessfully()
    {
        // Arrange
        var engine = new PolicyEngine();
        string[] jsonPolicies = [
            @"{
  ""namespace"": ""test1"",
  ""policy"": {
    ""id"": ""policy1"",
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
}",
            @"{
  ""namespace"": ""test2"",
  ""policy"": {
    ""id"": ""policy2"",
    ""rules"": [
      {
        ""effect"": ""deny"",
        ""condition"": {
          ""expression"": {
            ""kind"": ""booleanLiteral"",
            ""value"": false
          }
        }
      }
    ]
  }
}"
        ];

        // Act
        var registrations = engine.LoadPoliciesFromJson(jsonPolicies).ToList();

        // Assert
        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, r => r.PolicyId == "policy1");
        Assert.Contains(registrations, r => r.PolicyId == "policy2");
    }

    [Fact]
    public void ConvertAlfaToJson_ValidAlfa_ConvertsToJson()
    {
        // Arrange
        var engine = new PolicyEngine();
        string alfaPolicy = @"
namespace conversion {
    policy convertPolicy {
        rule rule1 {
            condition user.role == ""admin""
            permit
        }
    }
}";

        // Act
        string json = engine.ConvertAlfaToJson(alfaPolicy);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"namespace\": \"conversion\"", json);
        Assert.Contains("\"id\": \"convertPolicy\"", json);
    }

    [Fact]
    public void ConvertJsonToAlfa_ValidJson_ConvertsToAlfa()
    {
        // Arrange
        var engine = new PolicyEngine();
        string jsonPolicy = @"{
  ""namespace"": ""backconversion"",
  ""policy"": {
    ""id"": ""backPolicy"",
    ""rules"": [
      {
        ""effect"": ""permit"",
        ""condition"": {
          ""expression"": {
            ""kind"": ""comparison"",
            ""operator"": ""Equal"",
            ""left"": {
              ""kind"": ""attribute"",
              ""namespace"": ""user"",
              ""attribute"": ""role""
            },
            ""right"": {
              ""kind"": ""literalString"",
              ""value"": ""admin""
            }
          }
        }
      }
    ]
  }
}";

        // Act
        string alfa = engine.ConvertJsonToAlfa(jsonPolicy);

        // Assert
        Assert.NotNull(alfa);
        Assert.Contains("namespace backconversion", alfa);
        Assert.Contains("policy backPolicy", alfa);
        Assert.Contains("user.role == \"admin\"", alfa);
    }

    [Fact]
    public void LoadPolicyFromJson_ComplexPolicy_LoadsSuccessfully()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        string complexJson = @"{
  ""namespace"": ""complex"",
  ""policy"": {
    ""id"": ""complexPolicy"",
    ""apply"": ""first-applicable"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""comparison"",
            ""operator"": ""Equal"",
            ""left"": {
              ""kind"": ""attribute"",
              ""namespace"": ""resource"",
              ""attribute"": ""type""
            },
            ""right"": {
              ""kind"": ""literalString"",
              ""value"": ""sensitive""
            }
          }
        }
      ]
    },
    ""rules"": [
      {
        ""id"": ""adminRule"",
        ""effect"": ""permit"",
        ""target"": {
          ""clauses"": [
            {
              ""expression"": {
                ""kind"": ""comparison"",
                ""operator"": ""Equal"",
                ""left"": {
                  ""kind"": ""attribute"",
                  ""namespace"": ""user"",
                  ""attribute"": ""role""
                },
                ""right"": {
                  ""kind"": ""literalString"",
                  ""value"": ""admin""
                }
              }
            }
          ]
        },
        ""onPermit"": [
          {
            ""id"": ""logAccess"",
            ""attributes"": {
              ""message"": {
                ""kind"": ""literalString"",
                ""value"": ""Admin access granted""
              },
              ""level"": {
                ""kind"": ""literalString"",
                ""value"": ""INFO""
              }
            }
          }
        ]
      },
      {
        ""id"": ""denyRule"",
        ""effect"": ""deny"",
        ""condition"": {
          ""expression"": {
            ""kind"": ""comparison"",
            ""operator"": ""Equal"",
            ""left"": {
              ""kind"": ""attribute"",
              ""namespace"": ""user"",
              ""attribute"": ""role""
            },
            ""right"": {
              ""kind"": ""literalString"",
              ""value"": ""guest""
            }
          }
        },
        ""onDeny"": [
          {
            ""id"": ""notifyAdmin"",
            ""attributes"": {
              ""userId"": {
                ""kind"": ""attribute"",
                ""namespace"": ""user"",
                ""attribute"": ""id""
              }
            }
          }
        ]
      }
    ]
  }
}";

        // Act
        PolicyRegistration registration = engine.LoadPolicyFromJson(complexJson);

        // Assert
        Assert.NotNull(registration);
        Assert.Equal("complexPolicy", registration.PolicyId);

        // Verify the policy can be evaluated
        var context = new EvaluationContext();
        context.SetAttribute("user", "role", "admin");
        context.SetAttribute("user", "id", "user123");
        context.SetAttribute("resource", "type", "sensitive");
        context.SetAttribute("action", "id", "read");

        Decision result = engine.Evaluate("complexPolicy", context);

        // The result should be Permit because:
        // - Policy target matches (resource.type == "sensitive")
        // - adminRule target matches (user.role == "admin") -> Permit
        // - denyRule condition doesn't match (user.role != "guest")
        // With first-applicable, adminRule is the first applicable rule and returns Permit
        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void LoadPolicyFromJson_PolicySet_LoadsSuccessfully()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        string policySetJson = @"{
  ""namespace"": ""policyset"",
  ""policySet"": {
    ""id"": ""parentSet"",
    ""apply"": ""deny-overrides"",
    ""elements"": [
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""childPolicy1"",
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
      },
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""childPolicy2"",
          ""rules"": [
            {
              ""effect"": ""deny"",
              ""condition"": {
                ""expression"": {
                  ""kind"": ""booleanLiteral"",
                  ""value"": false
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
        PolicyRegistration registration = engine.LoadPolicyFromJson(policySetJson);

        // Assert
        Assert.NotNull(registration);
        Assert.Equal("parentSet", registration.PolicyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LoadPolicyFromJson_InvalidInput_ThrowsArgumentException(string? jsonText)
    {
        // Arrange
        var engine = new PolicyEngine();

        // Act & Assert
        if (jsonText == null)
        {
            Assert.Throws<ArgumentNullException>(() => engine.LoadPolicyFromJson(jsonText!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => engine.LoadPolicyFromJson(jsonText!));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LoadPolicyFromJsonFile_InvalidInput_ThrowsArgumentException(string? filePath)
    {
        // Arrange
        var engine = new PolicyEngine();

        // Act & Assert
        if (filePath == null)
        {
            Assert.Throws<ArgumentNullException>(() => engine.LoadPolicyFromJsonFile(filePath!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => engine.LoadPolicyFromJsonFile(filePath!));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ConvertAlfaToJson_InvalidInput_ThrowsArgumentException(string? alfaText)
    {
        // Arrange
        var engine = new PolicyEngine();

        // Act & Assert
        if (alfaText == null)
        {
            Assert.Throws<ArgumentNullException>(() => engine.ConvertAlfaToJson(alfaText!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => engine.ConvertAlfaToJson(alfaText!));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ConvertJsonToAlfa_InvalidInput_ThrowsArgumentException(string? jsonText)
    {
        // Arrange
        var engine = new PolicyEngine();

        // Act & Assert
        if (jsonText == null)
        {
            Assert.Throws<ArgumentNullException>(() => engine.ConvertJsonToAlfa(jsonText!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => engine.ConvertJsonToAlfa(jsonText!));
        }
    }

    [Fact]
    public void LoadPoliciesFromJson_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var engine = new PolicyEngine();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => engine.LoadPoliciesFromJson(null!));
    }

    [Fact]
    public void RoundTripConversion_JsonToAlfaToJson_ProducesEquivalentJson()
    {
        // Arrange
        var engine = new PolicyEngine();
        string originalJson = @"{
  ""namespace"": ""roundtrip"",
  ""policy"": {
    ""id"": ""roundtripPolicy"",
    ""apply"": ""permit-overrides"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""comparison"",
            ""operator"": ""Equal"",
            ""left"": {
              ""kind"": ""attribute"",
              ""namespace"": ""resource"",
              ""attribute"": ""type""
            },
            ""right"": {
              ""kind"": ""literalString"",
              ""value"": ""test""
            }
          }
        }
      ]
    },
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
}";

        // Act
        string alfa = engine.ConvertJsonToAlfa(originalJson);
        string convertedJson = engine.ConvertAlfaToJson(alfa);

        // Assert
        Assert.NotNull(convertedJson);

        // Parse both JSON documents and compare key elements
        using var originalDoc = System.Text.Json.JsonDocument.Parse(originalJson);
        using var convertedDoc = System.Text.Json.JsonDocument.Parse(convertedJson);

        Assert.Equal(
            originalDoc.RootElement.GetProperty("namespace").GetString(),
            convertedDoc.RootElement.GetProperty("namespace").GetString());

        Assert.Equal(
            originalDoc.RootElement.GetProperty("policy").GetProperty("id").GetString(),
            convertedDoc.RootElement.GetProperty("policy").GetProperty("id").GetString());
    }

    [Fact]
    public void LoadPolicyFromJson_EvaluatesCorrectly_AfterLoading()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        string jsonPolicy = @"{
  ""namespace"": ""evaluation"",
  ""policy"": {
    ""id"": ""evalPolicy"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""booleanLiteral"",
            ""value"": true
          }
        }
      ]
    },
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
}";

        // Act
        engine.LoadPolicyFromJson(jsonPolicy);

        var context = new EvaluationContext();
        // Simplified context for boolean literal policy

        Decision result = engine.Evaluate("evalPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void ComplexScenario_MultipleRulesAndConditions_WorksCorrectly()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        string complexJson = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""complexPolicy"",
    ""apply"": ""deny-overrides"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""booleanLiteral"",
            ""value"": true
          }
        }
      ]
    },
    ""rules"": [
      {
        ""id"": ""adminRule"",
        ""effect"": ""permit"",
        ""target"": {
          ""clauses"": [
            {
              ""expression"": {
                ""kind"": ""booleanLiteral"",
                ""value"": true
              }
            }
          ]
        },
        ""condition"": {
          ""expression"": {
            ""kind"": ""booleanLiteral"",
            ""value"": true
          }
        }
      },
      {
        ""id"": ""denyRule"",
        ""effect"": ""deny""
      }
    ]
  }
}";

        // Act
        PolicyRegistration registration = engine.LoadPolicyFromJson(complexJson);
        Assert.Equal("complexPolicy", registration.PolicyId);

        // Test evaluation with simplified boolean logic
        var context = new EvaluationContext();

        Decision result = engine.Evaluate("complexPolicy", context);
        // With deny-overrides, the deny rule should be applied since admin rule's target/condition are always true
        Assert.Equal(DecisionEffect.Deny, result.Effect);
    }

    [Fact]
    public void ComplexScenario_PolicySetWithNestedPolicies_WorksCorrectly()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        string policySetJson = @"{
  ""namespace"": ""test"",
  ""policySet"": {
    ""id"": ""parentSet"",
    ""apply"": ""permit-overrides"",
    ""elements"": [
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""denyAllPolicy"",
          ""rules"": [
            {
              ""effect"": ""deny""
            }
          ]
        }
      },
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""adminAllowPolicy"",
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
        PolicyRegistration registration = engine.LoadPolicyFromJson(policySetJson);
        Assert.Equal("parentSet", registration.PolicyId);

        // Test evaluation with permit-overrides
        var context = new EvaluationContext();

        Decision result = engine.Evaluate("parentSet", context);
        // With permit-overrides, the permit rule should override the deny rule
        Assert.Equal(DecisionEffect.Permit, result.Effect);
    }

    [Fact]
    public void ComplexScenario_RoundTripConversion_ProducesFunctionalPolicy()
    {
        // Arrange
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        var engine = new PolicyEngine(config);
        var engine2 = new PolicyEngine(config);
        string originalJson = @"{
  ""namespace"": ""roundtrip"",
  ""policy"": {
    ""id"": ""testPolicy"",
    ""apply"": ""first-applicable"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""booleanLiteral"",
            ""value"": true
          }
        }
      ]
    },
    ""rules"": [
      {
        ""id"": ""rule1"",
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
}";

        // Act
        // Convert to ALFA and back to JSON
        string alfaText = PolicyJsonConverter.AlfaFromJson(originalJson);
        string convertedJson = PolicyJsonConverter.JsonFromAlfa(alfaText);
        // Load both the original and converted policies
        _ = engine.LoadPolicyFromJson(originalJson);
        _ = engine2.LoadPolicyFromJson(convertedJson);

        // Test that both policies behave identically
        var testContext = new EvaluationContext();

        Decision originalResult = engine.Evaluate("testPolicy", testContext);
        Decision convertedResult = engine.Evaluate("testPolicy", testContext);

        // Assert
        Assert.Equal(originalResult.Effect, convertedResult.Effect);
        Assert.Equal(DecisionEffect.Permit, originalResult.Effect);
    }
}
