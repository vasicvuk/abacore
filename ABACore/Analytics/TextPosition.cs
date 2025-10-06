namespace ABACore.Analytics;

/// <summary>
/// Represents a position in a text document (line and column).
/// </summary>
public sealed class TextPosition
{
    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    public required int Column { get; init; }

    public override string ToString() => $"Line {Line}, Column {Column}";
}
