using System.Collections.Generic;
using ABACore;
using ABACore.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Base class for tests that should run against all compilation strategies.
/// </summary>
public abstract class CompilationStrategyTestBase
{
    public static IEnumerable<object[]> AllStrategies()
    {
        yield return new object[] { CompilationStrategy.Roslyn };
        yield return new object[] { CompilationStrategy.ExpressionTree };
        yield return new object[] { CompilationStrategy.Interpreter };
    }

    protected PolicyEngine CreateEngine(CompilationStrategy strategy)
    {
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        config.CompilationStrategy = strategy;
        return new PolicyEngine(config);
    }

    protected PolicyEngine CreateEngineStrict(CompilationStrategy strategy)
    {
        EvaluationConfiguration config = EvaluationConfiguration.Strict;
        config.CompilationStrategy = strategy;
        return new PolicyEngine(config);
    }
}
