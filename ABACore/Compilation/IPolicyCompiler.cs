using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Compilation;

/// <summary>
/// Interface for policy compilers that can compile ALFA policy AST nodes to executable delegates.
/// </summary>
public interface IPolicyCompiler
{
    /// <summary>
    /// Compiles a policy AST to an executable delegate.
    /// </summary>
    /// <param name="policy">The policy to compile.</param>
    /// <param name="enableCaching">Whether to cache the compiled policy.</param>
    /// <returns>A compiled policy that can be executed.</returns>
    public CompiledPolicy CompilePolicy(Policy policy, bool enableCaching = true);

    /// <summary>
    /// Compiles a policyset AST to an executable delegate.
    /// </summary>
    /// <param name="policySet">The policyset to compile.</param>
    /// <param name="repository">The policy repository for resolving policy references.</param>
    /// <param name="enableCaching">Whether to cache the compiled policy.</param>
    /// <returns>A compiled policyset that can be executed.</returns>
    public CompiledPolicy CompilePolicySet(PolicySet policySet, PolicyRepository repository, bool enableCaching = true);

    /// <summary>
    /// Clears the compiled policy cache.
    /// </summary>
    public void ClearCache();

    /// <summary>
    /// Removes a specific policy from the cache.
    /// </summary>
    /// <param name="policyId">The policy ID to remove.</param>
    /// <returns>True if the policy was removed, false otherwise.</returns>
    public bool RemoveFromCache(string policyId);
}
