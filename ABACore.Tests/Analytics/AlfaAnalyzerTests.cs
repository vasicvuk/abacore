using ABACore.Analytics;
using Xunit;

namespace ABACore.Tests.Analytics;

/// <summary>
/// Tests for the ALFA analyzer functionality.
/// </summary>
public class AlfaAnalyzerTests
{
    private readonly AlfaAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_NullOrEmptyText_ReturnsError()
    {
        // Act
        AnalysisResult result = _analyzer.Analyze("");

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, result.Diagnostics[0].Severity);
        Assert.Equal("ALFA001", result.Diagnostics[0].Code);
    }

    [Fact]
    public void Analyze_InvalidSyntax_ReturnsErrorWithPosition()
    {
        // Arrange
        string invalidAlfa = @"
namespace test {
    policy testPolicy {
        rule badRule {
            invalid_keyword
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(invalidAlfa);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        Diagnostic error = result.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(error.Range);
        Assert.True(error.Range.Start.Line > 0);
    }

    [Fact]
    public void Analyze_ValidPolicy_ExtractsAttributes()
    {
        // Arrange
        string validAlfa = @"
namespace test {
    policy testPolicy {
        rule rule1 {
            permit
            target clause subject.role == ""admin"" and resource.type == ""document""
            condition subject.clearance > 3
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(validAlfa);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.HasErrors);
        Assert.Equal(3, result.AttributeReferences.Count);

        // Check subject.role
        AttributeReference roleAttr = result.AttributeReferences.First(a => a.Name == "role");
        Assert.Equal("subject", roleAttr.Namespace);
        Assert.False(roleAttr.MustBePresent);

        // Check resource.type
        AttributeReference typeAttr = result.AttributeReferences.First(a => a.Name == "type");
        Assert.Equal("resource", typeAttr.Namespace);

        // Check subject.clearance
        AttributeReference clearanceAttr = result.AttributeReferences.First(a => a.Name == "clearance");
        Assert.Equal("subject", clearanceAttr.Namespace);

        // Get unique attribute names
        var uniqueNames = result.GetUniqueAttributeNames().ToList();
        Assert.Contains("subject.role", uniqueNames);
        Assert.Contains("resource.type", uniqueNames);
        Assert.Contains("subject.clearance", uniqueNames);
    }

    [Fact]
    public void Analyze_PolicyWithObligations_ExtractsObligations()
    {
        // Arrange
        string alfaWithObligations = @"
namespace test {
    policy testPolicy {
        rule rule1 {
            permit
            on permit {
                obligation logAccess {
                    message = ""Access granted""
                    level = ""INFO""
                }
                obligation notifyAdmin {
                    userId = subject.id
                }
            }
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithObligations);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ObligationReferences.Count);

        ObligationReference logAccess = result.ObligationReferences.First(o => o.Id == "logAccess");
        Assert.Equal("logAccess", logAccess.Id);
        Assert.Equal(2, logAccess.AttributeNames.Count);
        Assert.Contains("message", logAccess.AttributeNames);
        Assert.Contains("level", logAccess.AttributeNames);

        ObligationReference notifyAdmin = result.ObligationReferences.First(o => o.Id == "notifyAdmin");
        Assert.Equal("notifyAdmin", notifyAdmin.Id);
        Assert.Single(notifyAdmin.AttributeNames);
        Assert.Contains("userId", notifyAdmin.AttributeNames);

        // Check unique obligation IDs
        var uniqueObligations = result.GetUniqueObligationIds().ToList();
        Assert.Equal(2, uniqueObligations.Count);
        Assert.Contains("logAccess", uniqueObligations);
        Assert.Contains("notifyAdmin", uniqueObligations);

        // Check that subject.id attribute was also extracted
        Assert.Contains(result.AttributeReferences, a => a.Namespace == "subject" && a.Name == "id");
    }

    [Fact]
    public void Analyze_PolicyWithAdvice_ExtractsAdvice()
    {
        // Arrange
        string alfaWithAdvice = @"
namespace test {
    policy testPolicy {
        rule rule1 {
            deny
            on deny {
                advice logDenial {
                    reason = ""Insufficient permissions""
                    timestamp = ""2024-01-01""
                }
            }
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithAdvice);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.AdviceReferences);

        AdviceReference logDenial = result.AdviceReferences[0];
        Assert.Equal("logDenial", logDenial.Id);
        Assert.Equal(2, logDenial.AttributeNames.Count);
        Assert.Contains("reason", logDenial.AttributeNames);
        Assert.Contains("timestamp", logDenial.AttributeNames);

        // Check unique advice IDs
        var uniqueAdvice = result.GetUniqueAdviceIds().ToList();
        Assert.Single(uniqueAdvice);
        Assert.Contains("logDenial", uniqueAdvice);
    }

    [Fact]
    public void Analyze_ComplexPolicySet_ExtractsAllReferences()
    {
        // Arrange
        string complexAlfa = @"
namespace enterprise.security {
    policyset mainPolicySet {
        apply denyOverrides
        target clause resource.environment == ""production""

        policy adminPolicy {
            rule adminRule {
                permit
                target clause subject.role == ""admin""
                on permit {
                    obligation auditLog {
                        action = resource.action
                    }
                }
            }
        }

        policy userPolicy {
            rule userRule {
                permit
                target clause subject.role == ""user"" and resource.sensitivity < 5
                condition subject.authenticated == true
                on deny {
                    advice notifyUser {
                        message = ""Access denied""
                    }
                }
            }
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(complexAlfa);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.HasErrors);

        // Check attributes
        Assert.True(result.AttributeReferences.Count >= 6);
        var uniqueAttrs = result.GetUniqueAttributeNames().ToList();
        Assert.Contains("resource.environment", uniqueAttrs);
        Assert.Contains("subject.role", uniqueAttrs);
        Assert.Contains("resource.action", uniqueAttrs);
        Assert.Contains("resource.sensitivity", uniqueAttrs);
        Assert.Contains("subject.authenticated", uniqueAttrs);

        // Check namespaces
        var uniqueNamespaces = result.GetUniqueAttributeNamespaces().ToList();
        Assert.Contains("subject", uniqueNamespaces);
        Assert.Contains("resource", uniqueNamespaces);

        // Check obligations
        Assert.Single(result.ObligationReferences);
        Assert.Equal("auditLog", result.ObligationReferences[0].Id);

        // Check advice
        Assert.Single(result.AdviceReferences);
        Assert.Equal("notifyUser", result.AdviceReferences[0].Id);
    }

    [Fact]
    public void Analyze_PolicyWithMustBePresentAttribute_CapturesFlag()
    {
        // Arrange
        string alfaWithMustBePresent = @"
namespace test {
    policy testPolicy {
        rule rule1 {
            permit
            target clause subject.email[mustbepresent] == ""admin@example.com""
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithMustBePresent);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.AttributeReferences);

        AttributeReference emailAttr = result.AttributeReferences[0];
        Assert.Equal("subject", emailAttr.Namespace);
        Assert.Equal("email", emailAttr.Name);
        Assert.True(emailAttr.MustBePresent);
    }

    [Fact]
    public void Analyze_PolicyWithFunctionCalls_ExtractsAttributesFromParameters()
    {
        // Arrange
        string alfaWithFunctions = @"
namespace test {
    policy testPolicy {
        rule rule1 {
            permit
            condition stringEqual(subject.department, resource.owner)
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithFunctions);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AttributeReferences.Count);

        Assert.Contains(result.AttributeReferences, a => a.Namespace == "subject" && a.Name == "department");
        Assert.Contains(result.AttributeReferences, a => a.Namespace == "resource" && a.Name == "owner");
    }

    [Fact]
    public void Analyze_PolicyWithNoAttributes_ReturnsWarning()
    {
        // Arrange
        string alfaNoAttributes = @"
namespace test {
    policy emptyPolicy {
        rule rule1 {
            permit
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaNoAttributes);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Code == "ALFA102");
        Assert.Empty(result.AttributeReferences);
    }

    [Fact]
    public void Analyze_NestedPolicySet_ExtractsFromAllLevels()
    {
        // Arrange
        string nestedAlfa = @"
namespace test {
    policyset outerSet {
        apply permitOverrides

        policyset innerSet {
            apply denyOverrides
            target clause environment.location == ""US""

            policy innerPolicy {
                rule innerRule {
                    permit
                    condition subject.age >= 18
                }
            }
        }

        policy outerPolicy {
            rule outerRule {
                deny
                target clause resource.classification == ""secret""
            }
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(nestedAlfa);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.AttributeReferences.Count);

        var uniqueAttrs = result.GetUniqueAttributeNames().ToList();
        Assert.Contains("environment.location", uniqueAttrs);
        Assert.Contains("subject.age", uniqueAttrs);
        Assert.Contains("resource.classification", uniqueAttrs);
    }

    [Fact]
    public void Analyze_PolicyWithBinaryExpressions_ExtractsAllAttributes()
    {
        // Arrange
        string alfaWithBinary = @"
namespace test {
    policy mathPolicy {
        rule mathRule {
            permit
            condition subject.score + resource.bonus > 100
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithBinary);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AttributeReferences.Count);
        Assert.Contains(result.AttributeReferences, a => a.FullName == "subject.score");
        Assert.Contains(result.AttributeReferences, a => a.FullName == "resource.bonus");
    }

    [Fact]
    public void Analyze_PolicyWithLogicalOperators_ExtractsAllAttributes()
    {
        // Arrange
        string alfaWithLogical = @"
namespace test {
    policy logicPolicy {
        rule logicRule {
            permit
            target clause (subject.role == ""admin"" or subject.role == ""manager"") and resource.status == ""active""
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(alfaWithLogical);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.AttributeReferences.Count >= 2);

        // subject.role should appear at least twice
        var roleRefs = result.AttributeReferences.Where(a => a.FullName == "subject.role").ToList();
        Assert.True(roleRefs.Count >= 2);

        // resource.status should appear once
        Assert.Contains(result.AttributeReferences, a => a.FullName == "resource.status");
    }

    [Fact]
    public void Analyze_MultiplePolicies_AggregatesAllReferences()
    {
        // Arrange
        string multiPolicyAlfa = @"
namespace test {
    policy policy1 {
        rule rule1 {
            permit
            target clause subject.role == ""user""
            on permit {
                obligation log1
            }
        }
    }
}";

        string multiPolicyAlfa2 = @"
namespace test {
    policy policy2 {
        rule rule2 {
            deny
            target clause resource.type == ""sensitive""
            on deny {
                advice warn1
            }
        }
    }
}";

        // Act
        AnalysisResult result1 = _analyzer.Analyze(multiPolicyAlfa);
        AnalysisResult result2 = _analyzer.Analyze(multiPolicyAlfa2);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);

        // Verify they found different attributes
        Assert.Contains(result1.AttributeReferences, a => a.FullName == "subject.role");
        Assert.Contains(result2.AttributeReferences, a => a.FullName == "resource.type");

        // Verify obligations and advice
        Assert.Single(result1.ObligationReferences);
        Assert.Equal("log1", result1.ObligationReferences[0].Id);

        Assert.Single(result2.AdviceReferences);
        Assert.Equal("warn1", result2.AdviceReferences[0].Id);
    }

    [Fact]
    public void Analyze_SyntaxErrorAtSpecificLocation_ReportsCorrectPosition()
    {
        // Arrange
        string alfaWithError = @"
namespace test {
    policy testPolicy {
        rule testRule {
            permit
            target clause subject.role = ""admin""
        }
    }
}";

        // Act (should fail because = should be ==)
        AnalysisResult result = _analyzer.Analyze(alfaWithError);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasErrors);

        Diagnostic error = result.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(error.Range);
        Assert.Equal(6, error.Range.Start.Line);
    }

    [Fact]
    public void DiagnosticToString_FormatsCorrectly()
    {
        // Arrange
        Diagnostic diagnostic = new()
        {
            Severity = DiagnosticSeverity.Error,
            Message = "Test error",
            Code = "TEST001",
            Range = new TextRange
            {
                Start = new TextPosition { Line = 5, Column = 10 },
                End = new TextPosition { Line = 5, Column = 20 }
            }
        };

        // Act
        string formatted = diagnostic.ToString();

        // Assert
        Assert.Contains("Error", formatted);
        Assert.Contains("TEST001", formatted);
        Assert.Contains("Test error", formatted);
        Assert.Contains("Line 5", formatted);
    }

    [Fact]
    public void AttributeReference_FullName_CombinesNamespaceAndName()
    {
        // Arrange
        AttributeReference attr = new()
        {
            Namespace = "subject",
            Name = "role"
        };

        // Act & Assert
        Assert.Equal("subject.role", attr.FullName);
        Assert.Equal("subject.role", attr.ToString());
    }

    [Fact]
    public void AttributeReference_WithMustBePresent_IncludesQuestionMark()
    {
        // Arrange
        AttributeReference attr = new()
        {
            Namespace = "subject",
            Name = "email",
            MustBePresent = true
        };

        // Act & Assert
        Assert.Equal("subject.email?", attr.ToString());
    }

    [Fact]
    public void AnalysisResult_GetUniqueMethods_ReturnDistinctValues()
    {
        // Arrange
        AnalysisResult result = new()
        {
            AttributeReferences =
            [
                new AttributeReference { Namespace = "subject", Name = "role" },
                new AttributeReference { Namespace = "subject", Name = "role" },
                new AttributeReference { Namespace = "resource", Name = "type" }
            ],
            ObligationReferences =
            [
                new ObligationReference { Id = "log" },
                new ObligationReference { Id = "log" },
                new ObligationReference { Id = "notify" }
            ],
            AdviceReferences =
            [
                new AdviceReference { Id = "warn" },
                new AdviceReference { Id = "warn" }
            ]
        };

        // Act
        var uniqueAttrs = result.GetUniqueAttributeNames().ToList();
        var uniqueNamespaces = result.GetUniqueAttributeNamespaces().ToList();
        var uniqueObligations = result.GetUniqueObligationIds().ToList();
        var uniqueAdvice = result.GetUniqueAdviceIds().ToList();

        // Assert
        Assert.Equal(2, uniqueAttrs.Count);
        Assert.Contains("subject.role", uniqueAttrs);
        Assert.Contains("resource.type", uniqueAttrs);

        Assert.Equal(2, uniqueNamespaces.Count);
        Assert.Contains("subject", uniqueNamespaces);
        Assert.Contains("resource", uniqueNamespaces);

        Assert.Equal(2, uniqueObligations.Count);
        Assert.Contains("log", uniqueObligations);
        Assert.Contains("notify", uniqueObligations);

        Assert.Single(uniqueAdvice);
        Assert.Contains("warn", uniqueAdvice);
    }
}
