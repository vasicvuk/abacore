using System.Diagnostics;
using ABACore;
using ABACore.Models;
using ABACore.Runtime;

Console.WriteLine("=== ABACore AOT Sample Application ===\n");
Console.WriteLine("This sample demonstrates all three compilation strategies:");
Console.WriteLine("1. Roslyn - Dynamic C# code generation (best runtime performance)");
Console.WriteLine("2. ExpressionTree - LINQ expression compilation (balanced)");
Console.WriteLine("3. Interpreter - Direct AST evaluation (best for AOT/trimming)\n");

// Sample ALFA policy for document access control
string alfaPolicy = @"
policy DocumentAccessPolicy {
    apply denyOverrides

    rule AdminFullAccess {
        permit
        target clause subject.role == ""admin""
    }

    rule ManagerReadAccess {
        permit
        target clause subject.role == ""manager""
        condition action.type == ""read""
    }

    rule OwnerFullAccess {
        permit
        target clause subject.userId == resource.ownerId
    }

    rule DenyConfidentialToGuests {
        deny
        target
            clause subject.role == ""guest""
            clause resource.classification == ""confidential""
    }
}
";

// Test context setup
static EvaluationContext CreateAdminContext()
{
    var context = new EvaluationContext();
    context.SetAttribute("subject", "role", "admin");
    context.SetAttribute("subject", "userId", "admin001");
    context.SetAttribute("resource", "classification", "confidential");
    context.SetAttribute("resource", "ownerId", "user123");
    context.SetAttribute("action", "type", "read");
    return context;
}

try
{
    // Test all three compilation strategies
    CompilationStrategy[] strategies =
    [
        CompilationStrategy.Interpreter,
        CompilationStrategy.ExpressionTree,
        CompilationStrategy.Roslyn
    ];

    foreach (CompilationStrategy strategy in strategies)
    {
        try
        {
            Console.WriteLine($"═══════════════════════════════════════════════════════");
            Console.WriteLine($"Testing with {strategy} Strategy");
            Console.WriteLine($"═══════════════════════════════════════════════════════\n");

            // Create engine with specific strategy
            var config = new EvaluationConfiguration { CompilationStrategy = strategy };
            var engine = new PolicyEngine(config);

            // Measure compilation time
            var compileStopwatch = Stopwatch.StartNew();
            PolicyRegistration registration = engine.LoadPolicy(alfaPolicy);
            compileStopwatch.Stop();

            string policyId = registration.PolicyId;
            int version = registration.Version;

            Console.WriteLine($"✓ Policy loaded with ID: {policyId} (v{version})");
            Console.WriteLine($"  Compilation time: {compileStopwatch.ElapsedMilliseconds}ms\n");

            // Test Case 1: Admin access
            Console.WriteLine("Test 1: Admin accessing confidential document");
            EvaluationContext context1 = CreateAdminContext();
            var evalStopwatch = Stopwatch.StartNew();
            Decision decision1 = engine.Evaluate(policyId, context1);
            evalStopwatch.Stop();
            Console.WriteLine($"  Decision: {decision1.Effect}");
            Console.WriteLine($"  Evaluation time: {evalStopwatch.Elapsed.TotalMicroseconds:F2}μs\n");

            // Test Case 2: Manager reading a document
            Console.WriteLine("Test 2: Manager reading a document");
            var context2 = new EvaluationContext();
            context2.SetAttribute("subject", "role", "manager");
            context2.SetAttribute("subject", "userId", "mgr001");
            context2.SetAttribute("resource", "classification", "internal");
            context2.SetAttribute("resource", "ownerId", "user456");
            context2.SetAttribute("action", "type", "read");
            Decision decision2 = engine.Evaluate(policyId, context2);
            Console.WriteLine($"  Decision: {decision2.Effect}\n");

            // Test Case 3: Manager trying to delete
            Console.WriteLine("Test 3: Manager trying to delete a document");
            var context3 = new EvaluationContext();
            context3.SetAttribute("subject", "role", "manager");
            context3.SetAttribute("subject", "userId", "mgr001");
            context3.SetAttribute("resource", "classification", "internal");
            context3.SetAttribute("resource", "ownerId", "user456");
            context3.SetAttribute("action", "type", "delete");
            Decision decision3 = engine.Evaluate(policyId, context3);
            Console.WriteLine($"  Decision: {decision3.Effect}\n");

            // Test Case 4: Owner accessing their document
            Console.WriteLine("Test 4: Owner accessing their own document");
            var context4 = new EvaluationContext();
            context4.SetAttribute("subject", "role", "user");
            context4.SetAttribute("subject", "userId", "user789");
            context4.SetAttribute("resource", "classification", "personal");
            context4.SetAttribute("resource", "ownerId", "user789");
            context4.SetAttribute("action", "type", "read");
            Decision decision4 = engine.Evaluate(policyId, context4);
            Console.WriteLine($"  Decision: {decision4.Effect}\n");

            // Test Case 5: Guest trying to access confidential document
            Console.WriteLine("Test 5: Guest trying to access confidential document");
            var context5 = new EvaluationContext();
            context5.SetAttribute("subject", "role", "guest");
            context5.SetAttribute("subject", "userId", "guest001");
            context5.SetAttribute("resource", "classification", "confidential");
            context5.SetAttribute("resource", "ownerId", "user999");
            context5.SetAttribute("action", "type", "read");
            Decision decision5 = engine.Evaluate(policyId, context5);
            Console.WriteLine($"  Decision: {decision5.Effect}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during {strategy} strategy test: {e.Message}");
        }
    }

    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("Compilation Strategy Summary");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("Interpreter:");
    Console.WriteLine("  ✓ Fastest compilation (no code generation)");
    Console.WriteLine("  ✓ Best for AOT/trimming (no reflection/dynamic code)");
    Console.WriteLine("  ✓ Suitable for frequently changing policies");
    Console.WriteLine("  ⚠ Slower runtime performance");
    Console.WriteLine();
    Console.WriteLine("ExpressionTree:");
    Console.WriteLine("  ✓ Fast compilation");
    Console.WriteLine("  ✓ Good AOT compatibility");
    Console.WriteLine("  ✓ Balanced performance");
    Console.WriteLine("  ⚠ Some trimming warnings (PropertyInfo access)");
    Console.WriteLine();
    Console.WriteLine("Roslyn:");
    Console.WriteLine("  ✓ Fastest runtime performance");
    Console.WriteLine("  ✓ Best for static, long-lived policies");
    Console.WriteLine("  ⚠ Slower compilation (generates & compiles C# code)");
    Console.WriteLine("  ⚠ Not suitable for AOT/trimming scenarios");
    Console.WriteLine();
    Console.WriteLine("=== AOT Demonstration Complete ===");
    Console.WriteLine("For production AOT deployments, use Interpreter or ExpressionTree strategies.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Type: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
    return 1;
}

return 0;
