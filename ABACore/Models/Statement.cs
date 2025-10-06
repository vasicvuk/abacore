using System.Text.Json.Serialization;

namespace ABACore.Models;

/// <summary>
/// Base class for namespace statements.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Import), "import")]
[JsonDerivedType(typeof(AttributeDeclaration), "attributeDeclaration")]
[JsonDerivedType(typeof(PolicyStatement), "policy")]
[JsonDerivedType(typeof(PolicySetStatement), "policySet")]
public abstract class Statement : AlfaNode
{
}
