# ABACore

ABACore is a high-performance policy engine for the ALFA (Abbreviated Language for Authorization) language. It provides a complete implementation of ALFA with parsing, compilation, and runtime evaluation optimized for .NET applications.

## Introduction

ABACore enables fine-grained access control through declarative policies written in ALFA, a human-readable language based on the OASIS XACML standard. Policies are parsed once, and optionally compiled into optimized C# code using Roslyn, and executed as delegates with minimal allocations. There is an interpreter approuch as well.

The library is designed for:
- Microservices requiring dynamic authorization
- API gateways enforcing access policies
- Multi-tenant applications with complex permission models
- Native AOT deployments requiring zero reflection

## Features

- Complete ALFA grammar parser with strongly-typed AST
- Roslyn-based compiler generating optimized C# delegates
- Policy and PolicySet support with all 6 combining algorithms
- Obligations and Advice for policy enforcement metadata
- Namespace resolution and attribute validation
- Hot reload and policy versioning
- Native AOT compatible (no runtime reflection)
- Comprehensive test suite (270+ tests)

## Installation

```bash
dotnet add package ABACore
```

## Quick Start

### Basic Policy Evaluation

```csharp
using ABACore;
using ABACore.Models;

const string policy = @"
namespace Demo {
    policy DocumentAccess {
        apply denyOverrides

        rule PermitAdmins {
            permit
            target clause Subject.Role == ""admin""
        }

        rule PermitOwners {
            permit
            target clause Subject.UserId == Resource.OwnerId
        }

        rule FallbackDeny {
            deny
        }
    }
}";

var engine = new PolicyEngine();
engine.LoadPolicy(policy);

var context = new EvaluationContext();
context.SetAttribute("subject", "role", "admin");
context.SetAttribute("subject", "userId", "user-123");
context.SetAttribute("resource", "ownerId", "user-456");

Decision decision = engine.Evaluate("DocumentAccess", context);
// decision.Effect == DecisionEffect.Permit
```

### PolicySets with Obligations

```csharp
const string policySet = @"
namespace ECommerce {
    policyset OrderManagement {
        apply denyOverrides

        policy AdminPolicy {
            rule AllowAdmin {
                permit
                condition Subject.Role == ""admin""
                on permit {
                    obligation LogAccess {
                        user = Subject.UserId
                        action = Action.Id
                    }
                }
            }
        }

        policy OwnerPolicy {
            rule AllowOwner {
                permit
                condition Subject.UserId == Resource.OwnerId
            }
        }

        on permit {
            obligation AuditAccess
        }

        on deny {
            advice NotifyDenial
        }
    }
}";

var engine = new PolicyEngine();
engine.LoadPolicy(policySet);

var context = new EvaluationContext();
context.SetAttribute("subject", "role", "admin");
context.SetAttribute("subject", "userId", "admin-123");

Decision decision = engine.Evaluate("OrderManagement", context);

if (decision.Effect == DecisionEffect.Permit && decision.Obligations != null)
{
    foreach (var obligation in decision.Obligations)
    {
        Console.WriteLine($"Obligation: {obligation.Id}");
        if (obligation.Attributes != null)
        {
            foreach (var attr in obligation.Attributes)
            {
                Console.WriteLine($"  {attr.Key}: {attr.Value}");
            }
        }
    }
}
```

## How It Works

### Architecture

ABACore uses a three-stage pipeline:

**Variant A**

1. **Parsing**: ALFA policies are parsed using an ANTLR4 grammar into a strongly-typed AST
2a. **Compilation**: The AST is transformed into C# code and compiled using Roslyn into executable delegates
3. **Evaluation**: Policies are evaluated by executing the compiled delegates against an EvaluationContext


```
ALFA Policy Text
       |
       v
  [ANTLR Parser]
       |
       v
   AST (Policy/PolicySet)
       |
       v
  [Roslyn Compiler]
       |
       v
   C# Delegate
       |
       v
  [Runtime Evaluation]
       |
       v
    Decision
```

**Variant B**

1. **Parsing**: ALFA policies are parsed using an ANTLR4 grammar into a strongly-typed AST
2b. **Interpretation**: Interpreter mode interprets ALFA directlry

```
ALFA Policy Text
       |
       v
  [ANTLR Parser]
       |
       v
   AST (Policy/PolicySet)
       |
       v
  [Runtime Evaluation]
       |
       v
    Decision
```


### Combining Algorithms

Six combining algorithms control how multiple policy/rule decisions are combined:

- **denyOverrides**: Any deny decision wins, otherwise first permit wins
- **permitOverrides**: Any permit decision wins, otherwise first deny wins
- **firstApplicable**: First non-NotApplicable decision wins
- **onlyOne**: Exactly one applicable decision required, otherwise indeterminate
- **denyUnlessPermit**: Deny unless at least one permit exists
- **permitUnlessDeny**: Permit unless at least one deny exists

### Obligations and Advice

- **Obligations**: Must be fulfilled for a permit/deny decision (e.g., logging, notifications)
- **Advice**: Supplementary information for enforcement (e.g., error messages, recommendations)

Both can be defined at:
- Rule level (on permit/on deny)
- Policy level (on permit/on deny)
- PolicySet level (on permit/on deny)

All obligations and advice are merged in the final decision.

## Evaluation Modes

ABACore supports different evaluation modes:

```csharp
// Default: Non-strict mode
var engine = new PolicyEngine();

// Strict mode: Validates attribute imports
var strictEngine = new PolicyEngine(EvaluationConfiguration.Strict);
```

### Non-Strict Mode (Default)

Attributes can be used without declaration:

```csharp
policy SimplePolicy {
    rule Allow {
        permit
        condition subject.role == "admin"
    }
}
```

### Strict Mode

All attributes must be imported from defined namespaces:

```csharp
namespace MyApp {
    import Oasis.Attributes.Subject.*
    import Oasis.Attributes.Resource.*

    policy StrictPolicy {
        rule Allow {
            permit
            condition Subject.Role == "admin"
        }
    }
}
```

**Key Differences:**

| Feature | Non-Strict | Strict |
|---------|-----------|--------|
| Attribute declaration | Optional | Required |
| Namespace imports | Optional | Required |
| Type checking | Minimal | Enforced |
| Best for | Development, prototyping | Production, compliance |

## ALFA Syntax

For comprehensive ALFA syntax documentation, see [ALFA.md](ALFA.md).

Quick reference:

```alfa
namespace MyApp.Policies {
    import Oasis.Attributes.Subject.*

    policyset MainPolicies {
        apply denyOverrides

        policy ResourceAccess {
            apply firstApplicable

            rule AdminFullAccess {
                permit
                target clause Subject.Role == "admin"
            }

            rule UserReadAccess {
                permit
                condition Action.Id == "read"
                on permit {
                    obligation LogAccess
                }
            }

            rule DefaultDeny {
                deny
                on deny {
                    advice ContactSupport {
                        message = "Access denied"
                    }
                }
            }
        }

        on permit {
            obligation AuditLog
        }
    }
}
```

## Benchmarks

Run performance benchmarks:

```bash
cd ABACore.Benchmarks
dotnet run -c Release
```

Benchmark scenarios:
- Cached policy evaluation (warm path)
- Policy load, evaluate, and unload (cold path)
- Complex policyset with nested policies
- High-frequency evaluation with different contexts
- Obligation and advice collection overhead

## Project Structure

```
ABACore/                    Core library (parser, compiler, runtime)
  Parser/                   ANTLR-generated parser
  Models/                   AST and runtime models
  Compilation/              Roslyn-based compiler
  Runtime/                  Evaluation engine
ABACore.Tests/              xUnit test suite (270+ tests)
ABACore.Benchmarks/         BenchmarkDotNet performance tests
ABACore.Samples.Aot/        Native AOT sample application
```

## Development

### Building

```bash
dotnet restore
dotnet build
dotnet test
```

### Guidelines

- Follow .editorconfig conventions
- Treat nullable warnings as errors
- Keep delegates reflection-free for AOT compatibility
- Add tests for new combining algorithms or features

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ObligationAdviceTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Examples

See the following for complete examples:

- `ABACore.Tests/ObligationAdviceTests.cs` - 27 tests covering obligations, advice, and policysets
- `ABACore.Tests/TestData/Microservice/` - E-commerce policy examples
- `ABACore.Samples.Aot/` - Native AOT deployment sample

## Advanced Features

### Custom Attribute Categories

```csharp
var resolver = new NamespaceResolver();
resolver.CategoryRegistry.RegisterCategory(new CategoryInfo
{
    Name = "customcategory",
    Uri = "urn:example:attribute-category:custom",
    Description = "Custom attribute category"
});

var validator = new PolicyValidator(resolver);
```

### Policy Versioning

```csharp
PolicyRegistration registration = engine.LoadPolicy(policyText);
Console.WriteLine($"Loaded {registration.PolicyId} v{registration.Version}");
```

### Hot Reload

```csharp
// Load initial policy
engine.LoadPolicy(policyText, "my-tenant");

// Update policy
engine.LoadPolicy(updatedPolicyText, "my-tenant");
// Old version is replaced, new compiled delegate is used
```

## Native AOT Support

ABACore is fully compatible with Native AOT:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

See `ABACore.Samples.Aot` for a complete example.

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

ABACore is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- Based on the OASIS XACML and ALFA standards
- Uses ANTLR4 for parsing
- Uses Roslyn for code generation
