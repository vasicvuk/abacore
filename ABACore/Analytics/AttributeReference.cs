namespace ABACore.Analytics;

/// <summary>
/// Represents a reference to an attribute in ALFA code.
/// </summary>
public sealed class AttributeReference
{
    /// <summary>
    /// Namespace of the attribute.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Name of the attribute.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the attribute must be present.
    /// </summary>
    public bool MustBePresent { get; init; }

    /// <summary>
    /// Location where the attribute is referenced.
    /// </summary>
    public TextRange? Range { get; init; }

    /// <summary>
    /// Full qualified name (namespace.name).
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";

    public override string ToString() => MustBePresent ? $"{FullName}?" : FullName;
}
