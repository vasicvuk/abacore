namespace ABACore.Models;

/// <summary>
/// Represents a policy set statement within a namespace.
/// </summary>
public sealed class PolicySetStatement : Statement
{
    public required PolicySet PolicySet { get; init; }
}
