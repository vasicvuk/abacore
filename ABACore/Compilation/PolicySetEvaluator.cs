using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Compilation;

/// <summary>
/// Static helper class containing shared policy set evaluation logic for all compilation strategies.
/// Contains the core combining algorithm implementations used by all compilers.
/// </summary>
internal static class PolicySetEvaluator
{
    public static Decision EvaluateDenyOverrides(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        bool atLeastOnePermit = false;
        bool atLeastOneError = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch
            {
                atLeastOneError = true;
                continue;
            }

            switch (decision.Effect)
            {
                case DecisionEffect.Deny:
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
                    return Decision.Deny(obligations, advice);

                case DecisionEffect.Permit:
                    atLeastOnePermit = true;
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    break;

                case DecisionEffect.Indeterminate:
                    atLeastOneError = true;
                    break;
            }
        }

        if (atLeastOnePermit)
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
            return Decision.Permit(obligations, advice);
        }

        return atLeastOneError ? Decision.Indeterminate() : Decision.NotApplicable();
    }

    public static Decision EvaluatePermitOverrides(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        bool atLeastOneDeny = false;
        bool atLeastOneError = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch
            {
                atLeastOneError = true;
                continue;
            }

            switch (decision.Effect)
            {
                case DecisionEffect.Permit:
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
                    return Decision.Permit(obligations, advice);

                case DecisionEffect.Deny:
                    atLeastOneDeny = true;
                    obligations = MergeObligations(obligations, decision.Obligations);
                    advice = MergeAdvice(advice, decision.Advice);
                    break;

                case DecisionEffect.Indeterminate:
                    atLeastOneError = true;
                    break;
            }
        }

        if (atLeastOneDeny)
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
            return Decision.Deny(obligations, advice);
        }

        return atLeastOneError ? Decision.Indeterminate() : Decision.NotApplicable();
    }

    public static Decision EvaluateFirstApplicable(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch (Exception ex)
            {
                return Decision.Indeterminate(StatusInfo.ProcessingError(ex.Message));
            }

            if (decision.Effect != DecisionEffect.NotApplicable)
            {
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);

                if (decision.Effect == DecisionEffect.Permit)
                {
                    AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
                    return Decision.Permit(obligations, advice);
                }
                else if (decision.Effect == DecisionEffect.Deny)
                {
                    AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
                    return Decision.Deny(obligations, advice);
                }
                else
                {
                    return decision;
                }
            }
        }

        return Decision.NotApplicable();
    }

    public static Decision EvaluateOnlyOne(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        int applicableCount = 0;
        Decision? applicableDecision = null;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch
            {
                continue;
            }

            if (decision.Effect != DecisionEffect.NotApplicable)
            {
                applicableCount++;
                applicableDecision = decision;
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);
            }
        }

        if (applicableCount == 1 && applicableDecision != null)
        {
            if (applicableDecision.Effect == DecisionEffect.Permit)
            {
                AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
                return Decision.Permit(obligations, advice);
            }
            else if (applicableDecision.Effect == DecisionEffect.Deny)
            {
                AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
                return Decision.Deny(obligations, advice);
            }
            return applicableDecision;
        }

        return applicableCount > 1
            ? Decision.Indeterminate(StatusInfo.ProcessingError("Multiple policies applicable in only-one algorithm"))
            : Decision.NotApplicable();
    }

    public static Decision EvaluateDenyUnlessPermit(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        bool atLeastOnePermit = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch
            {
                continue;
            }

            if (decision.Effect == DecisionEffect.Permit)
            {
                atLeastOnePermit = true;
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);
            }
        }

        if (atLeastOnePermit)
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
            return Decision.Permit(obligations, advice);
        }
        else
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
            return Decision.Deny(obligations, advice);
        }
    }

    public static Decision EvaluatePermitUnlessDeny(List<CompiledPolicy> childPolicies, EvaluationContext context, PolicySet policySet)
    {
        bool atLeastOneDeny = false;
        List<ObligationResult>? obligations = null;
        List<AdviceResult>? advice = null;

        foreach (CompiledPolicy policy in childPolicies)
        {
            Decision decision;
            try
            {
                decision = policy.Evaluate(context);
            }
            catch
            {
                continue;
            }

            if (decision.Effect == DecisionEffect.Deny)
            {
                atLeastOneDeny = true;
                obligations = MergeObligations(obligations, decision.Obligations);
                advice = MergeAdvice(advice, decision.Advice);
            }
        }

        if (atLeastOneDeny)
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Deny, ref obligations, ref advice);
            return Decision.Deny(obligations, advice);
        }
        else
        {
            AddPolicySetObligationsAndAdvice(policySet, Effect.Permit, ref obligations, ref advice);
            return Decision.Permit(obligations, advice);
        }
    }

    private static void AddPolicySetObligationsAndAdvice(PolicySet policySet, Effect effect, ref List<ObligationResult>? obligations, ref List<AdviceResult>? advice)
    {
        // Add policyset-level obligations from on permit
        if (effect == Effect.Permit && policySet.OnPermit != null)
        {
            foreach (Obligation obligation in policySet.OnPermit)
            {
                obligations ??= [];

                if (obligation.Attributes != null && obligation.Attributes.Count > 0)
                {
                    var attrs = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, Expression> attr in obligation.Attributes)
                    {
                        // For now, we'll use literal values - in production this would evaluate expressions
                        if (attr.Value is LiteralStringExpression strExpr)
                        {
                            attrs[attr.Key] = strExpr.Value;
                        }
                        else if (attr.Value is LiteralIntegerExpression intExpr)
                        {
                            attrs[attr.Key] = intExpr.Value;
                        }
                        else if (attr.Value is LiteralBooleanExpression boolExpr)
                        {
                            attrs[attr.Key] = boolExpr.Value;
                        }
                        // Add more expression types as needed
                    }
                    obligations.Add(new ObligationResult { Id = obligation.Id, Attributes = attrs });
                }
                else
                {
                    obligations.Add(new ObligationResult { Id = obligation.Id });
                }
            }
        }

        // Add policyset-level advice from on deny
        if (effect == Effect.Deny && policySet.OnDeny != null)
        {
            foreach (Advice adviceItem in policySet.OnDeny)
            {
                advice ??= [];

                if (adviceItem.Attributes != null && adviceItem.Attributes.Count > 0)
                {
                    var attrs = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, Expression> attr in adviceItem.Attributes)
                    {
                        // For now, we'll use literal values - in production this would evaluate expressions
                        if (attr.Value is LiteralStringExpression strExpr)
                        {
                            attrs[attr.Key] = strExpr.Value;
                        }
                        else if (attr.Value is LiteralIntegerExpression intExpr)
                        {
                            attrs[attr.Key] = intExpr.Value;
                        }
                        else if (attr.Value is LiteralBooleanExpression boolExpr)
                        {
                            attrs[attr.Key] = boolExpr.Value;
                        }
                        // Add more expression types as needed
                    }
                    advice.Add(new AdviceResult { Id = adviceItem.Id, Attributes = attrs });
                }
                else
                {
                    advice.Add(new AdviceResult { Id = adviceItem.Id });
                }
            }
        }
    }

    private static List<ObligationResult>? MergeObligations(List<ObligationResult>? existing, List<ObligationResult>? newOnes)
    {
        if (newOnes == null || newOnes.Count == 0)
        {
            return existing;
        }

        if (existing == null)
        {
            return [.. newOnes];
        }

        existing.AddRange(newOnes);
        return existing;
    }

    private static List<AdviceResult>? MergeAdvice(List<AdviceResult>? existing, List<AdviceResult>? newOnes)
    {
        if (newOnes == null || newOnes.Count == 0)
        {
            return existing;
        }

        if (existing == null)
        {
            return [.. newOnes];
        }

        existing.AddRange(newOnes);
        return existing;
    }
}
