using System.Text;
using ABACore.Models;

namespace ABACore.Serialization;

/// <summary>
/// Provides formatting utilities for converting ALFA AST objects back to ALFA text.
/// </summary>
public static class AlfaFormatter
{
    /// <summary>
    /// Formats a PolicyDocument into ALFA text.
    /// </summary>
    /// <param name="document">The policy document to format.</param>
    /// <returns>The ALFA text representation.</returns>
    public static string Format(PolicyDocument document)
    {
        StringBuilder sb = new();

        // Check if policy/policyset is already in namespace statements
        bool policyInNamespace = false;
        if (document.Namespace?.Statements != null)
        {
            foreach (Statement stmt in document.Namespace.Statements)
            {
                if ((stmt is PolicyStatement && document.Policy != null) ||
                    (stmt is PolicySetStatement && document.PolicySet != null))
                {
                    policyInNamespace = true;
                    break;
                }
            }
        }

        // Format namespace
        if (document.Namespace != null)
        {
            FormatNamespaceWithPolicy(sb, document.Namespace, document.Policy, document.PolicySet, policyInNamespace);
        }
        else
        {
            // No namespace, output policy/policyset directly
            if (document.Policy != null)
            {
                FormatPolicy(sb, document.Policy, 0);
            }
            else if (document.PolicySet != null)
            {
                FormatPolicySet(sb, document.PolicySet, 0);
            }
        }

        return sb.ToString();
    }

    private static void FormatNamespaceWithPolicy(StringBuilder sb, Namespace ns, Policy? policy, PolicySet? policySet, bool policyInStatements)
    {
        sb.AppendLine($"namespace {ns.Name}");
        sb.AppendLine("{");

        if (ns.Statements != null)
        {
            foreach (Statement statement in ns.Statements)
            {
                FormatStatement(sb, statement, 1);
            }
        }

        // If policy/policyset not in statements, add it at the end of namespace
        if (!policyInStatements)
        {
            if (policy != null)
            {
                FormatPolicy(sb, policy, 1);
            }
            else if (policySet != null)
            {
                FormatPolicySet(sb, policySet, 1);
            }
        }

        sb.AppendLine("}");
    }

    private static void FormatStatement(StringBuilder sb, Statement statement, int indentLevel)
    {
        switch (statement)
        {
            case Import import:
                FormatImport(sb, import, indentLevel);
                break;
            case AttributeDeclaration attrDecl:
                FormatAttributeDeclaration(sb, attrDecl, indentLevel);
                break;
            case PolicyStatement policyStmt:
                FormatPolicy(sb, policyStmt.Policy, indentLevel);
                break;
            case PolicySetStatement policySetStmt:
                FormatPolicySet(sb, policySetStmt.PolicySet, indentLevel);
                break;
        }
    }

    private static void FormatImport(StringBuilder sb, Import import, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);
        sb.Append($"{indent}import {import.NamespacePath}");
        if (import.Wildcard)
        {
            sb.Append(".*");
        }
        sb.AppendLine();
    }

    private static void FormatAttributeDeclaration(StringBuilder sb, AttributeDeclaration attrDecl, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);
        sb.Append($"{indent}attribute {attrDecl.Name} : {attrDecl.Type}");

        if (!string.IsNullOrWhiteSpace(attrDecl.Category))
        {
            sb.Append($" category \"{attrDecl.Category}\"");
        }

        sb.AppendLine(";");
    }

    private static void FormatPolicy(StringBuilder sb, Policy policy, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        sb.Append($"{indent}policy ");

        if (!string.IsNullOrWhiteSpace(policy.Id))
        {
            sb.Append($"{policy.Id} ");
        }

        sb.AppendLine("{");

        // Format combining algorithm
        if (policy.Combinator.HasValue)
        {
            sb.AppendLine($"{new(' ', (indentLevel + 1) * 4)}apply {FormatCombiningAlgorithm(policy.Combinator.Value.ToString())}");
        }

        // Format target
        if (policy.Target?.Clauses.Count > 0)
        {
            sb.Append($"{new(' ', (indentLevel + 1) * 4)}target clause ");
            FormatTargetSingleLine(sb, policy.Target);
            sb.AppendLine();
        }

        // Format rules
        foreach (Rule rule in policy.Rules)
        {
            FormatRule(sb, rule, indentLevel + 1);
        }

        // Format obligations and advice
        if (policy.OnPermit?.Count > 0)
        {
            FormatObligations(sb, policy.OnPermit, indentLevel + 1, "on permit");
        }

        if (policy.OnDeny?.Count > 0)
        {
            FormatAdvice(sb, policy.OnDeny, indentLevel + 1, "on deny");
        }

        sb.AppendLine($"{indent}}}");
    }

    private static void FormatPolicySet(StringBuilder sb, PolicySet policySet, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        sb.Append($"{indent}policyset ");

        if (!string.IsNullOrWhiteSpace(policySet.Id))
        {
            sb.Append($"{policySet.Id} ");
        }

        sb.AppendLine("{");

        if (policySet.Combinator.HasValue)
        {
            sb.AppendLine($"{new(' ', (indentLevel + 1) * 4)}apply {FormatCombiningAlgorithm(policySet.Combinator.Value.ToString())}");
        }

        // Format target
        if (policySet.Target?.Clauses.Count > 0)
        {
            sb.Append($"{new(' ', (indentLevel + 1) * 4)}target clause ");
            FormatTargetSingleLine(sb, policySet.Target);
            sb.AppendLine();
        }

        // Format elements
        foreach (PolicySetElement element in policySet.Elements)
        {
            FormatPolicySetElement(sb, element, indentLevel + 1);
        }

        // Format obligations and advice
        if (policySet.OnPermit?.Count > 0)
        {
            FormatObligations(sb, policySet.OnPermit, indentLevel + 1, "on permit");
        }

        if (policySet.OnDeny?.Count > 0)
        {
            FormatAdvice(sb, policySet.OnDeny, indentLevel + 1, "on deny");
        }

        sb.AppendLine($"{indent}}}");
    }

    private static void FormatPolicySetElement(StringBuilder sb, PolicySetElement element, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        switch (element)
        {
            case Policy policy:
                FormatPolicy(sb, policy, indentLevel);
                break;
            case PolicySet policySet:
                FormatPolicySet(sb, policySet, indentLevel);
                break;
            case PolicyReference reference:
                sb.AppendLine($"{indent}reference {reference.PolicyId};");
                break;
        }
    }

    private static void FormatRule(StringBuilder sb, Rule rule, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        sb.Append($"{indent}rule ");

        if (!string.IsNullOrWhiteSpace(rule.Id))
        {
            sb.Append($"{rule.Id} ");
        }

        sb.AppendLine("{");

        // Format effect
        sb.AppendLine($"{new(' ', (indentLevel + 1) * 4)}{rule.Effect.ToString().ToLowerInvariant()}");

        // Format target
        if (rule.Target?.Clauses.Count > 0)
        {
            sb.Append($"{new(' ', (indentLevel + 1) * 4)}target clause ");
            FormatTargetSingleLine(sb, rule.Target);
            sb.AppendLine();
        }

        // Format condition
        if (rule.Condition != null)
        {
            sb.Append($"{new(' ', (indentLevel + 1) * 4)}condition ");
            FormatBooleanExpression(sb, rule.Condition.Expression);
            sb.AppendLine();
        }

        // Format obligations and advice
        if (rule.OnPermit?.Count > 0)
        {
            FormatObligations(sb, rule.OnPermit, indentLevel + 1, "on permit");
        }

        if (rule.OnDeny?.Count > 0)
        {
            FormatAdvice(sb, rule.OnDeny, indentLevel + 1, "on deny");
        }

        sb.AppendLine($"{indent}}}");
    }

    private static void FormatTarget(StringBuilder sb, Target target, int indentLevel)
    {
        foreach (Clause clause in target.Clauses)
        {
            string indent = new(' ', indentLevel * 4);
            sb.Append(indent);
            FormatBooleanExpression(sb, clause.Expression);
            sb.AppendLine(";");
        }
    }

    private static void FormatTargetSingleLine(StringBuilder sb, Target target)
    {
        for (int i = 0; i < target.Clauses.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(" and ");
            }
            FormatBooleanExpression(sb, target.Clauses[i].Expression);
        }
    }

    private static void FormatObligations(StringBuilder sb, IReadOnlyList<Obligation> obligations, int indentLevel, string keyword)
    {
        string indent = new(' ', indentLevel * 4);
        sb.AppendLine($"{indent}{keyword}");
        sb.AppendLine($"{indent}{{");

        foreach (Obligation obligation in obligations)
        {
            string obligationIndent = new(' ', (indentLevel + 1) * 4);
            sb.Append($"{obligationIndent}obligation {obligation.Id}");

            if (obligation.Attributes?.Count > 0)
            {
                sb.AppendLine(" {");
                foreach (KeyValuePair<string, Expression> kvp in obligation.Attributes)
                {
                    string attrIndent = new(' ', (indentLevel + 2) * 4);
                    sb.Append($"{attrIndent}{kvp.Key} = ");
                    FormatExpression(sb, kvp.Value);
                    sb.AppendLine();
                }
                sb.AppendLine($"{obligationIndent}}}");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
    }

    private static void FormatAdvice(StringBuilder sb, IReadOnlyList<Advice> advice, int indentLevel, string keyword)
    {
        string indent = new(' ', indentLevel * 4);
        sb.AppendLine($"{indent}{keyword}");
        sb.AppendLine($"{indent}{{");

        foreach (Advice adv in advice)
        {
            string adviceIndent = new(' ', (indentLevel + 1) * 4);
            sb.Append($"{adviceIndent}advice {adv.Id}");

            if (adv.Attributes?.Count > 0)
            {
                sb.AppendLine(" {");
                foreach (KeyValuePair<string, Expression> kvp in adv.Attributes)
                {
                    string attrIndent = new(' ', (indentLevel + 2) * 4);
                    sb.Append($"{attrIndent}{kvp.Key} = ");
                    FormatExpression(sb, kvp.Value);
                    sb.AppendLine();
                }
                sb.AppendLine($"{adviceIndent}}}");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
    }

    private static void FormatBooleanExpression(StringBuilder sb, BooleanExpression expression)
    {
        switch (expression)
        {
            case BooleanLiteralExpression literal:
                sb.Append(literal.Value.ToString().ToLowerInvariant());
                break;
            case BooleanAttributeDesignator designator:
                sb.Append($"{designator.Namespace}.{designator.AttributeName}");
                if (designator.MustBePresent)
                {
                    sb.Append('?');
                }
                break;
            case BooleanFunctionCall function:
                sb.Append($"{function.FunctionName}(");
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    FormatExpression(sb, function.Parameters[i]);
                }
                sb.Append(')');
                break;
            case BooleanAllExpression all:
                sb.Append("all ");
                FormatExpression(sb, all.InnerExpression);
                break;
            case NotExpression not:
                sb.Append("not ");
                if (not.InnerExpression is LogicalBinaryExpression or ComparisonExpression)
                {
                    sb.Append('(');
                    FormatBooleanExpression(sb, not.InnerExpression);
                    sb.Append(')');
                }
                else
                {
                    FormatBooleanExpression(sb, not.InnerExpression);
                }
                break;
            case LogicalBinaryExpression logical:
                bool needsParens = logical.Left is LogicalBinaryExpression leftLogical && leftLogical.Operator != logical.Operator;

                if (needsParens)
                {
                    sb.Append('(');
                }

                FormatBooleanExpression(sb, logical.Left);
                if (needsParens)
                {
                    sb.Append(')');
                }

                sb.Append($" {logical.Operator.ToString().ToLowerInvariant()} ");

                needsParens = logical.Right is LogicalBinaryExpression rightLogical && rightLogical.Operator != logical.Operator;
                if (needsParens)
                {
                    sb.Append('(');
                }

                FormatBooleanExpression(sb, logical.Right);
                if (needsParens)
                {
                    sb.Append(')');
                }

                break;
            case ComparisonExpression comparison:
                FormatExpression(sb, comparison.Left);
                sb.Append($" {FormatComparisonOperator(comparison.Operator)} ");
                FormatExpression(sb, comparison.Right);
                break;
        }
    }

    private static void FormatExpression(StringBuilder sb, Expression expression)
    {
        switch (expression)
        {
            case LiteralStringExpression literal:
                sb.Append($"\"{literal.Value}\"");
                break;
            case LiteralIntegerExpression literal:
                sb.Append(literal.Value);
                break;
            case LiteralDoubleExpression literal:
                sb.Append(literal.Value);
                break;
            case LiteralBooleanExpression literal:
                sb.Append(literal.Value.ToString().ToLowerInvariant());
                break;
            case AttributeDesignator attribute:
                sb.Append($"{attribute.Namespace}.{attribute.AttributeName}");
                if (attribute.MustBePresent)
                {
                    sb.Append('?');
                }
                break;
            case ValueCoercionExpression coercion:
                sb.Append($"{coercion.Value} as {coercion.TargetType}");
                break;
            case AllExpression all:
                sb.Append("all ");
                FormatExpression(sb, all.InnerExpression);
                break;
            case FunctionCall function:
                sb.Append($"{function.FunctionName}(");
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    FormatExpression(sb, function.Parameters[i]);
                }
                sb.Append(')');
                break;
            case BinaryExpression binary:
                FormatExpression(sb, binary.Left);
                sb.Append($" {binary.Operator.ToString().ToLowerInvariant()} ");
                FormatExpression(sb, binary.Right);
                break;
        }
    }

    private static string FormatCombiningAlgorithm(string algorithm)
    {
        // Convert from enum name (e.g., "DenyOverrides") to ALFA format (e.g., "denyOverrides")
        // Just make the first character lowercase for camelCase
        return string.IsNullOrEmpty(algorithm)
            ? throw new NotSupportedException("Empty combining algorithm")
            : char.ToLowerInvariant(algorithm[0]) + algorithm[1..];
    }

    private static string FormatComparisonOperator(ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => "==",
            ComparisonOperator.NotEqual => "!=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Unsupported comparison operator: {op}")
        };
    }
}
