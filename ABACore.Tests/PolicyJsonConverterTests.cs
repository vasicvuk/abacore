using System;
using System.IO;
using System.Text.Json;
using ABACore.Models;
using ABACore.Serialization;
using Xunit;

namespace ABACore.Tests;

/// <summary>
/// Tests for the PolicyJsonConverter class to ensure proper JSON/ALFA conversion functionality.
/// </summary>
public class PolicyJsonConverterTests
{
    private const string SimpleAlfaPolicy = @"
namespace test {
    policy simplePolicy {
        target clause resource.id == ""document1""

        rule rule1 {
            condition user.role == ""admin""
            permit
        }
    }
}";

    private const string ExpectedSimpleJson = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""simplePolicy"",
    ""target"": {
      ""clauses"": [
        {
          ""expression"": {
            ""kind"": ""comparison"",
            ""operator"": ""Equal"",
            ""left"": {
              ""kind"": ""attribute"",
              ""namespace"": ""resource"",
              ""attribute"": ""id""
            },
            ""right"": {
              ""kind"": ""literalString"",
              ""value"": ""document1""
            }
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

    [Fact]
    public void JsonFromAlfa_ConvertsSimplePolicyToJson()
    {
        // Arrange & Act
        string result = PolicyJsonConverter.JsonFromAlfa(SimpleAlfaPolicy);

        // Assert
        var expectedDoc = JsonDocument.Parse(ExpectedSimpleJson);
        var resultDoc = JsonDocument.Parse(result);

        Assert.True(JsonElementEqualityComparer.Equals(expectedDoc.RootElement, resultDoc.RootElement));
    }

    [Fact]
    public void AlfaFromJson_ConvertsJsonToAlfa()
    {
        // Arrange & Act
        string result = PolicyJsonConverter.AlfaFromJson(ExpectedSimpleJson);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("namespace test", result);
        Assert.Contains("policy simplePolicy", result);
        Assert.Contains("target clause resource.id == \"document1\"", result);
        Assert.Contains("rule rule1 {", result);
        Assert.Contains("condition user.role == \"admin\"", result);
    }

    [Fact]
    public void ToJson_PolicyDocument_ConvertsToJson()
    {
        // Arrange
        var document = new PolicyDocument
        {
            Namespace = new Namespace
            {
                Name = "test",
                Statements =
                [
                    new PolicyStatement
                    {
                        Policy = new Policy
                        {
                            Id = "testPolicy",
                            Rules =
                            [
                                new Rule
                                {
                                    Effect = Effect.Permit,
                                    Condition = new Condition
                                    {
                                        Expression = new BooleanLiteralExpression { Value = true }
                                    }
                                }
                            ]
                        }
                    }
                ]
            }
        };

        // Act
        string result = PolicyJsonConverter.ToJson(document);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("test", doc.RootElement.GetProperty("namespace").GetString());
        Assert.Equal("testPolicy", doc.RootElement.GetProperty("policy").GetProperty("id").GetString());
    }

    [Fact]
    public void FromJson_SimpleJson_ConvertsToPolicyDocument()
    {
        // Arrange & Act
        PolicyDocument result = PolicyJsonConverter.FromJson(ExpectedSimpleJson);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Namespace?.Name);
        Assert.NotNull(result.Policy);
        Assert.Equal("simplePolicy", result.Policy.Id);
        Assert.Single(result.Policy.Rules);
        Assert.Equal(Effect.Permit, result.Policy.Rules[0].Effect);
    }

    [Fact]
    public void JsonFromAlfa_HandlesPolicySet()
    {
        // Arrange
        string policySetAlfa = @"
namespace test {
    policyset parentSet {
        apply denyOverrides
        policy child1 {
            rule rule1 {
                deny
                condition user.role == ""guest""
            }
        }
        policy child2 {
            rule rule1 {
                permit
                condition user.age >= 18
            }
        }
    }
}";

        // Act
        string result = PolicyJsonConverter.JsonFromAlfa(policySetAlfa);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("test", doc.RootElement.GetProperty("namespace").GetString());
        Assert.True(doc.RootElement.TryGetProperty("policySet", out JsonElement policySetElement));
        Assert.Equal("deny-overrides", policySetElement.GetProperty("apply").GetString());
        Assert.Equal(2, policySetElement.GetProperty("elements").GetArrayLength());
    }

    [Fact]
    public void FromJson_HandlesPolicySet()
    {
        // Arrange
        string policySetJson = @"{
  ""namespace"": ""test"",
  ""policySet"": {
    ""id"": ""parentSet"",
    ""apply"": ""deny-overrides"",
    ""elements"": [
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""child1"",
          ""rules"": [
            {
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
              }
            }
          ]
        }
      }
    ]
  }
}";

        // Act
        PolicyDocument result = PolicyJsonConverter.FromJson(policySetJson);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Namespace?.Name);
        Assert.NotNull(result.PolicySet);
        Assert.Equal("parentSet", result.PolicySet.Id);
        Assert.Equal(CombiningAlgorithm.DenyOverrides, result.PolicySet.Combinator);
        Assert.Single(result.PolicySet.Elements);
    }

    [Fact]
    public void JsonFromAlfa_HandlesObligationsAndAdvice()
    {
        // Arrange
        // Test JSON->ALFA->JSON round trip for policy with obligations/advice
        string jsonWithObligations = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""accessPolicy"",
    ""rules"": [
      {
        ""id"": ""rule1"",
        ""effect"": ""permit"",
        ""onPermit"": [
          {
            ""id"": ""logAccess"",
            ""attributes"": {
              ""message"": {
                ""kind"": ""literalString"",
                ""value"": ""Access granted""
              }
            }
          }
        ],
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

        // Act - Convert JSON to ALFA and back to JSON
        string alfa = PolicyJsonConverter.AlfaFromJson(jsonWithObligations);
        string jsonResult = PolicyJsonConverter.JsonFromAlfa(alfa);

        // Assert
        Assert.NotNull(jsonResult);
        var doc = JsonDocument.Parse(jsonResult);
        JsonElement policy = doc.RootElement.GetProperty("policy");
        JsonElement rule = policy.GetProperty("rules")[0];

        Assert.True(rule.TryGetProperty("onPermit", out JsonElement onPermit));
        Assert.True(onPermit.GetArrayLength() > 0);

        Assert.True(rule.TryGetProperty("onDeny", out JsonElement onDeny));
        Assert.True(onDeny.GetArrayLength() > 0);
    }

    [Fact]
    public void JsonFromAlfa_HandlesComplexExpressions()
    {
        // Arrange
        string complexPolicy = @"
namespace test {
    policy complexPolicy {
        target clause resource.type == ""financial"" and (user.department == ""finance"" or user.role == ""auditor"")

        rule rule1 {
            permit
            condition user.age >= 21 and user.clearanceLevel >= 3
        }
    }
}";

        // Act
        string result = PolicyJsonConverter.JsonFromAlfa(complexPolicy);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        JsonElement target = doc.RootElement.GetProperty("policy").GetProperty("target");
        JsonElement clauses = target.GetProperty("clauses");

        // Should have a complex logical expression
        Assert.True(clauses.GetArrayLength() > 0);
    }

    [Fact]
    public void FromJson_HandlesAllExpressionTypes()
    {
        // Arrange
        string complexJson = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""complex"",
    ""rules"": [
      {
        ""effect"": ""permit"",
        ""condition"": {
          ""expression"": {
            ""kind"": ""logical"",
            ""operator"": ""And"",
            ""left"": {
              ""kind"": ""comparison"",
              ""operator"": ""GreaterThanOrEqual"",
              ""left"": {
                ""kind"": ""attribute"",
                ""namespace"": ""user"",
                ""attribute"": ""age""
              },
              ""right"": {
                ""kind"": ""literalInteger"",
                ""value"": 21
              }
            },
            ""right"": {
              ""kind"": ""not"",
              ""expression"": {
                ""kind"": ""booleanAttribute"",
                ""namespace"": ""user"",
                ""attribute"": ""blocked""
              }
            }
          }
        }
      }
    ]
  }
}";

        // Act
        PolicyDocument result = PolicyJsonConverter.FromJson(complexJson);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Policy);
        Assert.NotNull(result.Policy.Rules[0].Condition);

        BooleanExpression condition = result.Policy.Rules[0].Condition!.Expression;
        Assert.IsType<LogicalBinaryExpression>(condition);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void JsonFromAlfa_InvalidInput_ThrowsArgumentException(string? alfaText)
    {
        // Arrange, Act & Assert
        if (alfaText == null)
        {
            Assert.Throws<ArgumentNullException>(() => PolicyJsonConverter.JsonFromAlfa(alfaText!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => PolicyJsonConverter.JsonFromAlfa(alfaText!));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AlfaFromJson_InvalidInput_ThrowsArgumentException(string? jsonText)
    {
        // Arrange, Act & Assert
        if (jsonText == null)
        {
            Assert.Throws<ArgumentNullException>(() => PolicyJsonConverter.AlfaFromJson(jsonText!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => PolicyJsonConverter.AlfaFromJson(jsonText!));
        }
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        string invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() => PolicyJsonConverter.FromJson(invalidJson));
    }

    [Fact]
    public void RoundTripConversion_ProducesEquivalentPolicy()
    {
        // Arrange
        string originalAlfa = @"
namespace roundtrip.test {
    policy testPolicy {
        apply permitOverrides
        target clause resource.type == ""sensitive""
        rule adultAccess {
            permit
            condition user.age >= 18
        }
        rule blockedUsers {
            deny
            condition user.id == ""blocked_user""
        }
    }
}";

        // Act
        string json = PolicyJsonConverter.JsonFromAlfa(originalAlfa);
        string convertedAlfa = PolicyJsonConverter.AlfaFromJson(json);

        // Assert
        Assert.NotNull(convertedAlfa);
        Assert.Contains("namespace roundtrip.test", convertedAlfa);
        Assert.Contains("policy testPolicy", convertedAlfa);
        Assert.Contains("apply permitOverrides", convertedAlfa);
    }
}

/// <summary>
/// Helper class for comparing JsonElement objects for equality.
/// </summary>
internal static class JsonElementEqualityComparer
{
    public static bool Equals(JsonElement left, JsonElement right)
    {
        return left.ValueKind == right.ValueKind && left.ValueKind switch
        {
            JsonValueKind.Object => EqualsObject(left, right),
            JsonValueKind.Array => EqualsArray(left, right),
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.GetDouble() == right.GetDouble(),
            JsonValueKind.True => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null => true,
            _ => false
        };
    }

    private static bool EqualsObject(JsonElement left, JsonElement right)
    {
        var leftProps = left.EnumerateObject().ToList();
        var rightProps = right.EnumerateObject().ToList();

        if (leftProps.Count != rightProps.Count)
        {
            return false;
        }

        foreach (JsonProperty leftProp in leftProps)
        {
            JsonProperty rightProp = rightProps.FirstOrDefault(p => p.Name == leftProp.Name);
            if (rightProp.Name == null || !Equals(leftProp.Value, rightProp.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EqualsArray(JsonElement left, JsonElement right)
    {
        var leftItems = left.EnumerateArray().ToList();
        var rightItems = right.EnumerateArray().ToList();

        if (leftItems.Count != rightItems.Count)
        {
            return false;
        }

        for (int i = 0; i < leftItems.Count; i++)
        {
            if (!Equals(leftItems[i], rightItems[i]))
            {
                return false;
            }
        }

        return true;
    }
}
