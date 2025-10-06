using System;
using System.Collections.Generic;
using System.Linq;
using ABACore.Exceptions;
using ABACore.Models;

namespace ABACore.Runtime;

/// <summary>
/// Executes compiled policies with proper combining algorithm handling.
/// Supports single policy execution and policy set evaluation.
/// </summary>
public sealed class PolicyExecutor
{
    /// <summary>
    /// Executes a single compiled policy with the given evaluation context.
    /// </summary>
    /// <param name="policy">The compiled policy to execute.</param>
    /// <param name="context">The evaluation context containing attribute values.</param>
    /// <returns>The policy decision.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy or context is null.</exception>
    /// <exception cref="AlfaEvaluationException">Thrown when policy evaluation fails.</exception>
    public Decision Execute(CompiledPolicy policy, EvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return policy.Evaluate(context);
        }
        catch (Exception ex) when (ex is not AlfaEvaluationException)
        {
            throw new AlfaEvaluationException(
                ex.Message,
                policy.PolicyId ?? "unknown",
                ex);
        }
    }

    /// <summary>
    /// Executes multiple policies using the specified combining algorithm.
    /// </summary>
    /// <param name="policies">The compiled policies to execute.</param>
    /// <param name="context">The evaluation context containing attribute values.</param>
    /// <param name="combiningAlgorithm">The combining algorithm to use.</param>
    /// <returns>The combined policy decision.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policies or context is null.</exception>
    /// <exception cref="ArgumentException">Thrown when policies collection is empty.</exception>
    public Decision ExecutePolicySet(
        IEnumerable<CompiledPolicy> policies,
        EvaluationContext context,
        CombiningAlgorithm combiningAlgorithm = CombiningAlgorithm.DenyOverrides)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(context);

        var policyList = policies.ToList();

        return policyList.Count == 0
            ? throw new ArgumentException("Policy collection cannot be empty.", nameof(policies))
            : combiningAlgorithm switch
            {
                CombiningAlgorithm.DenyOverrides => ExecuteDenyOverrides(policyList, context),
                CombiningAlgorithm.PermitOverrides => ExecutePermitOverrides(policyList, context),
                CombiningAlgorithm.FirstApplicable => ExecuteFirstApplicable(policyList, context),
                CombiningAlgorithm.OnlyOne => ExecuteOnlyOne(policyList, context),
                CombiningAlgorithm.DenyUnlessPermit => ExecuteDenyUnlessPermit(policyList, context),
                CombiningAlgorithm.PermitUnlessDeny => ExecutePermitUnlessDeny(policyList, context),
                _ => ExecuteDenyOverrides(policyList, context)
            };
    }

    /// <summary>
    /// Executes policies using the deny-overrides combining algorithm.
    /// Any Deny decision returns Deny immediately. If at least one Permit and no Deny, returns Permit.
    /// </summary>
    private Decision ExecuteDenyOverrides(List<CompiledPolicy> policies, EvaluationContext context)
    {
        bool atLeastOnePermit = false;
        bool atLeastOneError = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception)
            {
                atLeastOneError = true;
                continue;
            }

            switch (decision.Effect)
            {
                case DecisionEffect.Deny:
                    return MergeObligationsAndAdvice(decision, obligations, advice);

                case DecisionEffect.Permit:
                    atLeastOnePermit = true;
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    break;

                case DecisionEffect.Indeterminate:
                    atLeastOneError = true;
                    break;

                case DecisionEffect.NotApplicable:
                    // Continue to next policy
                    break;
            }
        }

        return atLeastOnePermit
            ? Decision.Permit(obligations, advice)
            : atLeastOneError
            ? Decision.Indeterminate(StatusInfo.ProcessingError("At least one policy evaluation resulted in an error."))
            : Decision.NotApplicable();
    }

    /// <summary>
    /// Executes policies using the permit-overrides combining algorithm.
    /// Any Permit decision returns Permit immediately. If at least one Deny and no Permit, returns Deny.
    /// </summary>
    private Decision ExecutePermitOverrides(List<CompiledPolicy> policies, EvaluationContext context)
    {
        bool atLeastOneDeny = false;
        bool atLeastOneError = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception)
            {
                atLeastOneError = true;
                continue;
            }

            switch (decision.Effect)
            {
                case DecisionEffect.Permit:
                    return MergeObligationsAndAdvice(decision, obligations, advice);

                case DecisionEffect.Deny:
                    atLeastOneDeny = true;
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    break;

                case DecisionEffect.Indeterminate:
                    atLeastOneError = true;
                    break;

                case DecisionEffect.NotApplicable:
                    // Continue to next policy
                    break;
            }
        }

        return atLeastOneDeny
            ? Decision.Deny(obligations, advice)
            : atLeastOneError
            ? Decision.Indeterminate(StatusInfo.ProcessingError("At least one policy evaluation resulted in an error."))
            : Decision.NotApplicable();
    }

    /// <summary>
    /// Executes policies using the first-applicable combining algorithm.
    /// Returns the decision of the first applicable policy (Permit or Deny).
    /// </summary>
    private Decision ExecuteFirstApplicable(List<CompiledPolicy> policies, EvaluationContext context)
    {
        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception ex)
            {
                return Decision.Indeterminate(StatusInfo.ProcessingError($"Policy evaluation error: {ex.Message}"));
            }

            if (decision.Effect == DecisionEffect.Permit || decision.Effect == DecisionEffect.Deny)
            {
                return decision;
            }

            if (decision.Effect == DecisionEffect.Indeterminate)
            {
                return decision;
            }

            // NotApplicable - continue to next policy
        }

        return Decision.NotApplicable();
    }

    /// <summary>
    /// Executes policies using the only-one combining algorithm.
    /// Exactly one policy must be applicable. If more than one is applicable, returns Indeterminate.
    /// </summary>
    private Decision ExecuteOnlyOne(List<CompiledPolicy> policies, EvaluationContext context)
    {
        int applicableCount = 0;
        Decision? applicableDecision = null;

        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception)
            {
                // Skip policies that error
                continue;
            }

            if (decision.Effect == DecisionEffect.Permit || decision.Effect == DecisionEffect.Deny)
            {
                applicableCount++;
                applicableDecision = decision;

                if (applicableCount > 1)
                {
                    return Decision.Indeterminate(
                        StatusInfo.ProcessingError("Multiple policies applicable in only-one combining algorithm."));
                }
            }
        }

        return applicableCount == 1 && applicableDecision != null ? applicableDecision : Decision.NotApplicable();
    }

    /// <summary>
    /// Executes policies using the deny-unless-permit combining algorithm.
    /// Returns Deny unless at least one policy returns Permit.
    /// </summary>
    private Decision ExecuteDenyUnlessPermit(List<CompiledPolicy> policies, EvaluationContext context)
    {
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception)
            {
                // Ignore errors in this algorithm
                continue;
            }

            if (decision.Effect == DecisionEffect.Permit)
            {
                return MergeObligationsAndAdvice(decision, obligations, advice);
            }

            if (decision.Effect == DecisionEffect.Deny)
            {
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);
            }
        }

        return Decision.Deny(obligations, advice);
    }

    /// <summary>
    /// Executes policies using the permit-unless-deny combining algorithm.
    /// Returns Permit unless at least one policy returns Deny.
    /// </summary>
    private Decision ExecutePermitUnlessDeny(List<CompiledPolicy> policies, EvaluationContext context)
    {
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in policies)
        {
            Decision decision;

            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception)
            {
                // Ignore errors in this algorithm
                continue;
            }

            if (decision.Effect == DecisionEffect.Deny)
            {
                return MergeObligationsAndAdvice(decision, obligations, advice);
            }

            if (decision.Effect == DecisionEffect.Permit)
            {
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);
            }
        }

        return Decision.Permit(obligations, advice);
    }

    /// <summary>
    /// Merges obligations from multiple decisions.
    /// </summary>
    private static List<ObligationResult>? MergeObligations(List<ObligationResult>? existing, List<ObligationResult>? additional)
    {
        if (additional == null || additional.Count == 0)
        {
            return existing;
        }

        if (existing == null)
        {
            return [.. additional];
        }

        existing.AddRange(additional);
        return existing;
    }

    /// <summary>
    /// Merges advice from multiple decisions.
    /// </summary>
    private static List<AdviceResult>? MergeAdvice(List<AdviceResult>? existing, List<AdviceResult>? additional)
    {
        if (additional == null || additional.Count == 0)
        {
            return existing;
        }

        if (existing == null)
        {
            return [.. additional];
        }

        existing.AddRange(additional);
        return existing;
    }

    /// <summary>
    /// Merges obligations and advice from accumulated and current decisions.
    /// </summary>
    private static Decision MergeObligationsAndAdvice(
        Decision currentDecision,
        List<ObligationResult>? accumulatedObligations,
        List<AdviceResult>? accumulatedAdvice)
    {
        List<ObligationResult>? mergedObligations = MergeObligations(accumulatedObligations, currentDecision.Obligations);
        List<AdviceResult>? mergedAdvice = MergeAdvice(accumulatedAdvice, currentDecision.Advice);

        return mergedObligations == currentDecision.Obligations && mergedAdvice == currentDecision.Advice
            ? currentDecision
            : new Decision
            {
                Effect = currentDecision.Effect,
                Obligations = mergedObligations,
                Advice = mergedAdvice,
                Status = currentDecision.Status
            };
    }
}
