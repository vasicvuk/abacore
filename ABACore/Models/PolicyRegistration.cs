using System;
namespace ABACore.Models;

/// <summary>
/// Represents the result of registering a policy with the engine.
/// </summary>
public sealed class PolicyRegistration(string policyId, int version)
{

    /// <summary>
    /// Gets the policy identifier.
    /// </summary>
    public string PolicyId { get; } = policyId ?? throw new ArgumentNullException(nameof(policyId));

    /// <summary>
    /// Gets the version of the policy in the repository.
    /// </summary>
    public int Version { get; } = version;
}
