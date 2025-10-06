namespace ABACore.Analytics;

/// <summary>
/// Represents a range of text in a document.
/// </summary>
public sealed class TextRange
{
    /// <summary>
    /// Start position of the range.
    /// </summary>
    public required TextPosition Start { get; init; }

    /// <summary>
    /// End position of the range.
    /// </summary>
    public required TextPosition End { get; init; }

    public override string ToString() => $"{Start} to {End}";
}
