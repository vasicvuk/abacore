using ABACore;
using ABACore.Models;
using ABACore.Runtime;
using Xunit;

namespace ABACore.Tests;

public class CompilationStrategyTests
{
    private const string SimplePolicy = """
namespace test {
    attribute role {
        category = subject
        id = "role"
        type = string
    }

    policy TestPolicy {
        apply firstApplicable

        rule PermitAdmins {
            permit
            condition role == "admin"
        }

        rule DefaultDeny {
            deny
        }
    }
}
""";

    [Fact]
    public void Roslyn_Strategy_ShouldWork()
    {
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        config.CompilationStrategy = CompilationStrategy.Roslyn;
        var engine = new PolicyEngine(config);
        engine.LoadPolicy(SimplePolicy);

        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        Decision decision = engine.Evaluate("TestPolicy", context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void ExpressionTree_Strategy_ShouldWork()
    {
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        config.CompilationStrategy = CompilationStrategy.ExpressionTree;
        var engine = new PolicyEngine(config);
        engine.LoadPolicy(SimplePolicy);
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        Decision decision = engine.Evaluate("TestPolicy", context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void Interpreter_Strategy_ShouldWork()
    {
        EvaluationConfiguration config = EvaluationConfiguration.NonStrict;
        config.CompilationStrategy = CompilationStrategy.Interpreter;
        var engine = new PolicyEngine(config);
        engine.LoadPolicy(SimplePolicy);

        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        Decision decision = engine.Evaluate("TestPolicy", context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void AllStrategies_ShouldProduceSameResult()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "role", "admin");

        EvaluationConfiguration configRoslyn = EvaluationConfiguration.NonStrict;
        configRoslyn.CompilationStrategy = CompilationStrategy.Roslyn;
        var engineRoslyn = new PolicyEngine(configRoslyn);
        engineRoslyn.LoadPolicy(SimplePolicy);
        Decision decisionRoslyn = engineRoslyn.Evaluate("TestPolicy", context);

        EvaluationConfiguration configExprTree = EvaluationConfiguration.NonStrict;
        configExprTree.CompilationStrategy = CompilationStrategy.ExpressionTree;
        var engineExprTree = new PolicyEngine(configExprTree);
        engineExprTree.LoadPolicy(SimplePolicy);
        Decision decisionExprTree = engineExprTree.Evaluate("TestPolicy", context);

        EvaluationConfiguration configInterpreter = EvaluationConfiguration.NonStrict;
        configInterpreter.CompilationStrategy = CompilationStrategy.Interpreter;
        var engineInterpreter = new PolicyEngine(configInterpreter);
        engineInterpreter.LoadPolicy(SimplePolicy);
        Decision decisionInterpreter = engineInterpreter.Evaluate("TestPolicy", context);

        Assert.Equal(decisionRoslyn.Effect, decisionExprTree.Effect);
        Assert.Equal(decisionRoslyn.Effect, decisionInterpreter.Effect);
        Assert.Equal(DecisionEffect.Permit, decisionRoslyn.Effect);
    }
}
