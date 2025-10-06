using System;
using ABACore.Analytics;

namespace ABACore.Samples;

/// <summary>
/// Demonstrates the ALFA Analytics API for IDE-level error detection and metadata extraction.
/// </summary>
public static class AlfaAnalyticsSample
{
    public static void Run()
    {
        Console.WriteLine("=== ALFA Analytics Demo ===\n");

        // Example 1: Analyze valid ALFA code
        Console.WriteLine("Example 1: Analyzing valid ALFA code");
        Console.WriteLine("=====================================");
        AnalyzeValidPolicy();

        Console.WriteLine("\n");

        // Example 2: Analyze ALFA with syntax errors
        Console.WriteLine("Example 2: Analyzing ALFA with syntax errors");
        Console.WriteLine("============================================");
        AnalyzeInvalidPolicy();

        Console.WriteLine("\n");

        // Example 3: Extract attributes, obligations, and advice
        Console.WriteLine("Example 3: Extracting metadata from ALFA");
        Console.WriteLine("========================================");
        ExtractMetadata();
    }

    private static void AnalyzeValidPolicy()
    {
        string alfaCode = @"
namespace healthcare.access {
    policy patientRecordAccess {
        apply denyOverrides

        rule doctorAccess {
            permit
            target clause subject.role == ""doctor"" and resource.type == ""patient-record""
            condition resource.patientId == subject.assignedPatients
            on permit {
                obligation auditAccess {
                    timestamp = ""2024-01-01""
                    userId = subject.id
                    action = ""read""
                }
            }
        }

        rule emergencyAccess {
            permit
            target clause environment.emergency == true
            condition subject.authenticated == true
        }
    }
}";

        AlfaAnalyzer analyzer = new();
        AnalysisResult result = analyzer.Analyze(alfaCode);

        Console.WriteLine($"Analysis Success: {result.Success}");
        Console.WriteLine($"Errors: {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)}");
        Console.WriteLine($"Warnings: {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning)}");

        Console.WriteLine("\nAttributes referenced:");
        foreach (string attrName in result.GetUniqueAttributeNames())
        {
            Console.WriteLine($"  - {attrName}");
        }

        Console.WriteLine("\nObligations used:");
        foreach (string obligationId in result.GetUniqueObligationIds())
        {
            Console.WriteLine($"  - {obligationId}");
        }
    }

    private static void AnalyzeInvalidPolicy()
    {
        string invalidAlfa = @"
namespace test {
    policy testPolicy {
        rule badRule {
            permit
            target clause subject.role = ""admin""
        }
    }
}";

        AlfaAnalyzer analyzer = new();
        AnalysisResult result = analyzer.Analyze(invalidAlfa);

        Console.WriteLine($"Analysis Success: {result.Success}");
        Console.WriteLine($"\nDiagnostics:");

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            Console.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Message}");
            if (diagnostic.Range != null)
            {
                Console.WriteLine($"    Location: Line {diagnostic.Range.Start.Line}, Column {diagnostic.Range.Start.Column}");
            }
        }
    }

    private static void ExtractMetadata()
    {
        string alfaWithMetadata = @"
namespace enterprise.security {
    policyset mainPolicySet {
        apply permitOverrides
        target clause resource.environment == ""production""

        policy adminPolicy {
            rule adminRule {
                permit
                target clause subject.role == ""admin"" and subject.department == ""IT""
                condition subject.clearanceLevel >= 5
                on permit {
                    obligation logAdminAccess {
                        timestamp = ""2024-01-01""
                        adminId = subject.id
                        resourceId = resource.id
                    }
                    obligation notifySecurityTeam
                }
            }
        }

        policy userPolicy {
            rule userDenyRule {
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

        AlfaAnalyzer analyzer = new();
        AnalysisResult result = analyzer.Analyze(alfaWithMetadata);

        Console.WriteLine("Metadata Extraction Results:");
        Console.WriteLine("============================\n");

        // Attributes
        Console.WriteLine("Unique Attribute Namespaces:");
        foreach (string ns in result.GetUniqueAttributeNamespaces())
        {
            Console.WriteLine($"  - {ns}");
        }

        Console.WriteLine("\nAll Unique Attributes:");
        foreach (string attr in result.GetUniqueAttributeNames())
        {
            Console.WriteLine($"  - {attr}");
        }

        // Obligations
        Console.WriteLine("\nObligations:");
        foreach (ObligationReference obligation in result.ObligationReferences)
        {
            Console.WriteLine($"  - {obligation.Id}");
            if (obligation.AttributeNames.Count > 0)
            {
                Console.WriteLine($"    Attributes: {string.Join(", ", obligation.AttributeNames)}");
            }
        }

        // Advice
        Console.WriteLine("\nAdvice:");
        foreach (AdviceReference advice in result.AdviceReferences)
        {
            Console.WriteLine($"  - {advice.Id}");
            if (advice.AttributeNames.Count > 0)
            {
                Console.WriteLine($"    Attributes: {string.Join(", ", advice.AttributeNames)}");
            }
        }

        // Detailed attribute references
        Console.WriteLine("\nDetailed Attribute References:");
        foreach (AttributeReference attrRef in result.AttributeReferences)
        {
            string mustBePresent = attrRef.MustBePresent ? " (must be present)" : "";
            Console.WriteLine($"  - {attrRef.FullName}{mustBePresent}");
        }
    }
}
