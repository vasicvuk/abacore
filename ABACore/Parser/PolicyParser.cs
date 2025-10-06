using System.IO;
using System.Runtime.ExceptionServices;
using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Visitor;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ABACore.Parser;

/// <summary>
/// Provides helpers for parsing ALFA policy documents into AST objects.
/// </summary>
public static class PolicyParser
{
    /// <summary>
    /// Parses ALFA text into a <see cref="PolicyDocument"/> containing the namespace and the policy/policy set.
    /// </summary>
    /// <param name="policyText">The ALFA document text.</param>
    /// <returns>A <see cref="PolicyDocument"/> containing the parsed structures.</returns>
    /// <exception cref="AlfaParseException">Thrown when parsing fails.</exception>
    public static PolicyDocument Parse(string policyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyText);

        AntlrInputStream inputStream = new(policyText);
        AlfaLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        AlfaParser parser = new(tokenStream);

        parser.RemoveErrorListeners();
        ThrowingErrorListener errorListener = new();
        parser.AddErrorListener(errorListener);

        try
        {
            AlfaParser.BodyContext? bodyContext = null;
            AlfaParser.PolicyContext? policyContext = null;
            ExceptionDispatchInfo? bodyExceptionInfo = null;

            try
            {
                bodyContext = parser.body();
            }
            catch (Exception bodyEx)
            {
                bodyExceptionInfo = ExceptionDispatchInfo.Capture(bodyEx);

                try
                {
                    parser.Reset();
                    policyContext = parser.policy();
                }
                catch (Exception)
                {
                    if (bodyEx.Message.Contains("namespace") || bodyEx is AlfaParseException)
                    {
                        bodyExceptionInfo.Throw();
                    }

                    throw;
                }
            }

            AlfaAstBuilder visitor = new();

            if (bodyContext != null && bodyContext.@namespace() != null && bodyContext.@namespace().Length > 0)
            {
                AlfaParser.NamespaceContext namespaceContext = bodyContext.@namespace()[0];
                AlfaNode? node = visitor.Visit(namespaceContext);

                if (node is not Namespace ns)
                {
                    throw new AlfaParseException($"Failed to parse namespace: expected Namespace node but got {node?.GetType().Name ?? "null"}");
                }

                Policy? policy = null;
                PolicySet? policySet = null;

                if (ns.Statements != null)
                {
                    foreach (Statement stmt in ns.Statements)
                    {
                        if (stmt is PolicyStatement polStmt)
                        {
                            policy = polStmt.Policy;
                            break;
                        }

                        if (stmt is PolicySetStatement psStmt)
                        {
                            policySet = psStmt.PolicySet;
                            break;
                        }
                    }
                }

                if (policy == null && policySet == null)
                {
                    policy = new Policy
                    {
                        Id = $"{ns.Name}_AttributeDefinitions",
                        Rules = []
                    };
                }

                return new PolicyDocument
                {
                    Namespace = ns,
                    Policy = policy,
                    PolicySet = policySet
                };
            }

            if (policyContext != null)
            {
                AlfaNode? node = visitor.Visit(policyContext);
                if (node is not Policy policy)
                {
                    throw new AlfaParseException($"Failed to parse policy: expected Policy node but got {node?.GetType().Name ?? "null"}");
                }

                Namespace defaultNamespace = new()
                {
                    Name = "Default",
                    Statements = []
                };

                return new PolicyDocument
                {
                    Namespace = defaultNamespace,
                    Policy = policy
                };
            }

            throw new AlfaParseException("Failed to parse: no namespace or policy found.");
        }
        catch (AlfaParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AlfaParseException($"Unexpected error during policy parsing: {ex.Message}", ex);
        }
    }

    private sealed class ThrowingErrorListener : BaseErrorListener
    {
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new AlfaParseException(msg, line, charPositionInLine, offendingSymbol?.Text);
        }
    }
}
