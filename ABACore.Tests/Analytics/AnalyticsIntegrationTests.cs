using ABACore.Analytics;
using Xunit;

namespace ABACore.Tests.Analytics;

/// <summary>
/// Integration tests demonstrating real-world usage of the ALFA Analytics API.
/// </summary>
public class AnalyticsIntegrationTests
{
    private readonly AlfaAnalyzer _analyzer = new();

    [Fact]
    public void CompleteWorkflow_IDEScenario_ParseAnalyzeExtract()
    {
        // Simulate an IDE scenario where a user is writing ALFA code
        // and we provide real-time feedback

        // Arrange: User starts typing a policy
        string incompleteAlfa = @"
namespace healthcare {
    policy recordAccess {
        rule doctorAccess {
            permit
            target clause subject.role == ""doctor""
";

        // Act: Analyze incomplete code
        AnalysisResult result1 = _analyzer.Analyze(incompleteAlfa);

        // Assert: Should fail with syntax error
        Assert.False(result1.Success);
        Assert.True(result1.HasErrors);

        // Now user completes the policy
        string completeAlfa = @"
namespace healthcare {
    policy recordAccess {
        rule doctorAccess {
            permit
            target clause subject.role == ""doctor"" and resource.type == ""record""
            condition subject.department == resource.department
            on permit {
                obligation auditLog {
                    action = ""read""
                    userId = subject.id
                    resourceId = resource.id
                }
            }
        }
    }
}";

        // Act: Re-analyze complete code
        AnalysisResult result2 = _analyzer.Analyze(completeAlfa);

        // Assert: Should succeed
        Assert.True(result2.Success);
        Assert.False(result2.HasErrors);

        // Verify extracted metadata
        Assert.Equal(6, result2.AttributeReferences.Count);
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "subject.role");
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "resource.type");
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "subject.department");
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "resource.department");
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "subject.id");
        Assert.Contains(result2.GetUniqueAttributeNames(), a => a == "resource.id");

        Assert.Single(result2.GetUniqueObligationIds());
        Assert.Equal("auditLog", result2.GetUniqueObligationIds().First());

        // Verify obligation attributes
        ObligationReference obligation = result2.ObligationReferences[0];
        Assert.Equal(3, obligation.AttributeNames.Count);
        Assert.Contains("action", obligation.AttributeNames);
        Assert.Contains("userId", obligation.AttributeNames);
        Assert.Contains("resourceId", obligation.AttributeNames);
    }

    [Fact]
    public void PolicyDocumentation_ExtractAllMetadata()
    {
        // Scenario: Generate documentation from a policy file

        string policy = @"
namespace enterprise.hr {
    policyset employeeDataAccess {
        apply denyOverrides
        target clause resource.category == ""employee-data""

        policy managerAccess {
            rule canReadTeamData {
                permit
                target clause subject.role == ""manager""
                condition subject.teamId == resource.teamId
                on permit {
                    obligation logManagerAccess {
                        managerId = subject.id
                        teamId = resource.teamId
                        timestamp = ""2024-01-01""
                    }
                }
            }

            rule canUpdateTeamData {
                permit
                target clause subject.role == ""manager"" and action.id == ""update""
                condition subject.approvalLevel >= 3
            }
        }

        policy hrAccess {
            rule fullAccess {
                permit
                target clause subject.department == ""HR""
                on permit {
                    obligation auditHRAccess
                    obligation notifyCompliance {
                        hrUserId = subject.id
                    }
                }
            }
        }

        policy denySuspended {
            rule blockSuspended {
                deny
                target clause subject.suspended == true
                on deny {
                    advice notifyUser {
                        message = ""Account suspended""
                    }
                }
            }
        }
    }
}";

        // Act
        AnalysisResult result = _analyzer.Analyze(policy);

        // Assert: Basic success
        Assert.True(result.Success);
        Assert.False(result.HasErrors);

        // Assert: Attribute namespaces
        var namespaces = result.GetUniqueAttributeNamespaces().ToList();
        Assert.Contains("subject", namespaces);
        Assert.Contains("resource", namespaces);
        Assert.Contains("action", namespaces);

        // Assert: All unique attributes
        var attributes = result.GetUniqueAttributeNames().ToList();
        Assert.Contains("resource.category", attributes);
        Assert.Contains("subject.role", attributes);
        Assert.Contains("subject.teamId", attributes);
        Assert.Contains("resource.teamId", attributes);
        Assert.Contains("subject.id", attributes);
        Assert.Contains("action.id", attributes);
        Assert.Contains("subject.approvalLevel", attributes);
        Assert.Contains("subject.department", attributes);
        Assert.Contains("subject.suspended", attributes);

        // Assert: Obligations
        var obligations = result.GetUniqueObligationIds().ToList();
        Assert.Equal(3, obligations.Count);
        Assert.Contains("logManagerAccess", obligations);
        Assert.Contains("auditHRAccess", obligations);
        Assert.Contains("notifyCompliance", obligations);

        // Assert: Advice
        var advice = result.GetUniqueAdviceIds().ToList();
        Assert.Single(advice);
        Assert.Contains("notifyUser", advice);

        // Verify specific obligation details
        ObligationReference logManagerObligation = result.ObligationReferences.First(o => o.Id == "logManagerAccess");
        Assert.Equal(3, logManagerObligation.AttributeNames.Count);
        Assert.Contains("managerId", logManagerObligation.AttributeNames);
        Assert.Contains("teamId", logManagerObligation.AttributeNames);
        Assert.Contains("timestamp", logManagerObligation.AttributeNames);

        // Verify auditHRAccess has no attributes
        ObligationReference auditHRObligation = result.ObligationReferences.First(o => o.Id == "auditHRAccess");
        Assert.Empty(auditHRObligation.AttributeNames);

        // Verify advice details
        AdviceReference notifyAdvice = result.AdviceReferences.First(a => a.Id == "notifyUser");
        Assert.Single(notifyAdvice.AttributeNames);
        Assert.Contains("message", notifyAdvice.AttributeNames);
    }

    [Fact]
    public void ValidationScenario_DetectCommonErrors()
    {
        // Scenario: Validate policies before deployment

        // Test 1: Missing closing brace
        string missingBrace = @"
namespace test {
    policy test {
        rule test {
            permit
        }
";
        AnalysisResult result1 = _analyzer.Analyze(missingBrace);
        Assert.False(result1.Success);
        Assert.True(result1.HasErrors);

        // Test 2: Invalid operator
        string invalidOperator = @"
namespace test {
    policy test {
        rule test {
            permit
            target clause subject.role = ""admin""
        }
    }
}";
        AnalysisResult result2 = _analyzer.Analyze(invalidOperator);
        Assert.False(result2.Success);
        Assert.True(result2.HasErrors);
        Diagnostic error = result2.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(error.Range);
        Assert.True(error.Range.Start.Line > 0);

        // Test 3: Empty policy (warning only)
        string emptyPolicy = @"
namespace test {
    policy empty {
        rule emptyRule {
            permit
        }
    }
}";
        AnalysisResult result3 = _analyzer.Analyze(emptyPolicy);
        Assert.True(result3.Success);
        Assert.False(result3.HasErrors);
        Assert.True(result3.HasWarnings);
        Assert.Contains(result3.Diagnostics, d => d.Code == "ALFA102");
    }

    [Fact]
    public void AutocompleteScenario_ExtractAvailableAttributes()
    {
        // Scenario: Provide autocomplete suggestions based on existing policy

        string existingPolicy = @"
namespace app.security {
    policy accessControl {
        rule rule1 {
            permit
            target clause subject.userId == ""123""
        }
        rule rule2 {
            permit
            target clause subject.email == ""user@example.com""
        }
        rule rule3 {
            permit
            target clause resource.ownerId == subject.userId
        }
        rule rule4 {
            permit
            condition environment.location == ""US""
        }
    }
}";

        AnalysisResult result = _analyzer.Analyze(existingPolicy);

        // Get suggestions for "subject." prefix
        var subjectAttributes = result.AttributeReferences
            .Where(a => a.Namespace == "subject")
            .Select(a => a.Name)
            .Distinct()
            .ToList();

        Assert.Contains("userId", subjectAttributes);
        Assert.Contains("email", subjectAttributes);

        // Get suggestions for "resource." prefix
        var resourceAttributes = result.AttributeReferences
            .Where(a => a.Namespace == "resource")
            .Select(a => a.Name)
            .Distinct()
            .ToList();

        Assert.Contains("ownerId", resourceAttributes);

        // Get suggestions for "environment." prefix
        var environmentAttributes = result.AttributeReferences
            .Where(a => a.Namespace == "environment")
            .Select(a => a.Name)
            .Distinct()
            .ToList();

        Assert.Contains("location", environmentAttributes);

        // Get all namespaces for initial suggestions
        var allNamespaces = result.GetUniqueAttributeNamespaces().ToList();
        Assert.Equal(3, allNamespaces.Count);
        Assert.Contains("subject", allNamespaces);
        Assert.Contains("resource", allNamespaces);
        Assert.Contains("environment", allNamespaces);
    }

    [Fact]
    public void TestingScenario_IdentifyRequiredAttributes()
    {
        // Scenario: Generate test cases based on attributes used

        string policy = @"
namespace security {
    policy authentication {
        rule authenticatedUser {
            permit
            target clause subject.authenticated == true and subject.sessionValid == true
            condition subject.lastLoginTime > ""2024-01-01"":dateTime
        }

        rule mfaRequired {
            deny
            target clause resource.requiresMFA == true and subject.mfaEnabled == false
        }
    }
}";

        AnalysisResult result = _analyzer.Analyze(policy);

        // Extract all required attributes for test generation
        var allAttributes = result.GetUniqueAttributeNames().ToList();

        // Group by namespace for test organization
        var attributesByNamespace = result.AttributeReferences
            .GroupBy(a => a.Namespace)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.Name).Distinct().ToList()
            );

        // Verify we can generate test data structure
        Assert.True(attributesByNamespace.ContainsKey("subject"));
        Assert.True(attributesByNamespace.ContainsKey("resource"));

        // Subject attributes needed for tests
        Assert.Contains("authenticated", attributesByNamespace["subject"]);
        Assert.Contains("sessionValid", attributesByNamespace["subject"]);
        Assert.Contains("lastLoginTime", attributesByNamespace["subject"]);
        Assert.Contains("mfaEnabled", attributesByNamespace["subject"]);

        // Resource attributes needed for tests
        Assert.Contains("requiresMFA", attributesByNamespace["resource"]);

        // Can now generate test cases covering all attribute combinations
        Assert.Equal(5, allAttributes.Count);
    }

    [Fact]
    public void ComplianceScenario_AuditObligationsAndAdvice()
    {
        // Scenario: Audit policy for required compliance obligations

        string policy = @"
namespace finance.transactions {
    policy transactionApproval {
        rule smallTransactions {
            permit
            target clause resource.amount < 10000
            on permit {
                obligation logTransaction {
                    amount = resource.amount
                    userId = subject.id
                }
            }
        }

        rule largeTransactions {
            permit
            target clause resource.amount >= 10000
            condition subject.approvalLevel >= 3
            on permit {
                obligation logTransaction {
                    amount = resource.amount
                    userId = subject.id
                }
                obligation notifyCompliance {
                    transactionId = resource.transactionId
                    amount = resource.amount
                }
                obligation requireSecondaryApproval
            }
        }

        rule suspiciousTransaction {
            deny
            target clause resource.riskScore > 80
            on deny {
                advice alertFraudTeam {
                    riskScore = resource.riskScore
                    transactionId = resource.transactionId
                }
            }
        }
    }
}";

        AnalysisResult result = _analyzer.Analyze(policy);

        // Verify compliance obligations are present
        var obligations = result.GetUniqueObligationIds().ToList();
        Assert.Contains("logTransaction", obligations);
        Assert.Contains("notifyCompliance", obligations);
        Assert.Contains("requireSecondaryApproval", obligations);

        // Verify fraud detection advice
        var advice = result.GetUniqueAdviceIds().ToList();
        Assert.Contains("alertFraudTeam", advice);

        // Count total obligation invocations (logTransaction appears twice)
        Assert.Equal(4, result.ObligationReferences.Count);

        // Verify alertFraudTeam has required attributes
        AdviceReference fraudAlert = result.AdviceReferences.First(a => a.Id == "alertFraudTeam");
        Assert.Contains("riskScore", fraudAlert.AttributeNames);
        Assert.Contains("transactionId", fraudAlert.AttributeNames);
    }
}
