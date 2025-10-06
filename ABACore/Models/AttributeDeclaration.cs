namespace ABACore.Models;

/// <summary>
/// Represents an attribute declaration.
/// </summary>
public sealed class AttributeDeclaration : Statement
{
    public required string Name { get; init; }
    public string? Category { get; init; }
    public string? Id { get; init; }
    public AttributeType? Type { get; init; }
}
