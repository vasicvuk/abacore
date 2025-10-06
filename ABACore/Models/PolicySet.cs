namespace ABACore.Models;

/// <summary>
/// Represents a policy set.
/// </summary>
public sealed class PolicySet : PolicySetElement
{
    public string? Id { get; init; }
    public Target? Target { get; init; }
    public required List<PolicySetElement> Elements { get; init; }
    public CombiningAlgorithm? Combinator { get; init; }
    public List<Obligation>? OnPermit { get; init; }
    public List<Advice>? OnDeny { get; init; }
}
