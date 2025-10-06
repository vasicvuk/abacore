namespace ABACore.Models;

/// <summary>
/// Represents a policy statement within a namespace.
/// </summary>
public sealed class PolicyStatement : Statement
{
    public required Policy Policy { get; init; }
}
