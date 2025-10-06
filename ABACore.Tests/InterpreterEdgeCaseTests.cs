using ABACore.Compilation;
using ABACore.Models;
using ABACore.Runtime;

namespace ABACore.Tests;

public class InterpreterEdgeCaseTests
{
    private readonly InterpreterCompiler _compiler = new();

    [Fact]
    public void EvaluateBinaryExpression_Division_WithDivisionByZero_ReturnsNaN()
    {
        var policy = new Policy
        {
            Id = "div-by-zero",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new BinaryExpression
                            {
                                Operator = BinaryOperator.Divide,
                                Left = new LiteralDoubleExpression { Value = 10.0 },
                                Right = new LiteralDoubleExpression { Value = 0.0 }
                            },
                            Right = new BinaryExpression
                            {
                                Operator = BinaryOperator.Divide,
                                Left = new LiteralDoubleExpression { Value = 10.0 },
                                Right = new LiteralDoubleExpression { Value = 0.0 }
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateBinaryExpression_Subtract_CalculatesCorrectly()
    {
        var policy = new Policy
        {
            Id = "subtract-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new BinaryExpression
                            {
                                Operator = BinaryOperator.Subtract,
                                Left = new LiteralDoubleExpression { Value = 10.0 },
                                Right = new LiteralDoubleExpression { Value = 3.0 }
                            },
                            Right = new LiteralDoubleExpression { Value = 7.0 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateBinaryExpression_Multiply_CalculatesCorrectly()
    {
        var policy = new Policy
        {
            Id = "multiply-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new BinaryExpression
                            {
                                Operator = BinaryOperator.Multiply,
                                Left = new LiteralDoubleExpression { Value = 5.0 },
                                Right = new LiteralDoubleExpression { Value = 4.0 }
                            },
                            Right = new LiteralDoubleExpression { Value = 20.0 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_DateTime_ParsesCorrectly()
    {
        var policy = new Policy
        {
            Id = "datetime-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "2023-01-15T10:30:00",
                                TargetType = AttributeType.DateTime
                            },
                            Right = new ValueCoercionExpression
                            {
                                Value = "2023-01-15T10:30:00",
                                TargetType = AttributeType.DateTime
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Date_ExtractsDatePart()
    {
        var policy = new Policy
        {
            Id = "date-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "2023-01-15",
                                TargetType = AttributeType.Date
                            },
                            Right = new ValueCoercionExpression
                            {
                                Value = "2023-01-15",
                                TargetType = AttributeType.Date
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Time_ParsesTimeSpan()
    {
        var policy = new Policy
        {
            Id = "time-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "14:30:00",
                                TargetType = AttributeType.Time
                            },
                            Right = new ValueCoercionExpression
                            {
                                Value = "14:30:00",
                                TargetType = AttributeType.Time
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Duration_ParsesTimeSpan()
    {
        var policy = new Policy
        {
            Id = "duration-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "01:30:00",
                                TargetType = AttributeType.Duration
                            },
                            Right = new ValueCoercionExpression
                            {
                                Value = "01:30:00",
                                TargetType = AttributeType.Duration
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Integer_ParsesInteger()
    {
        var policy = new Policy
        {
            Id = "int-coercion-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "42",
                                TargetType = AttributeType.Integer
                            },
                            Right = new LiteralIntegerExpression { Value = 42 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Double_ParsesDouble()
    {
        var policy = new Policy
        {
            Id = "double-coercion-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "3.14",
                                TargetType = AttributeType.Double
                            },
                            Right = new LiteralDoubleExpression { Value = 3.14 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateValueCoercion_Boolean_ParsesBoolean()
    {
        var policy = new Policy
        {
            Id = "bool-coercion-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.Equal,
                            Left = new ValueCoercionExpression
                            {
                                Value = "true",
                                TargetType = AttributeType.Boolean
                            },
                            Right = new LiteralBooleanExpression { Value = true }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateBooleanAttributeDesignator_WithStringConversion_ConvertsToBoolean()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "enabled", "true");

        var policy = new Policy
        {
            Id = "bool-attr-string-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAttributeDesignator
                        {
                            Namespace = "urn:oasis:names:tc:xacml:1.0:subject",
                            AttributeName = "enabled",
                            MustBePresent = false
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateLogicalExpression_AndClause_EvaluatesCorrectly()
    {
        var policy = new Policy
        {
            Id = "and-clause-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new LogicalBinaryExpression
                        {
                            Operator = LogicalOperator.AndClause,
                            Left = new BooleanLiteralExpression { Value = true },
                            Right = new BooleanLiteralExpression { Value = true }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateLogicalExpression_OrClause_EvaluatesCorrectly()
    {
        var policy = new Policy
        {
            Id = "or-clause-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new LogicalBinaryExpression
                        {
                            Operator = LogicalOperator.OrClause,
                            Left = new BooleanLiteralExpression { Value = false },
                            Right = new BooleanLiteralExpression { Value = true }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateComparisonExpression_NotEqual_WorksCorrectly()
    {
        var policy = new Policy
        {
            Id = "not-equal-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.NotEqual,
                            Left = new LiteralIntegerExpression { Value = 5 },
                            Right = new LiteralIntegerExpression { Value = 10 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateComparisonExpression_LessThan_WorksCorrectly()
    {
        var policy = new Policy
        {
            Id = "less-than-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.LessThan,
                            Left = new LiteralIntegerExpression { Value = 5 },
                            Right = new LiteralIntegerExpression { Value = 10 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateComparisonExpression_GreaterThanOrEqual_WorksCorrectly()
    {
        var policy = new Policy
        {
            Id = "gte-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.GreaterThanOrEqual,
                            Left = new LiteralIntegerExpression { Value = 10 },
                            Right = new LiteralIntegerExpression { Value = 10 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateComparisonExpression_LessThanOrEqual_WorksCorrectly()
    {
        var policy = new Policy
        {
            Id = "lte-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.LessThanOrEqual,
                            Left = new LiteralIntegerExpression { Value = 5 },
                            Right = new LiteralIntegerExpression { Value = 10 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateBooleanAll_WithBooleanList_ReturnsTrue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "flags", new[] { true, true, true });

        var policy = new Policy
        {
            Id = "bool-all-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAllExpression
                        {
                            InnerExpression = new AttributeDesignator
                            {
                                Namespace = "urn:oasis:names:tc:xacml:1.0:subject",
                                AttributeName = "flags"
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void EvaluateBooleanAll_WithOneFalse_ReturnsFalse()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "flags", new[] { true, false, true });

        var policy = new Policy
        {
            Id = "bool-all-false-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAllExpression
                        {
                            InnerExpression = new AttributeDesignator
                            {
                                Namespace = "urn:oasis:names:tc:xacml:1.0:subject",
                                AttributeName = "flags"
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void EvaluateBooleanAll_WithNullInList_ReturnsFalse()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "flags", new object?[] { true, null, true });

        var policy = new Policy
        {
            Id = "bool-all-null-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAllExpression
                        {
                            InnerExpression = new AttributeDesignator
                            {
                                Namespace = "urn:oasis:names:tc:xacml:1.0:subject",
                                AttributeName = "flags"
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.NotApplicable, decision.Effect);
    }

    [Fact]
    public void EvaluateBooleanAll_WithSingleBooleanTrue_ReturnsTrue()
    {
        var context = new EvaluationContext();
        context.SetAttribute("subject", "flag", true);

        var policy = new Policy
        {
            Id = "bool-all-single-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanAllExpression
                        {
                            InnerExpression = new AttributeDesignator
                            {
                                Namespace = "urn:oasis:names:tc:xacml:1.0:subject",
                                AttributeName = "flag"
                            }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void CompareValues_WithNullValues_ReturnsNegative()
    {
        var policy = new Policy
        {
            Id = "null-compare-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new ComparisonExpression
                        {
                            Operator = ComparisonOperator.LessThan,
                            Left = new AttributeDesignator
                            {
                                Namespace = "test",
                                AttributeName = "missing1",
                                MustBePresent = false
                            },
                            Right = new LiteralIntegerExpression { Value = 10 }
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(new EvaluationContext());

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }

    [Fact]
    public void FunctionNameConversion_WithEmptyString_ReturnsEmptyString()
    {
        var context = new EvaluationContext();

        var policy = new Policy
        {
            Id = "func-test",
            Rules =
            [
                new Rule
                {
                    Id = "r1",
                    Effect = Effect.Permit,
                    Condition = new Condition
                    {
                        Expression = new BooleanFunctionCall
                        {
                            FunctionName = "stringEqual",
                            Parameters =
                            [
                                new LiteralStringExpression { Value = "test" },
                                new LiteralStringExpression { Value = "test" }
                            ]
                        }
                    }
                }
            ]
        };

        var compiled = _compiler.CompilePolicy(policy);
        var decision = compiled.EvaluationDelegate(context);

        Assert.Equal(DecisionEffect.Permit, decision.Effect);
    }
}
