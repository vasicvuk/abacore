using System.Text.Json;
using ABACore.Models;
using ABACore.Serialization;
using Xunit;

namespace ABACore.Tests;

/// <summary>
/// Tests for JSON import functionality.
/// Ensures that imports can be serialized to/from JSON just like in ALFA.
/// </summary>
public class JsonImportTests
{
    [Fact]
    public void JsonWithSingleImport_RoundTrip_PreservesImport()
    {
        // Arrange
        string jsonWithImport = @"{
  ""namespace"": ""test.policies"",
  ""imports"": [
    {
      ""namespacePath"": ""common.attributes""
    }
  ],
  ""policy"": {
    ""id"": ""testPolicy"",
    ""rules"": [
      {
        ""effect"": ""permit""
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithImport);
        string convertedJson = PolicyJsonConverter.ToJson(document);

        // Assert
        Assert.NotNull(document.Namespace);
        Assert.NotNull(document.Namespace.Statements);

        var imports = document.Namespace.Statements.OfType<Import>().ToList();
        Assert.Single(imports);
        Assert.Equal("common.attributes", imports[0].NamespacePath);
        Assert.False(imports[0].Wildcard);

        // Verify round-trip
        using var doc = JsonDocument.Parse(convertedJson);
        Assert.True(doc.RootElement.TryGetProperty("imports", out JsonElement importsElement));
        Assert.Equal(1, importsElement.GetArrayLength());
        Assert.Equal("common.attributes", importsElement[0].GetProperty("namespacePath").GetString());
    }

    [Fact]
    public void JsonWithMultipleImports_PreservesAllImports()
    {
        // Arrange
        string jsonWithImports = @"{
  ""namespace"": ""application"",
  ""imports"": [
    {
      ""namespacePath"": ""common.types""
    },
    {
      ""namespacePath"": ""security.roles"",
      ""wildcard"": true
    },
    {
      ""namespacePath"": ""data.access""
    }
  ],
  ""policy"": {
    ""id"": ""appPolicy"",
    ""rules"": [
      {
        ""effect"": ""deny""
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithImports);

        // Assert
        var imports = document.Namespace!.Statements.OfType<Import>().ToList();
        Assert.Equal(3, imports.Count);

        Assert.Equal("common.types", imports[0].NamespacePath);
        Assert.False(imports[0].Wildcard);

        Assert.Equal("security.roles", imports[1].NamespacePath);
        Assert.True(imports[1].Wildcard);

        Assert.Equal("data.access", imports[2].NamespacePath);
        Assert.False(imports[2].Wildcard);
    }

    [Fact]
    public void JsonWithWildcardImport_PreservesWildcard()
    {
        // Arrange
        string jsonWithWildcard = @"{
  ""namespace"": ""test"",
  ""imports"": [
    {
      ""namespacePath"": ""all.attributes"",
      ""wildcard"": true
    }
  ],
  ""policy"": {
    ""id"": ""testPolicy"",
    ""rules"": []
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithWildcard);

        // Assert
        var imports = document.Namespace!.Statements.OfType<Import>().ToList();
        Assert.Single(imports);
        Assert.Equal("all.attributes", imports[0].NamespacePath);
        Assert.True(imports[0].Wildcard);
    }

    [Fact]
    public void AlfaWithImports_ConvertsToJson_PreservesImports()
    {
        // Arrange
        string alfaWithImports = @"
namespace test {
    import common.attributes
    import security.roles

    policy testPolicy {
        rule rule1 {
            permit
        }
    }
}";

        // Act
        string json = PolicyJsonConverter.JsonFromAlfa(alfaWithImports);
        using var doc = JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("imports", out JsonElement importsElement));
        Assert.Equal(2, importsElement.GetArrayLength());

        JsonElement import1 = importsElement[0];
        Assert.Equal("common.attributes", import1.GetProperty("namespacePath").GetString());

        JsonElement import2 = importsElement[1];
        Assert.Equal("security.roles", import2.GetProperty("namespacePath").GetString());
    }

    [Fact]
    public void JsonToAlfa_WithImports_GeneratesCorrectAlfa()
    {
        // Arrange
        string jsonWithImports = @"{
  ""namespace"": ""test"",
  ""imports"": [
    {
      ""namespacePath"": ""common.types""
    }
  ],
  ""policy"": {
    ""id"": ""testPolicy"",
    ""rules"": [
      {
        ""effect"": ""permit""
      }
    ]
  }
}";

        // Act
        string alfa = PolicyJsonConverter.AlfaFromJson(jsonWithImports);

        // Assert
        Assert.Contains("namespace test", alfa);
        Assert.Contains("import common.types", alfa);
        Assert.Contains("policy testPolicy", alfa);
    }

    [Fact]
    public void JsonWithoutImports_WorksCorrectly()
    {
        // Arrange
        string jsonWithoutImports = @"{
  ""namespace"": ""test"",
  ""policy"": {
    ""id"": ""testPolicy"",
    ""rules"": [
      {
        ""effect"": ""permit""
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(jsonWithoutImports);
        string convertedJson = PolicyJsonConverter.ToJson(document);

        // Assert
        var imports = document.Namespace!.Statements.OfType<Import>().ToList();
        Assert.Empty(imports);

        // Verify imports property is not in JSON when there are none
        using var doc = JsonDocument.Parse(convertedJson);
        Assert.False(doc.RootElement.TryGetProperty("imports", out _));
    }

    [Fact]
    public void ComplexJsonWithImportsAndPolicySet_RoundTrip_Success()
    {
        // Arrange
        string complexJson = @"{
  ""namespace"": ""enterprise.security"",
  ""imports"": [
    {
      ""namespacePath"": ""enterprise.common""
    },
    {
      ""namespacePath"": ""enterprise.roles"",
      ""wildcard"": true
    }
  ],
  ""policySet"": {
    ""id"": ""mainPolicySet"",
    ""apply"": ""deny-overrides"",
    ""elements"": [
      {
        ""kind"": ""policy"",
        ""policy"": {
          ""id"": ""inlinePolicy"",
          ""rules"": [
            {
              ""effect"": ""permit""
            }
          ]
        }
      }
    ]
  }
}";

        // Act
        PolicyDocument document = PolicyJsonConverter.FromJson(complexJson);
        string convertedJson = PolicyJsonConverter.ToJson(document);
        PolicyDocument roundTripDocument = PolicyJsonConverter.FromJson(convertedJson);

        // Assert - Original
        var originalImports = document.Namespace!.Statements.OfType<Import>().ToList();
        Assert.Equal(2, originalImports.Count);

        // Assert - Round trip
        var roundTripImports = roundTripDocument.Namespace!.Statements.OfType<Import>().ToList();
        Assert.Equal(2, roundTripImports.Count);
        Assert.Equal("enterprise.common", roundTripImports[0].NamespacePath);
        Assert.False(roundTripImports[0].Wildcard);
        Assert.Equal("enterprise.roles", roundTripImports[1].NamespacePath);
        Assert.True(roundTripImports[1].Wildcard);
    }
}
