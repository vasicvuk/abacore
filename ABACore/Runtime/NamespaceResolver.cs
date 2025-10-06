using System.Collections.Concurrent;
using ABACore.Models;

namespace ABACore.Runtime;

/// <summary>
/// Manages namespace resolution for attributes and functions in ALFA policies.
/// Provides default Oasis namespaces and handles import resolution.
/// </summary>
public sealed class NamespaceResolver
{
    private readonly ConcurrentDictionary<string, NamespaceInfo> _namespaces;
    private readonly ConcurrentDictionary<string, HashSet<string>> _namespaceHierarchy;
    private readonly CategoryRegistry _categoryRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceResolver"/> class.
    /// </summary>
    public NamespaceResolver()
    {
        _namespaces = new ConcurrentDictionary<string, NamespaceInfo>(StringComparer.OrdinalIgnoreCase);
        _namespaceHierarchy = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        _categoryRegistry = new CategoryRegistry();

        LoadOasisNamespaces();
    }

    /// <summary>
    /// Gets the category registry.
    /// </summary>
    public CategoryRegistry CategoryRegistry => _categoryRegistry;

    /// <summary>
    /// Gets a namespace by name.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The namespace info, or null if not found.</returns>
    public NamespaceInfo? GetNamespace(string namespaceName)
    {
        return _namespaces.TryGetValue(namespaceName, out NamespaceInfo? ns) ? ns : null;
    }

    /// <summary>
    /// Resolves an attribute reference to its full namespace path.
    /// </summary>
    /// <param name="currentNamespace">The current namespace context.</param>
    /// <param name="attributeName">The attribute name to resolve.</param>
    /// <returns>The resolved attribute info, or null if not found.</returns>
    public AttributeInfo? ResolveAttribute(string currentNamespace, string attributeName)
    {
        // Check if attribute name includes namespace (e.g., "Subject.Role")
        if (attributeName.Contains('.'))
        {
            string[] parts = attributeName.Split('.');
            if (parts.Length >= 2)
            {
                string ns = string.Join(".", parts.Take(parts.Length - 1));
                string name = parts[^1];

                // Try to resolve the namespace - this handles short names like "Subject" -> "Oasis.Attributes.Subject"
                string resolvedNamespace = ResolveNamespace(ns);

                if (_namespaces.TryGetValue(resolvedNamespace, out NamespaceInfo? namespaceInfo))
                {
                    return namespaceInfo.Attributes.TryGetValue(name, out AttributeInfo? attr) ? attr : null;
                }
            }
        }

        // Search in current namespace first
        if (_namespaces.TryGetValue(currentNamespace, out NamespaceInfo? currentNs))
        {
            if (currentNs.Attributes.TryGetValue(attributeName, out AttributeInfo? attr))
            {
                return attr;
            }
        }

        // Search in imported namespaces
        IEnumerable<string> importedNamespaces = GetImportedNamespaces(currentNamespace);
        foreach (string importedNs in importedNamespaces)
        {
            if (_namespaces.TryGetValue(importedNs, out NamespaceInfo? nsInfo))
            {
                if (nsInfo.Attributes.TryGetValue(attributeName, out AttributeInfo? importedAttr))
                {
                    return importedAttr;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a short namespace name to its full namespace path.
    /// </summary>
    /// <param name="namespaceName">The namespace name to resolve.</param>
    /// <returns>The resolved namespace name.</returns>
    private string ResolveNamespace(string namespaceName)
    {
        // If it's already a full namespace, return it
        if (namespaceName.Contains('.'))
        {
            return namespaceName;
        }

        // Map short names to full namespaces
        return namespaceName.ToLowerInvariant() switch
        {
            "subject" => "Oasis.Attributes.Subject",
            "resource" => "Oasis.Attributes.Resource",
            "action" => "Oasis.Attributes.Action",
            "environment" => "Oasis.Attributes.Environment",
            "string" => "Oasis.Functions.String",
            "numeric" => "Oasis.Functions.Numeric",
            _ => namespaceName
        };
    }

    /// <summary>
    /// Resolves a function reference to its full namespace path.
    /// </summary>
    /// <param name="currentNamespace">The current namespace context.</param>
    /// <param name="functionName">The function name to resolve.</param>
    /// <returns>The resolved function info, or null if not found.</returns>
    public FunctionInfo? ResolveFunction(string currentNamespace, string functionName)
    {
        // Check if function name includes namespace (e.g., "String.Equal")
        if (functionName.Contains('.'))
        {
            string[] parts = functionName.Split('.');
            if (parts.Length >= 2)
            {
                string ns = string.Join(".", parts.Take(parts.Length - 1));
                string name = parts[^1];

                // Try to resolve the namespace
                string resolvedNamespace = ResolveNamespace(ns);

                if (_namespaces.TryGetValue(resolvedNamespace, out NamespaceInfo? namespaceInfo))
                {
                    return namespaceInfo.Functions.TryGetValue(name, out FunctionInfo? func) ? func : null;
                }
            }
        }

        // Search in current namespace first
        if (_namespaces.TryGetValue(currentNamespace, out NamespaceInfo? currentNs))
        {
            if (currentNs.Functions.TryGetValue(functionName, out FunctionInfo? func))
            {
                return func;
            }
        }

        // Search in imported namespaces
        IEnumerable<string> importedNamespaces = GetImportedNamespaces(currentNamespace);
        foreach (string importedNs in importedNamespaces)
        {
            if (_namespaces.TryGetValue(importedNs, out NamespaceInfo? nsInfo))
            {
                if (nsInfo.Functions.TryGetValue(functionName, out FunctionInfo? importedFunc))
                {
                    return importedFunc;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Registers a namespace with its attributes and functions.
    /// </summary>
    /// <param name="namespaceInfo">The namespace information to register.</param>
    public void RegisterNamespace(NamespaceInfo namespaceInfo)
    {
        _namespaces.AddOrUpdate(namespaceInfo.Name, namespaceInfo, (_, _) => namespaceInfo);
    }

    /// <summary>
    /// Processes import statements in a namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace containing imports.</param>
    /// <param name="imports">The import statements to process.</param>
    public void ProcessImports(string namespaceName, List<Import> imports)
    {
        if (!_namespaces.TryGetValue(namespaceName, out NamespaceInfo? nsInfo))
        {
            return; // Namespace not found
        }

        nsInfo.Imports.Clear();

        foreach (Import import in imports)
        {
            ProcessImport(nsInfo, import);
        }
    }

    /// <summary>
    /// Validates that all attribute references in a policy are properly imported.
    /// </summary>
    /// <param name="currentNamespace">The current namespace.</param>
    /// <param name="attributeName">The attribute name to validate.</param>
    /// <returns>True if the attribute is properly imported, false otherwise.</returns>
    public bool ValidateAttributeImport(string currentNamespace, string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            return false;
        }

        string reference = attributeName.Trim();
        AttributeInfo? attribute = ResolveAttribute(currentNamespace, reference);
        if (attribute is null)
        {
            return false;
        }

        if (string.Equals(attribute.FullNamespace, currentNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsFullyQualifiedReference(reference, attribute.FullNamespace))
        {
            return true;
        }

        bool imported = IsAttributeImported(currentNamespace, attribute.Name, attribute.FullNamespace);

        return imported;
    }

    /// <summary>
    /// Validates that all function references in a policy are properly imported.
    /// </summary>
    /// <param name="currentNamespace">The current namespace.</param>
    /// <param name="functionName">The function name to validate.</param>
    /// <returns>True if the function is properly imported, false otherwise.</returns>
    public bool ValidateFunctionImport(string currentNamespace, string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return false;
        }

        string reference = functionName.Trim();
        FunctionInfo? function = ResolveFunction(currentNamespace, reference);
        return function is not null && (string.Equals(function.FullNamespace, currentNamespace, StringComparison.OrdinalIgnoreCase) || IsFullyQualifiedReference(reference, function.FullNamespace) || IsFunctionImported(currentNamespace, function.Name, function.FullNamespace));
    }

    /// <summary>
    /// Gets all imported namespaces for a given namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace to get imports for.</param>
    /// <returns>A collection of imported namespace names.</returns>
    private IEnumerable<string> GetImportedNamespaces(string namespaceName)
    {
        HashSet<string> imported = [];

        if (_namespaces.TryGetValue(namespaceName, out NamespaceInfo? nsInfo))
        {
            foreach (ImportDeclaration import in nsInfo.Imports)
            {
                imported.Add(import.NamespacePath);
            }
        }

        return imported;
    }

    /// <summary>
    /// Processes a single import statement.
    /// </summary>
    /// <param name="namespaceInfo">The namespace containing the import.</param>
    /// <param name="import">The import statement to process.</param>
    private void ProcessImport(NamespaceInfo namespaceInfo, Import import)
    {
        string targetNamespaceName = import.NamespacePath;
        string? importedMember = null;

        if (!import.Wildcard)
        {
            int lastDotIndex = import.NamespacePath.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                targetNamespaceName = import.NamespacePath[..lastDotIndex];
                importedMember = import.NamespacePath[(lastDotIndex + 1)..];
            }
        }

        if (!_namespaces.TryGetValue(targetNamespaceName, out NamespaceInfo? targetNamespace))
        {
            return; // Namespace not found
        }

        var importDeclaration = new ImportDeclaration
        {
            NamespacePath = targetNamespaceName,
            Wildcard = import.Wildcard
        };

        if (import.Wildcard)
        {
            foreach (string attr in targetNamespace.Attributes.Keys)
            {
                importDeclaration.ImportedItems.Add($"Attribute:{attr}");
            }

            foreach (string func in targetNamespace.Functions.Keys)
            {
                importDeclaration.ImportedItems.Add($"Function:{func}");
            }
        }
        else if (!string.IsNullOrEmpty(importedMember))
        {
            if (targetNamespace.Attributes.ContainsKey(importedMember))
            {
                importDeclaration.ImportedItems.Add($"Attribute:{importedMember}");
            }

            if (targetNamespace.Functions.ContainsKey(importedMember))
            {
                importDeclaration.ImportedItems.Add($"Function:{importedMember}");
            }
        }

        if (!importDeclaration.Wildcard && importDeclaration.ImportedItems.Count == 0)
        {
            return;
        }

        namespaceInfo.Imports.Add(importDeclaration);
    }

    private bool IsAttributeImported(string currentNamespace, string attributeName, string attributeNamespace)
    {
        if (!_namespaces.TryGetValue(currentNamespace, out NamespaceInfo? nsInfo))
        {
            return false;
        }

        const string attributePrefix = "Attribute:";

        foreach (ImportDeclaration import in nsInfo.Imports)
        {
            if (!string.Equals(import.NamespacePath, attributeNamespace, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (import.Wildcard)
            {
                return true;
            }

            foreach (string item in import.ImportedItems)
            {
                if (item.StartsWith(attributePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string importedName = item[attributePrefix.Length..];
                    if (string.Equals(importedName, attributeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsFunctionImported(string currentNamespace, string functionName, string functionNamespace)
    {
        if (!_namespaces.TryGetValue(currentNamespace, out NamespaceInfo? nsInfo))
        {
            return false;
        }

        const string functionPrefix = "Function:";

        foreach (ImportDeclaration import in nsInfo.Imports)
        {
            if (!string.Equals(import.NamespacePath, functionNamespace, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (import.Wildcard)
            {
                return true;
            }

            foreach (string item in import.ImportedItems)
            {
                if (item.StartsWith(functionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string importedName = item[functionPrefix.Length..];
                    if (string.Equals(importedName, functionName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsFullyQualifiedReference(string reference, string fullNamespace)
    {
        return reference.StartsWith(fullNamespace + ".", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads Oasis namespaces from ALFA files embedded in the assembly.
    /// </summary>
    private void LoadOasisNamespaces()
    {
        try
        {
            LoadNamespaceFromResource("ABACore.Resources.Oasis.Attributes.alfa");
            LoadNamespaceFromResource("ABACore.Resources.Oasis.Functions.alfa");
        }
        catch
        {
            // Fallback to hardcoded namespaces if resource loading fails
            CreateFallbackOasisNamespaces();
        }
    }

    /// <summary>
    /// Loads a namespace from an embedded ALFA resource file.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource.</param>
    private void LoadNamespaceFromResource(string resourceName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return;
        }

        using var reader = new System.IO.StreamReader(stream);
        string alfaContent = reader.ReadToEnd();

        // Parse the ALFA content and register namespaces
        ParseAndRegisterNamespace(alfaContent);
    }

    /// <summary>
    /// Parses ALFA content and registers all namespaces found.
    /// </summary>
    /// <param name="alfaContent">The ALFA content to parse.</param>
    private void ParseAndRegisterNamespace(string alfaContent)
    {
        // This would need to use the same parsing logic as PolicyEngine
        // For now, we'll keep the fallback implementation
        CreateFallbackOasisNamespaces();
    }

    /// <summary>
    /// Creates fallback Oasis namespaces (used when resource loading fails).
    /// </summary>
    private void CreateFallbackOasisNamespaces()
    {
        CreateOasisAttributesNamespace();
        CreateOasisFunctionsNamespace();
    }

    /// <summary>
    /// Creates the Oasis.Attributes namespace with standard attributes.
    /// </summary>
    private void CreateOasisAttributesNamespace()
    {
        // Subject namespace
        var subjectNamespace = new NamespaceInfo
        {
            Name = "Oasis.Attributes.Subject"
        };

        subjectNamespace.Attributes["Id"] = new AttributeInfo
        {
            Name = "Id",
            FullNamespace = "Oasis.Attributes.Subject",
            Type = AttributeType.String,
            Category = "subject",
            Id = "urn:oasis:names:tc:xacml:1.0:subject:subject-id"
        };

        subjectNamespace.Attributes["Role"] = new AttributeInfo
        {
            Name = "Role",
            FullNamespace = "Oasis.Attributes.Subject",
            Type = AttributeType.String,
            Category = "subject",
            Id = "urn:oasis:names:tc:xacml:1.0:subject:role"
        };

        subjectNamespace.Attributes["Name"] = new AttributeInfo
        {
            Name = "Name",
            FullNamespace = "Oasis.Attributes.Subject",
            Type = AttributeType.String,
            Category = "subject",
            Id = "urn:oasis:names:tc:xacml:1.0:subject:name"
        };

        subjectNamespace.Attributes["Clearance"] = new AttributeInfo
        {
            Name = "Clearance",
            FullNamespace = "Oasis.Attributes.Subject",
            Type = AttributeType.String,
            Category = "subject",
            Id = "urn:oasis:names:tc:xacml:1.0:subject:clearance"
        };

        _namespaces.TryAdd("Oasis.Attributes.Subject", subjectNamespace);

        // Resource namespace
        var resourceNamespace = new NamespaceInfo
        {
            Name = "Oasis.Attributes.Resource"
        };

        resourceNamespace.Attributes["Id"] = new AttributeInfo
        {
            Name = "Id",
            FullNamespace = "Oasis.Attributes.Resource",
            Type = AttributeType.String,
            Category = "resource",
            Id = "urn:oasis:names:tc:xacml:1.0:resource:resource-id"
        };

        resourceNamespace.Attributes["Type"] = new AttributeInfo
        {
            Name = "Type",
            FullNamespace = "Oasis.Attributes.Resource",
            Type = AttributeType.String,
            Category = "resource",
            Id = "urn:oasis:names:tc:xacml:1.0:resource:resource-type"
        };

        resourceNamespace.Attributes["Classification"] = new AttributeInfo
        {
            Name = "Classification",
            FullNamespace = "Oasis.Attributes.Resource",
            Type = AttributeType.String,
            Category = "resource",
            Id = "urn:oasis:names:tc:xacml:1.0:resource:classification"
        };

        _namespaces.TryAdd("Oasis.Attributes.Resource", resourceNamespace);

        // Action namespace
        var actionNamespace = new NamespaceInfo
        {
            Name = "Oasis.Attributes.Action"
        };

        actionNamespace.Attributes["Id"] = new AttributeInfo
        {
            Name = "Id",
            FullNamespace = "Oasis.Attributes.Action",
            Type = AttributeType.String,
            Category = "action",
            Id = "urn:oasis:names:tc:xacml:1.0:action:action-id"
        };

        actionNamespace.Attributes["Name"] = new AttributeInfo
        {
            Name = "Name",
            FullNamespace = "Oasis.Attributes.Action",
            Type = AttributeType.String,
            Category = "action",
            Id = "urn:oasis:names:tc:xacml:1.0:action:action-name"
        };

        _namespaces.TryAdd("Oasis.Attributes.Action", actionNamespace);

        // Environment namespace
        var environmentNamespace = new NamespaceInfo
        {
            Name = "Oasis.Attributes.Environment"
        };

        environmentNamespace.Attributes["CurrentTime"] = new AttributeInfo
        {
            Name = "CurrentTime",
            FullNamespace = "Oasis.Attributes.Environment",
            Type = AttributeType.Time,
            Category = "environment",
            Id = "urn:oasis:names:tc:xacml:1.0:environment:current-time"
        };

        environmentNamespace.Attributes["CurrentDate"] = new AttributeInfo
        {
            Name = "CurrentDate",
            FullNamespace = "Oasis.Attributes.Environment",
            Type = AttributeType.Date,
            Category = "environment",
            Id = "urn:oasis:names:tc:xacml:1.0:environment:current-date"
        };

        environmentNamespace.Attributes["CurrentDateTime"] = new AttributeInfo
        {
            Name = "CurrentDateTime",
            FullNamespace = "Oasis.Attributes.Environment",
            Type = AttributeType.DateTime,
            Category = "environment",
            Id = "urn:oasis:names:tc:xacml:1.0:environment:current-dateTime"
        };

        _namespaces.TryAdd("Oasis.Attributes.Environment", environmentNamespace);
    }

    /// <summary>
    /// Creates the Oasis.Functions namespace with standard functions.
    /// </summary>
    private void CreateOasisFunctionsNamespace()
    {
        var stringFunctionsNamespace = new NamespaceInfo
        {
            Name = "Oasis.Functions.String"
        };

        // Add string functions with proper signatures
        stringFunctionsNamespace.Functions["Equal"] = new FunctionInfo
        {
            Name = "Equal",
            FullNamespace = "Oasis.Functions.String",
            Id = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
            Signatures =
            [
                new FunctionSignature
                {
                    Inputs =
                    [
                        new SimpleTypeArgument { Type = AttributeType.String },
                        new SimpleTypeArgument { Type = AttributeType.String }
                    ],
                    Output = new SimpleTypeArgument { Type = AttributeType.Boolean }
                }
            ]
        };

        stringFunctionsNamespace.Functions["Contains"] = new FunctionInfo
        {
            Name = "Contains",
            FullNamespace = "Oasis.Functions.String",
            Id = "urn:oasis:names:tc:xacml:1.0:function:string-contains",
            Signatures =
            [
                new FunctionSignature
                {
                    Inputs =
                    [
                        new SimpleTypeArgument { Type = AttributeType.String },
                        new SimpleTypeArgument { Type = AttributeType.String }
                    ],
                    Output = new SimpleTypeArgument { Type = AttributeType.Boolean }
                }
            ]
        };

        _namespaces.TryAdd("Oasis.Functions.String", stringFunctionsNamespace);

        var numericFunctionsNamespace = new NamespaceInfo
        {
            Name = "Oasis.Functions.Numeric"
        };

        numericFunctionsNamespace.Functions["Add"] = new FunctionInfo
        {
            Name = "Add",
            FullNamespace = "Oasis.Functions.Numeric",
            Id = "urn:oasis:names:tc:xacml:1.0:function:integer-add",
            Signatures =
            [
                new FunctionSignature
                {
                    Inputs =
                    [
                        new SimpleTypeArgument { Type = AttributeType.Integer },
                        new SimpleTypeArgument { Type = AttributeType.Integer }
                    ],
                    Output = new SimpleTypeArgument { Type = AttributeType.Integer }
                }
            ]
        };

        _namespaces.TryAdd("Oasis.Functions.Numeric", numericFunctionsNamespace);
    }
}
