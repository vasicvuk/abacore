namespace ABACore.Models;

/// <summary>
/// Represents the effect of a rule decision.
/// </summary>
public enum Effect
{
    Permit,
    Deny
}

/// <summary>
/// Represents the combining algorithms for policies and policy sets.
/// </summary>
public enum CombiningAlgorithm
{
    DenyOverrides,
    PermitOverrides,
    FirstApplicable,
    OnlyOne,
    DenyUnlessPermit,
    PermitUnlessDeny
}

/// <summary>
/// Represents attribute data types in ALFA.
/// </summary>
public enum AttributeType
{
    String,
    Boolean,
    Integer,
    Double,
    Time,
    DateTime,
    Date,
    Duration
}

/// <summary>
/// Represents binary operators for arithmetic expressions.
/// </summary>
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide
}

/// <summary>
/// Represents logical operators.
/// </summary>
public enum LogicalOperator
{
    And,        // &&
    Or,         // ||
    AndClause,  // and
    OrClause    // or
}

/// <summary>
/// Represents comparison operators.
/// </summary>
public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}
