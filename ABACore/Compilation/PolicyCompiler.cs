using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using ABACore.Models;
using ABACore.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ABACore.Compilation;

/// <summary>
/// Compiles ALFA policy AST nodes to executable C# code using Roslyn.
/// This compiler generates and caches compiled policy delegates for efficient runtime evaluation.
/// </summary>
public sealed class PolicyCompiler : IPolicyCompiler
{
    private readonly ConcurrentDictionary<string, Runtime.CompiledPolicy> _compiledPolicyCache;
    private readonly CSharpCompilationOptions _compilationOptions;
    private readonly List<MetadataReference> _references;
    private readonly InterpreterCompiler _interpreter = new();
    private static readonly string[] DefaultReferenceAssemblyNames =
    [
        "System.Private.CoreLib",
        "System.Runtime",
        "System.Console",
        "System.Linq",
        "System.Collections"
    ];

    private static readonly ConcurrentDictionary<string, string> AssemblyPathCache =
        new(BuildTrustedAssemblyIndex(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyCompiler"/> class.
    /// </summary>
    public PolicyCompiler()
    {
        _compiledPolicyCache = new ConcurrentDictionary<string, Runtime.CompiledPolicy>();

        _compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            allowUnsafe: false,
            nullableContextOptions: NullableContextOptions.Enable);

        _references = GetMetadataReferences();
    }

    /// <summary>
    /// Compiles a policy AST to an executable delegate.
    /// </summary>
    /// <param name="policy">The policy to compile.</param>
    /// <param name="enableCaching">Whether to cache the compiled policy.</param>
    /// <returns>A compiled policy that can be executed.</returns>
    public Runtime.CompiledPolicy CompilePolicy(Policy policy, bool enableCaching = true)
    {
        ArgumentNullException.ThrowIfNull(policy);

        string cacheKey = policy.Id ?? Guid.NewGuid().ToString();

        if (enableCaching && _compiledPolicyCache.TryGetValue(cacheKey, out Runtime.CompiledPolicy? cached))
        {
            return cached;
        }

        string generatedCode = GeneratePolicyCode(policy);
        Func<EvaluationContext, Decision> evaluationDelegate = CompileToDelegate(generatedCode);

        Runtime.CompiledPolicy compiledPolicy = new()
        {
            PolicyId = policy.Id,
            EvaluationDelegate = evaluationDelegate,
            OriginalPolicy = policy,
            GeneratedCode = generatedCode
        };

        if (enableCaching)
        {
            _compiledPolicyCache.TryAdd(cacheKey, compiledPolicy);
        }

        return compiledPolicy;
    }

    /// <summary>
    /// Clears the compiled policy cache.
    /// </summary>
    public void ClearCache()
    {
        _compiledPolicyCache.Clear();
        _interpreter.ClearCache();
    }

    /// <summary>
    /// Removes a specific policy from the cache.
    /// </summary>
    /// <param name="policyId">The policy ID to remove.</param>
    /// <returns>True if the policy was removed, false otherwise.</returns>
    public bool RemoveFromCache(string policyId)
    {
        _interpreter.RemoveFromCache(policyId);
        return _compiledPolicyCache.TryRemove(policyId, out _);
    }

    /// <summary>
    /// Compiles a policyset AST to an executable delegate.
    /// For Roslyn compiler, we delegate to the interpreter for policyset evaluation
    /// since code generation for dynamic policyset evaluation is complex.
    /// </summary>
    public Runtime.CompiledPolicy CompilePolicySet(PolicySet policySet, PolicyRepository repository, bool enableCaching = true)
    {
        // Delegate to interpreter for PolicySet compilation
        // Generating dynamic C# code for policy sets is complex and not worth the marginal performance gain
        return _interpreter.CompilePolicySet(policySet, repository, enableCaching);
    }

    /// <summary>
    /// Generates C# code for a policy.
    /// </summary>
    private static string GeneratePolicyCode(Policy policy)
    {
        StringBuilder sb = new();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using ABACore.Models;");
        sb.AppendLine("using ABACore.Runtime;");
        sb.AppendLine();
        sb.AppendLine("namespace ABACore.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class PolicyEvaluator");
        sb.AppendLine("    {");
        sb.AppendLine("        private static bool CompareValues(object left, object right, string op)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (left == null || right == null) return false;");
        sb.AppendLine("            ");
        sb.AppendLine("            // Try to convert to comparable types");
        sb.AppendLine("            if (left is IComparable leftComp && right is IComparable rightComp)");
        sb.AppendLine("            {");
        sb.AppendLine("                try");
        sb.AppendLine("                {");
        sb.AppendLine("                    int result;");
        sb.AppendLine("                    if (left.GetType() == right.GetType())");
        sb.AppendLine("                    {");
        sb.AppendLine("                        result = leftComp.CompareTo(rightComp);");
        sb.AppendLine("                    }");
        sb.AppendLine("                    else");
        sb.AppendLine("                    {");
        sb.AppendLine("                        // Try to convert to common type");
        sb.AppendLine("                        var leftDouble = Convert.ToDouble(left);");
        sb.AppendLine("                        var rightDouble = Convert.ToDouble(right);");
        sb.AppendLine("                        result = leftDouble.CompareTo(rightDouble);");
        sb.AppendLine("                    }");
        sb.AppendLine("                    ");
        sb.AppendLine("                    return op switch");
        sb.AppendLine("                    {");
        sb.AppendLine("                        \">\" => result > 0,");
        sb.AppendLine("                        \"<\" => result < 0,");
        sb.AppendLine("                        \">=\" => result >= 0,");
        sb.AppendLine("                        \"<=\" => result <= 0,");
        sb.AppendLine("                        _ => false");
        sb.AppendLine("                    };");
        sb.AppendLine("                }");
        sb.AppendLine("                catch { return false; }");
        sb.AppendLine("            }");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        private static object PerformArithmetic(object left, object right, string op)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (left == null || right == null) return 0;");
        sb.AppendLine("            ");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var leftNum = Convert.ToDouble(left);");
        sb.AppendLine("                var rightNum = Convert.ToDouble(right);");
        sb.AppendLine("                ");
        sb.AppendLine("                return op switch");
        sb.AppendLine("                {");
        sb.AppendLine("                    \"+\" => leftNum + rightNum,");
        sb.AppendLine("                    \"-\" => leftNum - rightNum,");
        sb.AppendLine("                    \"*\" => leftNum * rightNum,");
        sb.AppendLine("                    \"/\" => leftNum / rightNum,");
        sb.AppendLine("                    _ => 0");
        sb.AppendLine("                };");
        sb.AppendLine("            }");
        sb.AppendLine("            catch { return 0; }");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        public static Decision Evaluate(EvaluationContext context)");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");

        // Generate rule evaluation code
        if (policy.Rules.Count > 0)
        {
            GenerateRulesEvaluation(sb, policy);
        }
        else
        {
            sb.AppendLine("                return Decision.NotApplicable();");
        }

        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                return Decision.Indeterminate(StatusInfo.ProcessingError(ex.Message));");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates code for rules evaluation based on combining algorithm.
    /// </summary>
    private static void GenerateRulesEvaluation(StringBuilder sb, Policy policy)
    {
        CombiningAlgorithm algorithm = policy.Combinator ?? CombiningAlgorithm.DenyOverrides;

        switch (algorithm)
        {
            case CombiningAlgorithm.DenyOverrides:
                GenerateDenyOverrides(sb, policy);
                break;
            case CombiningAlgorithm.PermitOverrides:
                GeneratePermitOverrides(sb, policy);
                break;
            case CombiningAlgorithm.FirstApplicable:
                GenerateFirstApplicable(sb, policy);
                break;
            case CombiningAlgorithm.OnlyOne:
                GenerateOnlyOne(sb, policy);
                break;
            case CombiningAlgorithm.DenyUnlessPermit:
                GenerateDenyUnlessPermit(sb, policy);
                break;
            case CombiningAlgorithm.PermitUnlessDeny:
                GeneratePermitUnlessDeny(sb, policy);
                break;
            default:
                GenerateDenyOverrides(sb, policy);
                break;
        }
    }

    /// <summary>
    /// Generates code for deny-overrides combining algorithm.
    /// </summary>
    private static void GenerateDenyOverrides(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // Deny-overrides: any Deny returns Deny, otherwise first Permit");
        sb.AppendLine("                bool atLeastOnePermit = false;");
        sb.AppendLine("                bool atLeastOneError = false;");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluation(sb, rule, i, policy);
        }

        sb.AppendLine();
        sb.AppendLine("                if (atLeastOnePermit)");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Permit, "obligations", "advice");
        sb.AppendLine("                    return Decision.Permit(obligations, advice);");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (atLeastOneError)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.Indeterminate();");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.NotApplicable();");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// Generates code for permit-overrides combining algorithm.
    /// </summary>
    private static void GeneratePermitOverrides(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // Permit-overrides: any Permit returns Permit, otherwise first Deny");
        sb.AppendLine("                bool atLeastOneDeny = false;");
        sb.AppendLine("                bool atLeastOneError = false;");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluation(sb, rule, i, policy, permitOverrides: true);
        }

        sb.AppendLine();
        sb.AppendLine("                if (atLeastOneDeny)");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Deny, "obligations", "advice");
        sb.AppendLine("                    return Decision.Deny(obligations, advice);");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (atLeastOneError)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.Indeterminate();");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.NotApplicable();");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// Generates code for first-applicable combining algorithm.
    /// </summary>
    private static void GenerateFirstApplicable(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // First-applicable: return first applicable rule's decision");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluationFirstApplicable(sb, rule, i, policy);
        }

        sb.AppendLine();
        sb.AppendLine("                return Decision.NotApplicable();");
    }

    /// <summary>
    /// Generates code for only-one combining algorithm.
    /// </summary>
    private static void GenerateOnlyOne(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // Only-one: exactly one rule must be applicable");
        sb.AppendLine("                int applicableCount = 0;");
        sb.AppendLine("                Decision? applicableDecision = null;");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluationOnlyOne(sb, rule, i, policy);
        }

        sb.AppendLine();
        sb.AppendLine("                if (applicableCount == 1)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return applicableDecision!;");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (applicableCount > 1)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.Indeterminate(StatusInfo.ProcessingError(\"Multiple rules applicable in only-one algorithm\"));");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.NotApplicable();");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// Generates code for deny-unless-permit combining algorithm.
    /// </summary>
    private static void GenerateDenyUnlessPermit(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // Deny-unless-permit: return Deny unless there's a Permit");
        sb.AppendLine("                bool atLeastOnePermit = false;");
        sb.AppendLine("                bool atLeastOneError = false;");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluation(sb, rule, i, policy, permitOverrides: false);
        }

        sb.AppendLine();
        sb.AppendLine("                if (atLeastOnePermit)");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Permit, "obligations", "advice");
        sb.AppendLine("                    return Decision.Permit(obligations, advice);");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Deny, "obligations", "advice");
        sb.AppendLine("                    return Decision.Deny(obligations, advice);");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// Generates code for permit-unless-deny combining algorithm.
    /// </summary>
    private static void GeneratePermitUnlessDeny(StringBuilder sb, Policy policy)
    {
        sb.AppendLine("                // Permit-unless-deny: return Permit unless there's a Deny");
        sb.AppendLine("                bool atLeastOneDeny = false;");
        sb.AppendLine("                bool atLeastOneError = false;");
        sb.AppendLine("                List<ObligationResult>? obligations = null;");
        sb.AppendLine("                List<AdviceResult>? advice = null;");
        sb.AppendLine();

        for (int i = 0; i < policy.Rules.Count; i++)
        {
            Rule rule = policy.Rules[i];
            GenerateRuleEvaluation(sb, rule, i, policy, permitOverrides: true);
        }

        sb.AppendLine();
        sb.AppendLine("                if (atLeastOneDeny)");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Deny, "obligations", "advice");
        sb.AppendLine("                    return Decision.Deny(obligations, advice);");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Permit, "obligations", "advice");
        sb.AppendLine("                    return Decision.Permit(obligations, advice);");
        sb.AppendLine("                }");
    }

    /// <summary>
    /// Generates code for individual rule evaluation.
    /// </summary>
    private static void GenerateRuleEvaluation(StringBuilder sb, Rule rule, int ruleIndex, Policy policy, bool permitOverrides = false)
    {
        sb.AppendLine($"                // Rule {ruleIndex}: {rule.Id ?? "unnamed"}");
        sb.AppendLine("                try");
        sb.AppendLine("                {");

        // Generate target evaluation
        if (rule.Target != null && rule.Target.Clauses.Count > 0)
        {
            sb.AppendLine("                    bool targetMatch = true;");
            foreach (Clause clause in rule.Target.Clauses)
            {
                string clauseExpr = ExpressionCompiler.CompileBooleanExpression(clause.Expression);
                sb.AppendLine($"                    targetMatch = targetMatch && ({clauseExpr});");
            }
            sb.AppendLine("                    if (!targetMatch) { goto skipRule" + ruleIndex + "; }");
        }

        // Generate condition evaluation
        if (rule.Condition != null)
        {
            string conditionExpr = ExpressionCompiler.CompileBooleanExpression(rule.Condition.Expression);
            sb.AppendLine($"                    if (!({conditionExpr})) {{ goto skipRule{ruleIndex}; }}");
        }

        // Return based on rule effect
        if (rule.Effect == Effect.Deny)
        {
            if (permitOverrides)
            {
                sb.AppendLine("                    atLeastOneDeny = true;");
                GenerateObligationsAndAdviceCollection(sb, rule, Effect.Deny, "obligations", "advice");
                sb.AppendLine("                    goto skipRule" + ruleIndex + ";");
            }
            else
            {
                // For deny-overrides, need to add policy-level advice before returning
                GenerateObligationsAndAdviceCollection(sb, rule, Effect.Deny, "obligations", "advice");
                sb.AppendLine("                    goto denyDecision" + ruleIndex + ";");
            }
        }
        else
        {
            if (permitOverrides)
            {
                // For permit-overrides, need to add policy-level obligations before returning
                GenerateObligationsAndAdviceCollection(sb, rule, Effect.Permit, "obligations", "advice");
                sb.AppendLine("                    goto permitDecision" + ruleIndex + ";");
            }
            else
            {
                sb.AppendLine("                    atLeastOnePermit = true;");
                GenerateObligationsAndAdviceCollection(sb, rule, Effect.Permit, "obligations", "advice");
                sb.AppendLine("                    goto skipRule" + ruleIndex + ";");
            }
        }

        sb.AppendLine("                }");
        sb.AppendLine("                catch");
        sb.AppendLine("                {");
        sb.AppendLine("                    atLeastOneError = true;");
        sb.AppendLine("                }");

        // Add decision labels for handling policy-level obligations/advice
        if (rule.Effect == Effect.Deny && !permitOverrides)
        {
            sb.AppendLine("                goto skipRule" + ruleIndex + ";");
            sb.AppendLine("                denyDecision" + ruleIndex + ":");
            GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Deny, "obligations", "advice");
            sb.AppendLine("                return Decision.Deny(obligations, advice);");
        }
        else if (rule.Effect == Effect.Permit && permitOverrides)
        {
            sb.AppendLine("                goto skipRule" + ruleIndex + ";");
            sb.AppendLine("                permitDecision" + ruleIndex + ":");
            GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Permit, "obligations", "advice");
            sb.AppendLine("                return Decision.Permit(obligations, advice);");
        }

        sb.AppendLine("                skipRule" + ruleIndex + ":;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates code for rule evaluation in first-applicable algorithm.
    /// </summary>
    private static void GenerateRuleEvaluationFirstApplicable(StringBuilder sb, Rule rule, int ruleIndex, Policy policy)
    {
        sb.AppendLine($"                // Rule {ruleIndex}: {rule.Id ?? "unnamed"}");
        sb.AppendLine("                try");
        sb.AppendLine("                {");

        if (rule.Target != null && rule.Target.Clauses.Count > 0)
        {
            sb.AppendLine("                    bool targetMatch = true;");
            foreach (Clause clause in rule.Target.Clauses)
            {
                string clauseExpr = ExpressionCompiler.CompileBooleanExpression(clause.Expression);
                sb.AppendLine($"                    targetMatch = targetMatch && ({clauseExpr});");
            }
            sb.AppendLine("                    if (!targetMatch) { goto skipRule" + ruleIndex + "; }");
        }

        if (rule.Condition != null)
        {
            string conditionExpr = ExpressionCompiler.CompileBooleanExpression(rule.Condition.Expression);
            sb.AppendLine($"                    if (!({conditionExpr})) {{ goto skipRule{ruleIndex}; }}");
        }

        GenerateObligationsAndAdviceCollection(sb, rule, rule.Effect, "obligations", "advice");
        sb.AppendLine($"                    goto {(rule.Effect == Effect.Permit ? "permitDecision" : "denyDecision")}{ruleIndex}FA;");

        sb.AppendLine("                }");
        sb.AppendLine("                catch (Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return Decision.Indeterminate(StatusInfo.ProcessingError(ex.Message));");
        sb.AppendLine("                }");

        // Add decision labels for policy-level obligations/advice
        sb.AppendLine("                goto skipRule" + ruleIndex + ";");
        if (rule.Effect == Effect.Permit)
        {
            sb.AppendLine("                permitDecision" + ruleIndex + "FA:");
            GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Permit, "obligations", "advice");
            sb.AppendLine("                return Decision.Permit(obligations, advice);");
        }
        else
        {
            sb.AppendLine("                denyDecision" + ruleIndex + "FA:");
            GeneratePolicyLevelObligationsAndAdvice(sb, policy, Effect.Deny, "obligations", "advice");
            sb.AppendLine("                return Decision.Deny(obligations, advice);");
        }

        sb.AppendLine("                skipRule" + ruleIndex + ":;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates code for rule evaluation in only-one algorithm.
    /// </summary>
    private static void GenerateRuleEvaluationOnlyOne(StringBuilder sb, Rule rule, int ruleIndex, Policy policy)
    {
        sb.AppendLine($"                // Rule {ruleIndex}: {rule.Id ?? "unnamed"}");
        sb.AppendLine("                try");
        sb.AppendLine("                {");

        if (rule.Target != null && rule.Target.Clauses.Count > 0)
        {
            sb.AppendLine("                    bool targetMatch = true;");
            foreach (Clause clause in rule.Target.Clauses)
            {
                string clauseExpr = ExpressionCompiler.CompileBooleanExpression(clause.Expression);
                sb.AppendLine($"                    targetMatch = targetMatch && ({clauseExpr});");
            }
            sb.AppendLine("                    if (!targetMatch) { goto skipRule" + ruleIndex + "; }");
        }

        if (rule.Condition != null)
        {
            string conditionExpr = ExpressionCompiler.CompileBooleanExpression(rule.Condition.Expression);
            sb.AppendLine($"                    if (!({conditionExpr})) {{ goto skipRule{ruleIndex}; }}");
        }

        GenerateObligationsAndAdviceCollection(sb, rule, rule.Effect, "obligations", "advice");
        GeneratePolicyLevelObligationsAndAdvice(sb, policy, rule.Effect, "obligations", "advice");
        string effect = rule.Effect == Effect.Permit ? "Permit" : "Deny";
        sb.AppendLine("                    applicableCount++;");
        sb.AppendLine($"                    applicableDecision = Decision.{effect}(obligations, advice);");

        sb.AppendLine("                }");
        sb.AppendLine("                catch");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Skip this rule on error");
        sb.AppendLine("                }");
        sb.AppendLine("                skipRule" + ruleIndex + ":;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates code to collect obligations and advice from a rule based on its effect.
    /// </summary>
    private static void GenerateObligationsAndAdviceCollection(StringBuilder sb, Rule rule, Effect effect, string obligationsVar, string adviceVar)
    {
        // Add obligations from on permit
        if (effect == Effect.Permit && rule.OnPermit != null && rule.OnPermit.Count > 0)
        {
            foreach (Obligation obligation in rule.OnPermit)
            {
                GenerateObligationCode(sb, obligation, obligationsVar);
            }
        }

        // Add advice from on deny
        if (effect == Effect.Deny && rule.OnDeny != null && rule.OnDeny.Count > 0)
        {
            foreach (Advice advice in rule.OnDeny)
            {
                GenerateAdviceCode(sb, advice, adviceVar);
            }
        }
    }

    /// <summary>
    /// Generates code to add obligations from policy level based on effect.
    /// </summary>
    private static void GeneratePolicyLevelObligationsAndAdvice(StringBuilder sb, Policy policy, Effect effect, string obligationsVar, string adviceVar)
    {
        // Add policy-level obligations from on permit
        if (effect == Effect.Permit && policy.OnPermit != null && policy.OnPermit.Count > 0)
        {
            foreach (Obligation obligation in policy.OnPermit)
            {
                GenerateObligationCode(sb, obligation, obligationsVar);
            }
        }

        // Add policy-level advice from on deny
        if (effect == Effect.Deny && policy.OnDeny != null && policy.OnDeny.Count > 0)
        {
            foreach (Advice advice in policy.OnDeny)
            {
                GenerateAdviceCode(sb, advice, adviceVar);
            }
        }
    }

    /// <summary>
    /// Generates code to create and add an obligation to the list.
    /// </summary>
    private static void GenerateObligationCode(StringBuilder sb, Obligation obligation, string listVar)
    {
        sb.AppendLine($"                    if ({listVar} == null) {{ {listVar} = new List<ObligationResult>(); }}");

        if (obligation.Attributes != null && obligation.Attributes.Count > 0)
        {
            sb.AppendLine("                    {");
            sb.AppendLine("                        var attrs = new Dictionary<string, object>();");
            foreach (KeyValuePair<string, Expression> attr in obligation.Attributes)
            {
                string attrValue = ExpressionCompiler.CompileExpression(attr.Value);
                sb.AppendLine($"                        attrs[\"{attr.Key}\"] = {attrValue};");
            }
            sb.AppendLine($"                        {listVar}.Add(new ObligationResult {{ Id = \"{obligation.Id}\", Attributes = attrs }});");
            sb.AppendLine("                    }");
        }
        else
        {
            sb.AppendLine($"                    {listVar}.Add(new ObligationResult {{ Id = \"{obligation.Id}\" }});");
        }
    }

    /// <summary>
    /// Generates code to create and add advice to the list.
    /// </summary>
    private static void GenerateAdviceCode(StringBuilder sb, Advice advice, string listVar)
    {
        sb.AppendLine($"                    if ({listVar} == null) {{ {listVar} = new List<AdviceResult>(); }}");

        if (advice.Attributes != null && advice.Attributes.Count > 0)
        {
            sb.AppendLine("                    {");
            sb.AppendLine("                        var attrs = new Dictionary<string, object>();");
            foreach (KeyValuePair<string, Expression> attr in advice.Attributes)
            {
                string attrValue = ExpressionCompiler.CompileExpression(attr.Value);
                sb.AppendLine($"                        attrs[\"{attr.Key}\"] = {attrValue};");
            }
            sb.AppendLine($"                        {listVar}.Add(new AdviceResult {{ Id = \"{advice.Id}\", Attributes = attrs }});");
            sb.AppendLine("                    }");
        }
        else
        {
            sb.AppendLine($"                    {listVar}.Add(new AdviceResult {{ Id = \"{advice.Id}\" }});");
        }
    }

    /// <summary>
    /// Compiles generated C# code to a delegate using Roslyn.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime-generated assemblies expose known members; trimming does not apply.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Generated assembly members are accessed explicitly by name.")]
    private Func<EvaluationContext, Decision> CompileToDelegate(string code)
    {
        string assemblyName = $"ABACore.Generated.Policy_{Guid.NewGuid():N}";

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            _references,
            _compilationOptions);

        using MemoryStream ms = new();
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            StringBuilder errors = new();
            errors.AppendLine("Compilation failed:");
            foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                errors.AppendLine($"  {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
            throw new InvalidOperationException(errors.ToString());
        }

        ms.Seek(0, SeekOrigin.Begin);
        Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

        Type? evaluatorType = assembly.GetType("ABACore.Generated.PolicyEvaluator")
            ?? throw new InvalidOperationException("Generated PolicyEvaluator type not found");

        MethodInfo? evaluateMethod = evaluatorType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);
        return evaluateMethod == null
            ? throw new InvalidOperationException("Generated Evaluate method not found")
            : (Func<EvaluationContext, Decision>)Delegate.CreateDelegate(
            typeof(Func<EvaluationContext, Decision>),
            evaluateMethod);
    }

    /// <summary>
    /// Gets metadata references for compilation.
    /// </summary>
    private static List<MetadataReference> GetMetadataReferences()
    {
        HashSet<string> referencePaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (string assemblyName in DefaultReferenceAssemblyNames)
        {
            referencePaths.Add(ResolveAssemblyPath(assemblyName));
        }

        referencePaths.Add(ResolveAssemblyPath(GetAssemblySimpleName(typeof(Decision).Assembly)));
        referencePaths.Add(ResolveAssemblyPath(GetAssemblySimpleName(typeof(EvaluationContext).Assembly)));

        var references = new List<MetadataReference>();
        foreach (string path in referencePaths)
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }

        return references;
    }

    private static string GetAssemblySimpleName(Assembly assembly)
    {
        string? name = assembly.GetName().Name;
        return string.IsNullOrEmpty(name)
            ? throw new InvalidOperationException($"Assembly name could not be determined for '{assembly.FullName}'.")
            : name;
    }

    private static string ResolveAssemblyPath(string simpleName)
    {
        if (AssemblyPathCache.TryGetValue(simpleName, out string? cachedPath) && File.Exists(cachedPath))
        {
            return cachedPath;
        }

        string? baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            string baseCandidate = Path.Combine(baseDirectory, simpleName + ".dll");
            if (File.Exists(baseCandidate))
            {
                AssemblyPathCache[simpleName] = baseCandidate;
                return baseCandidate;
            }
        }

        string runtimeCandidate = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), simpleName + ".dll");
        if (File.Exists(runtimeCandidate))
        {
            AssemblyPathCache[simpleName] = runtimeCandidate;
            return runtimeCandidate;
        }

        throw new InvalidOperationException($"Unable to locate assembly '{simpleName}'.");
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildTrustedAssemblyIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpaList)
        {
            foreach (string path in tpaList.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(name))
                {
                    map[name] = path;
                }
            }
        }

        return map;
    }
}
