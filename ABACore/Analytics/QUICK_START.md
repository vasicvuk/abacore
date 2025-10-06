# ALFA Analytics - Quick Start Guide

## Installation

The ALFA Analytics API is part of the `ABACore` namespace.

```csharp
using ABACore.Analytics;
```

## 5-Minute Quick Start

### 1. Basic Analysis

```csharp
// Create analyzer (reusable)
AlfaAnalyzer analyzer = new();

// Analyze ALFA code
string alfaCode = @"
namespace example {
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
    foreach (var error in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.WriteLine($"Error: {error.Message}");
        if (error.Range != null)
        {
            Console.WriteLine($"  at Line {error.Range.Start.Line}, Column {error.Range.Start.Column}");
        }
    }
}
```

### 2. Extract Attributes

```csharp
// Get all unique attribute names
foreach (string attr in result.GetUniqueAttributeNames())
{
    Console.WriteLine($"Attribute: {attr}");
}

// Get attributes by namespace
var subjectAttrs = result.AttributeReferences
    .Where(a => a.Namespace == "subject")
    .Select(a => a.Name)
    .Distinct();
```

### 3. Track Obligations and Advice

```csharp
// Get all obligations
foreach (string obligation in result.GetUniqueObligationIds())
{
    Console.WriteLine($"Obligation: {obligation}");
}

// Get obligation details
foreach (var obligation in result.ObligationReferences)
{
    Console.WriteLine($"{obligation.Id}: {string.Join(", ", obligation.AttributeNames)}");
}

// Get all advice
foreach (string advice in result.GetUniqueAdviceIds())
{
    Console.WriteLine($"Advice: {advice}");
}
```

## Common Patterns

### Pattern 1: IDE Error Display

```csharp
void OnCodeChange(string code)
{
    AnalysisResult result = analyzer.Analyze(code);

    ClearAllDiagnostics();

    foreach (var diagnostic in result.Diagnostics)
    {
        if (diagnostic.Range != null)
        {
            AddDiagnosticMarker(
                line: diagnostic.Range.Start.Line,
                column: diagnostic.Range.Start.Column,
                endColumn: diagnostic.Range.End.Column,
                severity: diagnostic.Severity,
                message: diagnostic.Message
            );
        }
    }
}
```

### Pattern 2: Autocomplete Suggestions

```csharp
List<string> GetAutocompleteSuggestions(string code, string prefix)
{
    AnalysisResult result = analyzer.Analyze(code);

    if (prefix.Contains("."))
    {
        // Suggest attributes for namespace
        string ns = prefix.Split('.')[0];
        return result.AttributeReferences
            .Where(a => a.Namespace == ns)
            .Select(a => $"{ns}.{a.Name}")
            .Distinct()
            .ToList();
    }
    else
    {
        // Suggest namespaces
        return result.GetUniqueAttributeNamespaces().ToList();
    }
}
```

### Pattern 3: Pre-Deployment Validation

```csharp
void ValidatePolicy(string code)
{
    AnalysisResult result = analyzer.Analyze(code);

    if (!result.Success)
    {
        throw new Exception("Policy has syntax errors");
    }

    if (result.HasErrors)
    {
        var errors = string.Join("\n", result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Message));
        throw new Exception($"Policy validation failed:\n{errors}");
    }

    // Check for required elements
    if (!result.GetUniqueObligationIds().Contains("auditLog"))
    {
        throw new Exception("Policy must include 'auditLog' obligation");
    }
}
```

### Pattern 4: Test Data Generation

```csharp
Dictionary<string, List<string>> GenerateTestDataTemplate(string code)
{
    AnalysisResult result = analyzer.Analyze(code);

    return result.AttributeReferences
        .GroupBy(a => a.Namespace)
        .ToDictionary(
            g => g.Key,
            g => g.Select(a => a.Name).Distinct().ToList()
        );
}

// Output:
// {
//   "subject": ["role", "department", "id"],
//   "resource": ["type", "ownerId"],
//   "environment": ["location"]
// }
```

### Pattern 5: Documentation Generation

```csharp
void GenerateDocumentation(string code)
{
    AnalysisResult result = analyzer.Analyze(code);

    Console.WriteLine("## Policy Attributes");
    foreach (var attr in result.GetUniqueAttributeNames())
    {
        var refs = result.AttributeReferences.Where(a => a.FullName == attr).ToList();
        var mustBePresent = refs.Any(a => a.MustBePresent);
        Console.WriteLine($"- `{attr}`{(mustBePresent ? " *(required)*" : "")}");
    }

    Console.WriteLine("\n## Obligations");
    foreach (var obligation in result.ObligationReferences.DistinctBy(o => o.Id))
    {
        Console.WriteLine($"- `{obligation.Id}`");
        if (obligation.AttributeNames.Count > 0)
        {
            Console.WriteLine($"  - Parameters: {string.Join(", ", obligation.AttributeNames)}");
        }
    }

    Console.WriteLine("\n## Advice");
    foreach (var advice in result.AdviceReferences.DistinctBy(a => a.Id))
    {
        Console.WriteLine($"- `{advice.Id}`");
        if (advice.AttributeNames.Count > 0)
        {
            Console.WriteLine($"  - Parameters: {string.Join(", ", advice.AttributeNames)}");
        }
    }
}
```

## Tips & Best Practices

1. **Reuse Analyzer**: Create one `AlfaAnalyzer` instance and reuse it
2. **Check Success First**: Always check `result.Success` before using extracted data
3. **Handle Partial Results**: Even on parse errors, some data may be extracted
4. **Use Unique Methods**: Use `GetUniqueAttributeNames()` for distinct lists
5. **Position Information**: Line numbers are 1-based, columns are 0-based
6. **Filter by Severity**: Use LINQ to filter diagnostics by severity

## Error Handling

```csharp
try
{
    AnalysisResult result = analyzer.Analyze(code);

    if (!result.Success)
    {
        // Handle parse failures
    }

    if (result.HasErrors)
    {
        // Handle semantic errors
    }
}
catch (Exception ex)
{
    // Should rarely happen - analyzer handles most exceptions
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Performance

- **Fast**: Single-pass analysis
- **Memory Efficient**: Only stores references, not full AST
- **Incremental Friendly**: Suitable for real-time IDE analysis
- **Thread Safe**: Each analyzer instance can be used by one thread at a time

## Next Steps

- See `README.md` for complete API reference
- Check `ABACore.Samples/AlfaAnalyticsSample.cs` for working examples
- Review `ABACore.Tests/Analytics/` for comprehensive test examples
