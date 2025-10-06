using ABACore.Compilation;
using ABACore.Models;

namespace ABACore.Tests;

public class ExpressionCompilerTests
{
    #region Literal Expression Tests

    [Fact]
    public void CompileLiteralString_ReturnsQuotedString()
    {
        var expr = new LiteralStringExpression { Value = "test" };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Equal("\"test\"", result);
    }

    [Fact]
    public void CompileLiteralString_EscapesQuotes()
    {
        var expr = new LiteralStringExpression { Value = "test\"value" };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Equal("\"test\\\"value\"", result);
    }

    [Fact]
    public void CompileLiteralString_EscapesBackslashes()
    {
        var expr = new LiteralStringExpression { Value = "test\\value" };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Equal("\"test\\\\value\"", result);
    }

    [Fact]
    public void CompileLiteralInteger_ReturnsIntegerString()
    {
        var expr = new LiteralIntegerExpression { Value = 42 };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Equal("42", result);
    }

    [Fact]
    public void CompileLiteralDouble_ReturnsDoubleString()
    {
        var expr = new LiteralDoubleExpression { Value = 3.14 };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("3.14", result);
        Assert.EndsWith("d", result);
    }

    [Fact]
    public void CompileLiteralBoolean_True_ReturnsLowerCaseTrue()
    {
        var expr = new BooleanLiteralExpression { Value = true };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Equal("true", result);
    }

    [Fact]
    public void CompileLiteralBoolean_False_ReturnsLowerCaseFalse()
    {
        var expr = new BooleanLiteralExpression { Value = false };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Equal("false", result);
    }

    #endregion

    #region Attribute Designator Tests

    [Fact]
    public void CompileAttributeDesignator_WithMustBePresent_IncludesExceptionThrow()
    {
        var expr = new AttributeDesignator
        {
            Namespace = "test.namespace",
            AttributeName = "testAttr",
            MustBePresent = true
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("GetAttributeByNamespace", result);
        Assert.Contains("test.namespace", result);
        Assert.Contains("testAttr", result);
        Assert.Contains("throw new InvalidOperationException", result);
    }

    [Fact]
    public void CompileAttributeDesignator_WithoutMustBePresent_NoException()
    {
        var expr = new AttributeDesignator
        {
            Namespace = "test.namespace",
            AttributeName = "testAttr",
            MustBePresent = false
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("GetAttributeByNamespace", result);
        Assert.DoesNotContain("throw", result);
    }

    [Fact]
    public void CompileBooleanAttributeDesignator_WithMustBePresent_CastsToBool()
    {
        var expr = new BooleanAttributeDesignator
        {
            Namespace = "test.namespace",
            AttributeName = "enabled",
            MustBePresent = true
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("(bool)", result);
        Assert.Contains("throw new InvalidOperationException", result);
    }

    [Fact]
    public void CompileBooleanAttributeDesignator_WithoutMustBePresent_UsesFalseAsDefault()
    {
        var expr = new BooleanAttributeDesignator
        {
            Namespace = "test.namespace",
            AttributeName = "enabled",
            MustBePresent = false
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("GetAttributeByNamespace<bool>", result);
        Assert.Contains("false", result);
    }

    #endregion

    #region Value Coercion Tests

    [Fact]
    public void CompileValueCoercion_DateTime_UsesDateTimeParse()
    {
        var expr = new ValueCoercionExpression
        {
            Value = "2023-01-15",
            TargetType = AttributeType.DateTime
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("DateTime.Parse", result);
        Assert.Contains("2023-01-15", result);
    }

    [Fact]
    public void CompileValueCoercion_Date_UsesDateTimeParseThenDate()
    {
        var expr = new ValueCoercionExpression
        {
            Value = "2023-01-15",
            TargetType = AttributeType.Date
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("DateTime.Parse", result);
        Assert.Contains(".Date", result);
    }

    [Fact]
    public void CompileValueCoercion_Time_UsesTimeSpanParse()
    {
        var expr = new ValueCoercionExpression
        {
            Value = "14:30:00",
            TargetType = AttributeType.Time
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("TimeSpan.Parse", result);
        Assert.Contains("14:30:00", result);
    }

    [Fact]
    public void CompileValueCoercion_Duration_UsesTimeSpanParse()
    {
        var expr = new ValueCoercionExpression
        {
            Value = "PT1H30M",
            TargetType = AttributeType.Duration
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("TimeSpan.Parse", result);
    }

    [Fact]
    public void CompileValueCoercion_String_ReturnsQuotedString()
    {
        var expr = new ValueCoercionExpression
        {
            Value = "test",
            TargetType = AttributeType.String
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Equal("\"test\"", result);
    }

    #endregion

    #region Binary Expression Tests

    [Fact]
    public void CompileBinaryExpression_Add_GeneratesAdditionCode()
    {
        var expr = new BinaryExpression
        {
            Operator = BinaryOperator.Add,
            Left = new LiteralIntegerExpression { Value = 5 },
            Right = new LiteralIntegerExpression { Value = 3 }
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("PerformArithmetic", result);
        Assert.Contains("5", result);
        Assert.Contains("3", result);
        Assert.Contains("+", result);
    }

    [Fact]
    public void CompileBinaryExpression_Subtract_GeneratesSubtractionCode()
    {
        var expr = new BinaryExpression
        {
            Operator = BinaryOperator.Subtract,
            Left = new LiteralIntegerExpression { Value = 10 },
            Right = new LiteralIntegerExpression { Value = 4 }
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("PerformArithmetic", result);
        Assert.Contains("-", result);
    }

    [Fact]
    public void CompileBinaryExpression_Multiply_GeneratesMultiplicationCode()
    {
        var expr = new BinaryExpression
        {
            Operator = BinaryOperator.Multiply,
            Left = new LiteralIntegerExpression { Value = 6 },
            Right = new LiteralIntegerExpression { Value = 7 }
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("PerformArithmetic", result);
        Assert.Contains("*", result);
    }

    [Fact]
    public void CompileBinaryExpression_Divide_GeneratesDivisionCode()
    {
        var expr = new BinaryExpression
        {
            Operator = BinaryOperator.Divide,
            Left = new LiteralIntegerExpression { Value = 20 },
            Right = new LiteralIntegerExpression { Value = 4 }
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("PerformArithmetic", result);
        Assert.Contains("/", result);
    }

    #endregion

    #region Boolean Expression Tests

    [Fact]
    public void CompileNotExpression_NegatesInnerExpression()
    {
        var expr = new NotExpression
        {
            InnerExpression = new BooleanLiteralExpression { Value = true }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("!", result);
        Assert.Contains("true", result);
    }

    [Fact]
    public void CompileLogicalBinaryExpression_And_GeneratesAndOperator()
    {
        var expr = new LogicalBinaryExpression
        {
            Operator = LogicalOperator.And,
            Left = new BooleanLiteralExpression { Value = true },
            Right = new BooleanLiteralExpression { Value = false }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("&&", result);
        Assert.Contains("true", result);
        Assert.Contains("false", result);
    }

    [Fact]
    public void CompileLogicalBinaryExpression_Or_GeneratesOrOperator()
    {
        var expr = new LogicalBinaryExpression
        {
            Operator = LogicalOperator.Or,
            Left = new BooleanLiteralExpression { Value = true },
            Right = new BooleanLiteralExpression { Value = false }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("||", result);
    }

    [Fact]
    public void CompileLogicalBinaryExpression_AndClause_GeneratesAndOperator()
    {
        var expr = new LogicalBinaryExpression
        {
            Operator = LogicalOperator.AndClause,
            Left = new BooleanLiteralExpression { Value = true },
            Right = new BooleanLiteralExpression { Value = true }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("&&", result);
    }

    [Fact]
    public void CompileLogicalBinaryExpression_OrClause_GeneratesOrOperator()
    {
        var expr = new LogicalBinaryExpression
        {
            Operator = LogicalOperator.OrClause,
            Left = new BooleanLiteralExpression { Value = false },
            Right = new BooleanLiteralExpression { Value = true }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("||", result);
    }

    #endregion

    #region Comparison Expression Tests

    [Fact]
    public void CompileComparisonExpression_Equal_UsesObjectEquals()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.Equal,
            Left = new LiteralIntegerExpression { Value = 5 },
            Right = new LiteralIntegerExpression { Value = 5 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("object.Equals", result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void CompileComparisonExpression_NotEqual_UsesNegatedObjectEquals()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.NotEqual,
            Left = new LiteralIntegerExpression { Value = 5 },
            Right = new LiteralIntegerExpression { Value = 3 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("!object.Equals", result);
    }

    [Fact]
    public void CompileComparisonExpression_GreaterThan_UsesCompareValues()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.GreaterThan,
            Left = new LiteralIntegerExpression { Value = 10 },
            Right = new LiteralIntegerExpression { Value = 5 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("CompareValues", result);
        Assert.Contains(">", result);
    }

    [Fact]
    public void CompileComparisonExpression_LessThan_UsesCompareValues()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.LessThan,
            Left = new LiteralIntegerExpression { Value = 3 },
            Right = new LiteralIntegerExpression { Value = 7 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("CompareValues", result);
        Assert.Contains("<", result);
    }

    [Fact]
    public void CompileComparisonExpression_GreaterThanOrEqual_UsesCompareValues()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.GreaterThanOrEqual,
            Left = new LiteralIntegerExpression { Value = 10 },
            Right = new LiteralIntegerExpression { Value = 10 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("CompareValues", result);
        Assert.Contains(">=", result);
    }

    [Fact]
    public void CompileComparisonExpression_LessThanOrEqual_UsesCompareValues()
    {
        var expr = new ComparisonExpression
        {
            Operator = ComparisonOperator.LessThanOrEqual,
            Left = new LiteralIntegerExpression { Value = 5 },
            Right = new LiteralIntegerExpression { Value = 5 }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("CompareValues", result);
        Assert.Contains("<=", result);
    }

    #endregion

    #region Function Call Tests

    [Fact]
    public void CompileFunctionCall_StringEqual_MapsToCorrectFunction()
    {
        var expr = new FunctionCall
        {
            FunctionName = "stringEqual",
            Parameters =
            [
                new LiteralStringExpression { Value = "test" },
                new LiteralStringExpression { Value = "test" }
            ]
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("ABACore.Runtime.Functions.StringEqual", result);
        Assert.Contains("\"test\"", result);
    }

    [Fact]
    public void CompileFunctionCall_IntegerAdd_MapsToCorrectFunction()
    {
        var expr = new FunctionCall
        {
            FunctionName = "integerAdd",
            Parameters =
            [
                new LiteralIntegerExpression { Value = 5 },
                new LiteralIntegerExpression { Value = 3 }
            ]
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("ABACore.Runtime.Functions.IntegerAdd", result);
        Assert.Contains("5", result);
        Assert.Contains("3", result);
    }

    [Fact]
    public void CompileBooleanFunctionCall_AnyOf_MapsToCorrectFunction()
    {
        var expr = new BooleanFunctionCall
        {
            FunctionName = "anyOf",
            Parameters =
            [
                new LiteralStringExpression { Value = "admin" },
                new LiteralStringExpression { Value = "user" }
            ]
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("ABACore.Runtime.Functions.AnyOf", result);
    }

    #endregion

    #region All Expression Tests

    [Fact]
    public void CompileAllExpression_WrapsInnerExpression()
    {
        var expr = new AllExpression
        {
            InnerExpression = new LiteralStringExpression { Value = "test" }
        };
        var result = ExpressionCompiler.CompileExpression(expr);
        Assert.Contains("test", result);
    }

    [Fact]
    public void CompileBooleanAllExpression_WrapsInnerExpression()
    {
        var expr = new BooleanAllExpression
        {
            InnerExpression = new LiteralStringExpression { Value = "test" }
        };
        var result = ExpressionCompiler.CompileBooleanExpression(expr);
        Assert.Contains("test", result);
    }

    #endregion

    #region Unsupported Expression Tests

    [Fact]
    public void CompileExpression_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Create a custom expression type that's not supported
        var expr = new UnsupportedExpression();
        Assert.Throws<NotSupportedException>(() => ExpressionCompiler.CompileExpression(expr));
    }

    [Fact]
    public void CompileBooleanExpression_WithUnsupportedType_ThrowsNotSupportedException()
    {
        var expr = new UnsupportedBooleanExpression();
        Assert.Throws<NotSupportedException>(() => ExpressionCompiler.CompileBooleanExpression(expr));
    }

    // Helper classes for testing unsupported types
    private class UnsupportedExpression : Expression { }
    private class UnsupportedBooleanExpression : BooleanExpression { }

    #endregion
}
