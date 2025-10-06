namespace ABACore.Models;

/// <summary>
/// Represents a function declaration.
/// </summary>
public sealed class FunctionDeclaration : Statement
{
    public required string Name { get; init; }
    public required string Id { get; init; }
    public required List<FunctionSignature> Signatures { get; init; }
}

/// <summary>
/// Represents a function signature.
/// </summary>
public sealed class FunctionSignature
{
    public required List<FunctionArgument> Inputs { get; init; }
    public required FunctionArgument Output { get; init; }
    public bool HasVariableParams { get; init; }
}

/// <summary>
/// Represents a function argument type.
/// </summary>
public abstract class FunctionArgument
{
}

/// <summary>
/// Represents a simple type argument.
/// </summary>
public sealed class SimpleTypeArgument : FunctionArgument
{
    public required AttributeType Type { get; init; }
}

/// <summary>
/// Represents a bag type argument.
/// </summary>
public sealed class BagTypeArgument : FunctionArgument
{
    public required AttributeType ElementType { get; init; }
}
