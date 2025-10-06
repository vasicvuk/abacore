# ALFA Language Reference

ALFA (Abbreviated Language for Authorization) is a domain-specific language for defining access control policies. This document provides a comprehensive reference for the ALFA syntax supported by ABACore.

## Table of Contents

- [Basic Structure](#basic-structure)
- [Namespace](#namespace)
- [Imports](#imports)
- [Attributes](#attributes)
- [Policies](#policies)
- [PolicySets](#policysets)
- [Rules](#rules)
- [Targets and Conditions](#targets-and-conditions)
- [Effects](#effects)
- [Combining Algorithms](#combining-algorithms)
- [Obligations and Advice](#obligations-and-advice)
- [Expressions](#expressions)
- [Complete Examples](#complete-examples)

## Basic Structure

An ALFA file consists of a namespace containing policies, policysets, attributes, and imports:

```alfa
namespace MyApp.Authorization {
    import Oasis.Attributes.Subject.*

    attribute CustomAttribute {
        category = subject
        id = "urn:myapp:attr:custom"
        type = string
    }

    policy MyPolicy {
        // rules go here
    }
}
```

## Namespace

Namespaces organize policies and attributes:

```alfa
namespace CompanyName.Application.Module {
    // contents
}
```

Namespace naming conventions:
- Use dot notation for hierarchy
- Start with organization/company name
- Follow with application and module names
- Example: `Acme.ECommerce.Orders`

## Imports

Import predefined attributes and functions from other namespaces:

```alfa
// Import all OASIS subject attributes
import Oasis.Attributes.Subject.*

// Import specific attributes
import Oasis.Attributes.Resource.Id
import Oasis.Attributes.Resource.Type

// Import from custom namespaces
import MyApp.CustomAttributes.*
```

Available OASIS namespaces:
- `Oasis.Attributes.Subject.*` - Subject attributes (Id, Role, Name, Clearance)
- `Oasis.Attributes.Resource.*` - Resource attributes (Id, Type, Classification)
- `Oasis.Attributes.Action.*` - Action attributes (Id, Name)
- `Oasis.Attributes.Environment.*` - Environment attributes (CurrentTime, CurrentDate, CurrentDateTime)

## Attributes

Define custom attributes with type information:

```alfa
attribute AttributeName {
    category = categoryName
    id = "urn:unique:identifier"
    type = dataType
}
```

### Categories

Standard XACML categories:
- `subject` - Attributes of the requesting user/entity
- `resource` - Attributes of the resource being accessed
- `action` - Attributes of the action being performed
- `environment` - Environmental/contextual attributes

### Data Types

Supported types:
- `string` - Text values
- `boolean` - true/false
- `integer` - Whole numbers
- `double` - Floating-point numbers
- `time` - Time values
- `date` - Date values
- `dateTime` - Combined date and time

### Example

```alfa
attribute Department {
    category = subject
    id = "urn:myapp:subject:department"
    type = string
}

attribute AccessLevel {
    category = subject
    id = "urn:myapp:subject:access-level"
    type = integer
}

attribute Confidential {
    category = resource
    id = "urn:myapp:resource:confidential"
    type = boolean
}
```

## Policies

Policies contain rules that evaluate to permit or deny decisions:

```alfa
policy PolicyName {
    apply combiningAlgorithm

    target clause targetExpression

    rule RuleName {
        // rule definition
    }

    on permit {
        obligation ObligationName
    }

    on deny {
        advice AdviceName
    }
}
```

### Policy Components

- **apply**: Combining algorithm for rules (required if multiple rules)
- **target**: Optional filter determining if policy applies
- **rules**: One or more rules containing permit/deny logic
- **obligations**: Actions required when policy effect matches
- **advice**: Supplementary information when policy effect matches

### Example

```alfa
policy DocumentAccessPolicy {
    apply denyOverrides

    target clause Resource.Type == "document"

    rule AdminAccess {
        permit
        condition Subject.Role == "admin"
    }

    rule OwnerAccess {
        permit
        condition Subject.UserId == Resource.OwnerId
    }

    rule DenyOthers {
        deny
    }
}
```

## PolicySets

PolicySets contain multiple policies or nested policysets:

```alfa
policyset PolicySetName {
    apply combiningAlgorithm

    target clause targetExpression

    policy ChildPolicy {
        // policy definition
    }

    policyset NestedPolicySet {
        // policyset definition
    }

    on permit {
        obligation ObligationName
    }
}
```

### Nested PolicySets

PolicySets can contain other policysets:

```alfa
policyset MainAccessControl {
    apply denyOverrides

    policyset OrderManagement {
        apply permitOverrides

        policy OrderPolicy {
            // rules
        }
    }

    policyset InventoryManagement {
        apply firstApplicable

        policy InventoryPolicy {
            // rules
        }
    }
}
```

## Rules

Rules are the basic decision-making units:

```alfa
rule RuleName {
    effect
    target clause targetExpression
    condition conditionExpression
    on effect {
        obligation/advice ObligationName
    }
}
```

### Rule Components

- **effect**: Either `permit` or `deny`
- **target**: Optional clause filtering when rule applies
- **condition**: Optional expression that must be true for rule to apply
- **obligations/advice**: Actions/information attached to the rule's effect

### Examples

```alfa
// Simple permit rule
rule AllowRead {
    permit
    condition Action.Id == "read"
}

// Rule with target
rule AdminAccess {
    permit
    target clause Subject.Role == "admin"
}

// Rule with target and condition
rule OwnerModify {
    permit
    target clause Action.Id == "modify"
    condition Subject.UserId == Resource.OwnerId
}

// Deny rule with advice
rule BlockSuspended {
    deny
    condition Subject.AccountStatus == "suspended"
    on deny {
        advice ContactSupport {
            message = "Account suspended. Contact support."
        }
    }
}
```

## Targets and Conditions

### Target Clause

Targets filter applicability using boolean expressions:

```alfa
target clause Expression
```

Examples:
```alfa
target clause Resource.Type == "document"
target clause Subject.Role == "admin" && Resource.Confidential == true
target clause Action.Id == "read" || Action.Id == "write"
```

### Condition

Conditions are evaluated only if the target matches:

```alfa
condition Expression
```

Examples:
```alfa
condition Subject.Department == Resource.Department
condition Resource.RiskLevel == "high" && Subject.Clearance == "secret"
condition Subject.AccessLevel >= 5
```

### Difference Between Target and Condition

- **Target**: Determines if the policy/rule applies (evaluated first)
- **Condition**: Additional logic evaluated only if target matches (evaluated second)

Best practice: Use targets for simple filtering, conditions for complex logic.

## Effects

Two possible effects for rules:

### Permit

Grants access:

```alfa
rule AllowAccess {
    permit
    condition Subject.Authorized == true
}
```

### Deny

Denies access:

```alfa
rule BlockAccess {
    deny
    condition Subject.Blocked == true
}
```

## Combining Algorithms

Combining algorithms determine how multiple decisions are combined:

### denyOverrides

Any deny decision wins. If no denies, first permit wins.

```alfa
policy SecurePolicy {
    apply denyOverrides

    rule AllowNormalUsers {
        permit
        condition Subject.Role == "user"
    }

    rule BlockMalicious {
        deny
        condition Environment.ThreatDetected == true
    }
}
// If ThreatDetected, deny wins even if user role matches
```

### permitOverrides

Any permit decision wins. If no permits, first deny wins.

```alfa
policy FlexiblePolicy {
    apply permitOverrides

    rule DenyByDefault {
        deny
    }

    rule AllowAdmin {
        permit
        condition Subject.Role == "admin"
    }
}
// Admin permit wins over default deny
```

### firstApplicable

First rule with permit or deny wins. Other rules not evaluated.

```alfa
policy FastPolicy {
    apply firstApplicable

    rule CheckAdmin {
        permit
        condition Subject.Role == "admin"
    }

    rule CheckOwner {
        permit
        condition Subject.UserId == Resource.OwnerId
    }

    rule DenyOthers {
        deny
    }
}
```

### onlyOne

Exactly one rule must be applicable. If zero or multiple rules apply, returns indeterminate.

```alfa
policy StrictPolicy {
    apply onlyOne

    rule ReadAccess {
        permit
        condition Action.Id == "read"
    }

    rule WriteAccess {
        permit
        condition Action.Id == "write"
    }
}
// Returns indeterminate if neither or both conditions match
```

### denyUnlessPermit

Deny unless at least one permit exists.

```alfa
policy AllowListPolicy {
    apply denyUnlessPermit

    rule AllowedUsers {
        permit
        condition Subject.UserId in AllowedUserList
    }
}
// Automatically denies if no permit found
```

### permitUnlessDeny

Permit unless at least one deny exists.

```alfa
policy BlockListPolicy {
    apply permitUnlessDeny

    rule BlockedUsers {
        deny
        condition Subject.UserId in BlockedUserList
    }
}
// Automatically permits if no deny found
```

## Obligations and Advice

### Obligations

Must be fulfilled when policy/rule effect occurs:

```alfa
rule LoggedAccess {
    permit
    condition Subject.Authenticated == true
    on permit {
        obligation LogAccess {
            user = Subject.UserId
            resource = Resource.Id
            action = Action.Id
            timestamp = Environment.CurrentTime
        }
    }
}
```

### Advice

Supplementary information for enforcement:

```alfa
rule DenyWithReason {
    deny
    condition Subject.AccountStatus == "suspended"
    on deny {
        advice NotifyUser {
            message = "Account suspended. Contact administrator."
            supportEmail = "support@example.com"
        }
    }
}
```

### Simple Obligations/Advice

Without attributes:

```alfa
on permit {
    obligation AuditLog
}

on deny {
    advice NotifyAdministrator
}
```

### Multi-Level Obligations/Advice

Can be defined at rule, policy, and policyset levels:

```alfa
policyset MainPolicySet {
    apply denyOverrides

    policy SubPolicy {
        apply firstApplicable

        rule AllowAccess {
            permit
            on permit {
                obligation RuleObligation  // Rule level
            }
        }

        on permit {
            obligation PolicyObligation  // Policy level
        }
    }

    on permit {
        obligation PolicySetObligation  // PolicySet level
    }
}
// All three obligations are included in the final decision
```

## Expressions

### Comparison Operators

- `==` - Equal to
- `!=` - Not equal to
- `<` - Less than
- `<=` - Less than or equal
- `>` - Greater than
- `>=` - Greater than or equal

### Logical Operators

- `&&` - Logical AND
- `||` - Logical OR
- `!` - Logical NOT

### Literals

```alfa
// String literals
"admin"
"read"

// Integer literals
42
100

// Boolean literals
true
false

// Double literals
99.99
3.14
```

### Attribute References

```alfa
// Imported attributes (capitalize first letter)
Subject.Role
Resource.Type
Action.Id
Environment.CurrentTime

// Custom attributes
subject.customAttribute
resource.resourceType
```

### Complex Expressions

```alfa
// Multiple conditions
Subject.Role == "admin" && Resource.Confidential == true

// Nested conditions
(Subject.Role == "manager" || Subject.Role == "admin") &&
Resource.Department == Subject.Department

// Arithmetic
Resource.Amount > 1000 && Subject.ApprovalLimit >= Resource.Amount

// Negation
!Environment.MaintenanceMode && Subject.Active == true
```

## Complete Examples

### Basic Access Control

```alfa
namespace MyApp.BasicAuth {
    import Oasis.Attributes.Subject.*
    import Oasis.Attributes.Resource.*
    import Oasis.Attributes.Action.*

    policy DocumentAccess {
        apply denyOverrides

        rule AdminFullAccess {
            permit
            target clause Subject.Role == "admin"
        }

        rule OwnerAccess {
            permit
            condition Subject.Id == Resource.Id
        }

        rule DenyOthers {
            deny
        }
    }
}
```

### E-Commerce Order Management

```alfa
namespace ECommerce.Orders {
    import Oasis.Attributes.Subject.*
    import Oasis.Attributes.Resource.*
    import Oasis.Attributes.Action.*

    policyset OrderManagement {
        apply denyOverrides

        policy OrderAccess {
            apply firstApplicable

            rule AdminFullAccess {
                permit
                target clause Subject.Role == "admin"
                on permit {
                    obligation LogAdminAccess {
                        userId = Subject.Id
                        action = Action.Id
                    }
                }
            }

            rule CustomerViewOwn {
                permit
                target clause Subject.Role == "customer"
                condition Subject.Id == Resource.Id && Action.Id == "read"
            }

            rule ManagerApprove {
                permit
                target clause Subject.Role == "manager"
                condition Action.Id == "approve"
                on permit {
                    obligation NotifyCustomer
                    obligation UpdateOrderStatus
                }
            }

            rule DenyOthers {
                deny
                on deny {
                    advice ContactSupport {
                        message = "Insufficient permissions"
                    }
                }
            }
        }

        policy SecurityPolicy {
            apply denyOverrides

            rule BlockSuspended {
                deny
                condition Subject.Role == "suspended"
                on deny {
                    advice AccountSuspended {
                        message = "Account is suspended. Contact support."
                        supportEmail = "support@example.com"
                    }
                }
            }
        }

        on permit {
            obligation AuditAccess
        }
    }
}
```

### Multi-Tier Application

```alfa
namespace Corp.MultiTier {
    import Oasis.Attributes.Subject.*
    import Oasis.Attributes.Resource.*
    import Oasis.Attributes.Action.*
    import Oasis.Attributes.Environment.*

    attribute Department {
        category = subject
        id = "urn:corp:subject:department"
        type = string
    }

    attribute Tier {
        category = resource
        id = "urn:corp:resource:tier"
        type = string
    }

    policyset ApplicationAccess {
        apply denyOverrides

        target clause Resource.Type == "application"

        policy TierAccess {
            apply firstApplicable

            rule Tier1Access {
                permit
                condition Subject.Department == "engineering" &&
                          Resource.Tier == "tier1"
            }

            rule Tier2Access {
                permit
                condition (Subject.Department == "engineering" ||
                          Subject.Department == "support") &&
                          Resource.Tier == "tier2"
            }

            rule Tier3Access {
                permit
                condition Resource.Tier == "tier3"
            }

            rule DenyOthers {
                deny
            }
        }

        policy MaintenancePolicy {
            apply denyOverrides

            rule BlockDuringMaintenance {
                deny
                condition Environment.CurrentTime == "maintenance"
                on deny {
                    advice MaintenanceNotice {
                        message = "System under maintenance"
                        estimatedCompletion = "2024-12-01T18:00:00"
                    }
                }
            }
        }
    }
}
```

### Nested PolicySets

```alfa
namespace Enterprise.Access {
    import Oasis.Attributes.Subject.*
    import Oasis.Attributes.Resource.*
    import Oasis.Attributes.Action.*

    policyset EnterpriseAccessControl {
        apply denyOverrides

        policyset HRSystem {
            apply permitOverrides

            target clause Resource.Type == "hr-record"

            policy HRAccess {
                rule HRStaff {
                    permit
                    condition Subject.Role == "hr-staff"
                }
            }

            on permit {
                obligation LogHRAccess
            }
        }

        policyset FinanceSystem {
            apply denyOverrides

            target clause Resource.Type == "financial-record"

            policy FinanceAccess {
                rule FinanceStaff {
                    permit
                    condition Subject.Role == "finance-staff"
                }

                rule Auditor {
                    permit
                    target clause Action.Id == "read"
                    condition Subject.Role == "auditor"
                }
            }

            on permit {
                obligation LogFinanceAccess
                obligation NotifyCompliance
            }
        }

        on deny {
            advice UnauthorizedAccessAttempt
        }
    }
}
```

## Best Practices

### Naming Conventions

- Use PascalCase for policy, policyset, and rule names
- Use descriptive names that indicate purpose
- Prefix rules with effect: `AllowAdmin`, `DenyGuests`, `PermitOwner`

### Organization

- Group related policies in namespaces
- Use policysets to organize multiple policies
- Keep rules focused on single responsibility

### Performance

- Put most specific rules first in `firstApplicable`
- Use targets to filter early
- Avoid complex expressions when simple ones suffice

### Maintainability

- Add obligations for audit trails
- Use advice to provide user-friendly error messages
- Document complex conditions with comments
- Keep policies modular and reusable

### Security

- Use `denyOverrides` for security-critical policies
- Always include a default deny rule
- Validate all attribute references
- Test edge cases thoroughly
