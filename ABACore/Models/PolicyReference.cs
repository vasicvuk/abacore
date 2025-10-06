namespace ABACore.Models;

/// <summary>
/// Represents a policy reference in a policy set.
/// </summary>
public sealed class PolicyReference : PolicySetElement
{
    public required string PolicyId { get; init; }
}
