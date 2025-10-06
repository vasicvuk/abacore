using System;
using ABACore;
using ABACore.Models;
using ABACore.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace ABACore.Benchmarks;

[MemoryDiagnoser]
[ThreadingDiagnoser]
public class PolicyEngineBenchmark
{
    private const string SimplePolicy = """
policy DemoPolicy {
    apply denyOverrides

    rule PermitAdmins {
        permit
        target clause role == "admin"
    }

    rule PermitOwners {
        permit
        target clause userId == ownerId
    }

    rule DefaultDeny {
        deny
    }
}
""";

    private const string ComplexPolicy = """
policy OrderManagement {
    apply firstApplicable

    rule AdminFullAccess {
        permit
        target clause role == "admin"
        on permit {
            obligation LogAdminAccess {
                userId = userId
                action = actionId
            }
        }
    }

    rule ManagerApprove {
        permit
        target clause role == "manager"
        condition actionId == "approve"
        on permit {
            obligation LogManagerAction
        }
    }

    rule CustomerViewOwn {
        permit
        target clause role == "customer"
        condition userId == ownerId
        on permit {
            obligation LogCustomerAccess
        }
    }

    rule DenyOthers {
        deny
        on deny {
            advice AccessDenied {
                message = "Insufficient permissions"
            }
        }
    }
}
""";

    private PolicyEngine _engineRoslyn = null!;
    private PolicyEngine _engineExpressionTree = null!;
    private PolicyEngine _engineInterpreter = null!;
    private string _simplePolicyId = string.Empty;
    private string _complexPolicyId = string.Empty;
    private int _simpleVersion;
    private int _complexVersion;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Roslyn strategy
        var configRoslyn = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Roslyn };
        _engineRoslyn = new PolicyEngine(configRoslyn);
        _ = _engineRoslyn.LoadPolicy(SimplePolicy);
        _engineRoslyn.LoadPolicy(ComplexPolicy);

        // Setup ExpressionTree strategy (default)
        var configExprTree = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.ExpressionTree };
        _engineExpressionTree = new PolicyEngine(configExprTree);
        PolicyRegistration simpleResultExprTree = _engineExpressionTree.LoadPolicy(SimplePolicy);
        _simplePolicyId = simpleResultExprTree.PolicyId;
        _simpleVersion = simpleResultExprTree.Version;
        PolicyRegistration complexResultExprTree = _engineExpressionTree.LoadPolicy(ComplexPolicy);
        _complexPolicyId = complexResultExprTree.PolicyId;
        _complexVersion = complexResultExprTree.Version;

        // Setup Interpreter strategy
        var configInterpreter = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Interpreter };
        _engineInterpreter = new PolicyEngine(configInterpreter);
        _engineInterpreter.LoadPolicy(SimplePolicy);
        _engineInterpreter.LoadPolicy(ComplexPolicy);
    }

    [Benchmark(Description = "Simple - Roslyn", Baseline = true)]
    public Decision SimplePolicy_Roslyn()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "user-123");
        context.SetAttribute("resource", "ownerId", "user-456");
        return _engineRoslyn.Evaluate(_simplePolicyId, context);
    }

    [Benchmark(Description = "Simple - ExpressionTree")]
    public Decision SimplePolicy_ExpressionTree()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "user-123");
        context.SetAttribute("resource", "ownerId", "user-456");
        return _engineExpressionTree.Evaluate(_simplePolicyId, context, _simpleVersion);
    }

    [Benchmark(Description = "Simple - Interpreter")]
    public Decision SimplePolicy_Interpreter()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "user-123");
        context.SetAttribute("resource", "ownerId", "user-456");
        return _engineInterpreter.Evaluate(_simplePolicyId, context);
    }

    [Benchmark(Description = "Complex - Roslyn")]
    public Decision ComplexPolicy_Roslyn()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "admin-123");
        context.SetAttribute("action", "actionId", "read");
        return _engineRoslyn.Evaluate(_complexPolicyId, context);
    }

    [Benchmark(Description = "Complex - ExpressionTree")]
    public Decision ComplexPolicy_ExpressionTree()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "admin-123");
        context.SetAttribute("action", "actionId", "read");
        return _engineExpressionTree.Evaluate(_complexPolicyId, context, _complexVersion);
    }

    [Benchmark(Description = "Complex - Interpreter")]
    public Decision ComplexPolicy_Interpreter()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "admin-123");
        context.SetAttribute("action", "actionId", "read");
        return _engineInterpreter.Evaluate(_complexPolicyId, context);
    }

    [Benchmark(Description = "Load Simple - Roslyn")]
    public PolicyRegistration LoadSimplePolicy_Roslyn()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Roslyn };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(SimplePolicy);
    }

    [Benchmark(Description = "Load Simple - ExpressionTree")]
    public PolicyRegistration LoadSimplePolicy_ExpressionTree()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.ExpressionTree };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(SimplePolicy);
    }

    [Benchmark(Description = "Load Simple - Interpreter")]
    public PolicyRegistration LoadSimplePolicy_Interpreter()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Interpreter };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(SimplePolicy);
    }

    [Benchmark(Description = "Load Complex - Roslyn")]
    public PolicyRegistration LoadComplexPolicy_Roslyn()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Roslyn };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(ComplexPolicy);
    }

    [Benchmark(Description = "Load Complex - ExpressionTree")]
    public PolicyRegistration LoadComplexPolicy_ExpressionTree()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.ExpressionTree };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(ComplexPolicy);
    }

    [Benchmark(Description = "Load Complex - Interpreter")]
    public PolicyRegistration LoadComplexPolicy_Interpreter()
    {
        var config = new EvaluationConfiguration { CompilationStrategy = CompilationStrategy.Interpreter };
        var engine = new PolicyEngine(config);
        return engine.LoadPolicy(ComplexPolicy);
    }

}
