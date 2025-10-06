namespace ABACore.Models;

/// <summary>
/// Represents a namespace with its imported and declared items.
/// </summary>
public sealed class NamespaceInfo
{
    public required string Name { get; init; }
    public List<ImportDeclaration> Imports { get; init; } = [];
    public Dictionary<string, AttributeInfo> Attributes { get; init; } = [];
    public Dictionary<string, FunctionInfo> Functions { get; init; } = [];
}

/// <summary>
/// Represents an import declaration with resolved items.
/// </summary>
public sealed class ImportDeclaration
{
    public required string NamespacePath { get; init; }
    public bool Wildcard { get; init; }
    public List<string> ImportedItems { get; init; } = [];
}

/// <summary>
/// Represents attribute information in a namespace.
/// </summary>
public sealed class AttributeInfo
{
    public required string Name { get; init; }
    public required string FullNamespace { get; init; }
    public AttributeType Type { get; init; }
    public string? Category { get; init; }
    public string? Id { get; init; }
}

/// <summary>
/// Represents function information in a namespace.
/// </summary>
public sealed class FunctionInfo
{
    public required string Name { get; init; }
    public required string FullNamespace { get; init; }
    public required List<FunctionSignature> Signatures { get; init; }
    public string? Id { get; init; }
}
