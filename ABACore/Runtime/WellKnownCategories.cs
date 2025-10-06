namespace ABACore.Runtime;

/// <summary>
/// Defines well-known XACML/OASIS attribute categories with their URIs and short names.
/// Provides constants for category names to avoid magic strings and ensure consistency.
/// </summary>
public static class WellKnownCategories
{
    /// <summary>
    /// Subject access category - the entity requesting access
    /// </summary>
    public const string Subject = "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject";

    /// <summary>
    /// Resource category - the resource being accessed
    /// </summary>
    public const string Resource = "urn:oasis:names:tc:xacml:3.0:attribute-category:resource";

    /// <summary>
    /// Action category - the action being performed
    /// </summary>
    public const string Action = "urn:oasis:names:tc:xacml:3.0:attribute-category:action";

    /// <summary>
    /// Environment category - environmental context
    /// </summary>
    public const string Environment = "urn:oasis:names:tc:xacml:3.0:attribute-category:environment";

    /// <summary>
    /// Recipient subject category - the subject that is the recipient of data
    /// </summary>
    public const string Recipient = "urn:oasis:names:tc:xacml:1.0:subject-category:recipient-subject";

    /// <summary>
    /// Intermediary subject category - the subject that intermediates the data exchange
    /// </summary>
    public const string Intermediary = "urn:oasis:names:tc:xacml:1.0:subject-category:intermediary-subject";

    /// <summary>
    /// Codebase subject category - the codebase from which the request originates
    /// </summary>
    public const string Codebase = "urn:oasis:names:tc:xacml:1.0:subject-category:codebase";

    /// <summary>
    /// Requesting machine subject category - the machine from which the request originates
    /// </summary>
    public const string RequestingMachine = "urn:oasis:names:tc:xacml:1.0:subject-category:requesting-machine";

    /// <summary>
    /// Obligation category - attributes related to policy obligations
    /// </summary>
    public const string Obligation = "urn:oasis:names:tc:xacml:3.0:attribute-category:obligation";

    /// <summary>
    /// Advice category - attributes related to policy advice
    /// </summary>
    public const string Advice = "urn:oasis:names:tc:xacml:3.0:attribute-category:advice";

    /// <summary>
    /// Short name mappings to full URIs
    /// </summary>
    public static readonly Dictionary<string, string> ShortNameToUrn = new(StringComparer.OrdinalIgnoreCase)
    {
        { "subject", Subject },
        { "access-subject", Subject },
        { "resource", Resource },
        { "target-resource", Resource },
        { "action", Action },
        { "requested-action", Action },
        { "environment", Environment },
        { "context", Environment },
        { "recipient", Recipient },
        { "recipient-subject", Recipient },
        { "intermediary", Intermediary },
        { "intermediary-subject", Intermediary },
        { "codebase", Codebase },
        { "requesting-machine", RequestingMachine },
        { "obligation", Obligation },
        { "advice", Advice }
    };

    /// <summary>
    /// URN to short name mappings
    /// </summary>
    public static readonly Dictionary<string, string> UrnToShortName = new(StringComparer.OrdinalIgnoreCase)
    {
        { Subject, "subject" },
        { Resource, "resource" },
        { Action, "action" },
        { Environment, "environment" },
        { Recipient, "recipient" },
        { Intermediary, "intermediary" },
        { Codebase, "codebase" },
        { RequestingMachine, "requesting-machine" },
        { Obligation, "obligation" },
        { Advice, "advice" }
    };

    /// <summary>
    /// Resolves a category identifier to its full URN form.
    /// </summary>
    /// <param name="categoryIdentifier">The category identifier (short name or full URN).</param>
    /// <returns>The full URN of the category.</returns>
    public static string ResolveToUrn(string categoryIdentifier)
    {
        if (string.IsNullOrWhiteSpace(categoryIdentifier))
        {
            throw new ArgumentException("Category identifier cannot be null or empty.", nameof(categoryIdentifier));
        }

        // If it's already a full URN, return it
        if (categoryIdentifier.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
        {
            return categoryIdentifier;
        }

        // Try to resolve short name to URN
        if (ShortNameToUrn.TryGetValue(categoryIdentifier, out string? urn))
        {
            return urn;
        }

        // If not found, assume it's a custom category and return as-is
        return categoryIdentifier;
    }

    /// <summary>
    /// Resolves a category identifier to its short name form.
    /// </summary>
    /// <param name="categoryIdentifier">The category identifier (short name or full URN).</param>
    /// <returns>The short name of the category.</returns>
    public static string ResolveToShortName(string categoryIdentifier)
    {
        if (string.IsNullOrWhiteSpace(categoryIdentifier))
        {
            throw new ArgumentException("Category identifier cannot be null or empty.", nameof(categoryIdentifier));
        }

        // If it's a full URN, try to resolve to short name
        if (categoryIdentifier.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
        {
            if (UrnToShortName.TryGetValue(categoryIdentifier, out string? shortName))
            {
                return shortName;
            }

            return categoryIdentifier; // Return full URN if not found
        }

        // It's already a short name, return it
        return categoryIdentifier;
    }

    /// <summary>
    /// Checks if a category identifier is a well-known category.
    /// </summary>
    /// <param name="categoryIdentifier">The category identifier (short name or full URN).</param>
    /// <returns>true if the category is well-known; otherwise, false.</returns>
    public static bool IsWellKnownCategory(string categoryIdentifier)
    {
        if (string.IsNullOrWhiteSpace(categoryIdentifier))
        {
            return false;
        }

        string urn = ResolveToUrn(categoryIdentifier);
        return UrnToShortName.ContainsKey(urn);
    }
}
