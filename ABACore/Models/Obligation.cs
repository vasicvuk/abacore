namespace ABACore.Models;

/// <summary>
/// Represents an obligation.
/// </summary>
public sealed class Obligation : AlfaNode
{
    public required string Id { get; init; }
    public Dictionary<string, Expression>? Attributes { get; init; }
}

/// <summary>
/// Represents advice.
/// </summary>
public sealed class Advice : AlfaNode
{
    public required string Id { get; init; }
    public Dictionary<string, Expression>? Attributes { get; init; }
}
