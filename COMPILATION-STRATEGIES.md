# ABACore Compilation Strategies

ABACore offers three policy compilation strategies. Each produces identical authorization decisions; the differences are in load time, evaluation speed, and deployment constraints (single-file, trimming, Native AOT).

## Quick Comparison

| Strategy       | Compilation Speed | Runtime Speed | Single File | Trimmed | Recommended Scenarios |
|----------------|-------------------|---------------|-------------|---------|-----------------------|
| Interpreter    | Very fast         | Moderate      | Yes         | Yes     | Native AOT, frequently changing policies, sandboxed hosts |
| ExpressionTree | Fast              | Fast          | Yes         | Yes    | Balanced deployments needing good throughput and AOT support |
| Roslyn         | Slow              | Fastest        | Yes         | No      | Long-lived policies where maximum evaluation throughput matters |

## Benchmark Summary

Measurements were taken with ABACore.Benchmarks (Release, .NET 9, Windows x64). "Simple" benchmarks use a small policy; "Complex" benchmarks use a larger one.

### Evaluation Throughput

| Benchmark                        | Mean (ns) | Ratio vs Roslyn | Allocated |
|----------------------------------|----------:|----------------:|----------:|
| Simple evaluation - Roslyn       |     342.9 | 1.00            |     744 B |
| Simple evaluation - ExpressionTree |   352.3 | 1.03            |     744 B |
| Simple evaluation - Interpreter  |     360.7 | 1.05            |     744 B |
| Complex evaluation - Roslyn      |     371.1 | 1.08            |   1,080 B |
| Complex evaluation - ExpressionTree | 424.7  | 1.24            |   1,368 B |
| Complex evaluation - Interpreter |     429.0 | 1.25            |   1,368 B |

### Compilation / Load Time

| Benchmark                         | Mean (ns)     | Ratio vs Interpreter | Allocated   |
|-----------------------------------|--------------:|---------------------:|------------:|
| Load simple policy - Interpreter  |      19,403.3 | 1.00                 |   75,264 B  |
| Load simple policy - ExpressionTree |   119,537.2 | 6.16                 |   84,010 B  |
| Load simple policy - Roslyn       | 35,623,570.8 | 1,836.00             | 10,535,030 B |
| Load complex policy - Interpreter |      78,381.7 | 1.00                 |  271,008 B  |
| Load complex policy - ExpressionTree | 210,158.0 | 2.68                 |  279,615 B  |
| Load complex policy - Roslyn      | 40,755,894.4 | 520.00               | 11,196,384 B |

Interpreter compiles fastest by far but evaluates slightly slower. ExpressionTree strikes a balance. Roslyn yields the best per-request throughput at the cost of significant compilation overhead and memory.

## Strategy Notes

### Interpreter

* **Implementation**: Direct AST evaluation (no code generation).
* **Strengths**: Instant load time, trimming/AOT friendly, minimal dependencies, deterministic behaviour even in restricted environments.
* **Trade-offs**: Evaluation traverses the AST each request; throughput is lower than compiled strategies.
* **Best fit**: Native AOT binaries, serverless/Function-as-a-Service workloads, embedded devices, dynamic policy generation.

### ExpressionTree

* **Implementation**: Builds LINQ expression trees and compiles them into delegates.
* **Strengths**: Fast compilation, good runtime speed, AOT-compatible (tested single-file trimmed publish succeeded).
* **Caveats**: Emits IL2026 warnings because of Expression.Property; ensure trimming keeps required members when using aggressive trimming.
* **Best fit**: General-purpose services needing both responsiveness and compatibility.

### Roslyn

* **Implementation**: Generates C# source, compiles with Microsoft.CodeAnalysis, loads the resulting assembly.
* **Strengths**: Fastest evaluation throughput, generates readable C# for auditing, takes advantage of JIT optimisations.
* **Limitations**: Slowest compilation (tens of milliseconds per policy), large memory overhead, fails under trimming because Roslyn assemblies are not trimmable. Works as single-file only when trimming is disabled.
* **Best fit**: Long-lived services with static policies where evaluation speed dominates startup cost.

## Selecting a Strategy

| Deployment Goal                          | Suggested Strategy |
|-----------------------------------------|--------------------|
| Native AOT / trimmed / sandboxed host   | Interpreter        |
| Balanced startup vs throughput          | ExpressionTree     |
| Maximum evaluation throughput (no trim) | Roslyn             |

Switching strategies is transparent:

`csharp
var engine = new PolicyEngine(new EvaluationConfiguration
{
    CompilationStrategy = CompilationStrategy.ExpressionTree
});
`

You may create multiple PolicyEngine instances with different strategies if you need side-by-side comparisons or blue/green deployments.

## Publishing Guidance

| Publish Mode     | Interpreter | ExpressionTree | Roslyn |
|------------------|-------------|----------------|--------|
| Single-file      | Yes         | Yes            | Yes    |
| Single-file + trim | Yes       | Yes (warnings) | No     |
| Native AOT       | Yes         | Yes            | Works but incurs very large payloads |

## Benchmark Reproduction

`
cd ABACore.Benchmarks
 dotnet run -c Release
`

## Reference Implementations

* Interpreter & ExpressionTree: ABACore/Compilation/ExpressionTreeCompiler.cs
* Roslyn: ABACore/Compilation/PolicyCompiler.cs
* Shared strategy enum: ABACore/CompilationStrategy.cs
* Native AOT sample: ABACore.Samples.Aot

_Last updated: 2025-10-04_
