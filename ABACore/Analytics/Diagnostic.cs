namespace ABACore.Analytics;

/// <summary>
/// Represents a diagnostic message (error, warning, or info) for ALFA code.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>
    /// Severity level of the diagnostic.
    /// </summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Range of text that the diagnostic applies to.
    /// </summary>
    public TextRange? Range { get; init; }

    /// <summary>
    /// Diagnostic code for categorization.
    /// </summary>
    public string? Code { get; init; }

    public override string ToString()
    {
        string location = Range != null ? $"[{Range}] " : "";
        string code = !string.IsNullOrEmpty(Code) ? $"{Code}: " : "";
        return $"{Severity}: {location}{code}{Message}";
    }
}
