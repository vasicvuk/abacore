namespace ABACore.Models;

/// <summary>
/// Represents the decision result from policy evaluation.
/// </summary>
public enum DecisionEffect
{
    /// <summary>
    /// The decision is to permit the requested access.
    /// </summary>
    Permit,

    /// <summary>
    /// The decision is to deny the requested access.
    /// </summary>
    Deny,

    /// <summary>
    /// No policy or rule was applicable to the request.
    /// </summary>
    NotApplicable,

    /// <summary>
    /// An error occurred during evaluation or required information was missing.
    /// </summary>
    Indeterminate
}

/// <summary>
/// Represents the result of a policy evaluation.
/// </summary>
public sealed class Decision
{
    /// <summary>
    /// Gets the effect of the decision.
    /// </summary>
    public required DecisionEffect Effect { get; init; }

    /// <summary>
    /// Gets the list of obligations that must be fulfilled.
    /// Only applicable when Effect is Permit or Deny.
    /// </summary>
    public List<ObligationResult>? Obligations { get; init; }

    /// <summary>
    /// Gets the list of advice that should be considered.
    /// Only applicable when Effect is Permit or Deny.
    /// </summary>
    public List<AdviceResult>? Advice { get; init; }

    /// <summary>
    /// Gets optional status information about the decision.
    /// </summary>
    public StatusInfo? Status { get; init; }

    /// <summary>
    /// Creates a Permit decision.
    /// </summary>
    /// <param name="obligations">Optional obligations.</param>
    /// <param name="advice">Optional advice.</param>
    /// <returns>A Decision with Permit effect.</returns>
    public static Decision Permit(List<ObligationResult>? obligations = null, List<AdviceResult>? advice = null)
    {
        return new Decision
        {
            Effect = DecisionEffect.Permit,
            Obligations = obligations,
            Advice = advice
        };
    }

    /// <summary>
    /// Creates a Deny decision.
    /// </summary>
    /// <param name="obligations">Optional obligations.</param>
    /// <param name="advice">Optional advice.</param>
    /// <returns>A Decision with Deny effect.</returns>
    public static Decision Deny(List<ObligationResult>? obligations = null, List<AdviceResult>? advice = null)
    {
        return new Decision
        {
            Effect = DecisionEffect.Deny,
            Obligations = obligations,
            Advice = advice
        };
    }

    /// <summary>
    /// Creates a NotApplicable decision.
    /// </summary>
    /// <returns>A Decision with NotApplicable effect.</returns>
    public static Decision NotApplicable()
    {
        return new Decision
        {
            Effect = DecisionEffect.NotApplicable
        };
    }

    /// <summary>
    /// Creates an Indeterminate decision.
    /// </summary>
    /// <param name="status">Optional status information about why the decision is indeterminate.</param>
    /// <returns>A Decision with Indeterminate effect.</returns>
    public static Decision Indeterminate(StatusInfo? status = null)
    {
        return new Decision
        {
            Effect = DecisionEffect.Indeterminate,
            Status = status
        };
    }
}

/// <summary>
/// Represents an obligation result from policy evaluation.
/// </summary>
public sealed class ObligationResult
{
    /// <summary>
    /// Gets the obligation identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the obligation attribute assignments.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; init; }
}

/// <summary>
/// Represents an advice result from policy evaluation.
/// </summary>
public sealed class AdviceResult
{
    /// <summary>
    /// Gets the advice identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the advice attribute assignments.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; init; }
}

/// <summary>
/// Represents status information about a decision.
/// </summary>
public sealed class StatusInfo
{
    /// <summary>
    /// Gets the status code.
    /// </summary>
    public required string StatusCode { get; init; }

    /// <summary>
    /// Gets the status message.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets additional status details.
    /// </summary>
    public string? StatusDetail { get; init; }

    /// <summary>
    /// Creates a status info for a missing attribute.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <returns>A StatusInfo representing a missing attribute error.</returns>
    public static StatusInfo MissingAttribute(string attributeName)
    {
        return new StatusInfo
        {
            StatusCode = "urn:oasis:names:tc:xacml:1.0:status:missing-attribute",
            StatusMessage = $"Missing required attribute: {attributeName}"
        };
    }

    /// <summary>
    /// Creates a status info for a syntax error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A StatusInfo representing a syntax error.</returns>
    public static StatusInfo SyntaxError(string message)
    {
        return new StatusInfo
        {
            StatusCode = "urn:oasis:names:tc:xacml:1.0:status:syntax-error",
            StatusMessage = message
        };
    }

    /// <summary>
    /// Creates a status info for a processing error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A StatusInfo representing a processing error.</returns>
    public static StatusInfo ProcessingError(string message)
    {
        return new StatusInfo
        {
            StatusCode = "urn:oasis:names:tc:xacml:1.0:status:processing-error",
            StatusMessage = message
        };
    }
}
