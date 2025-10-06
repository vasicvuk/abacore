using ABACore.Exceptions;

namespace ABACore.Tests;

public class ExceptionTests
{
    [Fact]
    public void AlfaException_WithMessage_SetsMessage()
    {
        var ex = new AlfaParseException("Test message");
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void AlfaException_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new AlfaParseException("Test message", inner);

        Assert.Equal("Test message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AlfaParseException_WithLineAndColumn_SetsPosition()
    {
        var ex = new AlfaParseException("Test", 10, 5);

        Assert.Equal(10, ex.Line);
        Assert.Equal(5, ex.Column);
    }

    [Fact]
    public void AlfaParseException_WithLineColumnAndSymbol_SetsAll()
    {
        var ex = new AlfaParseException("Test", 10, 5, "symbol");

        Assert.Equal(10, ex.Line);
        Assert.Equal(5, ex.Column);
        Assert.Equal("symbol", ex.OffendingSymbol);
    }

    [Fact]
    public void AlfaParseException_WithoutPosition_HasNullPosition()
    {
        var ex = new AlfaParseException("Test");

        Assert.Null(ex.Line);
        Assert.Null(ex.Column);
        Assert.Null(ex.OffendingSymbol);
    }

    [Fact]
    public void AlfaCompilationException_WithMessage_SetsMessage()
    {
        var ex = new AlfaCompilationException("Compilation failed");
        Assert.Equal("Compilation failed", ex.Message);
    }

    [Fact]
    public void AlfaCompilationException_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new AlfaCompilationException("Compilation failed", inner);

        Assert.Equal("Compilation failed", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AlfaEvaluationException_WithMessageAndPolicyId_SetsBoth()
    {
        var ex = new AlfaEvaluationException("Evaluation failed", "policy123");

        Assert.Contains("Evaluation failed", ex.Message);
        Assert.Contains("policy123", ex.Message);
        Assert.Equal("policy123", ex.PolicyId);
    }

    [Fact]
    public void AlfaEvaluationException_WithMessagePolicyIdAndInnerException_SetsAll()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new AlfaEvaluationException("Evaluation failed", "policy123", inner);

        Assert.Contains("Evaluation failed", ex.Message);
        Assert.Contains("policy123", ex.Message);
        Assert.Equal("policy123", ex.PolicyId);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void PolicyNotFoundException_WithPolicyId_SetsMessage()
    {
        var ex = new PolicyNotFoundException("policy123");

        Assert.Contains("policy123", ex.Message);
        Assert.Equal("policy123", ex.PolicyId);
    }

    [Fact]
    public void PolicyNotFoundException_WithCustomMessage_SetsMessage()
    {
        var ex = new PolicyNotFoundException("policy123", "Custom error message");

        Assert.Contains("Custom error message", ex.Message);
        Assert.Equal("policy123", ex.PolicyId);
    }

    [Fact]
    public void AlfaCompilationException_DefaultConstructor_CreatesException()
    {
        var ex = new AlfaCompilationException();
        Assert.NotNull(ex);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void AlfaEvaluationException_DefaultConstructor_CreatesException()
    {
        var ex = new AlfaEvaluationException();
        Assert.NotNull(ex);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void AlfaEvaluationException_WithMessage_SetsMessage()
    {
        var ex = new AlfaEvaluationException("Evaluation error");
        Assert.Equal("Evaluation error", ex.Message);
    }

    [Fact]
    public void AlfaEvaluationException_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new AlfaEvaluationException("Evaluation error", inner);

        Assert.Equal("Evaluation error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AlfaException_DefaultConstructor_CreatesException()
    {
        var ex = new AlfaException();
        Assert.NotNull(ex);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void AlfaException_WithMessageOnly_SetsMessage()
    {
        var ex = new AlfaException("ALFA error");
        Assert.Equal("ALFA error", ex.Message);
    }

    [Fact]
    public void AlfaException_WithMessageAndInner_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new AlfaException("ALFA error", inner);

        Assert.Equal("ALFA error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AlfaParseException_DefaultConstructor_CreatesException()
    {
        var ex = new AlfaParseException();
        Assert.NotNull(ex);
        Assert.NotNull(ex.Message);
    }
}
