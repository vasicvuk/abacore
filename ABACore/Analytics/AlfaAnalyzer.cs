using System.IO;
using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Parser;
using Antlr4.Runtime;

namespace ABACore.Analytics;

/// <summary>
/// Analyzes ALFA code to extract diagnostics, attribute references, obligations, and advice.
/// </summary>
public sealed class AlfaAnalyzer
{
    /// <summary>
    /// Analyzes ALFA policy text and returns diagnostics and extracted information.
    /// </summary>
    /// <param name="alfaText">The ALFA code to analyze.</param>
    /// <returns>Analysis result containing diagnostics and references.</returns>
    public AnalysisResult Analyze(string alfaText)
    {
        if (string.IsNullOrWhiteSpace(alfaText))
        {
            return new AnalysisResult
            {
                Success = false,
                Diagnostics =
                [
                    new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = "ALFA text cannot be null or empty",
                        Code = "ALFA001"
                    }
                ]
            };
        }

        AnalysisResult result = new();
        DiagnosticCollector diagnosticCollector = new();

        try
        {
            // Parse the ALFA code
            AntlrInputStream inputStream = new(alfaText);
            AlfaLexer lexer = new(inputStream);
            CommonTokenStream tokenStream = new(lexer);
            AlfaParser parser = new(tokenStream);

            // Remove default error listeners and add our diagnostic collector
            parser.RemoveErrorListeners();
            parser.AddErrorListener(diagnosticCollector);

            // Try to parse
            AlfaParser.BodyContext? bodyContext = parser.body();

            if (bodyContext != null && diagnosticCollector.Diagnostics.Count == 0)
            {
                // Successfully parsed, now extract information
                PolicyDocument document = PolicyParser.Parse(alfaText);
                ExtractReferences(document, result);
                result = result with { Success = true };
            }
            else
            {
                result = result with { Success = false };
            }

            // Add collected diagnostics
            result.Diagnostics.AddRange(diagnosticCollector.Diagnostics);

            // Add semantic warnings
            AddSemanticWarnings(result);
        }
        catch (AlfaParseException ex)
        {
            result = result with { Success = false };
            result.Diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = ex.Message,
                Code = "ALFA002",
                Range = ex.Line.HasValue && ex.Column.HasValue
                    ? new TextRange
                    {
                        Start = new TextPosition { Line = ex.Line.Value, Column = ex.Column.Value },
                        End = new TextPosition { Line = ex.Line.Value, Column = ex.Column.Value + (ex.OffendingSymbol?.Length ?? 1) }
                    }
                    : null
            });
        }
        catch (Exception ex)
        {
            result = result with { Success = false };
            result.Diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Unexpected error during analysis: {ex.Message}",
                Code = "ALFA999"
            });
        }

        return result;
    }

    private void ExtractReferences(PolicyDocument document, AnalysisResult result)
    {
        // Check if policy/policyset is in namespace statements to avoid double extraction
        bool policyInStatements = false;
        bool policySetInStatements = false;

        if (document.Namespace?.Statements != null)
        {
            foreach (Statement statement in document.Namespace.Statements)
            {
                if (statement is PolicyStatement policyStmt)
                {
                    ExtractFromPolicy(policyStmt.Policy, result);
                    if (document.Policy != null && ReferenceEquals(policyStmt.Policy, document.Policy))
                    {
                        policyInStatements = true;
                    }
                }
                else if (statement is PolicySetStatement policySetStmt)
                {
                    ExtractFromPolicySet(policySetStmt.PolicySet, result);
                    if (document.PolicySet != null && ReferenceEquals(policySetStmt.PolicySet, document.PolicySet))
                    {
                        policySetInStatements = true;
                    }
                }
            }
        }

        // Extract from direct policy/policyset only if not already extracted from statements
        if (document.Policy != null && !policyInStatements)
        {
            ExtractFromPolicy(document.Policy, result);
        }

        if (document.PolicySet != null && !policySetInStatements)
        {
            ExtractFromPolicySet(document.PolicySet, result);
        }
    }

    private void ExtractFromPolicy(Policy policy, AnalysisResult result)
    {
        // Extract from target
        if (policy.Target != null)
        {
            ExtractFromTarget(policy.Target, result);
        }

        // Extract from rules
        foreach (Rule rule in policy.Rules)
        {
            ExtractFromRule(rule, result);
        }

        // Extract obligations and advice
        if (policy.OnPermit != null)
        {
            foreach (Obligation obligation in policy.OnPermit)
            {
                result.ObligationReferences.Add(new ObligationReference
                {
                    Id = obligation.Id,
                    AttributeNames = obligation.Attributes?.Keys.ToList() ?? []
                });
            }
        }

        if (policy.OnDeny != null)
        {
            foreach (Advice advice in policy.OnDeny)
            {
                result.AdviceReferences.Add(new AdviceReference
                {
                    Id = advice.Id,
                    AttributeNames = advice.Attributes?.Keys.ToList() ?? []
                });
            }
        }
    }

    private void ExtractFromPolicySet(PolicySet policySet, AnalysisResult result)
    {
        // Extract from target
        if (policySet.Target != null)
        {
            ExtractFromTarget(policySet.Target, result);
        }

        // Extract from elements
        foreach (PolicySetElement element in policySet.Elements)
        {
            if (element is Policy policy)
            {
                ExtractFromPolicy(policy, result);
            }
            else if (element is PolicySet nestedPolicySet)
            {
                ExtractFromPolicySet(nestedPolicySet, result);
            }
        }

        // Extract obligations and advice
        if (policySet.OnPermit != null)
        {
            foreach (Obligation obligation in policySet.OnPermit)
            {
                result.ObligationReferences.Add(new ObligationReference
                {
                    Id = obligation.Id,
                    AttributeNames = obligation.Attributes?.Keys.ToList() ?? []
                });
            }
        }

        if (policySet.OnDeny != null)
        {
            foreach (Advice advice in policySet.OnDeny)
            {
                result.AdviceReferences.Add(new AdviceReference
                {
                    Id = advice.Id,
                    AttributeNames = advice.Attributes?.Keys.ToList() ?? []
                });
            }
        }
    }

    private void ExtractFromRule(Rule rule, AnalysisResult result)
    {
        // Extract from target
        if (rule.Target != null)
        {
            ExtractFromTarget(rule.Target, result);
        }

        // Extract from condition
        if (rule.Condition != null)
        {
            ExtractFromBooleanExpression(rule.Condition.Expression, result);
        }

        // Extract obligations and advice
        if (rule.OnPermit != null)
        {
            foreach (Obligation obligation in rule.OnPermit)
            {
                result.ObligationReferences.Add(new ObligationReference
                {
                    Id = obligation.Id,
                    AttributeNames = obligation.Attributes?.Keys.ToList() ?? []
                });

                // Extract attributes from obligation expressions
                if (obligation.Attributes != null)
                {
                    foreach (Expression expr in obligation.Attributes.Values)
                    {
                        ExtractFromExpression(expr, result);
                    }
                }
            }
        }

        if (rule.OnDeny != null)
        {
            foreach (Advice advice in rule.OnDeny)
            {
                result.AdviceReferences.Add(new AdviceReference
                {
                    Id = advice.Id,
                    AttributeNames = advice.Attributes?.Keys.ToList() ?? []
                });

                // Extract attributes from advice expressions
                if (advice.Attributes != null)
                {
                    foreach (Expression expr in advice.Attributes.Values)
                    {
                        ExtractFromExpression(expr, result);
                    }
                }
            }
        }
    }

    private void ExtractFromTarget(Target target, AnalysisResult result)
    {
        foreach (Clause clause in target.Clauses)
        {
            ExtractFromBooleanExpression(clause.Expression, result);
        }
    }

    private void ExtractFromBooleanExpression(BooleanExpression expression, AnalysisResult result)
    {
        switch (expression)
        {
            case BooleanAttributeDesignator designator:
                result.AttributeReferences.Add(new AttributeReference
                {
                    Namespace = designator.Namespace,
                    Name = designator.AttributeName,
                    MustBePresent = designator.MustBePresent
                });
                break;

            case BooleanFunctionCall function:
                foreach (Expression param in function.Parameters)
                {
                    ExtractFromExpression(param, result);
                }
                break;

            case BooleanAllExpression all:
                ExtractFromExpression(all.InnerExpression, result);
                break;

            case NotExpression notExpr:
                ExtractFromBooleanExpression(notExpr.InnerExpression, result);
                break;

            case LogicalBinaryExpression logical:
                ExtractFromBooleanExpression(logical.Left, result);
                ExtractFromBooleanExpression(logical.Right, result);
                break;

            case ComparisonExpression comparison:
                ExtractFromExpression(comparison.Left, result);
                ExtractFromExpression(comparison.Right, result);
                break;
        }
    }

    private void ExtractFromExpression(Expression expression, AnalysisResult result)
    {
        switch (expression)
        {
            case AttributeDesignator designator:
                result.AttributeReferences.Add(new AttributeReference
                {
                    Namespace = designator.Namespace,
                    Name = designator.AttributeName,
                    MustBePresent = designator.MustBePresent
                });
                break;

            case AllExpression all:
                ExtractFromExpression(all.InnerExpression, result);
                break;

            case FunctionCall function:
                foreach (Expression param in function.Parameters)
                {
                    ExtractFromExpression(param, result);
                }
                break;

            case BinaryExpression binary:
                ExtractFromExpression(binary.Left, result);
                ExtractFromExpression(binary.Right, result);
                break;
        }
    }

    private void AddSemanticWarnings(AnalysisResult result)
    {
        // Warn about duplicate attribute references (might indicate copy-paste errors)
        IEnumerable<string> duplicateAttrs = result.AttributeReferences
            .GroupBy(a => a.FullName)
            .Where(g => g.Count() > 5)
            .Select(g => g.Key);

        foreach (string attrName in duplicateAttrs)
        {
            result.Diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Info,
                Message = $"Attribute '{attrName}' is referenced multiple times (consider if this is intentional)",
                Code = "ALFA101"
            });
        }

        // Warn if no attributes are referenced (might be incomplete policy)
        if (result.Success && result.AttributeReferences.Count == 0)
        {
            result.Diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Message = "No attributes are referenced in this policy. Policies typically reference at least one attribute.",
                Code = "ALFA102"
            });
        }
    }

    private sealed class DiagnosticCollector : BaseErrorListener
    {
        public List<Diagnostic> Diagnostics { get; } = [];

        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            TextRange? range = null;
            if (offendingSymbol != null)
            {
                range = new TextRange
                {
                    Start = new TextPosition { Line = line, Column = charPositionInLine },
                    End = new TextPosition
                    {
                        Line = line,
                        Column = charPositionInLine + (offendingSymbol.Text?.Length ?? 1)
                    }
                };
            }

            Diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = msg,
                Code = "ALFA003",
                Range = range
            });
        }
    }
}
