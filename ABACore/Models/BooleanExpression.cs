namespace ABACore.Models;

/// <summary>
/// Base class for boolean expressions.
/// </summary>
public abstract class BooleanExpression : AlfaNode
{
}

/// <summary>
/// Represents a boolean literal.
/// </summary>
public sealed class BooleanLiteralExpression : BooleanExpression
{
    public required bool Value { get; init; }
}

/// <summary>
/// Represents a boolean attribute designator with proper namespace reference.
/// </summary>
public sealed class BooleanAttributeDesignator : BooleanExpression
{
    public required string Namespace { get; init; }
    public required string AttributeName { get; init; }
    public bool MustBePresent { get; init; }
}

/// <summary>
/// Represents a boolean function call.
/// </summary>
public sealed class BooleanFunctionCall : BooleanExpression
{
    public required string FunctionName { get; init; }
    public required List<Expression> Parameters { get; init; }
}

/// <summary>
/// Represents the all() function in boolean context.
/// </summary>
public sealed class BooleanAllExpression : BooleanExpression
{
    public required Expression InnerExpression { get; init; }
}

/// <summary>
/// Represents a logical NOT expression.
/// </summary>
public sealed class NotExpression : BooleanExpression
{
    public required BooleanExpression InnerExpression { get; init; }
}

/// <summary>
/// Represents a logical binary expression (AND, OR).
/// </summary>
public sealed class LogicalBinaryExpression : BooleanExpression
{
    public required BooleanExpression Left { get; init; }
    public required LogicalOperator Operator { get; init; }
    public required BooleanExpression Right { get; init; }
}

/// <summary>
/// Represents a comparison expression.
/// </summary>
public sealed class ComparisonExpression : BooleanExpression
{
    public required Expression Left { get; init; }
    public required ComparisonOperator Operator { get; init; }
    public required Expression Right { get; init; }
}
