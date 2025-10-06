# ALFA Analytics

The ALFA Analytics API provides IDE-level analysis capabilities for ALFA policy code, including:

- **Error Detection**: Syntax errors with precise line and column positions
- **Warnings**: Semantic warnings about potential issues
- **Attribute Extraction**: List all attributes referenced in policies
- **Obligation Tracking**: Extract all obligations with their attributes
- **Advice Tracking**: Extract all advice with their attributes

## Quick Start

```csharp
using ABACore.Analytics;

// Create an analyzer
AlfaAnalyzer analyzer = new();

// Analyze ALFA code
string alfaCode = @"
namespace test {
    policy myPolicy {
        rule myRule {
            permit
            target clause subject.role == ""admin""
        }
    }
}";

AnalysisResult result = analyzer.Analyze(alfaCode);

// Check for errors
if (result.HasErrors)
{
    foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.WriteLine($"Error at line {diagnostic.Range?.Start.Line}: {diagnostic.Message}");
    }
}

// Get unique attributes
foreach (string attr in result.GetUniqueAttributeNames())
{
    Console.WriteLine($"Attribute: {attr}");
}
```

## API Reference

### AlfaAnalyzer

Main class for analyzing ALFA code.

**Methods:**
- `AnalysisResult Analyze(string alfaText)` - Analyzes ALFA code and returns comprehensive results

### AnalysisResult

Contains complete analysis results.

**Properties:**
- `List<Diagnostic> Diagnostics` - All errors, warnings, and info messages
- `List<AttributeReference> AttributeReferences` - All attribute references found
- `List<ObligationReference> ObligationReferences` - All obligations used
- `List<AdviceReference> AdviceReferences` - All advice used
- `bool Success` - Whether the code parsed successfully
- `bool HasErrors` - Whether there are any errors
- `bool HasWarnings` - Whether there are any warnings

**Methods:**
- `IEnumerable<string> GetUniqueAttributeNamespaces()` - Get distinct attribute namespaces
- `IEnumerable<string> GetUniqueAttributeNames()` - Get distinct attribute full names
- `IEnumerable<string> GetUniqueObligationIds()` - Get distinct obligation IDs
- `IEnumerable<string> GetUniqueAdviceIds()` - Get distinct advice IDs

### Diagnostic

Represents an error, warning, or informational message.

**Properties:**
- `DiagnosticSeverity Severity` - Error, Warning, or Info
- `string Message` - Diagnostic message
- `TextRange? Range` - Location in source code (line/column)
- `string? Code` - Diagnostic code (e.g., "ALFA001")

### AttributeReference

Represents a reference to an attribute in ALFA code.

**Properties:**
- `string Namespace` - Attribute namespace (e.g., "subject")
- `string Name` - Attribute name (e.g., "role")
- `bool MustBePresent` - Whether the attribute must be present
- `TextRange? Range` - Location where referenced
- `string FullName` - Full qualified name (e.g., "subject.role")

### ObligationReference

Represents a reference to an obligation.

**Properties:**
- `string Id` - Obligation ID
- `TextRange? Range` - Location where referenced
- `List<string> AttributeNames` - Attribute names passed to the obligation

### AdviceReference

Represents a reference to advice.

**Properties:**
- `string Id` - Advice ID
- `TextRange? Range` - Location where referenced
- `List<string> AttributeNames` - Attribute names passed to the advice

## Use Cases

### IDE Integration

Use the analyzer to provide real-time feedback as users write ALFA code:

```csharp
// On text change
void OnTextChanged(string alfaCode)
{
    AnalysisResult result = analyzer.Analyze(alfaCode);

    // Show errors in IDE
    foreach (Diagnostic diagnostic in result.Diagnostics)
    {
        if (diagnostic.Range != null)
        {
            ShowSquiggle(
                diagnostic.Range.Start.Line,
                diagnostic.Range.Start.Column,
                diagnostic.Range.End.Column,
                diagnostic.Severity,
                diagnostic.Message
            );
        }
    }
}
```

### Autocomplete Support

Extract available attributes for autocomplete:

```csharp
AnalysisResult result = analyzer.Analyze(currentDocument);

// Get all attribute namespaces for "namespace." autocomplete
var namespaces = result.GetUniqueAttributeNamespaces();

// Get all full attribute names for suggestions
var attributes = result.GetUniqueAttributeNames();
```

### Policy Validation

Validate policies before deployment:

```csharp
AnalysisResult result = analyzer.Analyze(policyCode);

if (result.HasErrors)
{
    throw new ValidationException($"Policy has {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} errors");
}

if (result.AttributeReferences.Count == 0)
{
    Console.WriteLine("Warning: Policy doesn't reference any attributes");
}
```

### Metadata Extraction

Extract metadata for documentation or testing:

```csharp
AnalysisResult result = analyzer.Analyze(policyCode);

Console.WriteLine("This policy uses the following attributes:");
foreach (string attr in result.GetUniqueAttributeNames())
{
    Console.WriteLine($"  - {attr}");
}

Console.WriteLine("\nObligations that may be triggered:");
foreach (string obligation in result.GetUniqueObligationIds())
{
    Console.WriteLine($"  - {obligation}");
}
```

## Diagnostic Codes

- `ALFA001` - Empty or null ALFA text
- `ALFA002` - Parse exception
- `ALFA003` - Syntax error from parser
- `ALFA101` - Info: Attribute referenced many times
- `ALFA102` - Warning: No attributes referenced
- `ALFA999` - Unexpected error

## Examples

See `ABACore.Samples/AlfaAnalyticsSample.cs` for complete working examples.
