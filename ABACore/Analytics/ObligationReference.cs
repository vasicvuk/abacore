namespace ABACore.Analytics;

/// <summary>
/// Represents a reference to an obligation in ALFA code.
/// </summary>
public sealed class ObligationReference
{
    /// <summary>
    /// ID of the obligation.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Location where the obligation is referenced.
    /// </summary>
    public TextRange? Range { get; init; }

    /// <summary>
    /// Names of attributes passed to the obligation.
    /// </summary>
    public List<string> AttributeNames { get; init; } = [];

    public override string ToString() => Id;
}
