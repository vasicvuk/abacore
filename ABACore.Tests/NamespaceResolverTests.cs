using ABACore.Compilation;
using ABACore.Models;
using ABACore.Parser;
using ABACore.Runtime;
using ABACore.Visitor;
using Antlr4.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for namespace resolution and policy validation.
/// </summary>
public class NamespaceResolverTests
{
    #region Helper Methods

    private static Namespace ParseNamespace(string namespaceText)
    {
        AntlrInputStream inputStream = new(namespaceText);
        AlfaLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        AlfaParser parser = new(tokenStream);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());

        AlfaParser.NamespaceContext namespaceContext = parser.@namespace();
        AlfaAstBuilder visitor = new();
        AlfaNode? node = visitor.Visit(namespaceContext);

        return node is not Namespace ns ? throw new InvalidOperationException("Expected Namespace node") : ns;
    }

    private class ThrowingErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new InvalidOperationException($"Parse error at {line}:{charPositionInLine}: {msg}");
        }
    }

    #endregion

    #region NamespaceResolver Tests

    [Fact]
    public void NamespaceResolver_Initialize_ShouldContainDefaultNamespaces()
    {
        // Arrange & Act
        var resolver = new NamespaceResolver();

        // Assert
        Assert.NotNull(resolver.GetNamespace("Oasis.Attributes.Subject"));
        Assert.NotNull(resolver.GetNamespace("Oasis.Attributes.Resource"));
        Assert.NotNull(resolver.GetNamespace("Oasis.Attributes.Action"));
        Assert.NotNull(resolver.GetNamespace("Oasis.Attributes.Environment"));
        Assert.NotNull(resolver.GetNamespace("Oasis.Functions.String"));
        Assert.NotNull(resolver.GetNamespace("Oasis.Functions.Numeric"));
    }

    [Fact]
    public void ResolveAttribute_WithValidSubjectAttribute_ShouldReturnAttributeInfo()
    {
        // Arrange
        var resolver = new NamespaceResolver();

        // Act
        AttributeInfo? result = resolver.ResolveAttribute("TestNamespace", "Subject.Role");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Role", result.Name);
        Assert.Equal("Oasis.Attributes.Subject", result.FullNamespace);
        Assert.Equal(AttributeType.String, result.Type);
    }

    [Fact]
    public void ResolveAttribute_WithValidResourceAttribute_ShouldReturnAttributeInfo()
    {
        // Arrange
        var resolver = new NamespaceResolver();

        // Act
        AttributeInfo? result = resolver.ResolveAttribute("TestNamespace", "Resource.Type");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Type", result.Name);
        Assert.Equal("Oasis.Attributes.Resource", result.FullNamespace);
        Assert.Equal(AttributeType.String, result.Type);
    }

    [Fact]
    public void ResolveAttribute_WithInvalidAttribute_ShouldReturnNull()
    {
        // Arrange
        var resolver = new NamespaceResolver();

        // Act
        AttributeInfo? result = resolver.ResolveAttribute("TestNamespace", "Invalid.Attribute");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFunction_WithValidStringFunction_ShouldReturnFunctionInfo()
    {
        // Arrange
        var resolver = new NamespaceResolver();

        // Act
        FunctionInfo? result = resolver.ResolveFunction("TestNamespace", "String.Equal");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Equal", result.Name);
        Assert.Equal("Oasis.Functions.String", result.FullNamespace);
        Assert.NotEmpty(result.Signatures);
    }

    [Fact]
    public void ResolveFunction_WithInvalidFunction_ShouldReturnNull()
    {
        // Arrange
        var resolver = new NamespaceResolver();

        // Act
        FunctionInfo? result = resolver.ResolveFunction("TestNamespace", "Invalid.Function");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region PolicyValidator Tests

    [Fact]
    public void PolicyValidator_WithValidImports_ShouldPassValidation()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Test.Policy {
                import Oasis.Attributes.Subject.*
                import Oasis.Attributes.Resource.*

                policy TestPolicy {
                    target clause Subject.Role == ""admin"" and Resource.Type == ""system""

                    rule AllowAdmin {
                        permit
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PolicyValidator_WithUnimportedAttribute_ShouldFailValidation()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Test.Policy {
                // No imports

                policy TestPolicy {
                    target clause Subject.Role == ""admin""

                    rule AllowAdmin {
                        permit
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("Subject.Role") && e.Message.Contains("not imported"));
    }

    [Fact]
    public void PolicyValidator_WithUnimportedFunction_ShouldFailValidation()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Test.Policy {
                import Oasis.Attributes.Subject.*
                // No function imports

                policy TestPolicy {
                    target clause Subject.Role == ""admin""

                    rule AllowAdmin {
                        permit
                        condition String.Equal(Subject.Role, ""admin"")
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("String.Equal") && e.Message.Contains("not imported"));
    }

    [Fact]
    public void PolicyValidator_WithMixedValidAndInvalidReferences_ShouldReportAllErrors()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Test.Policy {
                import Oasis.Attributes.Subject.*
                // Missing imports for Resource and String functions

                policy TestPolicy {
                    target clause Subject.Role == ""admin"" and Resource.Type == ""system""

                    rule AllowAdmin {
                        permit
                        condition String.Equal(Subject.Role, ""admin"")
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
        Assert.Contains(result.Errors, e => e.Message.Contains("Resource.Type"));
        Assert.Contains(result.Errors, e => e.Message.Contains("String.Equal"));
    }

    [Fact]
    public void PolicyValidator_WithFullyQualifiedAttributes_ShouldPassWithoutImports()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Test.Policy {
                // No imports needed for fully qualified attributes

                policy TestPolicy {
                    target clause Oasis.Attributes.Subject.Role == ""admin"" and Oasis.Attributes.Resource.Type == ""system""

                    rule AllowAdmin {
                        permit
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PolicyValidator_WithCustomAttributeCategory_ShouldRegisterCategory()
    {
        // Arrange
        var resolver = new NamespaceResolver();
        resolver.CategoryRegistry.RegisterCategory(new CategoryInfo
        {
            Name = "customcategory",
            Uri = "urn:example:attribute-category:custom",
            Description = "Custom attribute category for testing"
        });

        var validator = new PolicyValidator(resolver);

        string namespaceText = @"
            namespace Custom.Policy {
                attribute AccountTier {
                    category = customcategory
                    id = ""urn:example:attr:account-tier""
                    type = string
                }

                policy Default {
                    rule AllowAll {
                        permit
                    }
                }
            }
        ";

        Namespace ns = ParseNamespace(namespaceText);

        // Act
        ValidationResult result = validator.ValidateNamespace(ns);

        // Assert
        Assert.True(result.IsValid);
        AttributeInfo? attribute = resolver.ResolveAttribute("Custom.Policy", "AccountTier");
        Assert.NotNull(attribute);
        Assert.Equal("customcategory", attribute!.Category);
        Assert.Equal("urn:example:attribute-category:custom", resolver.CategoryRegistry.GetCategoryUri("customcategory"));
    }

    #endregion
}
