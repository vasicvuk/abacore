namespace ABACore.Analytics;

/// <summary>
/// Represents a reference to an advice in ALFA code.
/// </summary>
public sealed class AdviceReference
{
    /// <summary>
    /// ID of the advice.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Location where the advice is referenced.
    /// </summary>
    public TextRange? Range { get; init; }

    /// <summary>
    /// Names of attributes passed to the advice.
    /// </summary>
    public List<string> AttributeNames { get; init; } = [];

    public override string ToString() => Id;
}
