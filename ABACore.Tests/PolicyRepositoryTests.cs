using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class PolicyRepositoryTests
{
    private readonly PolicyRepository _repository = new();

    [Fact]
    public void AddPolicy_WithNullPolicy_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repository.AddPolicy(null!));
    }

    [Fact]
    public void AddPolicy_WithoutPolicyId_ThrowsArgumentException()
    {
        var policy = new CompiledPolicy
        {
            PolicyId = null,
            EvaluationDelegate = _ => Decision.Permit()
        };

        var ex = Assert.Throws<ArgumentException>(() => _repository.AddPolicy(policy));
        Assert.Contains("Policy ID must be specified", ex.Message);
    }

    [Fact]
    public void AddPolicy_FirstVersion_ReturnsVersion1()
    {
        var policy = CreateTestPolicy("policy1");

        var version = _repository.AddPolicy(policy);

        Assert.Equal(1, version);
    }

    [Fact]
    public void AddPolicy_SecondVersion_ReturnsVersion2()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        var version = _repository.AddPolicy(policy2);

        Assert.Equal(2, version);
    }

    [Fact]
    public void AddPolicy_WithOverridePolicyId_UsesOverride()
    {
        var policy = CreateTestPolicy("originalId");

        var version = _repository.AddPolicy(policy, "overrideId");

        Assert.Equal(1, version);
        Assert.NotNull(_repository.GetPolicy("overrideId"));
        Assert.Null(_repository.GetPolicy("originalId"));
    }

    [Fact]
    public void GetPolicy_WhenNotFound_ReturnsNull()
    {
        var result = _repository.GetPolicy("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetPolicy_WithoutVersion_ReturnsLatestVersion()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var result = _repository.GetPolicy("policy1");

        Assert.NotNull(result);
        Assert.Same(policy2, result);
    }

    [Fact]
    public void GetPolicy_WithSpecificVersion_ReturnsThatVersion()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var result = _repository.GetPolicy("policy1", 1);

        Assert.NotNull(result);
        Assert.Same(policy1, result);
    }

    [Fact]
    public void GetPolicy_WithNonExistentVersion_ReturnsNull()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        var result = _repository.GetPolicy("policy1", 99);

        Assert.Null(result);
    }

    [Fact]
    public void GetPolicy_CaseInsensitive_FindsPolicy()
    {
        var policy = CreateTestPolicy("PolicyOne");
        _repository.AddPolicy(policy);

        var result = _repository.GetPolicy("policyone");

        Assert.NotNull(result);
        Assert.Same(policy, result);
    }

    [Fact]
    public void TryGetPolicy_WhenFound_ReturnsTrueAndPolicy()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        var found = _repository.TryGetPolicy("policy1", out var result);

        Assert.True(found);
        Assert.NotNull(result);
        Assert.Same(policy, result);
    }

    [Fact]
    public void TryGetPolicy_WhenNotFound_ReturnsFalseAndNull()
    {
        var found = _repository.TryGetPolicy("nonexistent", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void RemovePolicy_WithoutVersion_RemovesAllVersions()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var removed = _repository.RemovePolicy("policy1");

        Assert.True(removed);
        Assert.Null(_repository.GetPolicy("policy1", 1));
        Assert.Null(_repository.GetPolicy("policy1", 2));
    }

    [Fact]
    public void RemovePolicy_WithSpecificVersion_RemovesOnlyThatVersion()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var removed = _repository.RemovePolicy("policy1", 1);

        Assert.True(removed);
        Assert.Null(_repository.GetPolicy("policy1", 1));
        Assert.NotNull(_repository.GetPolicy("policy1", 2));
    }

    [Fact]
    public void RemovePolicy_LastVersion_CleansUpPolicy()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        _repository.RemovePolicy("policy1", 1);

        var policies = _repository.ListPolicies();
        Assert.DoesNotContain("policy1", policies);
    }

    [Fact]
    public void RemovePolicy_NonExistent_ReturnsFalse()
    {
        var removed = _repository.RemovePolicy("nonexistent");

        Assert.False(removed);
    }

    [Fact]
    public void ListPolicies_ReturnsAllPolicies()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy2");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var policies = _repository.ListPolicies().ToList();

        Assert.Equal(2, policies.Count);
        Assert.Contains("policy1", policies);
        Assert.Contains("policy2", policies);
    }

    [Fact]
    public void GetPolicyMetadata_IncludesLatestVersionFlag()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var metadata = _repository.GetPolicyMetadata().ToList();

        var version1 = metadata.FirstOrDefault(p => p.Version == 1);
        var version2 = metadata.FirstOrDefault(p => p.Version == 2);

        Assert.NotNull(version1);
        Assert.NotNull(version2);
        Assert.False(version1.IsLatestVersion);
        Assert.True(version2.IsLatestVersion);
    }

    [Fact]
    public void ListVersions_ReturnsAllVersionsForPolicy()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var versions = _repository.ListVersions("policy1").ToList();

        Assert.Equal(2, versions.Count);
        Assert.Contains(1, versions);
        Assert.Contains(2, versions);
    }

    [Fact]
    public void ListVersions_NonExistentPolicy_ReturnsEmpty()
    {
        var versions = _repository.ListVersions("nonexistent").ToList();

        Assert.Empty(versions);
    }

    [Fact]
    public void GetLatestVersion_ReturnsCorrectVersion()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");
        var policy3 = CreateTestPolicy("policy1");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);
        _repository.AddPolicy(policy3);

        var latestVersion = _repository.GetLatestVersion("policy1");

        Assert.Equal(3, latestVersion);
    }

    [Fact]
    public void GetLatestVersion_NonExistentPolicy_ReturnsNull()
    {
        var latestVersion = _repository.GetLatestVersion("nonexistent");

        Assert.Null(latestVersion);
    }

    [Fact]
    public void GetPolicyCount_ReturnsCorrectCount()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy2");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);

        var count = _repository.GetPolicyCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public void GetTotalVersionCount_ReturnsCorrectCount()
    {
        var policy1 = CreateTestPolicy("policy1");
        var policy2 = CreateTestPolicy("policy1");
        var policy3 = CreateTestPolicy("policy2");

        _repository.AddPolicy(policy1);
        _repository.AddPolicy(policy2);
        _repository.AddPolicy(policy3);

        var count = _repository.GetTotalVersionCount();

        Assert.Equal(3, count);
    }

    [Fact]
    public void ContainsPolicy_WhenExists_ReturnsTrue()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        var contains = _repository.ContainsPolicy("policy1");

        Assert.True(contains);
    }

    [Fact]
    public void ContainsPolicy_WhenNotExists_ReturnsFalse()
    {
        var contains = _repository.ContainsPolicy("nonexistent");

        Assert.False(contains);
    }

    [Fact]
    public void ContainsPolicy_WithVersion_ChecksSpecificVersion()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        var containsV1 = _repository.ContainsPolicy("policy1", 1);
        var containsV2 = _repository.ContainsPolicy("policy1", 2);

        Assert.True(containsV1);
        Assert.False(containsV2);
    }

    [Fact]
    public void GetPolicyMetadata_ReturnsCorrectMetadata()
    {
        var policy = CreateTestPolicy("policy1");
        _repository.AddPolicy(policy);

        var metadata = _repository.GetPolicyMetadata().ToList();

        Assert.Single(metadata);
        Assert.Equal("policy1", metadata[0].PolicyId);
        Assert.Equal(1, metadata[0].Version);
        Assert.True(metadata[0].IsLatestVersion);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAdds_AllSucceed()
    {
        var tasks = new List<Task<int>>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var policy = CreateTestPolicy("concurrent-policy");
                return _repository.AddPolicy(policy);
            }));
        }

        await Task.WhenAll(tasks);

        var versions = tasks.Select(t => t.Result).Distinct().ToList();
        Assert.Equal(10, versions.Count);
        Assert.Equal(10, _repository.GetLatestVersion("concurrent-policy"));
    }

    private static CompiledPolicy CreateTestPolicy(string policyId)
    {
        return new CompiledPolicy
        {
            PolicyId = policyId,
            EvaluationDelegate = _ => Decision.Permit()
        };
    }
}
