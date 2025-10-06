using ABACore.Models;

namespace ABACore.Runtime;

/// <summary>
/// Represents a compiled ALFA policy that can be executed.
/// This wrapper stores the policy metadata and the compiled evaluation delegate.
/// </summary>
public sealed class CompiledPolicy
{
    /// <summary>
    /// Gets the policy identifier.
    /// </summary>
    public string? PolicyId { get; init; }

    /// <summary>
    /// Gets the compiled evaluation delegate.
    /// </summary>
    public required Func<EvaluationContext, Decision> EvaluationDelegate { get; init; }

    /// <summary>
    /// Gets the original policy AST (optional, for debugging/inspection).
    /// </summary>
    public Policy? OriginalPolicy { get; init; }

    /// <summary>
    /// Gets the compilation timestamp.
    /// </summary>
    public DateTime CompiledAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the generated C# code (optional, for debugging).
    /// </summary>
    public string? GeneratedCode { get; init; }

    /// <summary>
    /// Executes the compiled policy with the given evaluation context.
    /// </summary>
    /// <param name="context">The evaluation context containing attribute values.</param>
    /// <returns>The policy decision.</returns>
    public Decision Evaluate(EvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return EvaluationDelegate(context);
        }
        catch (Exception ex)
        {
            return Decision.Indeterminate(StatusInfo.ProcessingError($"Policy evaluation error: {ex.Message}"));
        }
    }
}
