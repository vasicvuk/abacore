namespace ABACore.Models;

/// <summary>
/// Represents a policy.
/// </summary>
public sealed class Policy : PolicySetElement
{
    public string? Id { get; init; }
    public Target? Target { get; init; }
    public required List<Rule> Rules { get; init; }
    public CombiningAlgorithm? Combinator { get; init; }
    public List<Obligation>? OnPermit { get; init; }
    public List<Advice>? OnDeny { get; init; }
}
