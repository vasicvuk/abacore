namespace ABACore.Runtime;

/// <summary>
/// Defines evaluation modes for policy evaluation with different strictness levels.
/// Controls how attribute resolution and validation is handled.
/// </summary>
public enum EvaluationMode
{
    /// <summary>
    /// Strict mode: All attributes must be properly defined and imported.
    /// Undefined attributes will cause evaluation to fail.
    /// </summary>
    Strict,

    /// <summary>
    /// Non-strict mode: Allows undefined attributes with default behavior.
    /// Useful for testing and backward compatibility.
    /// </summary>
    NonStrict,

    /// <summary>
    /// Development mode: Provides helpful error messages for missing attributes
    /// but doesn't fail evaluation.
    /// </summary>
    Development
}

/// <summary>
/// Configuration for policy evaluation behavior.
/// </summary>
public sealed class EvaluationConfiguration
{
    /// <summary>
    /// Gets or sets the evaluation mode.
    /// </summary>
    public EvaluationMode Mode { get; set; } = EvaluationMode.NonStrict;

    /// <summary>
    /// Gets or sets whether to allow custom categories in strict mode.
    /// </summary>
    public bool AllowCustomCategories { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate attribute imports.
    /// </summary>
    public bool ValidateImports { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to cache compilation results.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default value for missing attributes in non-strict mode.
    /// </summary>
    public object? DefaultAttributeValue { get; set; } = null;

    /// <summary>
    /// Gets or sets the compilation strategy to use.
    /// </summary>
    public CompilationStrategy CompilationStrategy { get; set; } = CompilationStrategy.ExpressionTree;

    /// <summary>
    /// Creates a default evaluation configuration.
    /// </summary>
    public static EvaluationConfiguration Default => new();

    /// <summary>
    /// Creates a strict evaluation configuration.
    /// </summary>
    public static EvaluationConfiguration Strict => new()
    {
        Mode = EvaluationMode.Strict,
        ValidateImports = true,
        EnableCaching = true
    };

    /// <summary>
    /// Creates a non-strict evaluation configuration.
    /// </summary>
    public static EvaluationConfiguration NonStrict => new()
    {
        Mode = EvaluationMode.NonStrict,
        ValidateImports = false,
        EnableCaching = true
    };

    /// <summary>
    /// Creates a development evaluation configuration.
    /// </summary>
    public static EvaluationConfiguration Development => new()
    {
        Mode = EvaluationMode.Development,
        ValidateImports = true,
        EnableCaching = false
    };
}
