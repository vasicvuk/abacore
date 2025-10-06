namespace ABACore.Analytics;

/// <summary>
/// Severity levels for ALFA diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message that doesn't prevent compilation.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message that indicates invalid ALFA code.
    /// </summary>
    Error
}
