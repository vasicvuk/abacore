using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Parser;

namespace ABACore.Serialization;

/// <summary>
/// Provides conversion utilities between ALFA policy documents and a JSON representation.
/// </summary>
public static class PolicyJsonConverter
{
    private const string NamespaceProperty = "namespace";
    private const string PolicyProperty = "policy";
    private const string PolicySetProperty = "policySet";

    public static string JsonFromAlfa(string alfaText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alfaText);
        PolicyDocument document = PolicyParser.Parse(alfaText);
        return ToJson(document);
    }

    public static string AlfaFromJson(string jsonText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonText);
        PolicyDocument document = FromJson(jsonText);
        return AlfaFormatter.Format(document);
    }

    public static string ToJson(PolicyDocument document)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            WritePolicyDocument(writer, document);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static PolicyDocument FromJson(string jsonText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonText);

        try
        {
            using var document = JsonDocument.Parse(jsonText);
            JsonElement root = document.RootElement;

            Namespace? namespaceInfo = null;
            Policy? policy = null;
            PolicySet? policySet = null;

            if (root.TryGetProperty(NamespaceProperty, out JsonElement namespaceElement))
            {
                string? name = namespaceElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    namespaceInfo = new Namespace
                    {
                        Name = name!,
                        Statements = []
                    };
                }
            }

            if (root.TryGetProperty(PolicyProperty, out JsonElement policyElement))
            {
                policy = ReadPolicy(policyElement);
            }

            if (root.TryGetProperty(PolicySetProperty, out JsonElement policySetElement))
            {
                policySet = ReadPolicySet(policySetElement);
            }

            namespaceInfo ??= new Namespace
            {
                Name = "Default",
                Statements = []
            };

            // Read imports if present
            if (root.TryGetProperty("imports", out JsonElement importsElement) && importsElement.ValueKind == JsonValueKind.Array)
            {
                var importsList = new List<Import>();
                foreach (JsonElement importElement in importsElement.EnumerateArray())
                {
                    Import import = ReadImport(importElement);
                    importsList.Add(import);
                }
                // Add imports in order at the beginning
                namespaceInfo.Statements.InsertRange(0, importsList);
            }

            if (policy != null)
            {
                namespaceInfo.Statements.Add(new PolicyStatement { Policy = policy });
            }
            else if (policySet != null)
            {
                namespaceInfo.Statements.Add(new PolicySetStatement { PolicySet = policySet });
            }

            return new PolicyDocument
            {
                Namespace = namespaceInfo,
                Policy = policy,
                PolicySet = policySet
            };
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new System.Text.Json.JsonException($"Failed to parse JSON: {ex.Message}", ex);
        }
    }

    private static void WritePolicyDocument(Utf8JsonWriter writer, PolicyDocument document)
    {
        writer.WriteStartObject();

        string? namespaceName = document.Namespace?.Name;
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            writer.WriteString(NamespaceProperty, namespaceName);
        }

        // Write imports if present
        if (document.Namespace?.Statements != null)
        {
            var imports = document.Namespace.Statements.OfType<Import>().ToList();
            if (imports.Count > 0)
            {
                writer.WritePropertyName("imports");
                writer.WriteStartArray();
                foreach (Import? import in imports)
                {
                    WriteImport(writer, import);
                }
                writer.WriteEndArray();
            }
        }

        Policy? policy = document.Policy;
        PolicySet? policySet = document.PolicySet;

        // If policy/policySet not directly set, look in namespace statements
        if (policy == null && policySet == null && document.Namespace?.Statements != null)
        {
            foreach (Statement stmt in document.Namespace.Statements)
            {
                if (stmt is PolicyStatement polStmt && polStmt.Policy != null)
                {
                    policy = polStmt.Policy;
                    break;
                }
                else if (stmt is PolicySetStatement psStmt && psStmt.PolicySet != null)
                {
                    policySet = psStmt.PolicySet;
                    break;
                }
            }
        }

        if (policy != null)
        {
            writer.WritePropertyName(PolicyProperty);
            WritePolicy(writer, policy);
        }
        else if (policySet != null)
        {
            writer.WritePropertyName(PolicySetProperty);
            WritePolicySet(writer, policySet);
        }

        writer.WriteEndObject();
    }

    private static void WriteImport(Utf8JsonWriter writer, Import import)
    {
        writer.WriteStartObject();
        writer.WriteString("namespacePath", import.NamespacePath);
        if (import.Wildcard)
        {
            writer.WriteBoolean("wildcard", import.Wildcard);
        }
        writer.WriteEndObject();
    }

    private static void WritePolicy(Utf8JsonWriter writer, Policy policy)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(policy.Id))
        {
            writer.WriteString("id", policy.Id);
        }

        if (policy.Combinator.HasValue)
        {
            writer.WriteString("apply", FormatCombiningAlgorithm(policy.Combinator.Value));
        }

        if (policy.Target is { Clauses.Count: > 0 })
        {
            writer.WritePropertyName("target");
            WriteTarget(writer, policy.Target);
        }

        writer.WritePropertyName("rules");
        writer.WriteStartArray();
        foreach (Rule rule in policy.Rules)
        {
            WriteRule(writer, rule);
        }
        writer.WriteEndArray();

        if (policy.OnPermit is { Count: > 0 })
        {
            writer.WritePropertyName("onPermit");
            WriteObligations(writer, policy.OnPermit);
        }

        if (policy.OnDeny is { Count: > 0 })
        {
            writer.WritePropertyName("onDeny");
            WriteAdvice(writer, policy.OnDeny);
        }

        writer.WriteEndObject();
    }

    private static void WritePolicySet(Utf8JsonWriter writer, PolicySet policySet)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(policySet.Id))
        {
            writer.WriteString("id", policySet.Id);
        }

        if (policySet.Combinator.HasValue)
        {
            writer.WriteString("apply", FormatCombiningAlgorithm(policySet.Combinator.Value));
        }

        if (policySet.Target is { Clauses.Count: > 0 })
        {
            writer.WritePropertyName("target");
            WriteTarget(writer, policySet.Target);
        }

        writer.WritePropertyName("elements");
        writer.WriteStartArray();
        foreach (PolicySetElement element in policySet.Elements)
        {
            switch (element)
            {
                case Policy childPolicy:
                    writer.WriteStartObject();
                    writer.WriteString("kind", "policy");
                    writer.WritePropertyName("policy");
                    WritePolicy(writer, childPolicy);
                    writer.WriteEndObject();
                    break;
                case PolicySet childPolicySet:
                    writer.WriteStartObject();
                    writer.WriteString("kind", "policySet");
                    writer.WritePropertyName("policySet");
                    WritePolicySet(writer, childPolicySet);
                    writer.WriteEndObject();
                    break;
                case PolicyReference reference:
                    writer.WriteStartObject();
                    writer.WriteString("kind", "reference");
                    writer.WriteString("policyId", reference.PolicyId);
                    writer.WriteEndObject();
                    break;
            }
        }
        writer.WriteEndArray();

        if (policySet.OnPermit is { Count: > 0 })
        {
            writer.WritePropertyName("onPermit");
            WriteObligations(writer, policySet.OnPermit);
        }

        if (policySet.OnDeny is { Count: > 0 })
        {
            writer.WritePropertyName("onDeny");
            WriteAdvice(writer, policySet.OnDeny);
        }

        writer.WriteEndObject();
    }

    private static void WriteRule(Utf8JsonWriter writer, Rule rule)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrWhiteSpace(rule.Id))
        {
            writer.WriteString("id", rule.Id);
        }

        writer.WriteString("effect", rule.Effect.ToString().ToLowerInvariant());

        if (rule.Target is { Clauses.Count: > 0 })
        {
            writer.WritePropertyName("target");
            WriteTarget(writer, rule.Target);
        }

        if (rule.Condition != null)
        {
            writer.WritePropertyName("condition");
            WriteCondition(writer, rule.Condition);
        }

        if (rule.OnPermit is { Count: > 0 })
        {
            writer.WritePropertyName("onPermit");
            WriteObligations(writer, rule.OnPermit);
        }

        if (rule.OnDeny is { Count: > 0 })
        {
            writer.WritePropertyName("onDeny");
            WriteAdvice(writer, rule.OnDeny);
        }

        writer.WriteEndObject();
    }

    private static void WriteTarget(Utf8JsonWriter writer, Target target)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("clauses");
        writer.WriteStartArray();
        foreach (Clause clause in target.Clauses)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("expression");
            WriteBooleanExpression(writer, clause.Expression);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteCondition(Utf8JsonWriter writer, Condition condition)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("expression");
        WriteBooleanExpression(writer, condition.Expression);
        writer.WriteEndObject();
    }

    private static Import ReadImport(JsonElement element)
    {
        string? namespacePath = element.GetProperty("namespacePath").GetString();
        bool wildcard = element.TryGetProperty("wildcard", out JsonElement wildcardElement) && wildcardElement.GetBoolean();

        return new Import
        {
            NamespacePath = namespacePath ?? throw new System.Text.Json.JsonException("Import must have a namespacePath"),
            Wildcard = wildcard
        };
    }

    private static void WriteObligations(Utf8JsonWriter writer, IReadOnlyList<Obligation> obligations)
    {
        writer.WriteStartArray();
        foreach (Obligation obligation in obligations)
        {
            writer.WriteStartObject();
            writer.WriteString("id", obligation.Id);
            if (obligation.Attributes is { Count: > 0 })
            {
                writer.WritePropertyName("attributes");
                writer.WriteStartObject();
                foreach (KeyValuePair<string, Expression> kvp in obligation.Attributes)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteExpression(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteAdvice(Utf8JsonWriter writer, IReadOnlyList<Advice> advice)
    {
        writer.WriteStartArray();
        foreach (Advice adv in advice)
        {
            writer.WriteStartObject();
            writer.WriteString("id", adv.Id);
            if (adv.Attributes is { Count: > 0 })
            {
                writer.WritePropertyName("attributes");
                writer.WriteStartObject();
                foreach (KeyValuePair<string, Expression> kvp in adv.Attributes)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteExpression(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteBooleanExpression(Utf8JsonWriter writer, BooleanExpression expression)
    {
        writer.WriteStartObject();
        switch (expression)
        {
            case BooleanLiteralExpression literal:
                writer.WriteString("kind", "booleanLiteral");
                writer.WriteBoolean("value", literal.Value);
                break;
            case BooleanAttributeDesignator designator:
                writer.WriteString("kind", "booleanAttribute");
                writer.WriteString("namespace", designator.Namespace);
                writer.WriteString("attribute", designator.AttributeName);
                if (designator.MustBePresent)
                {
                    writer.WriteBoolean("mustBePresent", true);
                }
                break;
            case BooleanFunctionCall function:
                writer.WriteString("kind", "booleanFunction");
                writer.WriteString("name", function.FunctionName);
                writer.WritePropertyName("parameters");
                writer.WriteStartArray();
                foreach (Expression parameter in function.Parameters)
                {
                    WriteExpression(writer, parameter);
                }
                writer.WriteEndArray();
                break;
            case BooleanAllExpression all:
                writer.WriteString("kind", "booleanAll");
                writer.WritePropertyName("expression");
                WriteExpression(writer, all.InnerExpression);
                break;
            case NotExpression notExpression:
                writer.WriteString("kind", "not");
                writer.WritePropertyName("expression");
                WriteBooleanExpression(writer, notExpression.InnerExpression);
                break;
            case LogicalBinaryExpression logical:
                writer.WriteString("kind", "logical");
                writer.WriteString("operator", logical.Operator.ToString());
                writer.WritePropertyName("left");
                WriteBooleanExpression(writer, logical.Left);
                writer.WritePropertyName("right");
                WriteBooleanExpression(writer, logical.Right);
                break;
            case ComparisonExpression comparison:
                writer.WriteString("kind", "comparison");
                writer.WriteString("operator", comparison.Operator.ToString());
                writer.WritePropertyName("left");
                WriteExpression(writer, comparison.Left);
                writer.WritePropertyName("right");
                WriteExpression(writer, comparison.Right);
                break;
            default:
                throw new NotSupportedException($"Boolean expression type {expression.GetType().Name} is not supported by the JSON converter.");
        }
        writer.WriteEndObject();
    }

    private static void WriteExpression(Utf8JsonWriter writer, Expression expression)
    {
        writer.WriteStartObject();
        switch (expression)
        {
            case LiteralStringExpression literalString:
                writer.WriteString("kind", "literalString");
                writer.WriteString("value", literalString.Value);
                break;
            case LiteralIntegerExpression literalInt:
                writer.WriteString("kind", "literalInteger");
                writer.WriteNumber("value", literalInt.Value);
                break;
            case LiteralDoubleExpression literalDouble:
                writer.WriteString("kind", "literalDouble");
                writer.WriteNumber("value", literalDouble.Value);
                break;
            case LiteralBooleanExpression literalBool:
                writer.WriteString("kind", "literalBoolean");
                writer.WriteBoolean("value", literalBool.Value);
                break;
            case AttributeDesignator attribute:
                writer.WriteString("kind", "attribute");
                writer.WriteString("namespace", attribute.Namespace);
                writer.WriteString("attribute", attribute.AttributeName);
                if (attribute.MustBePresent)
                {
                    writer.WriteBoolean("mustBePresent", true);
                }
                break;
            case ValueCoercionExpression coercion:
                writer.WriteString("kind", "valueCoercion");
                writer.WriteString("value", coercion.Value);
                writer.WriteString("targetType", coercion.TargetType.ToString());
                break;
            case AllExpression all:
                writer.WriteString("kind", "all");
                writer.WritePropertyName("expression");
                WriteExpression(writer, all.InnerExpression);
                break;
            case FunctionCall function:
                writer.WriteString("kind", "function");
                writer.WriteString("name", function.FunctionName);
                writer.WritePropertyName("parameters");
                writer.WriteStartArray();
                foreach (Expression parameter in function.Parameters)
                {
                    WriteExpression(writer, parameter);
                }
                writer.WriteEndArray();
                break;
            case BinaryExpression binary:
                writer.WriteString("kind", "binary");
                writer.WriteString("operator", binary.Operator.ToString());
                writer.WritePropertyName("left");
                WriteExpression(writer, binary.Left);
                writer.WritePropertyName("right");
                WriteExpression(writer, binary.Right);
                break;
            default:
                throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported by the JSON converter.");
        }
        writer.WriteEndObject();
    }

    private static Policy ReadPolicy(JsonElement element)
    {
        string? id = element.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
        CombiningAlgorithm? combinator = element.TryGetProperty("apply", out JsonElement applyElement) ? ParseCombiningAlgorithm(applyElement.GetString()) : null;

        Target? target = null;
        if (element.TryGetProperty("target", out JsonElement targetElement))
        {
            target = ReadTarget(targetElement);
        }

        List<Rule> rules = [];
        if (element.TryGetProperty("rules", out JsonElement rulesElement))
        {
            foreach (JsonElement ruleElement in rulesElement.EnumerateArray())
            {
                rules.Add(ReadRule(ruleElement));
            }
        }

        List<Obligation>? onPermit = null;
        if (element.TryGetProperty("onPermit", out JsonElement onPermitElement))
        {
            onPermit = ReadObligations(onPermitElement);
        }

        List<Advice>? onDeny = null;
        if (element.TryGetProperty("onDeny", out JsonElement onDenyElement))
        {
            onDeny = ReadAdvice(onDenyElement);
        }

        return new Policy
        {
            Id = id,
            Target = target,
            Rules = rules,
            Combinator = combinator,
            OnPermit = onPermit,
            OnDeny = onDeny
        };
    }

    private static PolicySet ReadPolicySet(JsonElement element)
    {
        string? id = element.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
        CombiningAlgorithm? combinator = element.TryGetProperty("apply", out JsonElement applyElement) ? ParseCombiningAlgorithm(applyElement.GetString()) : null;

        Target? target = null;
        if (element.TryGetProperty("target", out JsonElement targetElement))
        {
            target = ReadTarget(targetElement);
        }

        List<PolicySetElement> elements = [];
        if (element.TryGetProperty("elements", out JsonElement elementsElement))
        {
            foreach (JsonElement child in elementsElement.EnumerateArray())
            {
                string kind = child.GetProperty("kind").GetString() ?? throw new AlfaParseException("Policy set element is missing \"kind\".");
                switch (kind)
                {
                    case "policy":
                        elements.Add(ReadPolicy(child.GetProperty("policy")));
                        break;
                    case "policySet":
                        elements.Add(ReadPolicySet(child.GetProperty("policySet")));
                        break;
                    case "reference":
                        elements.Add(new PolicyReference { PolicyId = child.GetProperty("policyId").GetString() ?? throw new AlfaParseException("Policy reference missing policyId.") });
                        break;
                    default:
                        throw new AlfaParseException($"Unknown policy set element kind '{kind}'.");
                }
            }
        }

        List<Obligation>? onPermit = null;
        if (element.TryGetProperty("onPermit", out JsonElement onPermitElement))
        {
            onPermit = ReadObligations(onPermitElement);
        }

        List<Advice>? onDeny = null;
        if (element.TryGetProperty("onDeny", out JsonElement onDenyElement))
        {
            onDeny = ReadAdvice(onDenyElement);
        }

        return new PolicySet
        {
            Id = id,
            Target = target,
            Elements = elements,
            Combinator = combinator,
            OnPermit = onPermit,
            OnDeny = onDeny
        };
    }

    private static Rule ReadRule(JsonElement element)
    {
        string? id = element.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
        Effect effect = element.TryGetProperty("effect", out JsonElement effectElement) ?
            ParseEffect(effectElement.GetString()) : Effect.Permit;

        Target? target = null;
        if (element.TryGetProperty("target", out JsonElement targetElement))
        {
            target = ReadTarget(targetElement);
        }

        Condition? condition = null;
        if (element.TryGetProperty("condition", out JsonElement conditionElement))
        {
            condition = ReadCondition(conditionElement);
        }

        List<Obligation>? onPermit = null;
        if (element.TryGetProperty("onPermit", out JsonElement onPermitElement))
        {
            onPermit = ReadObligations(onPermitElement);
        }

        List<Advice>? onDeny = null;
        if (element.TryGetProperty("onDeny", out JsonElement onDenyElement))
        {
            onDeny = ReadAdvice(onDenyElement);
        }

        return new Rule
        {
            Id = id,
            Effect = effect,
            Target = target,
            Condition = condition,
            OnPermit = onPermit,
            OnDeny = onDeny
        };
    }

    private static Target ReadTarget(JsonElement element)
    {
        List<Clause> clauses = [];
        if (element.TryGetProperty("clauses", out JsonElement clausesElement))
        {
            foreach (JsonElement clauseElement in clausesElement.EnumerateArray())
            {
                if (clauseElement.TryGetProperty("expression", out JsonElement expressionElement))
                {
                    clauses.Add(new Clause { Expression = ReadBooleanExpression(expressionElement) });
                }
            }
        }

        return new Target { Clauses = clauses };
    }

    private static Condition ReadCondition(JsonElement element)
    {
        return element.TryGetProperty("expression", out JsonElement expressionElement)
            ? new Condition { Expression = ReadBooleanExpression(expressionElement) }
            : throw new AlfaParseException("Condition missing expression property.");
    }

    private static List<Obligation> ReadObligations(JsonElement element)
    {
        List<Obligation> obligations = [];
        foreach (JsonElement obligationElement in element.EnumerateArray())
        {
            string id = obligationElement.GetProperty("id").GetString() ??
                throw new AlfaParseException("Obligation missing id property.");

            Dictionary<string, Expression> attributes = [];
            if (obligationElement.TryGetProperty("attributes", out JsonElement attributesElement))
            {
                foreach (JsonProperty attribute in attributesElement.EnumerateObject())
                {
                    attributes[attribute.Name] = ReadExpression(attribute.Value);
                }
            }

            obligations.Add(new Obligation { Id = id, Attributes = attributes });
        }

        return obligations;
    }

    private static List<Advice> ReadAdvice(JsonElement element)
    {
        List<Advice> advice = [];
        foreach (JsonElement adviceElement in element.EnumerateArray())
        {
            string id = adviceElement.GetProperty("id").GetString() ??
                throw new AlfaParseException("Advice missing id property.");

            Dictionary<string, Expression> attributes = [];
            if (adviceElement.TryGetProperty("attributes", out JsonElement attributesElement))
            {
                foreach (JsonProperty attribute in attributesElement.EnumerateObject())
                {
                    attributes[attribute.Name] = ReadExpression(attribute.Value);
                }
            }

            advice.Add(new Advice { Id = id, Attributes = attributes });
        }

        return advice;
    }

    private static BooleanExpression ReadBooleanExpression(JsonElement element)
    {
        string kind = element.GetProperty("kind").GetString() ??
            throw new AlfaParseException("Boolean expression missing kind property.");

        return kind switch
        {
            "booleanLiteral" => new BooleanLiteralExpression
            {
                Value = element.GetProperty("value").GetBoolean()
            },
            "booleanAttribute" => new BooleanAttributeDesignator
            {
                Namespace = element.GetProperty("namespace").GetString() ?? string.Empty,
                AttributeName = element.GetProperty("attribute").GetString() ?? string.Empty,
                MustBePresent = element.TryGetProperty("mustBePresent", out JsonElement mustBePresent) && mustBePresent.GetBoolean()
            },
            "booleanFunction" => new BooleanFunctionCall
            {
                FunctionName = element.GetProperty("name").GetString() ?? string.Empty,
                Parameters = ReadExpressionList(element.GetProperty("parameters"))
            },
            "booleanAll" => new BooleanAllExpression
            {
                InnerExpression = ReadExpression(element.GetProperty("expression"))
            },
            "not" => new NotExpression
            {
                InnerExpression = ReadBooleanExpression(element.GetProperty("expression"))
            },
            "logical" => new LogicalBinaryExpression
            {
                Operator = Enum.Parse<LogicalOperator>(element.GetProperty("operator").GetString() ?? "And"),
                Left = ReadBooleanExpression(element.GetProperty("left")),
                Right = ReadBooleanExpression(element.GetProperty("right"))
            },
            "comparison" => new ComparisonExpression
            {
                Operator = Enum.Parse<ComparisonOperator>(element.GetProperty("operator").GetString() ?? "Equal"),
                Left = ReadExpression(element.GetProperty("left")),
                Right = ReadExpression(element.GetProperty("right"))
            },
            _ => throw new AlfaParseException($"Unknown boolean expression kind: {kind}")
        };
    }

    private static Expression ReadExpression(JsonElement element)
    {
        string kind = element.GetProperty("kind").GetString() ??
            throw new AlfaParseException("Expression missing kind property.");

        return kind switch
        {
            "literalString" => new LiteralStringExpression
            {
                Value = element.GetProperty("value").GetString() ?? string.Empty
            },
            "literalInteger" => new LiteralIntegerExpression
            {
                Value = element.GetProperty("value").GetInt32()
            },
            "literalDouble" => new LiteralDoubleExpression
            {
                Value = element.GetProperty("value").GetDouble()
            },
            "literalBoolean" => new LiteralBooleanExpression
            {
                Value = element.GetProperty("value").GetBoolean()
            },
            "attribute" => new AttributeDesignator
            {
                Namespace = element.GetProperty("namespace").GetString() ?? string.Empty,
                AttributeName = element.GetProperty("attribute").GetString() ?? string.Empty,
                MustBePresent = element.TryGetProperty("mustBePresent", out JsonElement mustBePresent) && mustBePresent.GetBoolean()
            },
            "valueCoercion" => new ValueCoercionExpression
            {
                Value = element.GetProperty("value").GetString() ?? string.Empty,
                TargetType = Enum.Parse<AttributeType>(element.GetProperty("targetType").GetString() ?? "String")
            },
            "all" => new AllExpression
            {
                InnerExpression = ReadExpression(element.GetProperty("expression"))
            },
            "function" => new FunctionCall
            {
                FunctionName = element.GetProperty("name").GetString() ?? string.Empty,
                Parameters = ReadExpressionList(element.GetProperty("parameters"))
            },
            "binary" => new BinaryExpression
            {
                Operator = Enum.Parse<BinaryOperator>(element.GetProperty("operator").GetString() ?? "Add"),
                Left = ReadExpression(element.GetProperty("left")),
                Right = ReadExpression(element.GetProperty("right"))
            },
            _ => throw new AlfaParseException($"Unknown expression kind: {kind}")
        };
    }

    private static List<Expression> ReadExpressionList(JsonElement element)
    {
        List<Expression> expressions = [];
        foreach (JsonElement expressionElement in element.EnumerateArray())
        {
            expressions.Add(ReadExpression(expressionElement));
        }
        return expressions;
    }

    private static CombiningAlgorithm? ParseCombiningAlgorithm(string? algorithm)
    {
        return string.IsNullOrWhiteSpace(algorithm)
            ? null
            : algorithm.ToLowerInvariant() switch
        {
            "deny-overrides" => CombiningAlgorithm.DenyOverrides,
            "permit-overrides" => CombiningAlgorithm.PermitOverrides,
            "first-applicable" => CombiningAlgorithm.FirstApplicable,
            "only-one" => CombiningAlgorithm.OnlyOne,
            "deny-unless-permit" => CombiningAlgorithm.DenyUnlessPermit,
            "permit-unless-deny" => CombiningAlgorithm.PermitUnlessDeny,
            _ => throw new AlfaParseException($"Unknown combining algorithm: {algorithm}")
        };
    }

    private static Effect ParseEffect(string? effect)
    {
        return string.IsNullOrWhiteSpace(effect)
            ? Effect.Permit
            : effect.ToLowerInvariant() switch
        {
            "permit" => Effect.Permit,
            "deny" => Effect.Deny,
            _ => throw new AlfaParseException($"Unknown effect: {effect}")
        };
    }

    private static string FormatCombiningAlgorithm(CombiningAlgorithm algorithm)
    {
        return algorithm switch
        {
            CombiningAlgorithm.DenyOverrides => "deny-overrides",
            CombiningAlgorithm.PermitOverrides => "permit-overrides",
            CombiningAlgorithm.FirstApplicable => "first-applicable",
            CombiningAlgorithm.OnlyOne => "only-one",
            CombiningAlgorithm.DenyUnlessPermit => "deny-unless-permit",
            CombiningAlgorithm.PermitUnlessDeny => "permit-unless-deny",
            _ => throw new NotSupportedException($"Unsupported combining algorithm: {algorithm}")
        };
    }
}
