namespace ABACore.Models;

/// <summary>
/// Represents an import statement.
/// </summary>
public sealed class Import : Statement
{
    public required string NamespacePath { get; init; }
    public bool Wildcard { get; init; }
}
