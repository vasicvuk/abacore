using System.Collections.Generic;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Compilation;

/// <summary>
/// Validates ALFA policies for proper attribute and function imports.
/// Ensures compliance with ALFA specification requirements.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PolicyValidator"/> class.
/// </remarks>
/// <param name="namespaceResolver">The namespace resolver to use for validation.</param>
public sealed class PolicyValidator(NamespaceResolver namespaceResolver)
{
    private readonly NamespaceResolver _namespaceResolver = namespaceResolver ?? throw new ArgumentNullException(nameof(namespaceResolver));

    /// <summary>
    /// Validates a namespace and all its statements.
    /// </summary>
    /// <param name="namespace">The namespace to validate.</param>
    /// <returns>A validation result containing any errors found.</returns>
    public ValidationResult ValidateNamespace(Namespace @namespace)
    {
        var errors = new List<ValidationError>();
        string currentNamespace = @namespace.Name;

        var imports = new List<Import>();
        var declaredAttributes = new Dictionary<string, AttributeDeclaration>();
        var declaredFunctions = new Dictionary<string, FunctionDeclaration>();

        foreach (Statement statement in @namespace.Statements)
        {
            switch (statement)
            {
                case Import import:
                    imports.Add(import);
                    break;
                case AttributeDeclaration attr:
                    declaredAttributes[attr.Name] = attr;
                    break;
                case FunctionDeclaration func:
                    declaredFunctions[func.Name] = func;
                    break;
            }
        }

        RegisterNamespaceWithDeclarations(currentNamespace, declaredAttributes, declaredFunctions);
        _namespaceResolver.ProcessImports(currentNamespace, imports);

        foreach (Statement statement in @namespace.Statements)
        {
            ValidateNodeForUnimportedReferences(statement, currentNamespace, errors);
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates a node for unimported references.
    /// </summary>
    /// <param name="node">The node to validate.</param>
    /// <param name="currentNamespace">The current namespace.</param>
    /// <param name="errors">The errors list to add to.</param>
    private void ValidateNodeForUnimportedReferences(AlfaNode node, string currentNamespace, List<ValidationError> errors)
    {
        switch (node)
        {
            case PolicyStatement policyStatement:
                ValidatePolicyForUnimportedReferences(policyStatement.Policy, currentNamespace, errors);
                break;
            case PolicySetStatement policySetStatement:
                ValidatePolicySetForUnimportedReferences(policySetStatement.PolicySet, currentNamespace, errors);
                break;
            case Policy policy:
                ValidatePolicyForUnimportedReferences(policy, currentNamespace, errors);
                break;
            case PolicySet policySet:
                ValidatePolicySetForUnimportedReferences(policySet, currentNamespace, errors);
                break;
        }
    }

    /// <summary>
    /// Validates a policy for unimported references.
    /// </summary>
    private void ValidatePolicyForUnimportedReferences(Policy policy, string currentNamespace, List<ValidationError> errors)
    {
        // Validate target
        if (policy.Target != null)
        {
            foreach (Clause clause in policy.Target.Clauses)
            {
                ValidateBooleanExpressionForUnimportedReferences(clause.Expression, currentNamespace, errors);
            }
        }

        // Validate rules
        foreach (Rule rule in policy.Rules)
        {
            ValidateRuleForUnimportedReferences(rule, currentNamespace, errors);
        }
    }

    /// <summary>
    /// Validates a rule for unimported references.
    /// </summary>
    private void ValidateRuleForUnimportedReferences(Rule rule, string currentNamespace, List<ValidationError> errors)
    {
        // Validate target
        if (rule.Target != null)
        {
            foreach (Clause clause in rule.Target.Clauses)
            {
                ValidateBooleanExpressionForUnimportedReferences(clause.Expression, currentNamespace, errors);
            }
        }

        // Validate condition
        if (rule.Condition != null)
        {
            ValidateBooleanExpressionForUnimportedReferences(rule.Condition.Expression, currentNamespace, errors);
        }
    }

    /// <summary>
    /// Validates a policy set for unimported references.
    /// </summary>
    private void ValidatePolicySetForUnimportedReferences(PolicySet policySet, string currentNamespace, List<ValidationError> errors)
    {
        // Validate target
        if (policySet.Target != null)
        {
            foreach (Clause clause in policySet.Target.Clauses)
            {
                ValidateBooleanExpressionForUnimportedReferences(clause.Expression, currentNamespace, errors);
            }
        }

        // Validate elements
        foreach (PolicySetElement element in policySet.Elements)
        {
            ValidateNodeForUnimportedReferences(element, currentNamespace, errors);
        }
    }

    /// <summary>
    /// Validates a boolean expression for unimported references.
    /// </summary>
    private void ValidateBooleanExpressionForUnimportedReferences(BooleanExpression expression, string currentNamespace, List<ValidationError> errors)
    {
        switch (expression)
        {
            case BooleanAttributeDesignator attr:
                // Construct the attribute reference (with namespace prefix if provided)
                string attrReference = string.IsNullOrEmpty(attr.Namespace)
                    ? attr.AttributeName
                    : $"{attr.Namespace}.{attr.AttributeName}";

                if (!_namespaceResolver.ValidateAttributeImport(currentNamespace, attrReference))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Attribute '{attrReference}' is not imported in namespace '{currentNamespace}'",
                        Severity = ValidationSeverity.Error
                    });
                }
                break;

            case BooleanFunctionCall func:
                if (!_namespaceResolver.ValidateFunctionImport(currentNamespace, func.FunctionName))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Function '{func.FunctionName}' is not imported in namespace '{currentNamespace}'",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Validate function parameters
                foreach (Expression param in func.Parameters)
                {
                    ValidateExpressionForUnimportedReferences(param, currentNamespace, errors);
                }
                break;

            case NotExpression not:
                ValidateBooleanExpressionForUnimportedReferences(not.InnerExpression, currentNamespace, errors);
                break;

            case LogicalBinaryExpression logical:
                ValidateBooleanExpressionForUnimportedReferences(logical.Left, currentNamespace, errors);
                ValidateBooleanExpressionForUnimportedReferences(logical.Right, currentNamespace, errors);
                break;

            case ComparisonExpression comparison:
                ValidateExpressionForUnimportedReferences(comparison.Left, currentNamespace, errors);
                ValidateExpressionForUnimportedReferences(comparison.Right, currentNamespace, errors);
                break;
        }
    }

    /// <summary>
    /// Validates an expression for unimported references.
    /// </summary>
    private void ValidateExpressionForUnimportedReferences(Expression expression, string currentNamespace, List<ValidationError> errors)
    {
        switch (expression)
        {
            case AttributeDesignator attr:
                // Construct the attribute reference (with namespace prefix if provided)
                string attrRef = string.IsNullOrEmpty(attr.Namespace)
                    ? attr.AttributeName
                    : $"{attr.Namespace}.{attr.AttributeName}";

                if (!_namespaceResolver.ValidateAttributeImport(currentNamespace, attrRef))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Attribute '{attrRef}' is not imported in namespace '{currentNamespace}'",
                        Severity = ValidationSeverity.Error
                    });
                }
                break;

            case FunctionCall func:
                if (!_namespaceResolver.ValidateFunctionImport(currentNamespace, func.FunctionName))
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Function '{func.FunctionName}' is not imported in namespace '{currentNamespace}'",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Validate function parameters
                foreach (Expression param in func.Parameters)
                {
                    ValidateExpressionForUnimportedReferences(param, currentNamespace, errors);
                }
                break;

            case AllExpression all:
                ValidateExpressionForUnimportedReferences(all.InnerExpression, currentNamespace, errors);
                break;

            case BinaryExpression binary:
                ValidateExpressionForUnimportedReferences(binary.Left, currentNamespace, errors);
                ValidateExpressionForUnimportedReferences(binary.Right, currentNamespace, errors);
                break;
        }
    }

    /// <summary>
    /// Registers a namespace with its declared attributes and functions.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="attributes">The declared attributes.</param>
    /// <param name="functions">The declared functions.</param>
    private void RegisterNamespaceWithDeclarations(
        string namespaceName,
        Dictionary<string, AttributeDeclaration> attributes,
        Dictionary<string, FunctionDeclaration> functions)
    {
        var namespaceInfo = new NamespaceInfo { Name = namespaceName };

        // Register attributes
        foreach (KeyValuePair<string, AttributeDeclaration> kvp in attributes)
        {
            AttributeDeclaration attr = kvp.Value;
            namespaceInfo.Attributes[kvp.Key] = new AttributeInfo
            {
                Name = attr.Name,
                FullNamespace = namespaceName,
                Type = attr.Type ?? AttributeType.String,
                Category = attr.Category,
                Id = attr.Id
            };
        }

        // Register functions
        foreach (KeyValuePair<string, FunctionDeclaration> kvp in functions)
        {
            FunctionDeclaration func = kvp.Value;
            namespaceInfo.Functions[kvp.Key] = new FunctionInfo
            {
                Name = func.Name,
                FullNamespace = namespaceName,
                Signatures = func.Signatures,
                Id = func.Id
            };
        }

        _namespaceResolver.RegisterNamespace(namespaceInfo);
    }
}

/// <summary>
/// Represents the result of policy validation.
/// </summary>
public sealed class ValidationResult(List<ValidationError> errors)
{
    public List<ValidationError> Errors { get; } = errors ?? throw new ArgumentNullException(nameof(errors));

    /// <summary>
    /// Gets a value indicating whether the validation passed without errors.
    /// </summary>
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    public bool HasWarnings => Errors.Any(e => e.Severity == ValidationSeverity.Warning);
}

/// <summary>
/// Represents a validation error or warning.
/// </summary>
public sealed class ValidationError
{
    public required string Message { get; init; }
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public string? Location { get; init; }
}

/// <summary>
/// Represents the severity of a validation message.
/// </summary>
public enum ValidationSeverity
{
    Warning,
    Error
}
