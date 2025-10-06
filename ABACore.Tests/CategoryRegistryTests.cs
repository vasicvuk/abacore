using ABACore.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for the Oasis category registry system.
/// </summary>
public class CategoryRegistryTests
{
    [Fact]
    public void CategoryRegistry_Initialize_ShouldContainWellKnownCategories()
    {
        // Arrange & Act
        var registry = new CategoryRegistry();

        // Assert - Standard Oasis categories should be available
        Assert.NotNull(registry.GetCategory("subject"));
        Assert.NotNull(registry.GetCategory("resource"));
        Assert.NotNull(registry.GetCategory("action"));
        Assert.NotNull(registry.GetCategory("environment"));

        // Test URIs match Oasis standards
        Assert.Equal("urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", registry.GetCategoryUri("subject"));
        Assert.Equal("urn:oasis:names:tc:xacml:3.0:attribute-category:resource", registry.GetCategoryUri("resource"));
        Assert.Equal("urn:oasis:names:tc:xacml:3.0:attribute-category:action", registry.GetCategoryUri("action"));
        Assert.Equal("urn:oasis:names:tc:xacml:3.0:attribute-category:environment", registry.GetCategoryUri("environment"));
    }

    [Fact]
    public void CategoryRegistry_ShouldSupportAliases()
    {
        // Arrange & Act
        var registry = new CategoryRegistry();

        // Assert - Aliases should resolve to the same category
        string? subjectUri = registry.GetCategoryUri("subject");
        Assert.Equal(subjectUri, registry.GetCategoryUri("access-subject"));
        Assert.Equal(subjectUri, registry.GetCategoryUri("subject-id"));

        string? resourceUri = registry.GetCategoryUri("resource");
        Assert.Equal(resourceUri, registry.GetCategoryUri("target-resource"));
    }

    [Fact]
    public void CategoryRegistry_ShouldAllowCustomCategories()
    {
        // Arrange
        var registry = new CategoryRegistry();
        var customCategory = new CategoryInfo
        {
            Name = "custom",
            Uri = "urn:example:names:custom:category",
            Description = "Custom category for testing",
            Aliases = ["test-category", "example"],
            IsStandard = false,
            Version = "1.0"
        };

        // Act
        registry.RegisterCategory(customCategory);

        // Assert
        Assert.NotNull(registry.GetCategory("custom"));
        Assert.Equal("urn:example:names:custom:category", registry.GetCategoryUri("custom"));
        Assert.Equal("urn:example:names:custom:category", registry.GetCategoryUri("test-category"));
        Assert.Equal("urn:example:names:custom:category", registry.GetCategoryUri("example"));
    }

    [Fact]
    public void CategoryRegistry_ShouldPreventDuplicateCategoryNames()
    {
        // Arrange
        var registry = new CategoryRegistry();
        var customCategory = new CategoryInfo
        {
            Name = "subject", // Try to override standard category
            Uri = "urn:example:names:custom:subject",
            Description = "Custom subject category"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterCategory(customCategory));
    }

    [Fact]
    public void CategoryInfo_Extensions_ShouldIdentifyCategoryTypes()
    {
        // Arrange
        var registry = new CategoryRegistry();

        // Act & Assert
        CategoryInfo? subjectCategory = registry.GetCategory("subject");
        Assert.NotNull(subjectCategory);
        Assert.True(subjectCategory.IsSubjectCategory());
        Assert.False(subjectCategory.IsResourceCategory());
        Assert.False(subjectCategory.IsActionCategory());
        Assert.False(subjectCategory.IsEnvironmentCategory());

        CategoryInfo? resourceCategory = registry.GetCategory("resource");
        Assert.NotNull(resourceCategory);
        Assert.True(resourceCategory.IsResourceCategory());
        Assert.False(resourceCategory.IsSubjectCategory());

        CategoryInfo? actionCategory = registry.GetCategory("action");
        Assert.NotNull(actionCategory);
        Assert.True(actionCategory.IsActionCategory());
        Assert.False(actionCategory.IsSubjectCategory());

        CategoryInfo? environmentCategory = registry.GetCategory("environment");
        Assert.NotNull(environmentCategory);
        Assert.True(environmentCategory.IsEnvironmentCategory());
        Assert.False(environmentCategory.IsSubjectCategory());
    }

    [Fact]
    public void CategoryRegistry_ShouldListAllCategories()
    {
        // Arrange & Act
        var registry = new CategoryRegistry();
        var allCategories = registry.GetAllCategories().ToList();

        // Assert
        Assert.NotEmpty(allCategories);
        Assert.Contains(allCategories, c => c.Name == "subject");
        Assert.Contains(allCategories, c => c.Name == "resource");
        Assert.Contains(allCategories, c => c.Name == "action");
        Assert.Contains(allCategories, c => c.Name == "environment");
    }

    [Fact]
    public void CategoryRegistry_ShouldCheckCategoryRegistration()
    {
        // Arrange & Act
        var registry = new CategoryRegistry();

        // Assert
        Assert.True(registry.IsCategoryRegistered("subject"));
        Assert.True(registry.IsCategoryRegistered("access-subject")); // Alias
        Assert.False(registry.IsCategoryRegistered("nonexistent"));
    }
}
