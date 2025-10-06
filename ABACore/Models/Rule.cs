namespace ABACore.Models;

/// <summary>
/// Represents a rule.
/// </summary>
public sealed class Rule : AlfaNode
{
    public string? Id { get; init; }
    public required Effect Effect { get; init; }
    public Target? Target { get; init; }
    public Condition? Condition { get; init; }
    public List<Obligation>? OnPermit { get; init; }
    public List<Advice>? OnDeny { get; init; }
}

/// <summary>
/// Represents a target.
/// </summary>
public sealed class Target : AlfaNode
{
    public required List<Clause> Clauses { get; init; }
}

/// <summary>
/// Represents a clause in a target.
/// </summary>
public sealed class Clause : AlfaNode
{
    public required BooleanExpression Expression { get; init; }
}

/// <summary>
/// Represents a condition.
/// </summary>
public sealed class Condition : AlfaNode
{
    public required BooleanExpression Expression { get; init; }
}
