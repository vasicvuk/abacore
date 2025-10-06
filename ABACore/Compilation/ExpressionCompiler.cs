using System.Text;
using ABACore.Models;

namespace ABACore.Compilation;

/// <summary>
/// Compiles ALFA expression AST nodes to C# code strings.
/// This compiler generates executable C# code from expression trees for runtime evaluation.
/// </summary>
public static class ExpressionCompiler
{
    /// <summary>
    /// Compiles an Expression AST node to C# code.
    /// </summary>
    /// <param name="expression">The expression to compile.</param>
    /// <returns>A C# code string representing the expression.</returns>
    public static string CompileExpression(Expression expression)
    {
        return expression switch
        {
            LiteralStringExpression lit => CompileLiteralString(lit),
            LiteralIntegerExpression lit => CompileLiteralInteger(lit),
            LiteralDoubleExpression lit => CompileLiteralDouble(lit),
            LiteralBooleanExpression lit => CompileLiteralBoolean(lit),
            AttributeDesignator attr => CompileAttributeDesignator(attr),
            ValueCoercionExpression coerce => CompileValueCoercion(coerce),
            AllExpression all => CompileAll(all),
            FunctionCall func => CompileFunctionCall(func),
            BinaryExpression binary => CompileBinaryExpression(binary),
            _ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported")
        };
    }

    /// <summary>
    /// Compiles a BooleanExpression AST node to C# code.
    /// </summary>
    /// <param name="expression">The boolean expression to compile.</param>
    /// <returns>A C# code string representing the boolean expression.</returns>
    public static string CompileBooleanExpression(BooleanExpression expression)
    {
        return expression switch
        {
            BooleanLiteralExpression lit => lit.Value.ToString().ToLowerInvariant(),
            BooleanAttributeDesignator attr => CompileBooleanAttributeDesignator(attr),
            BooleanFunctionCall func => CompileBooleanFunctionCall(func),
            BooleanAllExpression all => CompileBooleanAll(all),
            NotExpression not => CompileNotExpression(not),
            LogicalBinaryExpression logical => CompileLogicalBinaryExpression(logical),
            ComparisonExpression comparison => CompileComparisonExpression(comparison),
            _ => throw new NotSupportedException($"Boolean expression type {expression.GetType().Name} is not supported")
        };
    }

    /// <summary>
    /// Compiles a literal string expression.
    /// </summary>
    private static string CompileLiteralString(LiteralStringExpression lit)
    {
        string escaped = lit.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    /// <summary>
    /// Compiles a literal integer expression.
    /// </summary>
    private static string CompileLiteralInteger(LiteralIntegerExpression lit)
    {
        return lit.Value.ToString();
    }

    /// <summary>
    /// Compiles a literal double expression.
    /// </summary>
    private static string CompileLiteralDouble(LiteralDoubleExpression lit)
    {
        return lit.Value.ToString("G17") + "d";
    }

    /// <summary>
    /// Compiles a literal boolean expression.
    /// </summary>
    private static string CompileLiteralBoolean(LiteralBooleanExpression lit)
    {
        return lit.Value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Compiles an attribute designator to C# code that retrieves the attribute from the context.
    /// </summary>
    private static string CompileAttributeDesignator(AttributeDesignator attr)
    {
        // The category should be determined from the attribute definition, not namespace
        // For now, we'll use a simple approach and let the namespace resolver handle category mapping
        string attributeName = attr.AttributeName;

        // Try to resolve the attribute to get its category
        // This will be handled by the namespace resolver at runtime
        return attr.MustBePresent
            ? $"context.GetAttributeByNamespace(\"{attr.Namespace}\", \"{attributeName}\") ?? throw new InvalidOperationException(\"Required attribute {attr.Namespace}.{attr.AttributeName} is missing\")"
            : $"context.GetAttributeByNamespace(\"{attr.Namespace}\", \"{attributeName}\")";
    }

    /// <summary>
    /// Compiles a boolean attribute designator.
    /// </summary>
    private static string CompileBooleanAttributeDesignator(BooleanAttributeDesignator attr)
    {
        // The category should be determined from the attribute definition, not namespace
        string attributeName = attr.AttributeName;

        return attr.MustBePresent
            ? $"(bool)(context.GetAttributeByNamespace(\"{attr.Namespace}\", \"{attributeName}\") ?? throw new InvalidOperationException(\"Required attribute {attr.Namespace}.{attr.AttributeName} is missing\"))"
            : $"(context.GetAttributeByNamespace<bool>(\"{attr.Namespace}\", \"{attributeName}\") ?? false)";
    }

    /// <summary>
    /// Compiles a value coercion expression.
    /// </summary>
    private static string CompileValueCoercion(ValueCoercionExpression coerce)
    {
        string escaped = coerce.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return coerce.TargetType switch
        {
            AttributeType.DateTime => $"DateTime.Parse(\"{escaped}\")",
            AttributeType.Date => $"DateTime.Parse(\"{escaped}\").Date",
            AttributeType.Time => $"TimeSpan.Parse(\"{escaped}\")",
            AttributeType.Duration => $"TimeSpan.Parse(\"{escaped}\")",
            _ => $"\"{escaped}\""
        };
    }

    /// <summary>
    /// Compiles an all() expression.
    /// </summary>
    private static string CompileAll(AllExpression all)
    {
        string innerExpr = CompileExpression(all.InnerExpression);
        // For now, we'll just evaluate the inner expression
        // In a full implementation, this would need to handle bag evaluation
        return $"({innerExpr})";
    }

    /// <summary>
    /// Compiles a boolean all() expression.
    /// </summary>
    private static string CompileBooleanAll(BooleanAllExpression all)
    {
        string innerExpr = CompileExpression(all.InnerExpression);
        return $"({innerExpr})";
    }

    /// <summary>
    /// Compiles a function call.
    /// </summary>
    private static string CompileFunctionCall(FunctionCall func)
    {
        StringBuilder sb = new();

        // Map ALFA function names to C# implementations
        string csFunction = MapFunctionName(func.FunctionName);
        sb.Append(csFunction);
        sb.Append('(');

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(CompileExpression(func.Parameters[i]));
        }

        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Compiles a boolean function call.
    /// </summary>
    private static string CompileBooleanFunctionCall(BooleanFunctionCall func)
    {
        StringBuilder sb = new();

        string csFunction = MapFunctionName(func.FunctionName);
        sb.Append(csFunction);
        sb.Append('(');

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(CompileExpression(func.Parameters[i]));
        }

        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Compiles a binary arithmetic expression.
    /// </summary>
    private static string CompileBinaryExpression(BinaryExpression binary)
    {
        string left = CompileExpression(binary.Left);
        string right = CompileExpression(binary.Right);
        string op = binary.Operator switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            _ => throw new NotSupportedException($"Binary operator {binary.Operator} is not supported")
        };

        // Use PerformArithmetic helper to handle object types
        return $"PerformArithmetic({left}, {right}, \"{op}\")";
    }

    /// <summary>
    /// Compiles a NOT expression.
    /// </summary>
    private static string CompileNotExpression(NotExpression not)
    {
        string inner = CompileBooleanExpression(not.InnerExpression);
        return $"!({inner})";
    }

    /// <summary>
    /// Compiles a logical binary expression (AND, OR).
    /// </summary>
    private static string CompileLogicalBinaryExpression(LogicalBinaryExpression logical)
    {
        string left = CompileBooleanExpression(logical.Left);
        string right = CompileBooleanExpression(logical.Right);
        string op = logical.Operator switch
        {
            LogicalOperator.And => "&&",
            LogicalOperator.Or => "||",
            LogicalOperator.AndClause => "&&",
            LogicalOperator.OrClause => "||",
            _ => throw new NotSupportedException($"Logical operator {logical.Operator} is not supported")
        };

        return $"({left} {op} {right})";
    }

    /// <summary>
    /// Compiles a comparison expression.
    /// </summary>
    private static string CompileComparisonExpression(ComparisonExpression comparison)
    {
        string left = CompileExpression(comparison.Left);
        string right = CompileExpression(comparison.Right);
        string op = comparison.Operator switch
        {
            ComparisonOperator.Equal => "==",
            ComparisonOperator.NotEqual => "!=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Comparison operator {comparison.Operator} is not supported")
        };

        // For equality checks, use object.Equals
        if (comparison.Operator == ComparisonOperator.Equal)
        {
            return $"object.Equals({left}, {right})";
        }
        else if (comparison.Operator == ComparisonOperator.NotEqual)
        {
            return $"!object.Equals({left}, {right})";
        }

        // For comparison operators, use CompareValues helper
        return $"CompareValues({left}, {right}, \"{op}\")";
    }

    /// <summary>
    /// Maps ALFA function names to C# function implementations.
    /// </summary>
    private static string MapFunctionName(string alfaFunctionName)
    {
        // Map common ALFA functions to C# equivalents
        return alfaFunctionName switch
        {
            "stringEqual" => "ABACore.Runtime.Functions.StringEqual",
            "stringEqualIgnoreCase" => "ABACore.Runtime.Functions.StringEqualIgnoreCase",
            "stringStartsWith" => "ABACore.Runtime.Functions.StringStartsWith",
            "stringEndsWith" => "ABACore.Runtime.Functions.StringEndsWith",
            "stringContains" => "ABACore.Runtime.Functions.StringContains",
            "stringRegexMatch" => "ABACore.Runtime.Functions.StringRegexMatch",
            "stringNormalizeSpace" => "ABACore.Runtime.Functions.StringNormalizeSpace",
            "stringNormalizeToLowerCase" => "ABACore.Runtime.Functions.StringNormalizeToLowerCase",
            "integerAdd" => "ABACore.Runtime.Functions.IntegerAdd",
            "integerSubtract" => "ABACore.Runtime.Functions.IntegerSubtract",
            "integerMultiply" => "ABACore.Runtime.Functions.IntegerMultiply",
            "integerDivide" => "ABACore.Runtime.Functions.IntegerDivide",
            "integerMod" => "ABACore.Runtime.Functions.IntegerMod",
            "integerAbs" => "ABACore.Runtime.Functions.IntegerAbs",
            "doubleAdd" => "ABACore.Runtime.Functions.DoubleAdd",
            "doubleSubtract" => "ABACore.Runtime.Functions.DoubleSubtract",
            "doubleMultiply" => "ABACore.Runtime.Functions.DoubleMultiply",
            "doubleDivide" => "ABACore.Runtime.Functions.DoubleDivide",
            "doubleAbs" => "ABACore.Runtime.Functions.DoubleAbs",
            "dateTimeAddDayTimeDuration" => "ABACore.Runtime.Functions.DateTimeAddDayTimeDuration",
            "dateTimeSubtractDayTimeDuration" => "ABACore.Runtime.Functions.DateTimeSubtractDayTimeDuration",
            "dateTimeGreaterThan" => "ABACore.Runtime.Functions.DateTimeGreaterThan",
            "dateTimeLessThan" => "ABACore.Runtime.Functions.DateTimeLessThan",
            "dateTimeEqual" => "ABACore.Runtime.Functions.DateTimeEqual",
            "dateGreaterThan" => "ABACore.Runtime.Functions.DateGreaterThan",
            "dateLessThan" => "ABACore.Runtime.Functions.DateLessThan",
            "dateEqual" => "ABACore.Runtime.Functions.DateEqual",
            "timeGreaterThan" => "ABACore.Runtime.Functions.TimeGreaterThan",
            "timeLessThan" => "ABACore.Runtime.Functions.TimeLessThan",
            "timeEqual" => "ABACore.Runtime.Functions.TimeEqual",
            "anyOf" => "ABACore.Runtime.Functions.AnyOf",
            "allOf" => "ABACore.Runtime.Functions.AllOf",
            "anyOfAny" => "ABACore.Runtime.Functions.AnyOfAny",
            "allOfAny" => "ABACore.Runtime.Functions.AllOfAny",
            "anyOfAll" => "ABACore.Runtime.Functions.AnyOfAll",
            "allOfAll" => "ABACore.Runtime.Functions.AllOfAll",
            "not" => "ABACore.Runtime.Functions.Not",
            "and" => "ABACore.Runtime.Functions.And",
            "or" => "ABACore.Runtime.Functions.Or",
            _ => $"ABACore.Runtime.Functions.{alfaFunctionName}"
        };
    }

}
