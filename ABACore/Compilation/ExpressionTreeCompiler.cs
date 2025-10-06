using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ABACore.Models;
using ABACore.Runtime;
using ConstantExpression = System.Linq.Expressions.ConstantExpression;
using ExpressionTree = System.Linq.Expressions.Expression;
using ParameterExpression = System.Linq.Expressions.ParameterExpression;
using SystemBinaryExpression = System.Linq.Expressions.BinaryExpression;

namespace ABACore.Compilation;

/// <summary>
/// Compiles ALFA policy AST nodes to executable delegates using System.Linq.Expressions.
/// Creates compiled lambda expressions for efficient evaluation.
/// Provides better AOT compatibility and smaller memory footprint than Roslyn.
/// </summary>
public sealed class ExpressionTreeCompiler : IPolicyCompiler
{
    private readonly ConcurrentDictionary<string, Runtime.CompiledPolicy> _compiledPolicyCache = new(StringComparer.OrdinalIgnoreCase);

    public Runtime.CompiledPolicy CompilePolicy(Policy policy, bool enableCaching = true)
    {
        ArgumentNullException.ThrowIfNull(policy);

        string cacheKey = policy.Id ?? Guid.NewGuid().ToString();
        if (enableCaching && _compiledPolicyCache.TryGetValue(cacheKey, out Runtime.CompiledPolicy? cached))
        {
            return cached;
        }

        CombiningAlgorithm algorithm = policy.Combinator ?? CombiningAlgorithm.DenyOverrides;

        // Build expression tree for policy evaluation
        ParameterExpression contextParam = ExpressionTree.Parameter(typeof(EvaluationContext), "context");

        // Create the evaluation logic using expression trees
        ExpressionTree evalExpression = BuildPolicyEvaluation(policy, algorithm, contextParam);

        // Wrap in try-catch
        ParameterExpression exceptionVar = ExpressionTree.Variable(typeof(Exception), "ex");
        // Use MemberExpression from a lambda to avoid GetProperty reflection call
        MemberInfo messageMember = ((System.Linq.Expressions.MemberExpression)((System.Linq.Expressions.Expression<Func<Exception, string>>)(ex => ex.Message)).Body).Member;
        System.Linq.Expressions.TryExpression tryCatch = ExpressionTree.TryCatch(
            evalExpression,
            ExpressionTree.Catch(
                exceptionVar,
                ExpressionTree.Call(
                    typeof(Decision).GetMethod(nameof(Decision.Indeterminate))!,
                    ExpressionTree.Call(
                        typeof(StatusInfo).GetMethod(nameof(StatusInfo.ProcessingError))!,
                        ExpressionTree.MakeMemberAccess(exceptionVar, messageMember)
                    )
                )
            )
        );

        var lambda = ExpressionTree.Lambda<Func<EvaluationContext, Decision>>(tryCatch, contextParam);
        Func<EvaluationContext, Decision> compiled = lambda.Compile();

        Runtime.CompiledPolicy compiledPolicy = new()
        {
            PolicyId = policy.Id,
            EvaluationDelegate = compiled,
            OriginalPolicy = policy,
            GeneratedCode = $"// Expression tree compiled policy: {policy.Id}"
        };

        if (enableCaching)
        {
            _compiledPolicyCache.TryAdd(cacheKey, compiledPolicy);
        }

        return compiledPolicy;
    }

    public Runtime.CompiledPolicy CompilePolicySet(PolicySet policySet, PolicyRepository repository, bool enableCaching = true)
    {
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(repository);

        string cacheKey = policySet.Id ?? Guid.NewGuid().ToString();
        if (enableCaching && _compiledPolicyCache.TryGetValue(cacheKey, out Runtime.CompiledPolicy? cached))
        {
            return cached;
        }

        // Compile all child policies first
        List<Runtime.CompiledPolicy> childPolicies = [];

        foreach (PolicySetElement element in policySet.Elements)
        {
            if (element is Policy childPolicy)
            {
                Runtime.CompiledPolicy compiledChild = CompilePolicy(childPolicy, enableCaching);
                childPolicies.Add(compiledChild);
            }
            else if (element is PolicyReference policyRef)
            {
                Runtime.CompiledPolicy? compiledChild = repository.GetPolicy(policyRef.PolicyId) ?? throw new InvalidOperationException($"Referenced policy not found: {policyRef.PolicyId}");
                childPolicies.Add(compiledChild);
            }
            else if (element is PolicySet childPolicySet)
            {
                Runtime.CompiledPolicy compiledChild = CompilePolicySet(childPolicySet, repository, enableCaching);
                childPolicies.Add(compiledChild);
            }
        }

        CombiningAlgorithm algorithm = policySet.Combinator ?? CombiningAlgorithm.DenyOverrides;

        // Build expression tree for policyset evaluation
        ParameterExpression contextParam = ExpressionTree.Parameter(typeof(EvaluationContext), "context");

        // Create constants for captured variables
        ConstantExpression childPoliciesConstant = ExpressionTree.Constant(childPolicies);
        ConstantExpression policySetConstant = ExpressionTree.Constant(policySet);
        ConstantExpression algorithmConstant = ExpressionTree.Constant(algorithm);

        // Build target evaluation if present
        ExpressionTree evalExpression;
        if (policySet.Target != null)
        {
            ConstantExpression targetConstant = ExpressionTree.Constant(policySet.Target);
            MethodInfo evaluateTargetMethod = typeof(ExpressionTreeCompiler).GetMethod(
                nameof(EvaluateTarget),
                BindingFlags.NonPublic | BindingFlags.Static)!;

            System.Linq.Expressions.MethodCallExpression targetCheck = ExpressionTree.Call(evaluateTargetMethod, targetConstant, contextParam);
            System.Linq.Expressions.MethodCallExpression notApplicableDecision = ExpressionTree.Call(typeof(Decision).GetMethod(nameof(Decision.NotApplicable))!);

            // Call the appropriate PolicySetEvaluator method based on algorithm
            ExpressionTree evaluatorCall = BuildPolicySetEvaluatorCall(algorithmConstant, childPoliciesConstant, contextParam, policySetConstant);

            evalExpression = ExpressionTree.Condition(
                targetCheck,
                evaluatorCall,
                notApplicableDecision
            );
        }
        else
        {
            evalExpression = BuildPolicySetEvaluatorCall(algorithmConstant, childPoliciesConstant, contextParam, policySetConstant);
        }

        // Wrap in try-catch
        ParameterExpression exceptionVar = ExpressionTree.Variable(typeof(Exception), "ex");
        // Use MemberExpression from a lambda to avoid GetProperty reflection call
        MemberInfo messageMember = ((System.Linq.Expressions.MemberExpression)((System.Linq.Expressions.Expression<Func<Exception, string>>)(ex => ex.Message)).Body).Member;
        System.Linq.Expressions.TryExpression tryCatch = ExpressionTree.TryCatch(
            evalExpression,
            ExpressionTree.Catch(
                exceptionVar,
                ExpressionTree.Call(
                    typeof(Decision).GetMethod(nameof(Decision.Indeterminate))!,
                    ExpressionTree.Call(
                        typeof(StatusInfo).GetMethod(nameof(StatusInfo.ProcessingError))!,
                        ExpressionTree.MakeMemberAccess(exceptionVar, messageMember)
                    )
                )
            )
        );

        var lambda = ExpressionTree.Lambda<Func<EvaluationContext, Decision>>(tryCatch, contextParam);
        Func<EvaluationContext, Decision> compiled = lambda.Compile();

        Runtime.CompiledPolicy compiledPolicySet = new()
        {
            PolicyId = policySet.Id,
            EvaluationDelegate = compiled,
            OriginalPolicy = null,
            GeneratedCode = $"// Expression tree compiled policyset: {policySet.Id} with {childPolicies.Count} child policies"
        };

        if (enableCaching)
        {
            _compiledPolicyCache.TryAdd(cacheKey, compiledPolicySet);
        }

        return compiledPolicySet;
    }

    private static ExpressionTree BuildPolicySetEvaluatorCall(
        ConstantExpression algorithm,
        ConstantExpression childPolicies,
        ParameterExpression context,
        ConstantExpression policySet)
    {
        // Get the combining algorithm value at compile time
        var algorithmValue = (CombiningAlgorithm)algorithm.Value!;

        MethodInfo evaluatorMethod = algorithmValue switch
        {
            CombiningAlgorithm.DenyOverrides => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluateDenyOverrides))!,
            CombiningAlgorithm.PermitOverrides => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluatePermitOverrides))!,
            CombiningAlgorithm.FirstApplicable => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluateFirstApplicable))!,
            CombiningAlgorithm.OnlyOne => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluateOnlyOne))!,
            CombiningAlgorithm.DenyUnlessPermit => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluateDenyUnlessPermit))!,
            CombiningAlgorithm.PermitUnlessDeny => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluatePermitUnlessDeny))!,
            _ => typeof(PolicySetEvaluator).GetMethod(nameof(PolicySetEvaluator.EvaluateDenyOverrides))!
        };

        return ExpressionTree.Call(evaluatorMethod, childPolicies, context, policySet);
    }

    private static ExpressionTree BuildPolicyEvaluation(Policy policy, CombiningAlgorithm algorithm, ParameterExpression contextParam)
    {
        // Check target first
        if (policy.Target != null)
        {
            ConstantExpression targetConstant = ExpressionTree.Constant(policy.Target);
            MethodInfo evaluateTargetMethod = typeof(ExpressionTreeCompiler).GetMethod(
                nameof(EvaluateTarget),
                BindingFlags.NonPublic | BindingFlags.Static)!;

            System.Linq.Expressions.MethodCallExpression targetCheck = ExpressionTree.Call(evaluateTargetMethod, targetConstant, contextParam);
            System.Linq.Expressions.MethodCallExpression notApplicableDecision = ExpressionTree.Call(typeof(Decision).GetMethod(nameof(Decision.NotApplicable))!);

            ExpressionTree policyEvaluation = BuildAlgorithmEvaluation(policy, algorithm, contextParam);

            return ExpressionTree.Condition(
                targetCheck,
                policyEvaluation,
                notApplicableDecision
            );
        }

        return BuildAlgorithmEvaluation(policy, algorithm, contextParam);
    }

    private static ExpressionTree BuildAlgorithmEvaluation(Policy policy, CombiningAlgorithm algorithm, ParameterExpression contextParam)
    {
        // For expression trees, we'll capture the policy and call static helper methods
        // This is simpler than building the entire algorithm logic as expression trees
        ConstantExpression policyConstant = ExpressionTree.Constant(policy);
        ConstantExpression algorithmConstant = ExpressionTree.Constant(algorithm);

        MethodInfo evaluatePolicyMethod = typeof(ExpressionTreeCompiler).GetMethod(
            nameof(EvaluatePolicy),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return ExpressionTree.Call(evaluatePolicyMethod, policyConstant, algorithmConstant, contextParam);
    }

    public void ClearCache()
    {
        _compiledPolicyCache.Clear();
    }

    public bool RemoveFromCache(string policyId)
    {
        return _compiledPolicyCache.TryRemove(policyId, out _);
    }

    #region Static Evaluation Helpers

    private static Decision EvaluatePolicy(Policy policy, CombiningAlgorithm algorithm, EvaluationContext context)
    {
        return algorithm switch
        {
            CombiningAlgorithm.DenyOverrides => EvaluateDenyOverrides(policy, context),
            CombiningAlgorithm.PermitOverrides => EvaluatePermitOverrides(policy, context),
            CombiningAlgorithm.FirstApplicable => EvaluateFirstApplicable(policy, context),
            CombiningAlgorithm.OnlyOne => EvaluateOnlyOne(policy, context),
            CombiningAlgorithm.DenyUnlessPermit => EvaluateDenyUnlessPermit(policy, context),
            CombiningAlgorithm.PermitUnlessDeny => EvaluatePermitUnlessDeny(policy, context),
            _ => EvaluateDenyOverrides(policy, context)
        };
    }

    private static Decision EvaluateDenyOverrides(Policy policy, EvaluationContext context)
    {
        bool atLeastOnePermit = false;
        bool atLeastOneError = false;
        List<ObligationResult>? collectedPermitObligations = null;

        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                if (result.Effect == Effect.Deny)
                {
                    List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                    MergeObligations(ref obligations, collectedPermitObligations);
                    List<AdviceResult>? advice = CloneAdvice(result.Advice);
                    MergeAdvice(ref advice, EvaluateAdvice(policy.OnDeny, context));
                    return Decision.Deny(obligations, advice);
                }

                MergeObligations(ref collectedPermitObligations, result.Obligations);
                atLeastOnePermit = true;
            }
            catch
            {
                atLeastOneError = true;
            }
        }

        if (atLeastOnePermit)
        {
            MergeObligations(ref collectedPermitObligations, EvaluateObligations(policy.OnPermit, context));
            return Decision.Permit(collectedPermitObligations, null);
        }

        return atLeastOneError ? Decision.Indeterminate(StatusInfo.ProcessingError("Rule evaluation error")) : Decision.NotApplicable();
    }

    private static Decision EvaluatePermitOverrides(Policy policy, EvaluationContext context)
    {
        bool atLeastOneDeny = false;
        bool atLeastOneError = false;
        List<AdviceResult>? collectedDenyAdvice = null;

        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                if (result.Effect == Effect.Permit)
                {
                    List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                    MergeObligations(ref obligations, EvaluateObligations(policy.OnPermit, context));
                    return Decision.Permit(obligations, null);
                }

                MergeAdvice(ref collectedDenyAdvice, result.Advice);
                atLeastOneDeny = true;
            }
            catch
            {
                atLeastOneError = true;
            }
        }

        if (atLeastOneDeny)
        {
            MergeAdvice(ref collectedDenyAdvice, EvaluateAdvice(policy.OnDeny, context));
            return Decision.Deny(null, collectedDenyAdvice);
        }

        return atLeastOneError ? Decision.Indeterminate(StatusInfo.ProcessingError("Rule evaluation error")) : Decision.NotApplicable();
    }

    private static Decision EvaluateFirstApplicable(Policy policy, EvaluationContext context)
    {
        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                if (result.Effect == Effect.Permit)
                {
                    List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                    MergeObligations(ref obligations, EvaluateObligations(policy.OnPermit, context));
                    return Decision.Permit(obligations, null);
                }

                List<AdviceResult>? advice = CloneAdvice(result.Advice);
                MergeAdvice(ref advice, EvaluateAdvice(policy.OnDeny, context));
                return Decision.Deny(null, advice);
            }
            catch (Exception ex)
            {
                return Decision.Indeterminate(StatusInfo.ProcessingError(ex.Message));
            }
        }

        return Decision.NotApplicable();
    }

    private static Decision EvaluateOnlyOne(Policy policy, EvaluationContext context)
    {
        Decision? capturedDecision = null;
        int applicableCount = 0;

        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                applicableCount++;
                if (applicableCount > 1)
                {
                    return Decision.Indeterminate(StatusInfo.ProcessingError("Multiple rules applicable in only-one algorithm"));
                }

                if (result.Effect == Effect.Permit)
                {
                    List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                    MergeObligations(ref obligations, EvaluateObligations(policy.OnPermit, context));
                    capturedDecision = Decision.Permit(obligations, null);
                }
                else
                {
                    List<AdviceResult>? advice = CloneAdvice(result.Advice);
                    MergeAdvice(ref advice, EvaluateAdvice(policy.OnDeny, context));
                    capturedDecision = Decision.Deny(null, advice);
                }
            }
            catch (Exception ex)
            {
                return Decision.Indeterminate(StatusInfo.ProcessingError(ex.Message));
            }
        }

        return applicableCount == 1 && capturedDecision != null ? capturedDecision : Decision.NotApplicable();
    }

    private static Decision EvaluateDenyUnlessPermit(Policy policy, EvaluationContext context)
    {
        Exception? evaluationError = null;

        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                if (result.Effect == Effect.Permit)
                {
                    List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                    MergeObligations(ref obligations, EvaluateObligations(policy.OnPermit, context));
                    return Decision.Permit(obligations, null);
                }

                List<AdviceResult>? advice = CloneAdvice(result.Advice);
                MergeAdvice(ref advice, EvaluateAdvice(policy.OnDeny, context));
                return Decision.Deny(null, advice);
            }
            catch (Exception ex)
            {
                evaluationError = ex;
            }
        }

        if (evaluationError != null)
        {
            return Decision.Indeterminate(StatusInfo.ProcessingError(evaluationError.Message));
        }

        List<AdviceResult>? defaultAdvice = EvaluateAdvice(policy.OnDeny, context);
        return Decision.Deny(null, defaultAdvice);
    }

    private static Decision EvaluatePermitUnlessDeny(Policy policy, EvaluationContext context)
    {
        Exception? evaluationError = null;

        foreach (Rule rule in policy.Rules)
        {
            try
            {
                RuleEvaluationResult result = EvaluateRule(rule, context);
                if (!result.IsApplicable)
                {
                    continue;
                }

                if (result.Effect == Effect.Deny)
                {
                    List<AdviceResult>? advice = CloneAdvice(result.Advice);
                    MergeAdvice(ref advice, EvaluateAdvice(policy.OnDeny, context));
                    return Decision.Deny(null, advice);
                }

                List<ObligationResult>? obligations = CloneObligations(result.Obligations);
                MergeObligations(ref obligations, EvaluateObligations(policy.OnPermit, context));
                return Decision.Permit(obligations, null);
            }
            catch (Exception ex)
            {
                evaluationError = ex;
            }
        }

        if (evaluationError != null)
        {
            return Decision.Indeterminate(StatusInfo.ProcessingError(evaluationError.Message));
        }

        List<ObligationResult>? defaultObligations = EvaluateObligations(policy.OnPermit, context);
        return Decision.Permit(defaultObligations, null);
    }

    private readonly struct RuleEvaluationResult(bool isApplicable, Effect effect, List<ObligationResult>? obligations, List<AdviceResult>? advice)
    {
        public bool IsApplicable { get; } = isApplicable;
        public Effect Effect { get; } = effect;
        public List<ObligationResult>? Obligations { get; } = obligations;
        public List<AdviceResult>? Advice { get; } = advice;

        public static RuleEvaluationResult NotApplicable => new(false, Effect.Permit, null, null);
    }

    private static RuleEvaluationResult EvaluateRule(Rule rule, EvaluationContext context)
    {
        if (rule.Target != null && !EvaluateTarget(rule.Target, context))
        {
            return RuleEvaluationResult.NotApplicable;
        }

        if (rule.Condition != null && !EvaluateBoolean(rule.Condition.Expression, context))
        {
            return RuleEvaluationResult.NotApplicable;
        }

        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        if (rule.Effect == Effect.Permit)
        {
            obligations = EvaluateObligations(rule.OnPermit, context);
        }
        else
        {
            advice = EvaluateAdvice(rule.OnDeny, context);
        }

        return new RuleEvaluationResult(true, rule.Effect, obligations, advice);
    }

    private static bool EvaluateTarget(Target target, EvaluationContext context)
    {
        foreach (Clause clause in target.Clauses)
        {
            if (!EvaluateBoolean(clause.Expression, context))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateBoolean(BooleanExpression expression, EvaluationContext context)
    {
        return expression switch
        {
            BooleanLiteralExpression literal => literal.Value,
            BooleanAttributeDesignator designator => EvaluateBooleanAttribute(designator, context),
            NotExpression notExpression => !EvaluateBoolean(notExpression.InnerExpression, context),
            LogicalBinaryExpression logical => EvaluateLogicalExpression(logical, context),
            ComparisonExpression comparison => EvaluateComparison(comparison, context),
            BooleanFunctionCall functionCall => InvokeBooleanFunction(functionCall.FunctionName, functionCall.Parameters, context),
            BooleanAllExpression allExpression => EvaluateBooleanAll(allExpression, context),
            _ => false
        };
    }

    private static bool EvaluateBooleanAttribute(BooleanAttributeDesignator designator, EvaluationContext context)
    {
        object? value = context.GetAttributeByNamespace(designator.Namespace, designator.AttributeName);
        return value is bool boolValue
            ? boolValue
            : value == null
            ? designator.MustBePresent
                ? throw new InvalidOperationException($"Required attribute {designator.Namespace}.{designator.AttributeName} is missing")
                : false
            : value is string stringValue && bool.TryParse(stringValue, out bool parsed)
            ? parsed
            : throw new InvalidOperationException($"Attribute {designator.Namespace}.{designator.AttributeName} cannot be converted to a boolean value");
    }

    private static bool EvaluateLogicalExpression(LogicalBinaryExpression logical, EvaluationContext context)
    {
        bool left = EvaluateBoolean(logical.Left, context);

        return logical.Operator switch
        {
            LogicalOperator.And or LogicalOperator.AndClause => left && EvaluateBoolean(logical.Right, context),
            LogicalOperator.Or or LogicalOperator.OrClause => left || EvaluateBoolean(logical.Right, context),
            _ => false
        };
    }

    private static bool EvaluateComparison(ComparisonExpression comparison, EvaluationContext context)
    {
        object? left = EvaluateValue(comparison.Left, context);
        object? right = EvaluateValue(comparison.Right, context);

        return comparison.Operator switch
        {
            ComparisonOperator.Equal => Equals(left, right),
            ComparisonOperator.NotEqual => !Equals(left, right),
            ComparisonOperator.GreaterThan => CompareValues(left, right) > 0,
            ComparisonOperator.LessThan => CompareValues(left, right) < 0,
            ComparisonOperator.GreaterThanOrEqual => CompareValues(left, right) >= 0,
            ComparisonOperator.LessThanOrEqual => CompareValues(left, right) <= 0,
            _ => false
        };
    }

    private static bool EvaluateBooleanAll(BooleanAllExpression expression, EvaluationContext context)
    {
        object? value = EvaluateValue(expression.InnerExpression, context);
        if (value is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                if (item is bool boolItem)
                {
                    if (!boolItem)
                    {
                        return false;
                    }
                }
                else if (item == null)
                {
                    return false;
                }
            }

            return true;
        }

        return value is bool single && single;
    }

    private static bool InvokeBooleanFunction(string functionName, IReadOnlyList<Expression> parameters, EvaluationContext context)
    {
        object? result = InvokeValueFunction(functionName, parameters, context);
        return result switch
        {
            bool boolResult => boolResult,
            null => false,
            _ => Convert.ToBoolean(result, CultureInfo.InvariantCulture)
        };
    }

    private static object? EvaluateValue(Expression expression, EvaluationContext context)
    {
        return expression switch
        {
            LiteralStringExpression literal => literal.Value,
            LiteralIntegerExpression literal => literal.Value,
            LiteralDoubleExpression literal => literal.Value,
            LiteralBooleanExpression literal => literal.Value,
            AttributeDesignator designator => EvaluateAttribute(designator, context),
            ValueCoercionExpression coercion => CoerceValue(coercion),
            BinaryExpression binary => EvaluateArithmetic(binary, context),
            FunctionCall functionCall => InvokeValueFunction(functionCall.FunctionName, functionCall.Parameters, context),
            _ => null
        };
    }

    private static object? EvaluateAttribute(AttributeDesignator designator, EvaluationContext context)
    {
        object? value = context.GetAttributeByNamespace(designator.Namespace, designator.AttributeName);
        return value == null && designator.MustBePresent
            ? throw new InvalidOperationException($"Required attribute {designator.Namespace}.{designator.AttributeName} is missing")
            : value;
    }

    private static object? CoerceValue(ValueCoercionExpression coercion)
    {
        string raw = coercion.Value;
        return coercion.TargetType switch
        {
            AttributeType.Boolean => bool.Parse(raw),
            AttributeType.Integer => int.Parse(raw, CultureInfo.InvariantCulture),
            AttributeType.Double => double.Parse(raw, CultureInfo.InvariantCulture),
            AttributeType.DateTime => DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            AttributeType.Date => DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Date,
            AttributeType.Time => TimeSpan.Parse(raw, CultureInfo.InvariantCulture),
            AttributeType.Duration => TimeSpan.Parse(raw, CultureInfo.InvariantCulture),
            _ => raw
        };
    }

    private static object? EvaluateArithmetic(BinaryExpression expression, EvaluationContext context)
    {
        object? leftValue = EvaluateValue(expression.Left, context);
        object? rightValue = EvaluateValue(expression.Right, context);

        double left = Convert.ToDouble(leftValue, CultureInfo.InvariantCulture);
        double right = Convert.ToDouble(rightValue, CultureInfo.InvariantCulture);

        return expression.Operator switch
        {
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => right == 0 ? double.NaN : left / right,
            _ => double.NaN
        };
    }

    private static object? InvokeValueFunction(string functionName, IReadOnlyList<Expression> parameters, EvaluationContext context)
    {
        string methodName = ConvertFunctionName(functionName);
        MethodInfo? method = typeof(Functions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException($"Function '{functionName}' is not supported.");

        object?[] arguments = new object?[method.GetParameters().Length];
        for (int i = 0; i < arguments.Length && i < parameters.Count; i++)
        {
            arguments[i] = EvaluateValue(parameters[i], context);
        }

        return method.Invoke(null, arguments);
    }

    private static string ConvertFunctionName(string functionName)
    {
        return string.IsNullOrWhiteSpace(functionName)
            ? functionName
            : functionName.Length == 1
            ? functionName.ToUpperInvariant()
            : char.ToUpperInvariant(functionName[0]) + functionName[1..];
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return -1;
        }

        if (left is IComparable leftComparable && right is IComparable rightComparable)
        {
            if (left.GetType() == right.GetType())
            {
                return leftComparable.CompareTo(rightComparable);
            }

            double leftDouble = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            double rightDouble = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftDouble.CompareTo(rightDouble);
        }

        throw new InvalidOperationException("Values are not comparable.");
    }

    private static List<ObligationResult>? EvaluateObligations(List<Obligation>? obligations, EvaluationContext context)
    {
        if (obligations == null || obligations.Count == 0)
        {
            return null;
        }

        List<ObligationResult> results = new(obligations.Count);
        foreach (Obligation obligation in obligations)
        {
            Dictionary<string, object>? attributes = EvaluateAttributes(obligation.Attributes, context);
            results.Add(new ObligationResult { Id = obligation.Id, Attributes = attributes });
        }

        return results;
    }

    private static List<AdviceResult>? EvaluateAdvice(List<Advice>? advice, EvaluationContext context)
    {
        if (advice == null || advice.Count == 0)
        {
            return null;
        }

        List<AdviceResult> results = new(advice.Count);
        foreach (Advice item in advice)
        {
            Dictionary<string, object>? attributes = EvaluateAttributes(item.Attributes, context);
            results.Add(new AdviceResult { Id = item.Id, Attributes = attributes });
        }

        return results;
    }

    private static Dictionary<string, object>? EvaluateAttributes(Dictionary<string, Expression>? attributes, EvaluationContext context)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return null;
        }

        Dictionary<string, object> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Expression> pair in attributes)
        {
            result[pair.Key] = EvaluateValue(pair.Value, context) ?? string.Empty;
        }

        return result;
    }

    private static void MergeObligations(ref List<ObligationResult>? target, List<ObligationResult>? source)
    {
        if (source == null || source.Count == 0)
        {
            return;
        }

        target ??= [];
        foreach (ObligationResult obligation in source)
        {
            target.Add(CloneObligation(obligation));
        }
    }

    private static void MergeAdvice(ref List<AdviceResult>? target, List<AdviceResult>? source)
    {
        if (source == null || source.Count == 0)
        {
            return;
        }

        target ??= [];
        foreach (AdviceResult advice in source)
        {
            target.Add(CloneAdvice(advice));
        }
    }

    private static List<ObligationResult>? CloneObligations(List<ObligationResult>? source)
    {
        if (source == null)
        {
            return null;
        }

        List<ObligationResult> clone = new(source.Count);
        foreach (ObligationResult obligation in source)
        {
            clone.Add(CloneObligation(obligation));
        }

        return clone;
    }

    private static List<AdviceResult>? CloneAdvice(List<AdviceResult>? source)
    {
        if (source == null)
        {
            return null;
        }

        List<AdviceResult> clone = new(source.Count);
        foreach (AdviceResult advice in source)
        {
            clone.Add(CloneAdvice(advice));
        }

        return clone;
    }

    private static ObligationResult CloneObligation(ObligationResult obligation)
    {
        return new ObligationResult
        {
            Id = obligation.Id,
            Attributes = obligation.Attributes != null ? new Dictionary<string, object>(obligation.Attributes, StringComparer.OrdinalIgnoreCase) : null
        };
    }

    private static AdviceResult CloneAdvice(AdviceResult advice)
    {
        return new AdviceResult
        {
            Id = advice.Id,
            Attributes = advice.Attributes != null ? new Dictionary<string, object>(advice.Attributes, StringComparer.OrdinalIgnoreCase) : null
        };
    }

    #endregion
}
