namespace ABACore;

/// <summary>
/// Defines the compilation strategy used by the PolicyEngine.
/// </summary>
public enum CompilationStrategy
{
    /// <summary>
    /// Uses Roslyn to compile policies to C# code and then to IL.
    /// Provides excellent runtime performance but has higher compilation overhead
    /// and larger memory footprint. Not ideal for AOT scenarios.
    /// </summary>
    Roslyn,

    /// <summary>
    /// Uses System.Linq.Expressions to build and compile expression trees to delegates.
    /// Provides good runtime performance with faster compilation than Roslyn.
    /// Better AOT compatibility and smaller memory footprint.
    /// Recommended for most scenarios.
    /// </summary>
    ExpressionTree,

    /// <summary>
    /// Uses an interpreter to directly evaluate the policy AST at runtime.
    /// Provides fastest compilation (no compilation needed) but slower runtime performance.
    /// Best for scenarios where policies change frequently or compilation overhead is critical.
    /// Perfect for AOT scenarios as it requires no code generation.
    /// </summary>
    Interpreter
}
