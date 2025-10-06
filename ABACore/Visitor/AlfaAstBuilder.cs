using ABACore.Models;
using ABACore.Parser;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ABACore.Visitor;

/// <summary>
/// ANTLR visitor implementation that converts parse tree to ALFA AST models.
/// This visitor traverses the ANTLR parse tree and builds strongly-typed AST nodes.
/// </summary>
public sealed class AlfaAstBuilder : AlfaBaseVisitor<AlfaNode>
{
    /// <summary>
    /// Visits a namespace declaration.
    /// </summary>
    /// <param name="context">The namespace context.</param>
    /// <returns>A Namespace AST node.</returns>
    public override AlfaNode VisitNamespace([NotNull] AlfaParser.NamespaceContext context)
    {
        string name = context.IDENTIFIER()?.GetText() ?? string.Empty;
        List<Statement> statements = [];

        if (context.statement() != null)
        {
            foreach (AlfaParser.StatementContext stmtCtx in context.statement())
            {
                AlfaNode? node = Visit(stmtCtx);
                switch (node)
                {
                    case Statement stmt:
                        statements.Add(stmt);
                        break;
                    case Policy policy:
                        statements.Add(new PolicyStatement { Policy = policy });
                        break;
                    case PolicySet policySet:
                        statements.Add(new PolicySetStatement { PolicySet = policySet });
                        break;
                }
            }
        }

        return new Namespace
        {
            Name = name,
            Statements = statements
        };
    }

    /// <summary>
    /// Visits an import statement.
    /// </summary>
    /// <param name="context">The import namespace context.</param>
    /// <returns>An Import AST node.</returns>
    public override AlfaNode VisitImportNamespace([NotNull] AlfaParser.ImportNamespaceContext context)
    {
        string namespacePath = context.IDENTIFIER()?.GetText() ?? string.Empty;
        bool wildcard = context.WILDCARD() != null;

        return new Import
        {
            NamespacePath = namespacePath,
            Wildcard = wildcard
        };
    }

    /// <summary>
    /// Visits an attribute declaration.
    /// </summary>
    /// <param name="context">The attribute declaration context.</param>
    /// <returns>An AttributeDeclaration AST node.</returns>
    public override AlfaNode VisitAttributeDeclaration([NotNull] AlfaParser.AttributeDeclarationContext context)
    {
        string name = context.IDENTIFIER()?.GetText() ?? string.Empty;
        string? category = null;
        string? id = null;
        AttributeType? type = null;

        if (context.attributeBody() != null)
        {
            AlfaParser.AttributeBodyContext body = context.attributeBody();

            if (body.attributeCategoryAssignment() != null)
            {
                foreach (AlfaParser.AttributeCategoryAssignmentContext catCtx in body.attributeCategoryAssignment())
                {
                    category = catCtx.IDENTIFIER()?.GetText();
                }
            }

            if (body.attributeIdAssignment() != null)
            {
                foreach (AlfaParser.AttributeIdAssignmentContext idCtx in body.attributeIdAssignment())
                {
                    id = idCtx.LITERAL_STRING()?.GetText()?.Trim('"', '\'');
                }
            }

            if (body.attributeTypeAssignment() != null)
            {
                foreach (AlfaParser.AttributeTypeAssignmentContext typeCtx in body.attributeTypeAssignment())
                {
                    type = ParseAttributeType(typeCtx.typeName());
                }
            }
        }

        return new AttributeDeclaration
        {
            Name = name,
            Category = category,
            Id = id,
            Type = type
        };
    }

    /// <summary>
    /// Visits a policy set.
    /// </summary>
    /// <param name="context">The policyset context.</param>
    /// <returns>A PolicySet AST node.</returns>
    public override AlfaNode VisitPolicyset([NotNull] AlfaParser.PolicysetContext context)
    {
        string? id = context.IDENTIFIER()?.GetText();

        if (context.policysetBody() == null)
        {
            return new PolicySet
            {
                Id = id,
                Elements = []
            };
        }

        AlfaParser.PolicysetBodyContext body = context.policysetBody();
        (List<PolicySetElement> elements, List<Obligation>? onPermit, List<Advice>? onDeny) = ExtractPolicySetElements(body);

        return new PolicySet
        {
            Id = id,
            Target = ExtractTargetFromPolicySet(body),
            Combinator = ExtractPolicySetCombinator(body),
            Elements = elements,
            OnPermit = onPermit,
            OnDeny = onDeny
        };
    }

    private Target? ExtractTargetFromPolicySet(AlfaParser.PolicysetBodyContext body)
    {
        if (body.target() == null) return null;

        foreach (AlfaParser.TargetContext targetCtx in body.target())
        {
            if (Visit(targetCtx) is Target t)
            {
                return t;
            }
        }
        return null;
    }

    private CombiningAlgorithm? ExtractPolicySetCombinator(AlfaParser.PolicysetBodyContext body)
    {
        if (body.policyCombinator() == null) return null;

        foreach (AlfaParser.PolicyCombinatorContext combCtx in body.policyCombinator())
        {
            return ParseCombiningAlgorithm(combCtx);
        }
        return null;
    }

    private (List<PolicySetElement>, List<Obligation>?, List<Advice>?) ExtractPolicySetElements(AlfaParser.PolicysetBodyContext body)
    {
        List<PolicySetElement> elements = [];
        List<Obligation>? onPermit = null;
        List<Advice>? onDeny = null;

        if (body.policysetPolicy() == null)
        {
            return (elements, onPermit, onDeny);
        }

        foreach (AlfaParser.PolicysetPolicyContext policyCtx in body.policysetPolicy())
        {
            ProcessPolicySetElement(policyCtx, elements, ref onPermit, ref onDeny);
        }

        return (elements, onPermit, onDeny);
    }

    private void ProcessPolicySetElement(
        AlfaParser.PolicysetPolicyContext policyCtx,
        List<PolicySetElement> elements,
        ref List<Obligation>? onPermit,
        ref List<Advice>? onDeny)
    {
        if (policyCtx.policy() != null)
        {
            if (Visit(policyCtx.policy()) is Policy p)
            {
                elements.Add(p);
            }
        }
        else if (policyCtx.POLICY() != null && policyCtx.IDENTIFIER() != null)
        {
            elements.Add(new PolicyReference
            {
                PolicyId = policyCtx.IDENTIFIER().GetText()
            });
        }
        else if (policyCtx.policyset() != null)
        {
            if (Visit(policyCtx.policyset()) is PolicySet ps)
            {
                elements.Add(ps);
            }
        }
        else if (policyCtx.onPermit() != null)
        {
            if (Visit(policyCtx.onPermit()) is OnPermitNode opn)
            {
                onPermit = opn.Obligations;
            }
        }
        else if (policyCtx.onDeny() != null)
        {
            if (Visit(policyCtx.onDeny()) is OnDenyNode odn)
            {
                onDeny = odn.Advice;
            }
        }
    }

    /// <summary>
    /// Visits a policy.
    /// </summary>
    /// <param name="context">The policy context.</param>
    /// <returns>A Policy AST node.</returns>
    public override AlfaNode VisitPolicy([NotNull] AlfaParser.PolicyContext context)
    {
        string? id = context.IDENTIFIER()?.GetText();

        if (context.policyBody() == null)
        {
            return new Policy
            {
                Id = id,
                Rules = []
            };
        }

        AlfaParser.PolicyBodyContext body = context.policyBody();

        return new Policy
        {
            Id = id,
            Target = ExtractTarget(body),
            Combinator = ExtractCombinator(body),
            Rules = ExtractRules(body),
            OnPermit = ExtractOnPermit(body),
            OnDeny = ExtractOnDeny(body)
        };
    }

    private Target? ExtractTarget(AlfaParser.PolicyBodyContext body)
    {
        if (body.target() == null) return null;

        foreach (AlfaParser.TargetContext targetCtx in body.target())
        {
            if (Visit(targetCtx) is Target t)
            {
                return t;
            }
        }
        return null;
    }

    private CombiningAlgorithm? ExtractCombinator(AlfaParser.PolicyBodyContext body)
    {
        if (body.policyCombinator() == null) return null;

        foreach (AlfaParser.PolicyCombinatorContext combCtx in body.policyCombinator())
        {
            return ParseCombiningAlgorithm(combCtx);
        }
        return null;
    }

    private List<Rule> ExtractRules(AlfaParser.PolicyBodyContext body)
    {
        List<Rule> rules = [];

        if (body.permitDenyRule() == null) return rules;

        foreach (AlfaParser.PermitDenyRuleContext ruleCtx in body.permitDenyRule())
        {
            if (Visit(ruleCtx) is Rule r)
            {
                rules.Add(r);
            }
        }
        return rules;
    }

    private List<Obligation>? ExtractOnPermit(AlfaParser.PolicyBodyContext body)
    {
        if (body.onPermit() == null) return null;

        foreach (AlfaParser.OnPermitContext opCtx in body.onPermit())
        {
            if (Visit(opCtx) is OnPermitNode opn)
            {
                return opn.Obligations;
            }
        }
        return null;
    }

    private List<Advice>? ExtractOnDeny(AlfaParser.PolicyBodyContext body)
    {
        if (body.onDeny() == null) return null;

        foreach (AlfaParser.OnDenyContext odCtx in body.onDeny())
        {
            if (Visit(odCtx) is OnDenyNode odn)
            {
                return odn.Advice;
            }
        }
        return null;
    }

    /// <summary>
    /// Visits a rule.
    /// </summary>
    /// <param name="context">The permit/deny rule context.</param>
    /// <returns>A Rule AST node.</returns>
    public override AlfaNode VisitPermitDenyRule([NotNull] AlfaParser.PermitDenyRuleContext context)
    {
        string? id = context.IDENTIFIER()?.GetText();
        Effect effect = Effect.Deny;
        Target? target = null;
        Condition? condition = null;
        List<Obligation>? onPermit = null;
        List<Advice>? onDeny = null;

        if (context.ruleEffect() != null && context.ruleEffect().Length > 0)
        {
            AlfaParser.RuleEffectContext effectCtx = context.ruleEffect()[0];
            if (effectCtx.PERMIT() != null)
            {
                effect = Effect.Permit;
            }
            else if (effectCtx.DENY() != null)
            {
                effect = Effect.Deny;
            }
        }

        if (context.target() != null)
        {
            foreach (AlfaParser.TargetContext targetCtx in context.target())
            {
                AlfaNode? node = Visit(targetCtx);
                if (node is Target t)
                {
                    target = t;
                }
            }
        }

        if (context.condition() != null)
        {
            foreach (AlfaParser.ConditionContext condCtx in context.condition())
            {
                AlfaNode? node = Visit(condCtx);
                if (node is Condition c)
                {
                    condition = c;
                }
            }
        }

        if (context.onPermit() != null)
        {
            foreach (AlfaParser.OnPermitContext opCtx in context.onPermit())
            {
                AlfaNode? node = Visit(opCtx);
                if (node is OnPermitNode opn)
                {
                    onPermit = opn.Obligations;
                }
            }
        }

        if (context.onDeny() != null)
        {
            foreach (AlfaParser.OnDenyContext odCtx in context.onDeny())
            {
                AlfaNode? node = Visit(odCtx);
                if (node is OnDenyNode odn)
                {
                    onDeny = odn.Advice;
                }
            }
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

    /// <summary>
    /// Visits a target.
    /// </summary>
    /// <param name="context">The target context.</param>
    /// <returns>A Target AST node.</returns>
    public override AlfaNode VisitTarget([NotNull] AlfaParser.TargetContext context)
    {
        List<Clause> clauses = [];

        if (context.clause() != null)
        {
            foreach (AlfaParser.ClauseContext clauseCtx in context.clause())
            {
                AlfaNode? node = Visit(clauseCtx);
                if (node is Clause c)
                {
                    clauses.Add(c);
                }
            }
        }

        return new Target
        {
            Clauses = clauses
        };
    }

    /// <summary>
    /// Visits a clause.
    /// </summary>
    /// <param name="context">The clause context.</param>
    /// <returns>A Clause AST node.</returns>
    public override AlfaNode VisitClause([NotNull] AlfaParser.ClauseContext context)
    {
        BooleanExpression? expr = null;

        if (context.booleanExpression() != null)
        {
            AlfaNode? node = Visit(context.booleanExpression());
            if (node is BooleanExpression be)
            {
                expr = be;
            }
        }

        return new Clause
        {
            Expression = expr ?? new BooleanLiteralExpression { Value = false }
        };
    }

    /// <summary>
    /// Visits a condition.
    /// </summary>
    /// <param name="context">The condition context.</param>
    /// <returns>A Condition AST node.</returns>
    public override AlfaNode VisitCondition([NotNull] AlfaParser.ConditionContext context)
    {
        BooleanExpression? expr = null;

        if (context.booleanExpression() != null)
        {
            AlfaNode? node = Visit(context.booleanExpression());
            if (node is BooleanExpression be)
            {
                expr = be;
            }
        }

        return new Condition
        {
            Expression = expr ?? new BooleanLiteralExpression { Value = false }
        };
    }

    /// <summary>
    /// Visits an on permit clause.
    /// </summary>
    /// <param name="context">The on permit context.</param>
    /// <returns>An OnPermitNode containing obligations.</returns>
    public override AlfaNode VisitOnPermit([NotNull] AlfaParser.OnPermitContext context)
    {
        List<Obligation> obligations = [];

        if (context.obligation() != null)
        {
            foreach (AlfaParser.ObligationContext oblCtx in context.obligation())
            {
                AlfaNode? node = Visit(oblCtx);
                if (node is Obligation obl)
                {
                    obligations.Add(obl);
                }
            }
        }

        if (context.advice() != null)
        {
            foreach (AlfaParser.AdviceContext advCtx in context.advice())
            {
                AlfaNode? node = Visit(advCtx);
                if (node is Obligation obl)
                {
                    obligations.Add(obl);
                }
            }
        }

        return new OnPermitNode { Obligations = obligations };
    }

    /// <summary>
    /// Visits an on deny clause.
    /// </summary>
    /// <param name="context">The on deny context.</param>
    /// <returns>An OnDenyNode containing advice.</returns>
    public override AlfaNode VisitOnDeny([NotNull] AlfaParser.OnDenyContext context)
    {
        List<Advice> advice = [];

        if (context.obligation() != null)
        {
            foreach (AlfaParser.ObligationContext oblCtx in context.obligation())
            {
                AlfaNode? node = Visit(oblCtx);
                if (node is Advice adv)
                {
                    advice.Add(adv);
                }
            }
        }

        if (context.advice() != null)
        {
            foreach (AlfaParser.AdviceContext advCtx in context.advice())
            {
                AlfaNode? node = Visit(advCtx);
                if (node is Advice adv)
                {
                    advice.Add(adv);
                }
            }
        }

        return new OnDenyNode { Advice = advice };
    }

    /// <summary>
    /// Visits an obligation.
    /// </summary>
    /// <param name="context">The obligation context.</param>
    /// <returns>An Obligation AST node.</returns>
    public override AlfaNode VisitObligation([NotNull] AlfaParser.ObligationContext context)
    {
        string id = context.IDENTIFIER()?.GetText() ?? string.Empty;
        Dictionary<string, Expression>? attributes = null;

        if (context.attributeAssignments() != null)
        {
            attributes = ParseAttributeAssignments(context.attributeAssignments());
        }

        return new Obligation
        {
            Id = id,
            Attributes = attributes
        };
    }

    /// <summary>
    /// Visits advice.
    /// </summary>
    /// <param name="context">The advice context.</param>
    /// <returns>An Advice AST node.</returns>
    public override AlfaNode VisitAdvice([NotNull] AlfaParser.AdviceContext context)
    {
        string id = context.IDENTIFIER()?.GetText() ?? string.Empty;
        Dictionary<string, Expression>? attributes = null;

        if (context.attributeAssignments() != null)
        {
            attributes = ParseAttributeAssignments(context.attributeAssignments());
        }

        return new Advice
        {
            Id = id,
            Attributes = attributes
        };
    }

    /// <summary>
    /// Visits a boolean expression.
    /// </summary>
    /// <param name="context">The boolean expression context.</param>
    /// <returns>A BooleanExpression AST node.</returns>
    public override AlfaNode VisitBooleanExpression([NotNull] AlfaParser.BooleanExpressionContext context)
    {
        // Try simple boolean expressions first
        if (TryVisitSimpleBooleanExpression(context, out AlfaNode? simpleResult))
        {
            return simpleResult;
        }

        // Handle binary expressions (logical and comparison)
        if (context.op != null)
        {
            if (TryVisitLogicalBinaryExpression(context, out AlfaNode? logicalResult))
            {
                return logicalResult;
            }

            if (TryVisitComparisonExpression(context, out AlfaNode? comparisonResult))
            {
                return comparisonResult;
            }
        }

        // Parenthesized expression
        if (context.booleanExpression() != null && context.booleanExpression().Length == 1)
        {
            return Visit(context.booleanExpression()[0]);
        }

        // Fallback: visit children to handle labeled alternatives for boolean literals
        return base.VisitBooleanExpression(context) ?? new BooleanLiteralExpression { Value = false };
    }

    private bool TryVisitSimpleBooleanExpression(AlfaParser.BooleanExpressionContext context, out AlfaNode? result)
    {
        result = null;

        // Inverse boolean expression (NOT)
        if (context.inverseBooleanExpression() != null)
        {
            result = Visit(context.inverseBooleanExpression());
            return result is BooleanExpression;
        }

        // Boolean attribute designator
        if (context.booleanAttributeDesignator() != null)
        {
            result = Visit(context.booleanAttributeDesignator());
            return result is BooleanExpression;
        }

        // Boolean function
        if (context.booleanFunction() != null)
        {
            result = Visit(context.booleanFunction());
            return result is BooleanExpression;
        }

        // Boolean all
        if (context.booleanAll() != null)
        {
            result = Visit(context.booleanAll());
            return result is BooleanExpression;
        }

        return false;
    }

    private bool TryVisitLogicalBinaryExpression(AlfaParser.BooleanExpressionContext context, out AlfaNode? result)
    {
        result = null;

        AlfaParser.BooleanExpressionContext[] children = context.booleanExpression();
        if (children == null || children.Length != 2)
        {
            return false;
        }

        AlfaNode? left = Visit(children[0]);
        AlfaNode? right = Visit(children[1]);

        if (left is not BooleanExpression leftExpr || right is not BooleanExpression rightExpr)
        {
            return false;
        }

        LogicalOperator op = context.op.Type switch
        {
            AlfaParser.AND => LogicalOperator.And,
            AlfaParser.OR => LogicalOperator.Or,
            AlfaParser.ANDCLAUSE => LogicalOperator.AndClause,
            AlfaParser.ORCLAUSE => LogicalOperator.OrClause,
            _ => LogicalOperator.And
        };

        result = new LogicalBinaryExpression
        {
            Left = leftExpr,
            Operator = op,
            Right = rightExpr
        };

        return true;
    }

    private bool TryVisitComparisonExpression(AlfaParser.BooleanExpressionContext context, out AlfaNode? result)
    {
        result = null;

        AlfaParser.ExpressionContext[] exprChildren = context.expression();
        if (exprChildren == null || exprChildren.Length != 2)
        {
            return false;
        }

        AlfaNode? left = Visit(exprChildren[0]);
        AlfaNode? right = Visit(exprChildren[1]);

        if (left is not Expression leftExpr || right is not Expression rightExpr)
        {
            return false;
        }

        ComparisonOperator op = context.op.Type switch
        {
            AlfaParser.EQUAL => ComparisonOperator.Equal,
            AlfaParser.NOTEQUAL => ComparisonOperator.NotEqual,
            AlfaParser.GREATERTHAN => ComparisonOperator.GreaterThan,
            AlfaParser.LESSTHAN => ComparisonOperator.LessThan,
            AlfaParser.GREATERTHANANDEQUAL => ComparisonOperator.GreaterThanOrEqual,
            AlfaParser.LESSTHANANDEQUAL => ComparisonOperator.LessThanOrEqual,
            _ => ComparisonOperator.Equal
        };

        result = new ComparisonExpression
        {
            Left = leftExpr,
            Operator = op,
            Right = rightExpr
        };

        return true;
    }

    /// <summary>
    /// Visits boolean literal true.
    /// </summary>
    public override AlfaNode VisitLiteralBooleanTrue([NotNull] AlfaParser.LiteralBooleanTrueContext context)
    {
        return new BooleanLiteralExpression { Value = true };
    }

    /// <summary>
    /// Visits boolean literal false.
    /// </summary>
    public override AlfaNode VisitLiteralBooleanFalse([NotNull] AlfaParser.LiteralBooleanFalseContext context)
    {
        return new BooleanLiteralExpression { Value = false };
    }

    /// <summary>
    /// Visits inverse boolean expression (NOT).
    /// </summary>
    /// <param name="context">The inverse boolean expression context.</param>
    /// <returns>A NotExpression AST node.</returns>
    public override AlfaNode VisitInverseBooleanExpression([NotNull] AlfaParser.InverseBooleanExpressionContext context)
    {
        BooleanExpression? inner = null;

        if (context.booleanExpression() != null)
        {
            AlfaNode? node = Visit(context.booleanExpression());
            if (node is BooleanExpression be)
            {
                inner = be;
            }
        }
        else if (context.booleanFunction() != null)
        {
            AlfaNode? node = Visit(context.booleanFunction());
            if (node is BooleanExpression be)
            {
                inner = be;
            }
        }
        else if (context.booleanAll() != null)
        {
            AlfaNode? node = Visit(context.booleanAll());
            if (node is BooleanExpression be)
            {
                inner = be;
            }
        }
        else if (context.booleanAttributeDesignator() != null)
        {
            AlfaNode? node = Visit(context.booleanAttributeDesignator());
            if (node is BooleanExpression be)
            {
                inner = be;
            }
        }

        return new NotExpression
        {
            InnerExpression = inner ?? new BooleanLiteralExpression { Value = false }
        };
    }

    /// <summary>
    /// Visits boolean attribute designator.
    /// </summary>
    /// <param name="context">The boolean attribute designator context.</param>
    /// <returns>A BooleanAttributeDesignator AST node.</returns>
    public override AlfaNode VisitBooleanAttributeDesignator([NotNull] AlfaParser.BooleanAttributeDesignatorContext context)
    {
        if (context.attributeDesignator() != null)
        {
            AlfaNode? node = Visit(context.attributeDesignator());
            if (node is AttributeDesignator ad)
            {
                return new BooleanAttributeDesignator
                {
                    Namespace = ad.Namespace,
                    AttributeName = ad.AttributeName,
                    MustBePresent = ad.MustBePresent
                };
            }
        }

        return new BooleanAttributeDesignator
        {
            Namespace = "",
            AttributeName = string.Empty,
            MustBePresent = false
        };
    }

    /// <summary>
    /// Visits boolean function call.
    /// </summary>
    /// <param name="context">The boolean function context.</param>
    /// <returns>A BooleanFunctionCall AST node.</returns>
    public override AlfaNode VisitBooleanFunction([NotNull] AlfaParser.BooleanFunctionContext context)
    {
        if (context.function() != null)
        {
            AlfaNode? node = Visit(context.function());
            if (node is FunctionCall fc)
            {
                return new BooleanFunctionCall
                {
                    FunctionName = fc.FunctionName,
                    Parameters = fc.Parameters
                };
            }
        }

        return new BooleanFunctionCall
        {
            FunctionName = string.Empty,
            Parameters = []
        };
    }

    /// <summary>
    /// Visits boolean all expression.
    /// </summary>
    /// <param name="context">The boolean all context.</param>
    /// <returns>A BooleanAllExpression AST node.</returns>
    public override AlfaNode VisitBooleanAll([NotNull] AlfaParser.BooleanAllContext context)
    {
        if (context.all() != null)
        {
            AlfaNode? node = Visit(context.all());
            if (node is AllExpression ae)
            {
                return new BooleanAllExpression
                {
                    InnerExpression = ae.InnerExpression
                };
            }
        }

        return new BooleanAllExpression
        {
            InnerExpression = new LiteralBooleanExpression { Value = false }
        };
    }

    /// <summary>
    /// Visits an expression.
    /// </summary>
    /// <param name="context">The expression context.</param>
    /// <returns>An Expression AST node.</returns>
    public override AlfaNode VisitExpression([NotNull] AlfaParser.ExpressionContext context)
    {
        // Attribute designator
        if (context.attributeDesignator() != null)
        {
            return Visit(context.attributeDesignator());
        }

        // All expression
        if (context.all() != null)
        {
            return Visit(context.all());
        }

        // Function call
        if (context.function() != null)
        {
            return Visit(context.function());
        }

        // Binary expressions
        if (context.op != null)
        {
            AlfaParser.ExpressionContext[] children = context.expression();
            if (children != null && children.Length == 2)
            {
                AlfaNode? left = Visit(children[0]);
                AlfaNode? right = Visit(children[1]);

                if (left is Expression leftExpr && right is Expression rightExpr)
                {
                    BinaryOperator op = context.op.Type switch
                    {
                        AlfaParser.PLUS => BinaryOperator.Add,
                        AlfaParser.MINUS => BinaryOperator.Subtract,
                        AlfaParser.MULTIPLY => BinaryOperator.Multiply,
                        AlfaParser.DIVIDE => BinaryOperator.Divide,
                        _ => BinaryOperator.Add
                    };

                    return new BinaryExpression
                    {
                        Left = leftExpr,
                        Operator = op,
                        Right = rightExpr
                    };
                }
            }
        }

        // Parenthesized expression or other children
        if (context.expression() != null && context.expression().Length == 1)
        {
            return Visit(context.expression()[0]);
        }

        // Visit children to handle labeled alternatives
        return base.VisitExpression(context) ?? new LiteralStringExpression { Value = string.Empty };
    }

    /// <summary>
    /// Visits literal string.
    /// </summary>
    public override AlfaNode VisitLiteralString([NotNull] AlfaParser.LiteralStringContext context)
    {
        string value = context.LITERAL_STRING().GetText().Trim('"', '\'');
        return new LiteralStringExpression { Value = value };
    }

    /// <summary>
    /// Visits literal integer.
    /// </summary>
    public override AlfaNode VisitLiteralInteger([NotNull] AlfaParser.LiteralIntegerContext context)
    {
        return int.TryParse(context.LITERAL_INTEGER().GetText(), out int value)
            ? new LiteralIntegerExpression { Value = value }
            : (AlfaNode)new LiteralIntegerExpression { Value = 0 };
    }

    /// <summary>
    /// Visits literal double.
    /// </summary>
    public override AlfaNode VisitLiteralDouble([NotNull] AlfaParser.LiteralDoubleContext context)
    {
        return double.TryParse(context.LITERAL_DOUBLE().GetText(), out double value)
            ? new LiteralDoubleExpression { Value = value }
            : (AlfaNode)new LiteralDoubleExpression { Value = 0.0 };
    }

    /// <summary>
    /// Visits literal boolean.
    /// </summary>
    public override AlfaNode VisitLiteralBoolean([NotNull] AlfaParser.LiteralBooleanContext context)
    {
        AlfaNode? node = VisitChildren(context);
        return node is BooleanLiteralExpression ble
            ? new LiteralBooleanExpression { Value = ble.Value }
            : (AlfaNode)new LiteralBooleanExpression { Value = false };
    }

    /// <summary>
    /// Visits an attribute designator.
    /// </summary>
    /// <param name="context">The attribute designator context.</param>
    /// <returns>An AttributeDesignator AST node.</returns>
    public override AlfaNode VisitAttributeDesignator([NotNull] AlfaParser.AttributeDesignatorContext context)
    {
        string attributeName = context.IDENTIFIER()?.GetText() ?? string.Empty;
        bool mustBePresent = context.MUSTBEPRESENT() != null;

        // Parse namespace and attribute name from the identifier
        string[] parts = attributeName.Split('.');
        string ns = ""; // Empty namespace - will be resolved later based on imports
        string name = attributeName;

        if (parts.Length >= 2)
        {
            // If the identifier contains dots, treat the last part as the name
            // and everything before as the namespace prefix
            ns = string.Join(".", parts.Take(parts.Length - 1));
            name = parts[^1];
        }

        return new AttributeDesignator
        {
            Namespace = ns,
            AttributeName = name,
            MustBePresent = mustBePresent
        };
    }

    /// <summary>
    /// Visits string to DateTime coercion.
    /// </summary>
    public override AlfaNode VisitStringToDateTime([NotNull] AlfaParser.StringToDateTimeContext context)
    {
        string value = context.LITERAL_STRING()?.GetText().Trim('"', '\'') ?? string.Empty;
        return new ValueCoercionExpression
        {
            Value = value,
            TargetType = AttributeType.DateTime
        };
    }

    /// <summary>
    /// Visits string to Date coercion.
    /// </summary>
    public override AlfaNode VisitStringToDate([NotNull] AlfaParser.StringToDateContext context)
    {
        string value = context.LITERAL_STRING()?.GetText().Trim('"', '\'') ?? string.Empty;
        return new ValueCoercionExpression
        {
            Value = value,
            TargetType = AttributeType.Date
        };
    }

    /// <summary>
    /// Visits string to Time coercion.
    /// </summary>
    public override AlfaNode VisitStringToTime([NotNull] AlfaParser.StringToTimeContext context)
    {
        string value = context.LITERAL_STRING()?.GetText().Trim('"', '\'') ?? string.Empty;
        return new ValueCoercionExpression
        {
            Value = value,
            TargetType = AttributeType.Time
        };
    }

    /// <summary>
    /// Visits string to Duration coercion.
    /// </summary>
    public override AlfaNode VisitStringToDuration([NotNull] AlfaParser.StringToDurationContext context)
    {
        string value = context.LITERAL_STRING()?.GetText().Trim('"', '\'') ?? string.Empty;
        return new ValueCoercionExpression
        {
            Value = value,
            TargetType = AttributeType.Duration
        };
    }

    /// <summary>
    /// Visits an all expression.
    /// </summary>
    /// <param name="context">The all context.</param>
    /// <returns>An AllExpression AST node.</returns>
    public override AlfaNode VisitAll([NotNull] AlfaParser.AllContext context)
    {
        Expression? inner = null;

        if (context.expression() != null)
        {
            AlfaNode? node = Visit(context.expression());
            if (node is Expression expr)
            {
                inner = expr;
            }
        }

        return new AllExpression
        {
            InnerExpression = inner ?? new LiteralBooleanExpression { Value = false }
        };
    }

    /// <summary>
    /// Visits a function call.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>A FunctionCall AST node.</returns>
    public override AlfaNode VisitFunction([NotNull] AlfaParser.FunctionContext context)
    {
        string functionName = context.IDENTIFIER()?.GetText() ?? string.Empty;
        List<Expression> parameters = [];

        if (context.functionParameters() != null)
        {
            AlfaParser.FunctionParametersContext paramsCtx = context.functionParameters();
            if (paramsCtx.expression() != null)
            {
                foreach (AlfaParser.ExpressionContext exprCtx in paramsCtx.expression())
                {
                    AlfaNode? node = Visit(exprCtx);
                    if (node is Expression expr)
                    {
                        parameters.Add(expr);
                    }
                }
            }
        }

        return new FunctionCall
        {
            FunctionName = functionName,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Parses attribute type from type name context.
    /// </summary>
    /// <param name="context">The type name context.</param>
    /// <returns>The parsed AttributeType.</returns>
    private static AttributeType ParseAttributeType(AlfaParser.TypeNameContext? context)
    {
        if (context == null)
        {
            return AttributeType.String;
        }

        if (context.STRING() != null)
        {
            return AttributeType.String;
        }

        if (context.BOOLEAN() != null)
        {
            return AttributeType.Boolean;
        }

        if (context.INTEGER() != null)
        {
            return AttributeType.Integer;
        }

        if (context.DOUBLE() != null)
        {
            return AttributeType.Double;
        }

        return context.TIME() != null
            ? AttributeType.Time
            : context.DATETIME() != null
            ? AttributeType.DateTime
            : context.DATE() != null ? AttributeType.Date : context.DURATION() != null ? AttributeType.Duration : AttributeType.String;
    }

    /// <summary>
    /// Parses combining algorithm from combinator context.
    /// </summary>
    /// <param name="context">The policy combinator context.</param>
    /// <returns>The parsed CombiningAlgorithm.</returns>
    private static CombiningAlgorithm ParseCombiningAlgorithm(AlfaParser.PolicyCombinatorContext? context)
    {
        if (context == null)
        {
            return CombiningAlgorithm.DenyOverrides;
        }

        if (context.COMBINE_DENY_OVERRIDES() != null)
        {
            return CombiningAlgorithm.DenyOverrides;
        }

        if (context.COMBINE_PERMIT_OVERRIDES() != null)
        {
            return CombiningAlgorithm.PermitOverrides;
        }

        return context.COMBINE_FIRST_APPLICABLE() != null
            ? CombiningAlgorithm.FirstApplicable
            : context.COMBINE_ONLY_ONE_APPLICABLE() != null
            ? CombiningAlgorithm.OnlyOne
            : context.COMBINE_DENY_UNLESS_PERMIT() != null
            ? CombiningAlgorithm.DenyUnlessPermit
            : context.COMBINE_PERMIT_UNLESS_DENY() != null ? CombiningAlgorithm.PermitUnlessDeny : CombiningAlgorithm.DenyOverrides;
    }

    /// <summary>
    /// Parses attribute assignments.
    /// </summary>
    /// <param name="context">The attribute assignments context.</param>
    /// <returns>A dictionary of attribute assignments.</returns>
    private Dictionary<string, Expression> ParseAttributeAssignments(AlfaParser.AttributeAssignmentsContext? context)
    {
        Dictionary<string, Expression> attributes = [];

        if (context?.attributeAssignment() != null)
        {
            foreach (AlfaParser.AttributeAssignmentContext assignCtx in context.attributeAssignment())
            {
                string key = assignCtx.IDENTIFIER()?.GetText() ?? string.Empty;

                if (assignCtx.expression() != null)
                {
                    AlfaNode? node = Visit(assignCtx.expression());
                    if (node is Expression expr)
                    {
                        attributes[key] = expr;
                    }
                }
            }
        }

        return attributes;
    }

    /// <summary>
    /// Helper node for on permit clauses.
    /// </summary>
    private sealed class OnPermitNode : AlfaNode
    {
        public required List<Obligation> Obligations { get; init; }
    }

    /// <summary>
    /// Helper node for on deny clauses.
    /// </summary>
    private sealed class OnDenyNode : AlfaNode
    {
        public required List<Advice> Advice { get; init; }
    }
}
