using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for obligations and advice in ALFA policies.
/// These tests verify that policies can return obligations and advice with their decisions.
/// </summary>
public class ObligationAdviceTests
{
    #region Basic Obligation Tests

    [Fact]
    public void Evaluate_PermitWithObligation_ShouldReturnObligationInDecision()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitWithObligation {
                rule AccessRule {
                    permit
                    on permit {
                        obligation LogAccess
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitWithObligation", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal("LogAccess", decision.Obligations[0].Id);
    }

    [Fact]
    public void Evaluate_PermitWithObligationAndAttributes_ShouldReturnObligationWithAttributes()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitWithObligationAttributes {
                rule AccessRule {
                    permit
                    on permit {
                        obligation LogAccess {
                            logLevel = ""INFO""
                            timestamp = ""2024-01-01""
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitWithObligationAttributes", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal("LogAccess", decision.Obligations[0].Id);
        Assert.NotNull(decision.Obligations[0].Attributes);
        Assert.Equal(2, decision.Obligations[0].Attributes!.Count);
        Assert.True(decision.Obligations[0].Attributes!.ContainsKey("logLevel"));
        Assert.True(decision.Obligations[0].Attributes!.ContainsKey("timestamp"));
    }

    [Fact]
    public void Evaluate_PermitWithMultipleObligations_ShouldReturnAllObligations()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitWithMultipleObligations {
                rule AccessRule {
                    permit
                    on permit {
                        obligation LogAccess
                        obligation NotifyAdmin
                        obligation UpdateAuditTrail
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitWithMultipleObligations", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(3, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "LogAccess");
        Assert.Contains(decision.Obligations, o => o.Id == "NotifyAdmin");
        Assert.Contains(decision.Obligations, o => o.Id == "UpdateAuditTrail");
    }

    [Fact]
    public void Evaluate_DenyRuleWithObligation_ShouldNotReturnObligation()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWithPermitObligation {
                rule DenyRule {
                    deny
                    on permit {
                        obligation ShouldNotAppear
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWithPermitObligation", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.Null(decision.Obligations);
    }

    #endregion

    #region Basic Advice Tests

    [Fact]
    public void Evaluate_DenyWithAdvice_ShouldReturnAdviceInDecision()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWithAdvice {
                rule DenyRule {
                    deny
                    on deny {
                        advice LogDenial
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWithAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
        Assert.Equal("LogDenial", decision.Advice[0].Id);
    }

    [Fact]
    public void Evaluate_DenyWithAdviceAndAttributes_ShouldReturnAdviceWithAttributes()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWithAdviceAttributes {
                rule DenyRule {
                    deny
                    on deny {
                        advice SuggestAlternative {
                            message = ""Contact administrator for access""
                            contactEmail = ""admin@example.com""
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWithAdviceAttributes", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
        Assert.Equal("SuggestAlternative", decision.Advice[0].Id);
        Assert.NotNull(decision.Advice[0].Attributes);
        Assert.Equal(2, decision.Advice[0].Attributes!.Count);
        Assert.True(decision.Advice[0].Attributes!.ContainsKey("message"));
        Assert.True(decision.Advice[0].Attributes!.ContainsKey("contactEmail"));
    }

    [Fact]
    public void Evaluate_DenyWithMultipleAdvice_ShouldReturnAllAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWithMultipleAdvice {
                rule DenyRule {
                    deny
                    on deny {
                        advice LogDenial
                        advice NotifySecurity
                        advice SuggestAlternative
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWithMultipleAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Equal(3, decision.Advice.Count);
        Assert.Contains(decision.Advice, a => a.Id == "LogDenial");
        Assert.Contains(decision.Advice, a => a.Id == "NotifySecurity");
        Assert.Contains(decision.Advice, a => a.Id == "SuggestAlternative");
    }

    [Fact]
    public void Evaluate_PermitRuleWithDenyAdvice_ShouldNotReturnAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitWithDenyAdvice {
                rule PermitRule {
                    permit
                    on deny {
                        advice ShouldNotAppear
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitWithDenyAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.Null(decision.Advice);
    }

    #endregion

    #region Combined Obligation and Advice Tests

    [Fact]
    public void Evaluate_PermitWithObligationAndDenyWithAdvice_ShouldReturnCorrectly()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitWithBoth {
                rule PermitRule {
                    permit
                    on permit {
                        obligation LogAccess
                    }
                    on deny {
                        advice NotifyDenial
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitWithBoth", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal("LogAccess", decision.Obligations[0].Id);
        Assert.Null(decision.Advice); // Should not return deny advice on permit
    }

    [Fact]
    public void Evaluate_DenyWithObligationAndAdvice_ShouldReturnAdviceOnly()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWithBoth {
                rule DenyRule {
                    deny
                    on permit {
                        obligation LogAccess
                    }
                    on deny {
                        advice SuggestAlternative
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWithBoth", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
        Assert.Equal("SuggestAlternative", decision.Advice[0].Id);
        Assert.Null(decision.Obligations); // Should not return permit obligations on deny
    }

    #endregion

    #region Policy-Level Obligation and Advice Tests

    [Fact]
    public void Evaluate_PolicyLevelPermitObligation_ShouldReturnObligation()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PolicyLevelObligation {
                rule PermitRule {
                    permit
                }
                on permit {
                    obligation PolicyLevelLog
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PolicyLevelObligation", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Contains(decision.Obligations, o => o.Id == "PolicyLevelLog");
    }

    [Fact]
    public void Evaluate_PolicyLevelDenyAdvice_ShouldReturnAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PolicyLevelAdvice {
                rule DenyRule {
                    deny
                }
                on deny {
                    advice PolicyLevelWarning
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PolicyLevelAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Contains(decision.Advice, a => a.Id == "PolicyLevelWarning");
    }

    [Fact]
    public void Evaluate_RuleAndPolicyLevelObligations_ShouldMergeObligations()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy MergedObligations {
                rule PermitRule {
                    permit
                    on permit {
                        obligation RuleLevelObligation
                    }
                }
                on permit {
                    obligation PolicyLevelObligation
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("MergedObligations", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(2, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "RuleLevelObligation");
        Assert.Contains(decision.Obligations, o => o.Id == "PolicyLevelObligation");
    }

    #endregion

    #region Combining Algorithm with Obligations and Advice Tests

    [Fact]
    public void Evaluate_DenyOverrides_ShouldMergeObligationsFromMultiplePermits()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyOverridesObligations {
                apply denyOverrides
                rule PermitRule1 {
                    permit
                    on permit {
                        obligation FirstObligation
                    }
                }
                rule PermitRule2 {
                    permit
                    on permit {
                        obligation SecondObligation
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyOverridesObligations", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(2, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "FirstObligation");
        Assert.Contains(decision.Obligations, o => o.Id == "SecondObligation");
    }

    [Fact]
    public void Evaluate_PermitOverrides_ShouldMergeAdviceFromMultipleDenies()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy PermitOverridesAdvice {
                apply permitOverrides
                rule DenyRule1 {
                    deny
                    on deny {
                        advice FirstAdvice
                    }
                }
                rule DenyRule2 {
                    deny
                    on deny {
                        advice SecondAdvice
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("PermitOverridesAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Equal(2, decision.Advice.Count);
        Assert.Contains(decision.Advice, a => a.Id == "FirstAdvice");
        Assert.Contains(decision.Advice, a => a.Id == "SecondAdvice");
    }

    [Fact]
    public void Evaluate_DenyOverrides_DenyWinsWithAdvice_ShouldReturnDenyAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DenyWinsWithAdvice {
                apply denyOverrides
                rule PermitRule {
                    permit
                    on permit {
                        obligation PermitObligation
                    }
                }
                rule DenyRule {
                    deny
                    on deny {
                        advice DenyAdvice
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyWinsWithAdvice", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Contains(decision.Advice, a => a.Id == "DenyAdvice");
        // Deny overrides means permit obligations should be merged
        Assert.NotNull(decision.Obligations);
        Assert.Contains(decision.Obligations, o => o.Id == "PermitObligation");
    }

    #endregion

    #region Policy Set Obligation and Advice Tests

    [Fact]
    public void EvaluatePolicySet_ShouldMergeObligationsFromMultiplePolicies()
    {
        // Arrange
        PolicyEngine engine = new();
        engine.LoadPolicy(@"
            policy Policy1 {
                rule PermitRule {
                    permit
                    on permit {
                        obligation Policy1Obligation
                    }
                }
            }
        ");
        engine.LoadPolicy(@"
            policy Policy2 {
                rule PermitRule {
                    permit
                    on permit {
                        obligation Policy2Obligation
                    }
                }
            }
        ");

        EvaluationContext context = new();
        string[] policyIds = ["Policy1", "Policy2"];

        // Act
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.DenyOverrides);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(2, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "Policy1Obligation");
        Assert.Contains(decision.Obligations, o => o.Id == "Policy2Obligation");
    }

    [Fact]
    public void EvaluatePolicySet_ShouldMergeAdviceFromMultiplePolicies()
    {
        // Arrange
        PolicyEngine engine = new();
        engine.LoadPolicy(@"
            policy Policy1 {
                rule DenyRule {
                    deny
                    on deny {
                        advice Policy1Advice
                    }
                }
            }
        ");
        engine.LoadPolicy(@"
            policy Policy2 {
                rule DenyRule {
                    deny
                    on deny {
                        advice Policy2Advice
                    }
                }
            }
        ");

        EvaluationContext context = new();
        string[] policyIds = ["Policy1", "Policy2"];

        // Act
        Decision decision = engine.EvaluatePolicySet(policyIds, context, CombiningAlgorithm.PermitOverrides);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Equal(2, decision.Advice.Count);
        Assert.Contains(decision.Advice, a => a.Id == "Policy1Advice");
        Assert.Contains(decision.Advice, a => a.Id == "Policy2Advice");
    }

    #endregion

    #region Dynamic Attribute Values in Obligations and Advice

    [Fact]
    public void Evaluate_ObligationWithDynamicAttributes_ShouldEvaluateExpressions()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DynamicObligationAttributes {
                rule PermitRule {
                    permit
                    on permit {
                        obligation LogAccess {
                            userId = subject.userId
                            resourceId = resource.resourceId
                            accessTime = environment.currentTime
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "userId", "user123");
        context.SetAttribute("resource", "resourceId", "resource456");
        context.SetAttribute("environment", "currentTime", "2024-01-01T12:00:00");

        // Act
        Decision decision = engine.Evaluate("DynamicObligationAttributes", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Single(decision.Obligations);
        Assert.Equal("LogAccess", decision.Obligations[0].Id);
        Assert.NotNull(decision.Obligations[0].Attributes);
        Assert.Equal("user123", decision.Obligations[0].Attributes!["userId"]);
        Assert.Equal("resource456", decision.Obligations[0].Attributes!["resourceId"]);
        Assert.Equal("2024-01-01T12:00:00", decision.Obligations[0].Attributes!["accessTime"]);
    }

    [Fact]
    public void Evaluate_AdviceWithComputedValues_ShouldEvaluateExpressions()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy DynamicAdviceAttributes {
                rule DenyRule {
                    deny
                    on deny {
                        advice RetryLater {
                            waitTime = 60
                            message = ""Access denied. Please try again later.""
                            attemptCount = subject.loginAttempts
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "loginAttempts", 3);

        // Act
        Decision decision = engine.Evaluate("DynamicAdviceAttributes", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Single(decision.Advice);
        Assert.Equal("RetryLater", decision.Advice[0].Id);
        Assert.NotNull(decision.Advice[0].Attributes);
        Assert.Equal(60, decision.Advice[0].Attributes!["waitTime"]);
        Assert.Equal("Access denied. Please try again later.", decision.Advice[0].Attributes!["message"]);
        Assert.Equal(3, decision.Advice[0].Attributes!["attemptCount"]);
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void Evaluate_FinancialTransactionWithObligations_ShouldReturnAuditObligations()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy FinancialTransactionPolicy {
                apply denyOverrides
                rule HighValueTransaction {
                    permit
                    condition amount > 10000
                    on permit {
                        obligation LogTransaction {
                            transactionId = resource.transactionId
                            amount = resource.amount
                            requiresApproval = true
                        }
                        obligation NotifyCompliance {
                            department = ""compliance""
                            priority = ""high""
                        }
                    }
                }
                rule StandardTransaction {
                    permit
                    condition amount <= 10000
                    on permit {
                        obligation LogTransaction {
                            transactionId = resource.transactionId
                            amount = resource.amount
                            requiresApproval = false
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("resource", "amount", 15000);
        context.SetAttribute("resource", "transactionId", "TXN-12345");

        // Act
        Decision decision = engine.Evaluate("FinancialTransactionPolicy", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(2, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "LogTransaction");
        Assert.Contains(decision.Obligations, o => o.Id == "NotifyCompliance");
    }

    [Fact]
    public void Evaluate_AccessDeniedWithGuidance_ShouldReturnHelpfulAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policyText = @"
            policy AccessControlWithGuidance {
                apply denyOverrides
                rule InsufficientClearance {
                    deny
                    condition subject.clearanceLevel < resource.requiredClearance
                    on deny {
                        advice RequestClearanceUpgrade {
                            message = ""Your clearance level is insufficient""
                            requiredLevel = resource.requiredClearance
                            currentLevel = subject.clearanceLevel
                            contactPerson = ""security@example.com""
                        }
                    }
                }
                rule SuspendedAccount {
                    deny
                    condition subject.accountStatus == ""suspended""
                    on deny {
                        advice ContactSupport {
                            message = ""Your account has been suspended""
                            supportEmail = ""support@example.com""
                            supportPhone = ""1-800-SUPPORT""
                        }
                    }
                }
            }
        ";
        engine.LoadPolicy(policyText);
        EvaluationContext context = new();
        context.SetAttribute("subject", "clearanceLevel", 2);
        context.SetAttribute("subject", "accountStatus", "active");
        context.SetAttribute("resource", "requiredClearance", 5);

        // Act
        Decision decision = engine.Evaluate("AccessControlWithGuidance", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Contains(decision.Advice, a => a.Id == "RequestClearanceUpgrade");

        AdviceResult advice = decision.Advice.First(a => a.Id == "RequestClearanceUpgrade");
        Assert.NotNull(advice.Attributes);
        Assert.Equal(5, advice.Attributes["requiredLevel"]);
        Assert.Equal(2, advice.Attributes["currentLevel"]);
    }

    #endregion

    #region PolicySet Obligation and Advice Tests

    [Fact]
    public void Evaluate_PolicySetWithObligations_ShouldReturnPolicySetLevelObligations()
    {
        // Arrange
        PolicyEngine engine = new();
        string policySetText = @"
            namespace test {
                policyset AccessControlSet {
                    apply denyOverrides

                    policy Policy1 {
                        rule PermitRule {
                            permit
                            on permit {
                                obligation RuleObligation
                            }
                        }
                    }

                    on permit {
                        obligation PolicySetObligation
                    }
                }
            }
        ";
        engine.LoadPolicy(policySetText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("AccessControlSet", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.Equal(2, decision.Obligations.Count);
        Assert.Contains(decision.Obligations, o => o.Id == "RuleObligation");
        Assert.Contains(decision.Obligations, o => o.Id == "PolicySetObligation");
    }

    [Fact]
    public void Evaluate_PolicySetWithAdvice_ShouldReturnPolicySetLevelAdvice()
    {
        // Arrange
        PolicyEngine engine = new();
        string policySetText = @"
            namespace test {
                policyset DenyAccessSet {
                    apply permitOverrides

                    policy Policy1 {
                        rule DenyRule {
                            deny
                            on deny {
                                advice RuleAdvice
                            }
                        }
                    }

                    on deny {
                        advice PolicySetAdvice
                    }
                }
        }
        ";
        engine.LoadPolicy(policySetText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("DenyAccessSet", context);

        // Assert
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        Assert.Equal(2, decision.Advice.Count);
        Assert.Contains(decision.Advice, a => a.Id == "RuleAdvice");
        Assert.Contains(decision.Advice, a => a.Id == "PolicySetAdvice");
    }

    [Fact]
    public void Evaluate_PolicySetWithMultiplePoliciesAndObligations_ShouldMergeAll()
    {
        // Arrange
        PolicyEngine engine = new();
        string policySetText = @"
            namespace test {
                policyset MultiPolicySet {
                    apply denyOverrides

                    policy Policy1 {
                        rule PermitRule1 {
                            permit
                            on permit {
                                obligation Policy1Obligation
                            }
                        }
                    }

                    policy Policy2 {
                        rule PermitRule2 {
                            permit
                            on permit {
                                obligation Policy2Obligation
                            }
                        }
                    }

                    on permit {
                        obligation PolicySetObligation
                    }
                }
        }
        ";
        engine.LoadPolicy(policySetText);
        EvaluationContext context = new();

        // Act
        Decision decision = engine.Evaluate("MultiPolicySet", context);

        // Assert
        Assert.Equal(DecisionEffect.Permit, decision.Effect);
        Assert.NotNull(decision.Obligations);
        Assert.True(decision.Obligations.Count >= 2); // At least Policy1Obligation and PolicySetObligation
        Assert.Contains(decision.Obligations, o => o.Id == "PolicySetObligation");
    }

    [Fact]
    public void Evaluate_ECommerceResourcePolicySet_ShouldEvaluateCorrectly()
    {
        // Arrange
        PolicyEngine engine = new();
        string policySetText = @"
            namespace test {
                import Oasis.Attributes.Subject.*
                import Oasis.Attributes.Resource.*
                import Oasis.Attributes.Action.*
                import Oasis.Attributes.Environment.*

                policyset ECommerceAccessControl {
                    apply denyOverrides

                    policy OrderAccess {
                        rule AdminFullAccess {
                            permit
                            on permit {
                                obligation LogAdminAccess {
                                    resourceType = ""order""
                                }
                            }
                        }
                    }

                    policy SecurityPolicy {
                        rule DenySuspended {
                            deny
                            on deny {
                                advice ContactSupport {
                                    message = ""Account suspended. Contact support.""
                                }
                            }
                        }
                    }

                    on permit {
                        obligation AuditAccess {
                            timestamp = Environment.CurrentTime
                        }
                    }

                    on deny {
                        advice LogDenial
                    }
                }
        }
        ";
        engine.LoadPolicy(policySetText);

        // Test: With denyOverrides, the SecurityPolicy deny rule will execute and return Deny
        // This demonstrates policyset evaluation with multiple policies and obligation/advice merging
        EvaluationContext context = new();

        // Act - DenyOverrides means any deny wins, so SecurityPolicy's deny will win
        Decision decision = engine.Evaluate("ECommerceAccessControl", context);

        // Assert - Should get Deny from SecurityPolicy
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);
        // Should have both rule-level and policyset-level advice
        Assert.Contains(decision.Advice, a => a.Id == "ContactSupport");
        Assert.Contains(decision.Advice, a => a.Id == "LogDenial");
    }

    [Fact]
    public void Evaluate_ECommerceWithNestedPolicySetsAndReferences_ShouldEvaluateCorrectly()
    {
        // Arrange
        PolicyEngine engine = new();

        // This test demonstrates a real-world eCommerce scenario with:
        // 1. Multiple resource-specific policysets (Orders, Inventory, Security)
        // 2. A main policyset that contains nested policysets
        // 3. Obligations and advice at multiple levels
        string policySetText = @"
            namespace ECommerce.AccessControl {
                import Oasis.Attributes.Subject.*
                import Oasis.Attributes.Resource.*
                import Oasis.Attributes.Action.*
                import Oasis.Attributes.Environment.*

                // Main policyset containing multiple resource policysets
                policyset MainAccessControl {
                    apply denyOverrides

                    // Orders policyset
                    policyset OrderManagement {
                        apply permitOverrides

                        policy OrderReadPolicy {
                            rule AllowRead {
                                permit
                                on permit {
                                    obligation LogOrderAccess {
                                        operation = ""read""
                                    }
                                }
                            }
                        }

                        on permit {
                            obligation AuditOrderAccess
                        }
                    }

                    // Inventory policyset
                    policyset InventoryManagement {
                        apply denyOverrides

                        policy InventoryPolicy {
                            rule AllowInventoryAccess {
                                permit
                                on permit {
                                    obligation LogInventoryAccess
                                }
                            }
                        }

                        on permit {
                            obligation AuditInventoryAccess
                        }
                    }

                    // Security policyset (with deny rules)
                    policyset SecurityPolicies {
                        apply firstApplicable

                        policy GlobalSecurity {
                            rule BlockMalicious {
                                deny
                                on deny {
                                    advice SecurityAlert {
                                        severity = ""high""
                                    }
                                }
                            }
                        }

                        on deny {
                            advice LogSecurityDenial
                        }
                    }

                    on permit {
                        obligation MainAuditLog
                    }

                    on deny {
                        advice MainDenialLog
                    }
                }
            }
        ";

        engine.LoadPolicy(policySetText);
        EvaluationContext context = new();

        // Act - With denyOverrides at main level, SecurityPolicies deny will win
        Decision decision = engine.Evaluate("MainAccessControl", context);

        // Assert - Should get Deny from SecurityPolicies
        Assert.Equal(DecisionEffect.Deny, decision.Effect);
        Assert.NotNull(decision.Advice);

        // Should have advice from:
        // 1. SecurityPolicies/GlobalSecurity rule level
        // 2. SecurityPolicies policyset level
        // 3. MainAccessControl policyset level
        Assert.Contains(decision.Advice, a => a.Id == "SecurityAlert");
        Assert.Contains(decision.Advice, a => a.Id == "LogSecurityDenial");
        Assert.Contains(decision.Advice, a => a.Id == "MainDenialLog");
    }

    #endregion
}
