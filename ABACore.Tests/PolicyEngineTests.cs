using ABACore.Exceptions;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Integration tests for the PolicyEngine class.
/// Tests run against all compilation strategies to ensure consistent behavior.
/// </summary>
public class PolicyEngineTests : CompilationStrategyTestBase
{
    #region Load Single Policy Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_ValidPolicy_ShouldReturnPolicyIdAndVersion(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy TestPolicy {
                rule Rule1 { permit }
            }
        ";

        // Act
        PolicyRegistration registration = engine.LoadPolicy(policyText);
        string policyId = registration.PolicyId;
        int version = registration.Version;

        // Assert
        Assert.Equal("TestPolicy", policyId);
        Assert.Equal(1, version);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_WithExplicitId_ShouldUseProvidedId(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy OriginalPolicy {
                rule Rule1 { permit }
            }
        ";

        // Act
        PolicyRegistration registration = engine.LoadPolicy(policyText);
        string policyId = registration.PolicyId;
        int version = registration.Version;

        // Assert
        Assert.Equal("OriginalPolicy", policyId);
        Assert.Equal(1, version);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_EmptyString_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.LoadPolicy(""));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_NullString_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => engine.LoadPolicy(null!));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_InvalidSyntax_ShouldThrowAlfaParseException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy InvalidPolicy {
                invalid syntax here
            }
        ";

        // Act & Assert
        Assert.Throws<AlfaParseException>(() => engine.LoadPolicy(policyText));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_SamePolicyTwice_ShouldCreateNewVersion(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy VersionedPolicy {
                rule Rule1 { permit }
            }
        ";

        // Act
        PolicyRegistration firstRegistration = engine.LoadPolicy(policyText);
        string policyId1 = firstRegistration.PolicyId;
        int version1 = firstRegistration.Version;
        PolicyRegistration secondRegistration = engine.LoadPolicy(policyText);
        string policyId2 = secondRegistration.PolicyId;
        int version2 = secondRegistration.Version;

        // Assert
        Assert.Equal("VersionedPolicy", policyId1);
        Assert.Equal("VersionedPolicy", policyId2);
        Assert.Equal(1, version1);
        Assert.Equal(2, version2);
    }

    #endregion

    #region Load Multiple Policies Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicies_MultiplePolicies_ShouldLoadAll(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string[] policyTexts =
        [
            "policy Policy1 { rule Rule1 { permit } }",
            "policy Policy2 { rule Rule1 { deny} }",
            "policy Policy3 { rule Rule1 { permit } }"
        ];

        // Act
        var results = engine.LoadPolicies(policyTexts).ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("Policy1", results[0].PolicyId);
        Assert.Equal("Policy2", results[1].PolicyId);
        Assert.Equal("Policy3", results[2].PolicyId);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicies_EmptyCollection_ShouldReturnEmptyResult(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string[] policyTexts = [];

        // Act
        var results = engine.LoadPolicies(policyTexts).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicies_NullCollection_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => engine.LoadPolicies(null!));
    }

    #endregion

    #region Load From File Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicyFromFile_ValidFile_ShouldLoadPolicy(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_policy_{Guid.NewGuid()}.alfa");
        File.WriteAllText(tempFile, "policy FilePolicy { rule Rule1 { permit } }");

        try
        {
            // Act
            PolicyRegistration fileRegistration = engine.LoadPolicyFromFile(tempFile);
            string policyId = fileRegistration.PolicyId;
            int version = fileRegistration.Version;

            // Assert
            Assert.Equal("FilePolicy", policyId);
            Assert.Equal(1, version);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicyFromFile_NonExistentFile_ShouldThrowFileNotFoundException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string nonExistentFile = "nonexistent_policy.alfa";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => engine.LoadPolicyFromFile(nonExistentFile));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicyFromFile_EmptyPath_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.LoadPolicyFromFile(""));
    }

    #endregion

    #region Load From Directory Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPoliciesFromDirectory_ValidDirectory_ShouldLoadAllPolicies(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string tempDir = Path.Combine(Path.GetTempPath(), $"test_policies_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "policy1.alfa"), "policy DirPolicy1 { rule Rule1 { permit } }");
            File.WriteAllText(Path.Combine(tempDir, "policy2.alfa"), "policy DirPolicy2 { rule Rule1 { deny } }");

            // Act
            var results = engine.LoadPoliciesFromDirectory(tempDir).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.PolicyId == "DirPolicy1");
            Assert.Contains(results, r => r.PolicyId == "DirPolicy2");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPoliciesFromDirectory_EmptyDirectory_ShouldReturnEmpty(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string tempDir = Path.Combine(Path.GetTempPath(), $"empty_policies_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var results = engine.LoadPoliciesFromDirectory(tempDir).ToList();

            // Assert
            Assert.Empty(results);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPoliciesFromDirectory_NonExistentDirectory_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string nonExistentDir = "nonexistent_directory";

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => engine.LoadPoliciesFromDirectory(nonExistentDir));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPoliciesFromDirectory_CustomPattern_ShouldLoadMatchingFiles(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string tempDir = Path.Combine(Path.GetTempPath(), $"pattern_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "policy1.alfa"), "policy Pattern1 { rule Rule1 { permit } }");
            File.WriteAllText(Path.Combine(tempDir, "policy2.txt"), "policy Pattern2 { rule Rule1 { deny } }");

            // Act
            var results = engine.LoadPoliciesFromDirectory(tempDir, "*.alfa").ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Pattern1", results[0].PolicyId);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPoliciesFromDirectory_Recursive_ShouldLoadFromSubdirectories(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string tempDir = Path.Combine(Path.GetTempPath(), $"recursive_test_{Guid.NewGuid()}");
        string subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "policy1.alfa"), "policy Recursive1 { rule Rule1 { permit } }");
            File.WriteAllText(Path.Combine(subDir, "policy2.alfa"), "policy Recursive2 { rule Rule1 { deny } }");

            // Act
            var results = engine.LoadPoliciesFromDirectory(tempDir, "*.alfa", recursive: true).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.PolicyId == "Recursive1");
            Assert.Contains(results, r => r.PolicyId == "Recursive2");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Evaluate Policy Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_LoadedPolicy_ShouldReturnDecision(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy EvalTestPolicy {
                rule Rule1 { permit }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("EvalTestPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_WithContext_ShouldUseAttributes(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText = @"
            policy ContextPolicy {
                rule AgeCheck {
                    permit
                    condition age > 18
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "age", 25);

        // Act
        Decision decision = engine.Evaluate("ContextPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_NonExistentPolicy_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        EvaluationContext context = new();

        // Act & Assert
        Assert.Throws<PolicyNotFoundException>(() => engine.Evaluate("NonExistentPolicy", context));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_NullContext_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy TestPolicy { rule Rule1 { permit } }");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => engine.Evaluate("TestPolicy", null!));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_SpecificVersion_ShouldEvaluateCorrectVersion(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyText1 = "policy VersionPolicy { rule Rule1 { permit } }";
        string policyText2 = "policy VersionPolicy { rule Rule1 { deny } }";

        PolicyRegistration firstRegistration = engine.LoadPolicy(policyText1);
        int version1 = firstRegistration.Version;
        PolicyRegistration secondRegistration = engine.LoadPolicy(policyText2);
        int version2 = secondRegistration.Version;

        EvaluationContext context = new();
        Decision decision1 = engine.Evaluate("VersionPolicy", context, version1);
        Decision decision2 = engine.Evaluate("VersionPolicy", context, version2);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision1.Effect);
        Assert.Equal(DecisionEffect.Deny, decision2.Effect);
    }

    #endregion

    #region Evaluate Policy Set Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void EvaluatePolicySet_DenyOverrides_ShouldReturnDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy SetPolicy1 {  rule Rule1 { permit } }");
        engine.LoadPolicy("policy SetPolicy2 {  rule Rule1 { deny } }");

        EvaluationContext context = new();
        string[] policyIds = ["SetPolicy1", "SetPolicy2"];

        // Act
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.DenyOverrides);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void EvaluatePolicySet_PermitOverrides_ShouldReturnPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy SetPolicy1 { rule Rule1 { permit } }");
        engine.LoadPolicy("policy SetPolicy2 { rule Rule1 { deny } }");

        EvaluationContext context = new();
        string[] policyIds = ["SetPolicy1", "SetPolicy2"];

        // Act
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.PermitOverrides);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void EvaluatePolicySet_EmptyPolicyIds_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        EvaluationContext context = new();
        string[] policyIds = [];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.EvaluatePolicySet(policyIds, context));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void EvaluatePolicySet_NonExistentPolicy_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy ExistingPolicy { rule Rule1 { permit } }");

        EvaluationContext context = new();
        string[] policyIds = ["ExistingPolicy", "NonExistentPolicy"];

        // Act & Assert
        Assert.Throws<PolicyNotFoundException>(() => engine.EvaluatePolicySet(policyIds, context));
    }

    #endregion

    #region Policy Management Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void GetLoadedPolicies_ShouldReturnAllPolicyIds(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy ManagePolicy1 { rule Rule1 { permit } }");
        engine.LoadPolicy("policy ManagePolicy2 { rule Rule1 { deny } }");

        // Act
        var policyIds = engine.GetLoadedPolicies().ToList();

        // Assert
        Assert.Equal(2, policyIds.Count);
        Assert.Contains("ManagePolicy1", policyIds);
        Assert.Contains("ManagePolicy2", policyIds);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void GetPolicyVersions_ShouldReturnAllVersions(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy VersionedPolicy { rule Rule1 { permit } }");
        engine.LoadPolicy("policy VersionedPolicy { rule Rule1 { deny } }");
        engine.LoadPolicy("policy VersionedPolicy { rule Rule1 { permit } }");

        // Act
        var versions = engine.GetPolicyVersions("VersionedPolicy").ToList();

        // Assert
        Assert.Equal(3, versions.Count);
        Assert.Contains(1, versions);
        Assert.Contains(2, versions);
        Assert.Contains(3, versions);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ContainsPolicy_ExistingPolicy_ShouldReturnTrue(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy ContainsPolicy { rule Rule1 { permit } }");

        // Act
        bool contains = engine.ContainsPolicy("ContainsPolicy");

        // Assert
        Assert.True(contains);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ContainsPolicy_NonExistentPolicy_ShouldReturnFalse(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act
        bool contains = engine.ContainsPolicy("NonExistentPolicy");

        // Assert
        Assert.False(contains);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ContainsPolicy_SpecificVersion_ShouldCheckVersion(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        PolicyRegistration registration = engine.LoadPolicy("policy VersionCheckPolicy { rule Rule1 { permit } }");
        int version = registration.Version;

        // Act
        bool containsVersion = engine.ContainsPolicy("VersionCheckPolicy", version);
        bool containsNonExistentVersion = engine.ContainsPolicy("VersionCheckPolicy", 999);

        // Assert
        Assert.True(containsVersion);
        Assert.False(containsNonExistentVersion);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void RemovePolicy_ExistingPolicy_ShouldReturnTrue(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy RemovePolicy { rule Rule1 { permit } }");

        // Act
        bool removed = engine.RemovePolicy("RemovePolicy");

        // Assert
        Assert.True(removed);
        Assert.False(engine.ContainsPolicy("RemovePolicy"));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void RemovePolicy_NonExistentPolicy_ShouldReturnFalse(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Act
        bool removed = engine.RemovePolicy("NonExistentPolicy");

        // Assert
        Assert.False(removed);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void RemovePolicy_SpecificVersion_ShouldRemoveOnlyThatVersion(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        PolicyRegistration registration1 = engine.LoadPolicy("policy MultiVersionPolicy { rule Rule1 { permit } }");
        int version1 = registration1.Version;
        PolicyRegistration registration2 = engine.LoadPolicy("policy MultiVersionPolicy { rule Rule1 { deny } }");
        int version2 = registration2.Version;


        // Act
        bool removed = engine.RemovePolicy("MultiVersionPolicy", version1);

        // Assert
        Assert.True(removed);
        Assert.False(engine.ContainsPolicy("MultiVersionPolicy", version1));
        Assert.True(engine.ContainsPolicy("MultiVersionPolicy", version2));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Clear_ShouldRemoveAllPolicies(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        engine.LoadPolicy("policy ClearPolicy1 { rule Rule1 { permit } }");
        engine.LoadPolicy("policy ClearPolicy2 { rule Rule1 { deny } }");

        // Act
        engine.Clear();

        // Assert
        Assert.Empty(engine.GetLoadedPolicies());
    }

    #endregion

    #region Error Handling Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void LoadPolicy_ParseError_ShouldThrowAlfaParseException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string invalidPolicy = "this is not valid ALFA syntax";

        // Act & Assert
        Assert.Throws<AlfaParseException>(() => engine.LoadPolicy(invalidPolicy));
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Evaluate_EmptyPolicyId_ShouldThrowException(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        EvaluationContext context = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => engine.Evaluate("", context));
    }

    #endregion

    #region Integration Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Integration_CompleteWorkflow_ShouldWork(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Load a policy
        string policyText = @"
            policy IntegrationPolicy {
                apply denyOverrides
                rule AdminAccess {
                    permit
                    condition isAdmin == true
                }
                rule AgeRestriction {
                    deny
                    condition age < 18
                }
            }
        ";

        // Act
        PolicyRegistration registration = engine.LoadPolicy(policyText);
        string policyId = registration.PolicyId;
        int version = registration.Version;

        // Test with admin user
        EvaluationContext adminContext = new();
        adminContext.SetAttribute("subject", "isAdmin", true);
        adminContext.SetAttribute("subject", "age", 25);
        Decision adminDecision = engine.Evaluate(policyId, adminContext);

        // Test with minor user
        EvaluationContext minorContext = new();
        minorContext.SetAttribute("subject", "isAdmin", false);
        minorContext.SetAttribute("subject", "age", 15);
        Decision minorDecision = engine.Evaluate(policyId, minorContext);

        // Test with adult non-admin
        EvaluationContext adultContext = new();
        adultContext.SetAttribute("subject", "isAdmin", false);
        adultContext.SetAttribute("subject", "age", 25);
        Decision adultDecision = engine.Evaluate(policyId, adultContext);

        // Assert
        Assert.Equal("IntegrationPolicy", policyId);
        Assert.Equal(1, version);
        Assert.Equal(DecisionEffect.Permit, adminDecision.Effect);
        Assert.Equal(DecisionEffect.Deny, minorDecision.Effect);
        Assert.Equal(DecisionEffect.NotApplicable, adultDecision.Effect);
    }

    #endregion

    #region E-Commerce Microservice Complex Policy Tests

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_OrderManagement_AdminFullAccess_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "admin123");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "ownerId", "user456");
        context.SetAttribute("resource", "region", "US");

        // Action
        context.SetAttribute("action", "action", "delete");

        // Act
        Decision decision = engine.Evaluate("OrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_OrderManagement_OwnerCanReadOwnOrder_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer");
        context.SetAttribute("subject", "userId", "user123");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "ownerId", "user123");
        context.SetAttribute("resource", "region", "US");

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("OrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_OrderManagement_CustomerServiceRegionalAccess_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer_service");
        context.SetAttribute("subject", "userId", "cs789");
        context.SetAttribute("subject", "region", "EU");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "ownerId", "user456");
        context.SetAttribute("resource", "region", "EU");

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("OrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_OrderManagement_HighRiskWithoutApproval_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer_service");
        context.SetAttribute("subject", "approvalLevel", "junior");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "riskLevel", "high");

        // Action
        context.SetAttribute("action", "action", "update");

        // Act
        Decision decision = engine.Evaluate("OrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_OrderManagement_WarehouseUpdateShipping_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "warehouse_staff");
        context.SetAttribute("subject", "department", "warehouse");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "status", "processing");

        // Action
        context.SetAttribute("action", "action", "update");

        // Act
        Decision decision = engine.Evaluate("OrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_ProductCatalog_ProductManagerCategoryAccess_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-products-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "product_manager");
        context.SetAttribute("subject", "category", "electronics");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "product");
        context.SetAttribute("resource", "category", "electronics");

        // Action
        context.SetAttribute("action", "action", "update");

        // Act
        Decision decision = engine.Evaluate("ProductCatalogPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_ProductCatalog_DenyFeaturedProductDeletion_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-products-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "product_manager");
        context.SetAttribute("subject", "category", "electronics");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "product");
        context.SetAttribute("resource", "category", "electronics");
        context.SetAttribute("resource", "featured", true);

        // Action
        context.SetAttribute("action", "action", "delete");

        // Act
        Decision decision = engine.Evaluate("ProductCatalogPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_ProductCatalog_PricingTeamUpdatePrices_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-products-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "pricing_analyst");
        context.SetAttribute("subject", "department", "pricing");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "product");

        // Action
        context.SetAttribute("action", "action", "updatePrice");

        // Act
        Decision decision = engine.Evaluate("ProductCatalogPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Inventory_WarehouseStaffUpdateOwnWarehouse_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-inventory-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "warehouse_staff");
        context.SetAttribute("subject", "warehouseId", "WH001");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "inventory");
        context.SetAttribute("resource", "warehouseId", "WH001");

        // Action
        context.SetAttribute("action", "action", "update");

        // Act
        Decision decision = engine.Evaluate("InventoryManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Inventory_DenyInventoryDeletion_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-inventory-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "admin");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "inventory");

        // Action
        context.SetAttribute("action", "action", "delete");

        // Act
        Decision decision = engine.Evaluate("InventoryManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Inventory_EmergencyCriticalStockAccess_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-inventory-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "inventory_manager");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "inventory");
        context.SetAttribute("resource", "stockLevel", "critical");

        // Action
        context.SetAttribute("action", "action", "adjust");

        // Act
        Decision decision = engine.Evaluate("InventoryManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Payments_PaymentProcessorWithCertification_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-payments-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "payment_processor");
        context.SetAttribute("subject", "certified", true);
        context.SetAttribute("subject", "department", "payments");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "payment");

        // Action
        context.SetAttribute("action", "action", "process");

        // Act
        Decision decision = engine.Evaluate("PaymentProcessingPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Payments_DenyInternationalWithoutClearance_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-payments-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "payment_processor");
        context.SetAttribute("subject", "certified", true);
        context.SetAttribute("subject", "department", "payments");
        context.SetAttribute("subject", "internationalClearance", false);

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "payment");
        context.SetAttribute("resource", "paymentType", "international");

        // Action
        context.SetAttribute("action", "action", "process");

        // Act
        Decision decision = engine.Evaluate("PaymentProcessingPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_Payments_DenyLargePaymentsWithoutDualApproval_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-payments-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "payment_processor");
        context.SetAttribute("subject", "certified", true);
        context.SetAttribute("subject", "department", "payments");
        context.SetAttribute("subject", "dualApprovalObtained", false);

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "payment");
        context.SetAttribute("resource", "amount", 15000);

        // Action
        context.SetAttribute("action", "action", "process");

        // Act
        Decision decision = engine.Evaluate("PaymentProcessingPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_PolicySet_GlobalSecurityDuringMaintenance_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-policyset-all.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "admin");

        // Environment attributes
        context.SetAttribute("environment", "maintenanceMode", true);

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("GlobalSecurityPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_PolicySet_DenySuspendedUsers_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-policyset-all.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer");
        context.SetAttribute("subject", "accountStatus", "suspended");

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("GlobalSecurityPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_PolicySet_RequireMFAForSensitiveOperations_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-policyset-all.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "mfaVerified", false);

        // Action
        context.SetAttribute("action", "action", "delete");

        // Act
        Decision decision = engine.Evaluate("GlobalSecurityPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_PolicySet_EUDataResidencyCompliance_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-regional-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "analyst");
        context.SetAttribute("subject", "accessRegion", "US");
        context.SetAttribute("subject", "gdprCertified", false);

        // Resource attributes
        context.SetAttribute("resource", "dataRegion", "EU");

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("RegionalAccessPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_PolicySet_TimeBasedAfterHoursRestriction_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string policyPath = Path.Combine("TestData", "Microservice", "ecommerce-timebased-policy.alfa");
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "employee");
        context.SetAttribute("subject", "afterHoursApproval", false);

        // Environment attributes
        context.SetAttribute("environment", "businessHours", false);

        // Action
        context.SetAttribute("action", "action", "read");

        // Act
        Decision decision = engine.Evaluate("TimeBasedAccessPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_MultiplePolicySets_DenyOverrides_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string ordersPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        string globalPath = Path.Combine("TestData", "Microservice", "ecommerce-policyset-all.alfa");

        engine.LoadPolicyFromFile(ordersPath);
        engine.LoadPolicyFromFile(globalPath);

        EvaluationContext context = new();
        // Subject attributes - admin with suspended account
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "accountStatus", "suspended");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");

        // Action
        context.SetAttribute("action", "action", "read");

        // Act - evaluate policy set with DenyOverrides
        string[] policyIds = ["OrderManagementPolicy", "GlobalSecurityPolicy"];
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.DenyOverrides);

        // Assert - should deny because GlobalSecurityPolicy denies suspended users
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_MultiplePolicySets_PermitOverrides_ComplexScenario(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);
        string productsPath = Path.Combine("TestData", "Microservice", "ecommerce-products-policy.alfa");
        string timePath = Path.Combine("TestData", "Microservice", "ecommerce-timebased-policy.alfa");

        engine.LoadPolicyFromFile(productsPath);
        engine.LoadPolicyFromFile(timePath);

        EvaluationContext context = new();
        // Subject attributes - admin accessing after hours
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "afterHoursApproval", false);

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "product");

        // Environment attributes
        context.SetAttribute("environment", "businessHours", false);

        // Action
        context.SetAttribute("action", "action", "read");

        // Act - evaluate policy set with PermitOverrides
        string[] policyIds = ["TimeBasedAccessPolicy", "ProductCatalogPolicy"];
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.PermitOverrides);

        // Assert - TimeBasedAccessPolicy permits for admin 24/7
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceMicroservice_ComplexMultiAttributeScenario_AllPolicies(CompilationStrategy strategy)
    {
        // Arrange
        PolicyEngine engine = CreateEngine(strategy);

        // Load all microservice policies
        string ordersPath = Path.Combine("TestData", "Microservice", "ecommerce-orders-policy.alfa");
        string productsPath = Path.Combine("TestData", "Microservice", "ecommerce-products-policy.alfa");
        string inventoryPath = Path.Combine("TestData", "Microservice", "ecommerce-inventory-policy.alfa");
        string paymentsPath = Path.Combine("TestData", "Microservice", "ecommerce-payments-policy.alfa");
        string globalPath = Path.Combine("TestData", "Microservice", "ecommerce-policyset-all.alfa");

        engine.LoadPolicyFromFile(ordersPath);
        engine.LoadPolicyFromFile(productsPath);
        engine.LoadPolicyFromFile(inventoryPath);
        engine.LoadPolicyFromFile(paymentsPath);
        engine.LoadPolicyFromFile(globalPath);

        // Test scenario: Finance department trying to process refund during business hours
        EvaluationContext context = new();

        // Subject attributes
        context.SetAttribute("subject", "role", "finance");
        context.SetAttribute("subject", "department", "finance");
        context.SetAttribute("subject", "accountStatus", "active");
        context.SetAttribute("subject", "mfaVerified", true);
        context.SetAttribute("subject", "region", "US");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "status", "cancelled");
        context.SetAttribute("resource", "region", "US");

        // Environment attributes
        context.SetAttribute("environment", "maintenanceMode", false);
        context.SetAttribute("environment", "businessHours", true);
        context.SetAttribute("environment", "ipBlacklisted", false);

        // Action
        context.SetAttribute("action", "action", "refund");

        // Act - evaluate all policies with DenyOverrides
        string[] policyIds = ["OrderManagementPolicy", "GlobalSecurityPolicy"];
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.DenyOverrides);

        // Assert - should permit (finance can refund cancelled orders)
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion

    #region E-Commerce Microservice Strict Policy Tests (With Attribute Definitions)

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_OrderManagement_AdminFullAccess_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        // Load attribute definitions first
        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes - using defined attributes
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "userId", "admin123");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "ownerId", "user456");
        context.SetAttribute("resource", "region", "US");

        // Action
        context.SetAttribute("action", "actionId", "delete");

        // Act
        Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_OrderManagement_OwnerCanReadOwnOrder_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer");
        context.SetAttribute("subject", "userId", "user123");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "ownerId", "user123");

        // Action
        context.SetAttribute("action", "actionId", "read");

        // Act
        Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_OrderManagement_HighRiskWithoutApproval_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer_service");
        context.SetAttribute("subject", "approvalLevel", "junior");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "riskLevel", "high");

        // Action
        context.SetAttribute("action", "actionId", "update");

        // Act
        Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_OrderManagement_FinanceRefundCancelledOrder_ShouldPermit(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "finance");
        context.SetAttribute("subject", "department", "finance");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "status", "cancelled");

        // Action
        context.SetAttribute("action", "actionId", "refund");

        // Act
        Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_GlobalSecurity_DenySuspendedUser_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-global-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "customer");
        context.SetAttribute("subject", "accountStatus", "suspended");

        // Action
        context.SetAttribute("action", "actionId", "read");

        // Act
        Decision decision = engine.Evaluate("StrictGlobalSecurityPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_GlobalSecurity_RequireMFAForDelete_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string policyPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-global-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(policyPath);

        EvaluationContext context = new();
        // Subject attributes
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "mfaVerified", false);

        // Action
        context.SetAttribute("action", "actionId", "delete");

        // Act
        Decision decision = engine.Evaluate("StrictGlobalSecurityPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_MultiplePolicies_DenyOverrides_ShouldDeny(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string ordersPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");
        string globalPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-global-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(ordersPath);
        engine.LoadPolicyFromFile(globalPath);

        EvaluationContext context = new();
        // Subject attributes - admin with suspended account
        context.SetAttribute("subject", "role", "admin");
        context.SetAttribute("subject", "accountStatus", "suspended");
        context.SetAttribute("subject", "userId", "admin123");

        // Resource attributes
        context.SetAttribute("resource", "resourceType", "order");

        // Action
        context.SetAttribute("action", "actionId", "read");

        // Act - evaluate policy set with DenyOverrides
        string[] policyIds = ["StrictOrderManagementPolicy", "StrictGlobalSecurityPolicy"];
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.DenyOverrides);

        // Assert - should deny because global policy denies suspended users
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void ECommerceStrict_ComplexScenario_TypedAttributes_ShouldWork(CompilationStrategy strategy)
    {
        // Arrange - Create engine with STRICT mode
        PolicyEngine engine = CreateEngineStrict(strategy);

        string attributesPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-attributes.alfa");
        string ordersPath = Path.Combine("TestData", "Microservice", "Strict", "ecommerce-strict-orders-policy.alfa");

        engine.LoadPolicyFromFile(attributesPath);
        engine.LoadPolicyFromFile(ordersPath);

        EvaluationContext context = new();

        // Subject attributes with proper types
        context.SetAttribute("subject", "role", "manager");
        context.SetAttribute("subject", "region", "EU");

        // Resource attributes with proper types (totalAmount is double)
        context.SetAttribute("resource", "resourceType", "order");
        context.SetAttribute("resource", "region", "EU");
        context.SetAttribute("resource", "totalAmount", 500.50); // Using double as defined in attributes

        // Action
        context.SetAttribute("action", "actionId", "cancel");

        // Act
        Decision decision = engine.Evaluate("StrictOrderManagementPolicy", context);

        // Assert - should permit (manager in same region, amount < 1000)
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    #endregion
}
