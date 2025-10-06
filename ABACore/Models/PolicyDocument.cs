

namespace ABACore.Models;

/// <summary>
/// Represents a parsed policy document that may contain a namespace and either a policy or a policy set.
/// </summary>
public sealed class PolicyDocument
{
    /// <summary>
    /// Gets or sets the namespace associated with the document (if any).
    /// </summary>
    public Namespace? Namespace { get; init; }

    /// <summary>
    /// Gets or sets the policy contained in the document (when not a policy set).
    /// </summary>
    public Policy? Policy { get; init; }

    /// <summary>
    /// Gets or sets the policy set contained in the document.
    /// </summary>
    public PolicySet? PolicySet { get; init; }
}
