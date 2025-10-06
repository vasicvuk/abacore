namespace ABACore.Models;

/// <summary>
/// Base class for all expressions.
/// </summary>
public abstract class Expression : AlfaNode
{
}

/// <summary>
/// Represents a literal string expression.
/// </summary>
public sealed class LiteralStringExpression : Expression
{
    public required string Value { get; init; }
}

/// <summary>
/// Represents a literal integer expression.
/// </summary>
public sealed class LiteralIntegerExpression : Expression
{
    public required int Value { get; init; }
}

/// <summary>
/// Represents a literal double expression.
/// </summary>
public sealed class LiteralDoubleExpression : Expression
{
    public required double Value { get; init; }
}

/// <summary>
/// Represents a literal boolean expression.
/// </summary>
public sealed class LiteralBooleanExpression : Expression
{
    public required bool Value { get; init; }
}

/// <summary>
/// Represents an attribute designator with proper namespace reference.
/// </summary>
public sealed class AttributeDesignator : Expression
{
    public required string Namespace { get; init; }
    public required string AttributeName { get; init; }
    public bool MustBePresent { get; init; }
}

/// <summary>
/// Represents a value coercion expression (e.g., "2024-01-01":date).
/// </summary>
public sealed class ValueCoercionExpression : Expression
{
    public required string Value { get; init; }
    public required AttributeType TargetType { get; init; }
}

/// <summary>
/// Represents the all() function for bag evaluation.
/// </summary>
public sealed class AllExpression : Expression
{
    public required Expression InnerExpression { get; init; }
}

/// <summary>
/// Represents a function call.
/// </summary>
public sealed class FunctionCall : Expression
{
    public required string FunctionName { get; init; }
    public required List<Expression> Parameters { get; init; }
}

/// <summary>
/// Represents a binary arithmetic expression.
/// </summary>
public sealed class BinaryExpression : Expression
{
    public required Expression Left { get; init; }
    public required BinaryOperator Operator { get; init; }
    public required Expression Right { get; init; }
}
