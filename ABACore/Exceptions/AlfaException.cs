using System;

namespace ABACore.Exceptions;

/// <summary>
/// Base exception for all ALFA-related errors.
/// </summary>
public class AlfaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaException"/> class.
    /// </summary>
    public AlfaException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AlfaException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AlfaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when ALFA policy parsing fails.
/// </summary>
public sealed class AlfaParseException : AlfaException
{
    /// <summary>
    /// Gets the line number where the parse error occurred, if available.
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// Gets the column number where the parse error occurred, if available.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Gets the offending token or symbol, if available.
    /// </summary>
    public string? OffendingSymbol { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaParseException"/> class.
    /// </summary>
    public AlfaParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AlfaParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaParseException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AlfaParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaParseException"/> class with location information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    /// <param name="offendingSymbol">The offending token or symbol.</param>
    public AlfaParseException(string message, int line, int column, string? offendingSymbol = null)
        : base(FormatMessage(message, line, column, offendingSymbol))
    {
        Line = line;
        Column = column;
        OffendingSymbol = offendingSymbol;
    }

    private static string FormatMessage(string message, int line, int column, string? offendingSymbol)
    {
        return offendingSymbol != null
            ? $"{message} at line {line}, column {column}, near '{offendingSymbol}'"
            : $"{message} at line {line}, column {column}";
    }
}

/// <summary>
/// Exception thrown when ALFA policy compilation fails.
/// </summary>
public sealed class AlfaCompilationException : AlfaException
{
    /// <summary>
    /// Gets the compilation diagnostics, if available.
    /// </summary>
    public string? Diagnostics { get; init; }

    /// <summary>
    /// Gets the generated code that failed to compile, if available.
    /// </summary>
    public string? GeneratedCode { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaCompilationException"/> class.
    /// </summary>
    public AlfaCompilationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaCompilationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AlfaCompilationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaCompilationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AlfaCompilationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaCompilationException"/> class with diagnostics.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="diagnostics">The compilation diagnostics.</param>
    /// <param name="generatedCode">The generated code that failed to compile.</param>
    public AlfaCompilationException(string message, string diagnostics, string? generatedCode = null)
        : base(message)
    {
        Diagnostics = diagnostics;
        GeneratedCode = generatedCode;
    }
}

/// <summary>
/// Exception thrown when policy evaluation fails.
/// </summary>
public sealed class AlfaEvaluationException : AlfaException
{
    /// <summary>
    /// Gets the policy ID that was being evaluated, if available.
    /// </summary>
    public string? PolicyId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaEvaluationException"/> class.
    /// </summary>
    public AlfaEvaluationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaEvaluationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AlfaEvaluationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaEvaluationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AlfaEvaluationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlfaEvaluationException"/> class with policy information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="policyId">The policy ID that was being evaluated.</param>
    /// <param name="innerException">The inner exception.</param>
    public AlfaEvaluationException(string message, string policyId, Exception? innerException = null)
        : base($"Error evaluating policy '{policyId}': {message}", innerException!)
    {
        PolicyId = policyId;
    }
}

/// <summary>
/// Exception thrown when a requested policy is not found.
/// </summary>
public sealed class PolicyNotFoundException : AlfaException
{
    /// <summary>
    /// Gets the policy ID that was not found.
    /// </summary>
    public string PolicyId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyNotFoundException"/> class.
    /// </summary>
    /// <param name="policyId">The policy ID that was not found.</param>
    public PolicyNotFoundException(string policyId)
        : base($"Policy with ID '{policyId}' was not found in the repository.")
    {
        PolicyId = policyId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="policyId">The policy ID that was not found.</param>
    /// <param name="message">The custom error message.</param>
    public PolicyNotFoundException(string policyId, string message)
        : base(message)
    {
        PolicyId = policyId;
    }
}
