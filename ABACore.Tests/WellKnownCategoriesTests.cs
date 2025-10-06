using ABACore.Runtime;

namespace ABACore.Tests;

public class WellKnownCategoriesTests
{
    [Fact]
    public void ResolveToUrn_WithShortName_ReturnsUrn()
    {
        var urn = WellKnownCategories.ResolveToUrn("subject");
        Assert.Equal(WellKnownCategories.Subject, urn);
    }

    [Fact]
    public void ResolveToUrn_WithUrn_ReturnsUrn()
    {
        var urn = WellKnownCategories.ResolveToUrn(WellKnownCategories.Subject);
        Assert.Equal(WellKnownCategories.Subject, urn);
    }

    [Fact]
    public void ResolveToUrn_WithCustomCategory_ReturnsAsIs()
    {
        var urn = WellKnownCategories.ResolveToUrn("custom-category");
        Assert.Equal("custom-category", urn);
    }

    [Fact]
    public void ResolveToUrn_WithNullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToUrn(null!));
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToUrn(""));
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToUrn("   "));
    }

    [Fact]
    public void ResolveToShortName_WithUrn_ReturnsShortName()
    {
        var shortName = WellKnownCategories.ResolveToShortName(WellKnownCategories.Subject);
        Assert.Equal("subject", shortName);
    }

    [Fact]
    public void ResolveToShortName_WithShortName_ReturnsShortName()
    {
        var shortName = WellKnownCategories.ResolveToShortName("subject");
        Assert.Equal("subject", shortName);
    }

    [Fact]
    public void ResolveToShortName_WithUnknownUrn_ReturnsUrn()
    {
        var shortName = WellKnownCategories.ResolveToShortName("urn:custom:category");
        Assert.Equal("urn:custom:category", shortName);
    }

    [Fact]
    public void ResolveToShortName_WithNullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToShortName(null!));
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToShortName(""));
        Assert.Throws<ArgumentException>(() => WellKnownCategories.ResolveToShortName("   "));
    }

    [Fact]
    public void IsWellKnownCategory_WithWellKnownShortName_ReturnsTrue()
    {
        Assert.True(WellKnownCategories.IsWellKnownCategory("subject"));
        Assert.True(WellKnownCategories.IsWellKnownCategory("resource"));
        Assert.True(WellKnownCategories.IsWellKnownCategory("action"));
    }

    [Fact]
    public void IsWellKnownCategory_WithWellKnownUrn_ReturnsTrue()
    {
        Assert.True(WellKnownCategories.IsWellKnownCategory(WellKnownCategories.Subject));
        Assert.True(WellKnownCategories.IsWellKnownCategory(WellKnownCategories.Resource));
        Assert.True(WellKnownCategories.IsWellKnownCategory(WellKnownCategories.Action));
    }

    [Fact]
    public void IsWellKnownCategory_WithUnknownCategory_ReturnsFalse()
    {
        Assert.False(WellKnownCategories.IsWellKnownCategory("custom"));
        Assert.False(WellKnownCategories.IsWellKnownCategory("unknown"));
    }

    [Fact]
    public void IsWellKnownCategory_WithNullOrEmpty_ReturnsFalse()
    {
        Assert.False(WellKnownCategories.IsWellKnownCategory(null!));
        Assert.False(WellKnownCategories.IsWellKnownCategory(""));
        Assert.False(WellKnownCategories.IsWellKnownCategory("   "));
    }

    [Fact]
    public void ResolveToUrn_WithAlternativeShortNames_ReturnsCorrectUrn()
    {
        Assert.Equal(WellKnownCategories.Subject, WellKnownCategories.ResolveToUrn("access-subject"));
        Assert.Equal(WellKnownCategories.Resource, WellKnownCategories.ResolveToUrn("target-resource"));
        Assert.Equal(WellKnownCategories.Action, WellKnownCategories.ResolveToUrn("requested-action"));
        Assert.Equal(WellKnownCategories.Environment, WellKnownCategories.ResolveToUrn("context"));
    }

    [Fact]
    public void ResolveToUrn_IsCaseInsensitive()
    {
        Assert.Equal(WellKnownCategories.Subject, WellKnownCategories.ResolveToUrn("SUBJECT"));
        Assert.Equal(WellKnownCategories.Resource, WellKnownCategories.ResolveToUrn("Resource"));
        Assert.Equal(WellKnownCategories.Action, WellKnownCategories.ResolveToUrn("AcTiOn"));
    }

    [Fact]
    public void ResolveToShortName_IsCaseInsensitive()
    {
        var urnUpper = WellKnownCategories.Subject.ToUpperInvariant();
        var shortName = WellKnownCategories.ResolveToShortName(urnUpper);
        Assert.Equal("subject", shortName);
    }

    [Fact]
    public void AllWellKnownCategories_AreAccessible()
    {
        Assert.NotNull(WellKnownCategories.Subject);
        Assert.NotNull(WellKnownCategories.Resource);
        Assert.NotNull(WellKnownCategories.Action);
        Assert.NotNull(WellKnownCategories.Environment);
        Assert.NotNull(WellKnownCategories.Recipient);
        Assert.NotNull(WellKnownCategories.Intermediary);
        Assert.NotNull(WellKnownCategories.Codebase);
        Assert.NotNull(WellKnownCategories.RequestingMachine);
        Assert.NotNull(WellKnownCategories.Obligation);
        Assert.NotNull(WellKnownCategories.Advice);
    }

    [Fact]
    public void AllMappings_AreSymmetric()
    {
        foreach (var kvp in WellKnownCategories.ShortNameToUrn)
        {
            var urn = kvp.Value;
            if (WellKnownCategories.UrnToShortName.TryGetValue(urn, out var shortName))
            {
                // The primary short name should resolve back to the URN
                Assert.Equal(urn, WellKnownCategories.ResolveToUrn(shortName));
            }
        }
    }
}
