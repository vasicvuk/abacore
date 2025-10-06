using System.Text.Json.Serialization;

namespace ABACore.Models;

/// <summary>
/// Base class for policy set elements.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Policy), "policy")]
[JsonDerivedType(typeof(PolicySet), "policySet")]
[JsonDerivedType(typeof(PolicyReference), "reference")]
public abstract class PolicySetElement : AlfaNode
{
}
