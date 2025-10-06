namespace ABACore.Analytics;

/// <summary>
/// Contains the complete analysis result for ALFA code.
/// </summary>
public sealed record AnalysisResult
{
    /// <summary>
    /// List of diagnostics (errors, warnings, info messages).
    /// </summary>
    public List<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// List of attribute references found in the code.
    /// </summary>
    public List<AttributeReference> AttributeReferences { get; init; } = [];

    /// <summary>
    /// List of obligation references found in the code.
    /// </summary>
    public List<ObligationReference> ObligationReferences { get; init; } = [];

    /// <summary>
    /// List of advice references found in the code.
    /// </summary>
    public List<AdviceReference> AdviceReferences { get; init; } = [];

    /// <summary>
    /// Whether the analysis succeeded (code is parseable).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Returns true if there are any errors.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Returns true if there are any warnings.
    /// </summary>
    public bool HasWarnings => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    /// Returns all unique attribute namespaces referenced.
    /// </summary>
    public IEnumerable<string> GetUniqueAttributeNamespaces() =>
        AttributeReferences.Select(a => a.Namespace).Distinct().OrderBy(n => n);

    /// <summary>
    /// Returns all unique attribute names referenced.
    /// </summary>
    public IEnumerable<string> GetUniqueAttributeNames() =>
        AttributeReferences.Select(a => a.FullName).Distinct().OrderBy(n => n);

    /// <summary>
    /// Returns all unique obligation IDs referenced.
    /// </summary>
    public IEnumerable<string> GetUniqueObligationIds() =>
        ObligationReferences.Select(o => o.Id).Distinct().OrderBy(i => i);

    /// <summary>
    /// Returns all unique advice IDs referenced.
    /// </summary>
    public IEnumerable<string> GetUniqueAdviceIds() =>
        AdviceReferences.Select(a => a.Id).Distinct().OrderBy(i => i);
}
