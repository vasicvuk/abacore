using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ABACore.Runtime;

/// <summary>
/// Thread-safe repository for managing multiple compiled ALFA policies.
/// Supports storing, retrieving, and managing policy versions with optional storage context isolation.
/// Storage contexts (e.g., tenant IDs, microservice IDs) allow the same policy names to be stored
/// independently for different contexts.
/// </summary>
public sealed class PolicyRepository
{
    // storeId -> policyId -> version -> CompiledPolicy
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>> _stores;
    // storeId -> policyId -> latestVersion
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _latestVersions;
    private const string DefaultStoreId = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyRepository"/> class.
    /// </summary>
    public PolicyRepository()
    {
        _stores = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>>(StringComparer.OrdinalIgnoreCase);
        _latestVersions = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds a policy to the repository.
    /// If a policy with the same ID already exists, creates a new version.
    /// </summary>
    /// <param name="policy">The compiled policy to add.</param>
    /// <param name="policyId">Optional policy ID override. If not specified, uses the policy's ID.</param>
    /// <param name="storeId">Optional storage context ID (e.g., tenant ID, microservice ID). If not specified, uses the default store.</param>
    /// <returns>The version number assigned to the policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentException">Thrown when policy ID cannot be determined.</exception>
    public int AddPolicy(CompiledPolicy policy, string? policyId = null, string? storeId = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        string effectivePolicyId = policyId ?? policy.PolicyId ?? throw new ArgumentException("Policy ID must be specified either in the policy or as a parameter.", nameof(policyId));
        string effectiveStoreId = storeId ?? DefaultStoreId;

        ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>> store = _stores.GetOrAdd(
            effectiveStoreId,
            _ => new ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>(StringComparer.OrdinalIgnoreCase));

        ConcurrentDictionary<int, CompiledPolicy> versions = store.GetOrAdd(
            effectivePolicyId,
            _ => new ConcurrentDictionary<int, CompiledPolicy>());

        ConcurrentDictionary<string, int> storeLatestVersions = _latestVersions.GetOrAdd(
            effectiveStoreId,
            _ => new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        int newVersion = storeLatestVersions.AddOrUpdate(
            effectivePolicyId,
            1,
            (_, currentVersion) => currentVersion + 1);

        return !versions.TryAdd(newVersion, policy)
            ? throw new InvalidOperationException($"Failed to add policy '{effectivePolicyId}' version {newVersion} to store '{effectiveStoreId}'. This should not happen in normal operation.")
            : newVersion;
    }

    /// <summary>
    /// Removes a specific version of a policy from the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="version">The version number to remove. If not specified, removes all versions.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>true if the policy was removed; otherwise, false.</returns>
    public bool RemovePolicy(string policyId, int? version = null, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return false;
        }

        if (version.HasValue)
        {
            if (store.TryGetValue(policyId, out ConcurrentDictionary<int, CompiledPolicy>? versions))
            {
                bool removed = versions.TryRemove(version.Value, out _);

                if (versions.IsEmpty)
                {
                    store.TryRemove(policyId, out _);
                    if (_latestVersions.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, int>? storeLatestVersions))
                    {
                        storeLatestVersions.TryRemove(policyId, out _);
                    }
                }

                return removed;
            }

            return false;
        }
        else
        {
            bool removedVersions = store.TryRemove(policyId, out _);
            bool removedLatest = false;
            if (_latestVersions.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, int>? storeLatestVersions))
            {
                removedLatest = storeLatestVersions.TryRemove(policyId, out _);
            }
            return removedVersions || removedLatest;
        }
    }

    /// <summary>
    /// Gets a policy from the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="version">The version number. If not specified, returns the latest version.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The compiled policy, or null if not found.</returns>
    public CompiledPolicy? GetPolicy(string policyId, int? version = null, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return null;
        }

        if (!store.TryGetValue(policyId, out ConcurrentDictionary<int, CompiledPolicy>? versions))
        {
            return null;
        }

        if (version.HasValue)
        {
            return versions.TryGetValue(version.Value, out CompiledPolicy? policy) ? policy : null;
        }

        if (_latestVersions.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, int>? storeLatestVersions) &&
            storeLatestVersions.TryGetValue(policyId, out int latestVersion))
        {
            return versions.TryGetValue(latestVersion, out CompiledPolicy? policy) ? policy : null;
        }

        return null;
    }

    /// <summary>
    /// Tries to get a policy from the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="policy">The compiled policy if found.</param>
    /// <param name="version">The version number. If not specified, returns the latest version.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>true if the policy was found; otherwise, false.</returns>
    public bool TryGetPolicy(string policyId, out CompiledPolicy? policy, int? version = null, string? storeId = null)
    {
        policy = GetPolicy(policyId, version, storeId);
        return policy != null;
    }

    /// <summary>
    /// Lists all policy IDs in the repository for a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of policy identifiers.</returns>
    public IEnumerable<string> ListPolicies(string? storeId = null)
    {
        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return [];
        }

        return [.. store.Keys];
    }

    /// <summary>
    /// Lists all versions of a specific policy.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of version numbers, or an empty collection if the policy is not found.</returns>
    public IEnumerable<int> ListVersions(string policyId, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return [];
        }

        return store.TryGetValue(policyId, out ConcurrentDictionary<int, CompiledPolicy>? versions)
            ? [.. versions.Keys.OrderBy(v => v)]
            : [];
    }

    /// <summary>
    /// Gets the latest version number for a policy.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The latest version number, or null if the policy is not found.</returns>
    public int? GetLatestVersion(string policyId, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (_latestVersions.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, int>? storeLatestVersions) &&
            storeLatestVersions.TryGetValue(policyId, out int version))
        {
            return version;
        }

        return null;
    }

    /// <summary>
    /// Gets the total number of unique policies in the repository for a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The number of unique policy IDs.</returns>
    public int GetPolicyCount(string? storeId = null)
    {
        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return 0;
        }

        return store.Count;
    }

    /// <summary>
    /// Gets the total number of policy versions across all policies for a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The total number of policy versions.</returns>
    public int GetTotalVersionCount(string? storeId = null)
    {
        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return 0;
        }

        return store.Values.Sum(versions => versions.Count);
    }

    /// <summary>
    /// Checks if a policy exists in the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="version">The version number. If not specified, checks for any version.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>true if the policy exists; otherwise, false.</returns>
    public bool ContainsPolicy(string policyId, int? version = null, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return false;
        }

        return store.TryGetValue(policyId, out ConcurrentDictionary<int, CompiledPolicy>? versions) && (!version.HasValue || versions.ContainsKey(version.Value));
    }

    /// <summary>
    /// Clears all policies from the repository.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, clears all stores.</param>
    public void Clear(string? storeId = null)
    {
        if (storeId == null)
        {
            _stores.Clear();
            _latestVersions.Clear();
        }
        else
        {
            _stores.TryRemove(storeId, out _);
            _latestVersions.TryRemove(storeId, out _);
        }
    }

    /// <summary>
    /// Gets a snapshot of all policies with their metadata for a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of policy metadata.</returns>
    public IEnumerable<PolicyMetadata> GetPolicyMetadata(string? storeId = null)
    {
        string effectiveStoreId = storeId ?? DefaultStoreId;

        if (!_stores.TryGetValue(effectiveStoreId, out ConcurrentDictionary<string, ConcurrentDictionary<int, CompiledPolicy>>? store))
        {
            return [];
        }

        List<PolicyMetadata> metadata = [];

        foreach (KeyValuePair<string, ConcurrentDictionary<int, CompiledPolicy>> policyEntry in store)
        {
            string policyId = policyEntry.Key;
            int? latestVersion = GetLatestVersion(policyId, effectiveStoreId);

            foreach (KeyValuePair<int, CompiledPolicy> versionEntry in policyEntry.Value)
            {
                metadata.Add(new PolicyMetadata
                {
                    PolicyId = policyId,
                    Version = versionEntry.Key,
                    IsLatestVersion = versionEntry.Key == latestVersion,
                    CompiledAt = versionEntry.Value.CompiledAt
                });
            }
        }

        return metadata.OrderBy(m => m.PolicyId).ThenBy(m => m.Version);
    }

    /// <summary>
    /// Lists all storage context IDs (stores) in the repository.
    /// </summary>
    /// <returns>A collection of store identifiers.</returns>
    public IEnumerable<string> ListStores()
    {
        return [.. _stores.Keys.Where(k => k != DefaultStoreId)];
    }
}

/// <summary>
/// Represents metadata about a stored policy.
/// </summary>
public sealed class PolicyMetadata
{
    /// <summary>
    /// Gets the policy identifier.
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Gets the policy version number.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the latest version of the policy.
    /// </summary>
    public required bool IsLatestVersion { get; init; }

    /// <summary>
    /// Gets the timestamp when the policy was compiled.
    /// </summary>
    public required DateTime CompiledAt { get; init; }
}
