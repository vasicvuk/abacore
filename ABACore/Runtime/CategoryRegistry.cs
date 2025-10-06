using System.Collections.Concurrent;
using ABACore.Models;

namespace ABACore.Runtime;

/// <summary>
/// Manages Oasis attribute categories with well-known categories and extensibility support.
/// Provides category-to-URI mapping and category resolution for ALFA policies.
/// </summary>
public sealed class CategoryRegistry
{
    private readonly ConcurrentDictionary<string, CategoryInfo> _categories;
    private readonly ConcurrentDictionary<string, string> _aliasToCategory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRegistry"/> class.
    /// </summary>
    public CategoryRegistry()
    {
        _categories = new ConcurrentDictionary<string, CategoryInfo>(StringComparer.OrdinalIgnoreCase);
        _aliasToCategory = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        InitializeWellKnownCategories();
    }

    /// <summary>
    /// Gets a category by name.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The category information, or null if not found.</returns>
    public CategoryInfo? GetCategory(string categoryName)
    {
        return _categories.TryGetValue(categoryName, out CategoryInfo? category) ? category : null;
    }

    /// <summary>
    /// Registers a new category.
    /// </summary>
    /// <param name="categoryInfo">The category information to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when categoryInfo is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a category with the same name already exists.</exception>
    public void RegisterCategory(CategoryInfo categoryInfo)
    {
        ArgumentNullException.ThrowIfNull(categoryInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryInfo.Name);

        if (_categories.ContainsKey(categoryInfo.Name))
        {
            throw new ArgumentException($"Category '{categoryInfo.Name}' is already registered.", nameof(categoryInfo));
        }

        _categories.TryAdd(categoryInfo.Name, categoryInfo);

        // Register aliases
        foreach (string alias in categoryInfo.Aliases)
        {
            _aliasToCategory.TryAdd(alias, categoryInfo.Name);
        }
    }

    /// <summary>
    /// Resolves a category name from an alias or returns the original name if not an alias.
    /// </summary>
    /// <param name="nameOrAlias">The category name or alias.</param>
    /// <returns>The resolved category name.</returns>
    public string ResolveCategoryName(string nameOrAlias)
    {
        return _aliasToCategory.TryGetValue(nameOrAlias, out string? resolvedName)
            ? resolvedName
            : nameOrAlias;
    }

    /// <summary>
    /// Gets all registered categories.
    /// </summary>
    /// <returns>A collection of all registered categories.</returns>
    public IEnumerable<CategoryInfo> GetAllCategories()
    {
        return [.. _categories.Values];
    }

    /// <summary>
    /// Checks if a category is registered.
    /// </summary>
    /// <param name="categoryName">The category name to check.</param>
    /// <returns>true if the category is registered; otherwise, false.</returns>
    public bool IsCategoryRegistered(string categoryName)
    {
        return _categories.ContainsKey(categoryName) || _aliasToCategory.ContainsKey(categoryName);
    }

    /// <summary>
    /// Gets the URI for a category.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The category URI, or null if not found.</returns>
    public string? GetCategoryUri(string categoryName)
    {
        string resolvedName = ResolveCategoryName(categoryName);
        return _categories.TryGetValue(resolvedName, out CategoryInfo? category) ? category.Uri : null;
    }

    /// <summary>
    /// Initializes the well-known Oasis categories.
    /// </summary>
    private void InitializeWellKnownCategories()
    {
        // Subject Access Decision (SAD) categories
        RegisterCategory(new CategoryInfo
        {
            Name = "subject",
            Uri = "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject",
            Description = "The subject that is requesting access",
            Aliases = ["access-subject", "subject-id"]
        });

        RegisterCategory(new CategoryInfo
        {
            Name = "recipient",
            Uri = "urn:oasis:names:tc:xacml:1.0:subject-category:recipient-subject",
            Description = "The subject that is the recipient of data",
            Aliases = ["recipient-subject"]
        });

        RegisterCategory(new CategoryInfo
        {
            Name = "intermediary",
            Uri = "urn:oasis:names:tc:xacml:1.0:subject-category:intermediary-subject",
            Description = "The subject that intermediates the data exchange",
            Aliases = ["intermediary-subject"]
        });

        RegisterCategory(new CategoryInfo
        {
            Name = "codebase",
            Uri = "urn:oasis:names:tc:xacml:1.0:subject-category:codebase",
            Description = "The codebase from which the request originates",
            Aliases = ["codebase-subject"]
        });

        RegisterCategory(new CategoryInfo
        {
            Name = "requesting-machine",
            Uri = "urn:oasis:names:tc:xacml:1.0:subject-category:requesting-machine",
            Description = "The machine from which the request originates",
            Aliases = ["requesting-machine-subject"]
        });

        // Resource categories
        RegisterCategory(new CategoryInfo
        {
            Name = "resource",
            Uri = "urn:oasis:names:tc:xacml:3.0:attribute-category:resource",
            Description = "The resource to which access is requested",
            Aliases = ["target-resource"]
        });

        // Action categories
        RegisterCategory(new CategoryInfo
        {
            Name = "action",
            Uri = "urn:oasis:names:tc:xacml:3.0:attribute-category:action",
            Description = "The action requested on the resource",
            Aliases = ["requested-action"]
        });

        // Environment categories
        RegisterCategory(new CategoryInfo
        {
            Name = "environment",
            Uri = "urn:oasis:names:tc:xacml:3.0:attribute-category:environment",
            Description = "Environmental context of the access request",
            Aliases = ["context", "environmental-context"]
        });

        // Custom categories for extended functionality
        RegisterCategory(new CategoryInfo
        {
            Name = "obligation",
            Uri = "urn:oasis:names:tc:xacml:3.0:attribute-category:obligation",
            Description = "Attributes related to policy obligations",
            Aliases = ["policy-obligation"]
        });

        RegisterCategory(new CategoryInfo
        {
            Name = "advice",
            Uri = "urn:oasis:names:tc:xacml:3.0:attribute-category:advice",
            Description = "Attributes related to policy advice",
            Aliases = ["policy-advice"]
        });
    }
}

/// <summary>
/// Represents information about an Oasis attribute category.
/// </summary>
public sealed class CategoryInfo
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the category URI.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Gets or sets the category description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the aliases for this category.
    /// </summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this is a standard Oasis category.
    /// </summary>
    public bool IsStandard { get; init; } = false;

    /// <summary>
    /// Gets or sets the category version.
    /// </summary>
    public string? Version { get; init; }
}

/// <summary>
/// Provides extension methods for category operations.
/// </summary>
public static class CategoryExtensions
{
    /// <summary>
    /// Determines if a category is a subject category.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns>true if the category is a subject category; otherwise, false.</returns>
    public static bool IsSubjectCategory(this CategoryInfo category)
    {
        return category.Name.Equals("subject", StringComparison.OrdinalIgnoreCase) ||
               category.Aliases.Any(a => a.Contains("subject", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a category is a resource category.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns>true if the category is a resource category; otherwise, false.</returns>
    public static bool IsResourceCategory(this CategoryInfo category)
    {
        return category.Name.Equals("resource", StringComparison.OrdinalIgnoreCase) ||
               category.Aliases.Any(a => a.Contains("resource", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a category is an action category.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns>true if the category is an action category; otherwise, false.</returns>
    public static bool IsActionCategory(this CategoryInfo category)
    {
        return category.Name.Equals("action", StringComparison.OrdinalIgnoreCase) ||
               category.Aliases.Any(a => a.Contains("action", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a category is an environment category.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns>true if the category is an environment category; otherwise, false.</returns>
    public static bool IsEnvironmentCategory(this CategoryInfo category)
    {
        return category.Name.Equals("environment", StringComparison.OrdinalIgnoreCase) ||
               category.Aliases.Any(a => a.Contains("environment", StringComparison.OrdinalIgnoreCase));
    }
}
