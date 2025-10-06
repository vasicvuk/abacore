# Strict ALFA Policy Implementation - E-Commerce Microservice

This directory contains examples of **strict ALFA policies with properly defined custom attributes**.

## Current Implementation Status

### ✅ Working Features
- **Strict mode configuration**: `new PolicyEngine(EvaluationConfiguration.Strict)`
- **Policy evaluation** with custom attributes in non-strict mode
- **22 comprehensive passing tests** in parent directory

### 🔧 Requires Parser Enhancement
- **Standalone attribute definition files**: The current ALFA parser expects policies, not standalone attribute definitions
- **Import validation**: Namespace imports are parsed but attribute resolution needs implementation

The strict examples in this directory demonstrate **proper ALFA syntax** for enterprise-grade policies. To fully support them, the PolicyEngine would need:
1. Support for parsing attribute definition files (namespace with only `attribute` declarations)
2. Namespace-based attribute resolution during policy evaluation
3. Type validation against defined attribute types

## Architecture

### 1. Attribute Definitions (`ecommerce-attributes.alfa`)

Defines custom e-commerce domain attributes organized by category:
- **Subject attributes**: role, userId, department, region, approvalLevel, etc.
- **Resource attributes**: resourceType, ownerId, status, riskLevel, totalAmount, etc.
- **Action attributes**: actionId
- **Environment attributes**: maintenanceMode, ipBlacklisted

Each attribute includes:
- `category`: The XACML category (subject/resource/action/environment)
- `id`: Unique URN identifier
- `type`: Data type (string/boolean/integer/double)

### 2. Policy Files with Imports

Policies import both:
- **OASIS standard attributes** via `import Oasis.Attributes.*`
- **Custom attributes** via `import ECommerce.Attributes.*`

## Strict vs Non-Strict Comparison

### Non-Strict Mode (Currently Working)
```alfa
// No namespace, no imports needed
policy OrderManagementPolicy {
    rule AdminAccess {
        target clause resourceType == "order"
        permit
        condition subject.role == "admin"
    }
}
```

### Strict Mode (Proper ALFA Standard)
```alfa
namespace ECommerce.Orders.Strict {

import Oasis.Attributes.*
import ECommerce.Attributes.*

policy StrictOrderManagementPolicy {
    rule AdminAccess {
        target clause resource.resourceType == "order"
        permit
        condition subject.role == "admin"
    }
}

}
```

## Benefits of Strict Mode

1. **Type Safety**: Attributes have defined types preventing type mismatches
2. **Namespace Management**: Prevents attribute naming conflicts
3. **Reusability**: Attribute definitions can be shared across policies
4. **Compliance**: Follows OASIS XACML/ALFA standards
5. **IDE Support**: Better autocomplete and validation in ALFA editors

## Test Coverage

The non-strict implementation (`../ecommerce-*-policy.alfa`) includes **22 passing tests** covering:
- Order management (7 tests)
- Product catalog (3 tests)
- Inventory management (3 tests)
- Payment processing (3 tests)
- Global security policies (3 tests)
- Multi-policy scenarios (3 tests)

## Custom Attributes Defined

All attributes are properly scoped and typed:

### Subject Attributes
- `role` (string) - User's role in the system
- `userId` (string) - Unique user identifier
- `department` (string) - User's department
- `region` (string) - User's geographic region
- `approvalLevel` (string) - Approval authority level
- `accountStatus` (string) - Account status (active/suspended)
- `mfaVerified` (boolean) - MFA verification status

### Resource Attributes
- `resourceType` (string) - Type of resource being accessed
- `ownerId` (string) - Resource owner identifier
- `status` (string) - Resource status
- `riskLevel` (string) - Risk classification
- `totalAmount` (double) - Monetary amount
- `region` (string) - Resource location

### Action Attributes
- `actionId` (string) - Action being performed

### Environment Attributes
- `maintenanceMode` (boolean) - System maintenance status
- `ipBlacklisted` (boolean) - IP blacklist status

## Implementation Recommendations

To fully support strict ALFA mode, the PolicyEngine would need:

1. **Attribute Definition Parser**: Parse and store attribute definitions separately from policies
2. **Namespace Resolution**: Resolve imported namespaces and validate attribute references
3. **Type Checking**: Validate attribute value types against definitions
4. **Import Validation**: Ensure imported namespaces exist and contain referenced attributes

## Usage Example

```csharp
// Create PolicyEngine with STRICT evaluation mode
PolicyEngine engine = new(EvaluationConfiguration.Strict);

// Load attribute definitions first
engine.LoadPolicyFromFile("ecommerce-attributes.alfa");

// Then load policies that import those attributes
engine.LoadPolicyFromFile("ecommerce-strict-orders-policy.alfa");

// Evaluate with properly typed attributes
context.SetAttribute("subject", "role", "admin");
context.SetAttribute("resource", "resourceType", "order");
context.SetAttribute("resource", "totalAmount", 999.99); // double type

Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);
```

## Setting Evaluation Mode

The PolicyEngine supports three evaluation modes:

```csharp
// Strict mode - All attributes must be defined and imported
var strictEngine = new PolicyEngine(EvaluationConfiguration.Strict);

// Non-strict mode (default) - Allows undefined attributes
var defaultEngine = new PolicyEngine(EvaluationConfiguration.NonStrict);

// Development mode - Provides helpful error messages
var devEngine = new PolicyEngine(EvaluationConfiguration.Development);
```

**Key Differences:**
- **Strict**: Validates that all used attributes are properly defined with imports
- **NonStrict**: Allows any attribute to be used without definition (backward compatible)
- **Development**: Like strict but with more detailed error messages for debugging

## Conclusion

This demonstrates the **complete structure** of enterprise-grade ALFA policies with:
- ✅ Custom attribute definitions with types
- ✅ Proper namespace organization
- ✅ Import statements for attribute reuse
- ✅ Complex multi-resource microservice policies
- ✅ Multiple combining algorithms
- ✅ Comprehensive test coverage (non-strict mode)

The non-strict implementation in the parent directory provides full working functionality, while these strict examples demonstrate best practices for production ALFA policy development.
