using ABACore.Runtime;

namespace ABACore.Models;

/// <summary>
/// Represents the evaluation context for policy evaluation.
/// Contains attribute values organized by category and attribute ID.
/// </summary>
public sealed class EvaluationContext
{
    private readonly Dictionary<string, Dictionary<string, object>> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    public EvaluationContext()
    {
        _attributes = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class with initial attributes.
    /// </summary>
    /// <param name="attributes">Initial attributes organized by category.</param>
    public EvaluationContext(Dictionary<string, Dictionary<string, object>> attributes)
    {
        _attributes = new Dictionary<string, Dictionary<string, object>>(attributes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets an attribute value for a specific category and attribute ID.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <param name="value">The attribute value.</param>
    public void SetAttribute(string category, string attributeId, object value)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        if (!_attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes))
        {
            categoryAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _attributes[resolvedCategory] = categoryAttributes;
        }

        categoryAttributes[attributeId] = value;
    }

    /// <summary>
    /// Gets an attribute value for a specific category and attribute ID.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <returns>The attribute value, or null if not found.</returns>
    public object? GetAttribute(string category, string attributeId)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        if (_attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes))
        {
            if (categoryAttributes.TryGetValue(attributeId, out object? value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get an attribute value for a specific category and attribute ID.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <param name="value">The attribute value if found.</param>
    /// <returns>true if the attribute was found; otherwise, false.</returns>
    public bool TryGetAttribute(string category, string attributeId, out object? value)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        if (_attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes))
        {
            return categoryAttributes.TryGetValue(attributeId, out value);
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets a typed attribute value for a specific category and attribute ID.
    /// </summary>
    /// <typeparam name="T">The expected type of the attribute value.</typeparam>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <returns>The typed attribute value, or default(T) if not found or cannot be cast.</returns>
    public T? GetAttribute<T>(string category, string attributeId)
    {
        object? value = GetAttribute(category, attributeId);
        return value is T typedValue ? typedValue : default;
    }

    /// <summary>
    /// Tries to get a typed attribute value for a specific category and attribute ID.
    /// </summary>
    /// <typeparam name="T">The expected type of the attribute value.</typeparam>
    /// <param name="category">The attribute category.</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <param name="value">The typed attribute value if found and can be cast.</param>
    /// <returns>true if the attribute was found and successfully cast; otherwise, false.</returns>
    public bool TryGetAttribute<T>(string category, string attributeId, out T? value)
    {
        if (TryGetAttribute(category, attributeId, out object? objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Checks if an attribute exists in the context.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <returns>true if the attribute exists; otherwise, false.</returns>
    public bool HasAttribute(string category, string attributeId)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        return _attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes) && categoryAttributes.ContainsKey(attributeId);
    }

    /// <summary>
    /// Gets all attributes for a specific category.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <returns>A dictionary of attribute IDs to values, or null if the category doesn't exist.</returns>
    public Dictionary<string, object>? GetCategoryAttributes(string category)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        return _attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes)
            ? new Dictionary<string, object>(categoryAttributes, StringComparer.OrdinalIgnoreCase)
            : null;
    }

    /// <summary>
    /// Removes an attribute from the context.
    /// </summary>
    /// <param name="category">The attribute category (short name or full URN).</param>
    /// <param name="attributeId">The attribute identifier.</param>
    /// <returns>true if the attribute was found and removed; otherwise, false.</returns>
    public bool RemoveAttribute(string category, string attributeId)
    {
        string resolvedCategory = WellKnownCategories.ResolveToUrn(category);

        return _attributes.TryGetValue(resolvedCategory, out Dictionary<string, object>? categoryAttributes) && categoryAttributes.Remove(attributeId);
    }

    /// <summary>
    /// Clears all attributes from the context.
    /// </summary>
    public void Clear()
    {
        _attributes.Clear();
    }

    /// <summary>
    /// Gets a copy of all attributes in the context.
    /// </summary>
    /// <returns>A dictionary of categories to attribute dictionaries.</returns>
    public Dictionary<string, Dictionary<string, object>> GetAllAttributes()
    {
        var result = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, Dictionary<string, object>> category in _attributes)
        {
            result[category.Key] = new Dictionary<string, object>(category.Value, StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Gets an attribute value using namespace and attribute name.
    /// This method resolves the namespace to find the appropriate category and attribute ID.
    /// </summary>
    /// <param name="namespacePath">The namespace path of the attribute.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The attribute value, or null if not found.</returns>
    public object? GetAttributeByNamespace(string namespacePath, string attributeName)
    {
        // For backward compatibility, we'll look in all categories for the attribute name
        // In a full implementation, this would use the NamespaceResolver to map namespace to category
        foreach (string category in _attributes.Keys)
        {
            if (_attributes[category].TryGetValue(attributeName, out object? value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a typed attribute value using namespace and attribute name.
    /// </summary>
    /// <typeparam name="T">The expected type of the attribute value.</typeparam>
    /// <param name="namespacePath">The namespace path of the attribute.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The typed attribute value, or default(T) if not found or cannot be cast.</returns>
    public T? GetAttributeByNamespace<T>(string namespacePath, string attributeName)
    {
        object? value = GetAttributeByNamespace(namespacePath, attributeName);
        return value is T typedValue ? typedValue : default;
    }
}
