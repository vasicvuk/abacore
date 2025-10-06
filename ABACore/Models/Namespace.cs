namespace ABACore.Models;

/// <summary>
/// Represents a namespace declaration.
/// </summary>
public sealed class Namespace : AlfaNode
{
    public required string Name { get; init; }
    public required List<Statement> Statements { get; init; }
}
