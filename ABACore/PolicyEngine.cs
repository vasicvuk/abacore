using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using ABACore.Compilation;
using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Parser;
using ABACore.Runtime;
using ABACore.Serialization;
using ABACore.Visitor;
using Antlr4.Runtime;

namespace ABACore;

/// <summary>
/// Main entry point for the ALFA policy engine.
/// Provides methods to load, compile, store, and evaluate ALFA policies.
/// This class is thread-safe for evaluation operations.
/// </summary>
public sealed class PolicyEngine
{
    private readonly PolicyRepository _repository;
    private readonly IPolicyCompiler _compiler;
    private readonly PolicyCompiler? _roslynCompiler;
    private readonly ExpressionTreeCompiler? _expressionTreeCompiler;
    private readonly InterpreterCompiler? _interpreterCompiler;
    private readonly PolicyExecutor _executor;
    private readonly NamespaceResolver _namespaceResolver;
    private readonly PolicyValidator _validator;
    private readonly EvaluationConfiguration _configuration;
    private readonly Lock _loadLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyEngine"/> class with default configuration.
    /// Uses ExpressionTree compilation by default for best balance of performance and compatibility.
    /// </summary>
    public PolicyEngine() : this(EvaluationConfiguration.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyEngine"/> class with the specified configuration.
    /// The compilation strategy is specified in the configuration.
    /// </summary>
    /// <param name="configuration">The evaluation configuration (includes compilation strategy).</param>
    public PolicyEngine(EvaluationConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        CompilationStrategy = configuration.CompilationStrategy;
        _repository = new PolicyRepository();

        // Initialize the appropriate compiler based on strategy
        _compiler = CompilationStrategy switch
        {
            CompilationStrategy.Roslyn => _roslynCompiler = new PolicyCompiler(),
            CompilationStrategy.ExpressionTree => _expressionTreeCompiler = new ExpressionTreeCompiler(),
            CompilationStrategy.Interpreter => _interpreterCompiler = new InterpreterCompiler(),
            _ => _expressionTreeCompiler = new ExpressionTreeCompiler()
        };

        _executor = new PolicyExecutor();
        _namespaceResolver = new NamespaceResolver();
        _validator = new PolicyValidator(_namespaceResolver);
    }

    /// <summary>
    /// Gets the compilation strategy used by this engine.
    /// </summary>
    public CompilationStrategy CompilationStrategy { get; }

    /// <summary>
    /// Parses and compiles ALFA policies from text and stores them in the repository.
    /// ALWAYS loads ALL policies and policysets from the ALFA file (allowing them to reference each other).
    ///
    /// The storeId parameter is a storage context identifier (e.g., "tenant-123", "microservice-A") that isolates
    /// the entire policy set. When specified, all policies are stored in a separate storage context, allowing the
    /// same ALFA file to be loaded multiple times for different tenants or microservices.
    ///
    /// Example:
    /// - ALFA file contains: policy "accessPolicy" and policyset "securityPolicies"
    /// - LoadPolicy(alfaText, null): stored in default context as "accessPolicy" and "securityPolicies"
    /// - LoadPolicy(alfaText, "tenant-123"): stored in "tenant-123" context as "accessPolicy" and "securityPolicies"
    /// - LoadPolicy(alfaText, "tenant-456"): stored in "tenant-456" context as "accessPolicy" and "securityPolicies"
    ///
    /// To evaluate: engine.Evaluate("accessPolicy", context, storeId: "tenant-123")
    /// </summary>
    /// <param name="policyText">The ALFA policy text to parse and compile.</param>
    /// <param name="storeId">Optional storage context identifier (tenant ID, microservice ID, etc.) used to isolate this policy set.
    /// If not specified, uses the default storage context. This is independent of the policy names in the ALFA file.</param>
    /// <returns>The policy ID and version number of the first loaded policy.</returns>
    /// <exception cref="ArgumentException">Thrown when policyText is null or whitespace.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when policy compilation fails.</exception>
    public PolicyRegistration LoadPolicy(string policyText, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyText);

        lock (_loadLock)
        {
            // Parse namespace
            PolicyDocument parseResult = PolicyParser.Parse(policyText);
            Namespace namespaceInfo = parseResult.Namespace ?? new Namespace { Name = "Default", Statements = [] };

            // Validate the namespace
            ValidationResult validationResult = _validator.ValidateNamespace(namespaceInfo);
            if (!validationResult.IsValid)
            {
                throw new AlfaParseException($"Policy validation failed: {string.Join("; ", validationResult.Errors.Select(e => e.Message))}");
            }

            // Always load all policies from the namespace with optional storage context
            return LoadAllPoliciesFromNamespace(parseResult, namespaceInfo, storeId);
        }
    }

    /// <summary>
    /// Loads all policies and policysets from the namespace, allowing them to reference each other.
    /// </summary>
    /// <param name="storeId">Optional storage context ID (e.g., "tenant-123") to isolate this policy set.</param>
    private PolicyRegistration LoadAllPoliciesFromNamespace(PolicyDocument parseResult, Namespace namespaceInfo, string? storeId)
    {
        List<PolicyRegistration> results = [];

        // Load all policies from namespace statements first (they don't reference others)
        if (namespaceInfo.Statements?.Count > 0)
        {
            foreach (Statement statement in namespaceInfo.Statements)
            {
                if (statement is PolicyStatement policyStatement)
                {
                    Policy policy = policyStatement.Policy ?? throw new InvalidOperationException("PolicyStatement must contain a Policy");
                    CompiledPolicy compiled = CompilePolicy(policy, enableCaching: false);
                    string policyName = policy.Id ?? throw new InvalidOperationException("Policy must have an ID");
                    int version = _repository.AddPolicy(compiled, policyName, storeId);
                    results.Add(new PolicyRegistration(policyName, version));
                }
            }

            // Load all policysets (they may reference policies from first pass)
            foreach (Statement statement in namespaceInfo.Statements)
            {
                if (statement is PolicySetStatement policySetStatement)
                {
                    PolicySet policySet = policySetStatement.PolicySet ?? throw new InvalidOperationException("PolicySetStatement must contain a PolicySet");
                    CompileChildPoliciesAndPolicySets(policySet, namespaceInfo, storeId);
                    CompiledPolicy compiled = _compiler.CompilePolicySet(policySet, _repository, enableCaching: false);
                    string policySetName = policySet.Id ?? throw new InvalidOperationException("PolicySet must have an ID");
                    int version = _repository.AddPolicy(compiled, policySetName, storeId);
                    results.Add(new PolicyRegistration(policySetName, version));
                }
            }
        }

        // Handle top-level policy/policyset if present
        if (parseResult.Policy != null || parseResult.PolicySet != null)
        {
            PolicySet? topPolicySet = parseResult.PolicySet;
            Policy? topPolicy = parseResult.Policy;

            if (topPolicySet != null)
            {
                CompileChildPoliciesAndPolicySets(topPolicySet, namespaceInfo, storeId);
                CompiledPolicy compiled = _compiler.CompilePolicySet(topPolicySet, _repository, enableCaching: false);
                string policySetName = topPolicySet.Id ?? throw new InvalidOperationException("PolicySet must have an ID");
                int version = _repository.AddPolicy(compiled, policySetName, storeId);
                results.Add(new PolicyRegistration(policySetName, version));
            }
            else if (topPolicy != null)
            {
                CompiledPolicy compiled = CompilePolicy(topPolicy, enableCaching: false);
                string policyName = topPolicy.Id ?? throw new InvalidOperationException("Policy must have an ID");
                int version = _repository.AddPolicy(compiled, policyName, storeId);
                results.Add(new PolicyRegistration(policyName, version));
            }
        }

        if (results.Count == 0)
        {
            throw new AlfaParseException("No policies or policysets found in the parsed text.");
        }

        // Return the first registration (or could return a collection, but for backward compatibility return one)
        return results[0];
    }

    /// <summary>
    /// Loads a policy from a pre-parsed PolicyDocument.
    /// </summary>
    /// <param name="document">The parsed policy document.</param>
    /// <param name="storeId">Optional storage context ID.</param>
    /// <param name="skipValidation">Whether to skip validation.</param>
    /// <returns>The policy ID and version number.</returns>
    private PolicyRegistration LoadPolicyDocument(PolicyDocument document, string? storeId = null, bool skipValidation = false)
    {
        Namespace namespaceInfo = document.Namespace ?? new Namespace { Name = "Default", Statements = [] };
        Policy? policy = document.Policy;
        PolicySet? policySet = document.PolicySet;

        // Extract policy/policyset from namespace statements if not directly provided
        if (policy == null && policySet == null && namespaceInfo.Statements?.Count > 0)
        {
            foreach (Statement statement in namespaceInfo.Statements)
            {
                if (statement is PolicyStatement policyStatement)
                {
                    policy = policyStatement.Policy;
                    break;
                }
                else if (statement is PolicySetStatement policySetStatement)
                {
                    policySet = policySetStatement.PolicySet;
                    break;
                }
            }
        }

        // Validate the namespace and policy for proper imports
        // Skip validation for JSON-loaded policies as they don't have import statements
        if (!skipValidation)
        {
            ValidationResult validationResult = _validator.ValidateNamespace(namespaceInfo);
            if (!validationResult.IsValid)
            {
                throw new AlfaParseException($"Policy validation failed: {string.Join("; ", validationResult.Errors.Select(e => e.Message))}");
            }
        }

        CompiledPolicy compiledPolicy;
        string? extractedId;

        if (policySet != null)
        {
            // Compile all child policies/policysets first
            CompileChildPoliciesAndPolicySets(policySet, namespaceInfo, storeId);

            // Compile the policyset
            compiledPolicy = _compiler.CompilePolicySet(policySet, _repository, enableCaching: false);
            extractedId = policySet.Id;
        }
        else if (policy != null)
        {
            // Compile the policy
            compiledPolicy = CompilePolicy(policy, enableCaching: false);
            extractedId = policy.Id;
        }
        else
        {
            throw new AlfaParseException("No policy or policyset found in the parsed document.");
        }

        string effectivePolicyId = compiledPolicy.PolicyId ?? extractedId
            ?? throw new InvalidOperationException("Policy must have an ID.");

        int version = _repository.AddPolicy(compiledPolicy, effectivePolicyId, storeId);

        return new PolicyRegistration(effectivePolicyId, version);
    }

    /// <summary>
    /// Parses and compiles an ALFA policy from JSON format and stores it in the repository.
    /// </summary>
    /// <param name="jsonPolicyText">The JSON policy text to parse and compile.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The policy ID and version number.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonPolicyText is null or whitespace.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when policy compilation fails.</exception>
    public PolicyRegistration LoadPolicyFromJson(string jsonPolicyText, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPolicyText);

        lock (_loadLock)
        {
            // Parse JSON to PolicyDocument
            PolicyDocument document = PolicyJsonConverter.FromJson(jsonPolicyText);
            // Skip validation for JSON-loaded policies as they don't have import statements
            return LoadPolicyDocument(document, storeId, skipValidation: true);
        }
    }

    /// <summary>
    /// Parses and compiles an ALFA policy from a JSON file and stores it in the repository.
    /// </summary>
    /// <param name="jsonFilePath">The path to the JSON file containing the policy.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The policy ID and version number.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonFilePath is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when policy compilation fails.</exception>
    public PolicyRegistration LoadPolicyFromJsonFile(string jsonFilePath, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonFilePath);

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"JSON policy file not found: {jsonFilePath}");
        }

        string jsonText = File.ReadAllText(jsonFilePath);
        return LoadPolicyFromJson(jsonText, storeId);
    }

    /// <summary>
    /// Parses and compiles multiple ALFA policies from JSON text and stores them in the repository.
    /// </summary>
    /// <param name="jsonPolicyTexts">The collection of JSON policy texts to parse and compile.</param>
    /// <returns>A collection of policy IDs and their version numbers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when jsonPolicyTexts is null.</exception>
    /// <exception cref="AlfaParseException">Thrown when any policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when any policy compilation fails.</exception>
    public IEnumerable<PolicyRegistration> LoadPoliciesFromJson(IEnumerable<string> jsonPolicyTexts)
    {
        ArgumentNullException.ThrowIfNull(jsonPolicyTexts);

        List<PolicyRegistration> results = [];

        foreach (string jsonPolicyText in jsonPolicyTexts)
        {
            results.Add(LoadPolicyFromJson(jsonPolicyText));
        }

        return results;
    }

    /// <summary>
    /// Converts an ALFA policy text to JSON format.
    /// </summary>
    /// <param name="alfaPolicyText">The ALFA policy text to convert.</param>
    /// <returns>The JSON representation of the policy.</returns>
    /// <exception cref="ArgumentException">Thrown when alfaPolicyText is null or whitespace.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    public string ConvertAlfaToJson(string alfaPolicyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alfaPolicyText);
        return PolicyJsonConverter.JsonFromAlfa(alfaPolicyText);
    }

    /// <summary>
    /// Converts a JSON policy text to ALFA format.
    /// </summary>
    /// <param name="jsonPolicyText">The JSON policy text to convert.</param>
    /// <returns>The ALFA representation of the policy.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonPolicyText is null or whitespace.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    public string ConvertJsonToAlfa(string jsonPolicyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPolicyText);
        return PolicyJsonConverter.AlfaFromJson(jsonPolicyText);
    }

    /// <summary>
    /// Parses and compiles multiple ALFA policies from text and stores them in the repository.
    /// </summary>
    /// <param name="policyTexts">The collection of ALFA policy texts to parse and compile.</param>
    /// <returns>A collection of policy IDs and their version numbers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policyTexts is null.</exception>
    /// <exception cref="AlfaParseException">Thrown when any policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when any policy compilation fails.</exception>
    public IEnumerable<PolicyRegistration> LoadPolicies(IEnumerable<string> policyTexts)
    {
        ArgumentNullException.ThrowIfNull(policyTexts);

        List<PolicyRegistration> results = [];

        foreach (string policyText in policyTexts)
        {
            results.Add(LoadPolicy(policyText));
        }

        return results;
    }

    /// <summary>
    /// Loads and compiles an ALFA policy from a file.
    /// </summary>
    /// <param name="filePath">The path to the ALFA policy file.</param>
    /// <param name="policyId">Optional policy ID override.</param>
    /// <returns>The policy ID and version number.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="AlfaParseException">Thrown when policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when policy compilation fails.</exception>
    public PolicyRegistration LoadPolicyFromFile(string filePath, string? policyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Policy file not found: {filePath}", filePath);
        }

        string policyText = File.ReadAllText(filePath);
        return LoadPolicy(policyText, policyId);
    }

    /// <summary>
    /// Loads and compiles all ALFA policy files from a directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory containing ALFA policy files.</param>
    /// <param name="pattern">The file pattern to match. Defaults to "*.alfa".</param>
    /// <param name="recursive">Whether to search subdirectories recursively. Defaults to false.</param>
    /// <returns>A collection of policy IDs and their version numbers.</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    /// <exception cref="AlfaParseException">Thrown when any policy parsing fails.</exception>
    /// <exception cref="AlfaCompilationException">Thrown when any policy compilation fails.</exception>
    public IEnumerable<PolicyRegistration> LoadPoliciesFromDirectory(
        string directoryPath,
        string pattern = "*.alfa",
        bool recursive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Policy directory not found: {directoryPath}");
        }

        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(directoryPath, pattern, searchOption);

        List<PolicyRegistration> results = [];

        foreach (string file in files)
        {
            try
            {
                results.Add(LoadPolicyFromFile(file));
            }
            catch (Exception ex) when (ex is AlfaParseException or AlfaCompilationException)
            {
                throw new AlfaException($"Failed to load policy from file '{file}': {ex.Message}", ex);
            }
        }

        return results;
    }

    /// <summary>
    /// Evaluates a policy by ID using the provided evaluation context.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="context">The evaluation context containing attribute values.</param>
    /// <param name="version">Optional version number. If not specified, uses the latest version.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>The policy decision.</returns>
    /// <exception cref="ArgumentException">Thrown when policyId is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="PolicyNotFoundException">Thrown when the policy is not found.</exception>
    /// <exception cref="AlfaEvaluationException">Thrown when policy evaluation fails.</exception>
    public Decision Evaluate(string policyId, EvaluationContext context, int? version = null, string? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
        ArgumentNullException.ThrowIfNull(context);

        CompiledPolicy? policy = _repository.GetPolicy(policyId, version, storeId);

        if (policy == null)
        {
            string versionInfo = version.HasValue ? $" version {version.Value}" : " (latest version)";
            string storeInfo = storeId != null ? $" in store '{storeId}'" : "";
            throw new PolicyNotFoundException(policyId, $"Policy '{policyId}'{versionInfo}{storeInfo} was not found in the repository.");
        }

        return _executor.Execute(policy, context);
    }

    /// <summary>
    /// Evaluates multiple policies using the specified combining algorithm.
    /// </summary>
    /// <param name="policyIds">The collection of policy identifiers to evaluate.</param>
    /// <param name="context">The evaluation context containing attribute values.</param>
    /// <param name="combiningAlgorithm">The combining algorithm to use. Defaults to DenyOverrides.</param>
    /// <returns>The combined policy decision.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policyIds or context is null.</exception>
    /// <exception cref="ArgumentException">Thrown when policyIds is empty.</exception>
    /// <exception cref="PolicyNotFoundException">Thrown when any policy is not found.</exception>
    /// <exception cref="AlfaEvaluationException">Thrown when policy evaluation fails.</exception>
    public Decision EvaluatePolicySet(
        IEnumerable<string> policyIds,
        EvaluationContext context,
        CombiningAlgorithm combiningAlgorithm = CombiningAlgorithm.DenyOverrides)
    {
        ArgumentNullException.ThrowIfNull(policyIds);
        ArgumentNullException.ThrowIfNull(context);

        var policyIdList = policyIds.ToList();

        if (policyIdList.Count == 0)
        {
            throw new ArgumentException("Policy ID collection cannot be empty.", nameof(policyIds));
        }

        List<CompiledPolicy> policies = [];

        foreach (string policyId in policyIdList)
        {
            CompiledPolicy? policy = _repository.GetPolicy(policyId) ?? throw new PolicyNotFoundException(policyId);
            policies.Add(policy);
        }

        return _executor.ExecutePolicySet(policies, context, combiningAlgorithm);
    }

    /// <summary>
    /// Gets a list of all loaded policy IDs for a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of policy identifiers.</returns>
    public IEnumerable<string> GetLoadedPolicies(string? storeId = null)
    {
        return _repository.ListPolicies(storeId);
    }

    /// <summary>
    /// Gets metadata for all loaded policies in a specific store.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of policy metadata.</returns>
    public IEnumerable<PolicyMetadata> GetPolicyMetadata(string? storeId = null)
    {
        return _repository.GetPolicyMetadata(storeId);
    }

    /// <summary>
    /// Gets all versions of a specific policy.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>A collection of version numbers.</returns>
    public IEnumerable<int> GetPolicyVersions(string policyId, string? storeId = null)
    {
        return _repository.ListVersions(policyId, storeId);
    }

    /// <summary>
    /// Checks if a policy exists in the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="version">Optional version number.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>true if the policy exists; otherwise, false.</returns>
    public bool ContainsPolicy(string policyId, int? version = null, string? storeId = null)
    {
        return _repository.ContainsPolicy(policyId, version, storeId);
    }

    /// <summary>
    /// Removes a policy from the repository.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="version">Optional version number. If not specified, removes all versions.</param>
    /// <param name="storeId">Optional storage context ID. If not specified, uses the default store.</param>
    /// <returns>true if the policy was removed; otherwise, false.</returns>
    public bool RemovePolicy(string policyId, int? version = null, string? storeId = null)
    {
        return _repository.RemovePolicy(policyId, version, storeId);
    }

    /// <summary>
    /// Clears all policies from the repository.
    /// </summary>
    /// <param name="storeId">Optional storage context ID. If not specified, clears all stores.</param>
    public void Clear(string? storeId = null)
    {
        _repository.Clear(storeId);
        if (storeId == null)
        {
            _roslynCompiler?.ClearCache();
            _expressionTreeCompiler?.ClearCache();
            _interpreterCompiler?.ClearCache();
        }
    }

    /// <summary>
    /// Lists all storage context IDs (stores) in the repository.
    /// </summary>
    /// <returns>A collection of store identifiers.</returns>
    public IEnumerable<string> GetStores()
    {
        return _repository.ListStores();
    }

    /// <summary>
    /// Parses ALFA namespace and policy text into AST nodes.
    /// </summary>
    /// <param name="policyText">The ALFA policy text to parse.</param>
    /// <returns>An object containing the namespace and policy AST nodes.</returns>
    /// <exception cref="AlfaParseException">Thrown when parsing fails.</exception>
    private CompiledPolicy CompilePolicy(Policy policy, bool enableCaching)
    {
        return CompilationStrategy switch
        {
            CompilationStrategy.Roslyn => _roslynCompiler!.CompilePolicy(policy, enableCaching),
            CompilationStrategy.ExpressionTree => _expressionTreeCompiler!.CompilePolicy(policy, enableCaching),
            CompilationStrategy.Interpreter => _interpreterCompiler!.CompilePolicy(policy, enableCaching),
            _ => _expressionTreeCompiler!.CompilePolicy(policy, enableCaching)
        };
    }

    private void CompileChildPoliciesAndPolicySets(PolicySet policySet, Namespace namespaceInfo, string? storeId)
    {
        foreach (PolicySetElement element in policySet.Elements)
        {
            if (element is Policy childPolicy)
            {
                // Compile and store the child policy
                CompiledPolicy compiledChild = CompilePolicy(childPolicy, enableCaching: false);
                string childId = childPolicy.Id ?? throw new InvalidOperationException("Child policy must have an ID");
                _repository.AddPolicy(compiledChild, childId, storeId);
            }
            else if (element is PolicySet childPolicySet)
            {
                // Recursively compile child policyset
                CompileChildPoliciesAndPolicySets(childPolicySet, namespaceInfo, storeId);
                CompiledPolicy compiledChild = _compiler.CompilePolicySet(childPolicySet, _repository, enableCaching: false);
                string childId = childPolicySet.Id ?? throw new InvalidOperationException("Child policyset must have an ID");
                _repository.AddPolicy(compiledChild, childId, storeId);
            }
            // PolicyReference elements are already in the repository, no need to compile
        }
    }
}

