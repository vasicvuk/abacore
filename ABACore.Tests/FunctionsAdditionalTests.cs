using ABACore.Runtime;

namespace ABACore.Tests;

public class FunctionsAdditionalTests
{
    [Fact]
    public void DateTimeGreaterThan_WithGreaterDateTime_ReturnsTrue()
    {
        var dt1 = new DateTime(2024, 1, 2);
        var dt2 = new DateTime(2024, 1, 1);
        Assert.True(Functions.DateTimeGreaterThan(dt1, dt2));
    }

    [Fact]
    public void DateTimeLessThan_WithLesserDateTime_ReturnsTrue()
    {
        var dt1 = new DateTime(2024, 1, 1);
        var dt2 = new DateTime(2024, 1, 2);
        Assert.True(Functions.DateTimeLessThan(dt1, dt2));
    }

    [Fact]
    public void DateTimeEqual_WithEqualDateTimes_ReturnsTrue()
    {
        var dt1 = new DateTime(2024, 1, 1, 12, 0, 0);
        var dt2 = new DateTime(2024, 1, 1, 12, 0, 0);
        Assert.True(Functions.DateTimeEqual(dt1, dt2));
    }

    [Fact]
    public void DateGreaterThan_WithGreaterDate_ReturnsTrue()
    {
        var d1 = new DateTime(2024, 1, 2);
        var d2 = new DateTime(2024, 1, 1);
        Assert.True(Functions.DateGreaterThan(d1, d2));
    }

    [Fact]
    public void DateLessThan_WithLesserDate_ReturnsTrue()
    {
        var d1 = new DateTime(2024, 1, 1);
        var d2 = new DateTime(2024, 1, 2);
        Assert.True(Functions.DateLessThan(d1, d2));
    }

    [Fact]
    public void DateEqual_WithSameDateDifferentTime_ReturnsTrue()
    {
        var d1 = new DateTime(2024, 1, 1, 10, 0, 0);
        var d2 = new DateTime(2024, 1, 1, 15, 30, 0);
        Assert.True(Functions.DateEqual(d1, d2));
    }

    [Fact]
    public void TimeGreaterThan_WithGreaterTime_ReturnsTrue()
    {
        Assert.True(Functions.TimeGreaterThan("15:00:00", "14:00:00"));
    }

    [Fact]
    public void TimeLessThan_WithLesserTime_ReturnsTrue()
    {
        Assert.True(Functions.TimeLessThan("14:00:00", "15:00:00"));
    }

    [Fact]
    public void TimeEqual_WithEqualTimes_ReturnsTrue()
    {
        Assert.True(Functions.TimeEqual("14:00:00", "14:00:00"));
    }

    [Fact]
    public void AnyOf_WithMatchingElement_ReturnsTrue()
    {
        var bag = new object[] { 1, 2, 3, 4, 5 };
        Assert.True(Functions.AnyOf(x => (int)x! == 3, bag));
    }

    [Fact]
    public void AnyOf_WithNoMatches_ReturnsFalse()
    {
        var bag = new object[] { 1, 2, 3 };
        Assert.False(Functions.AnyOf(x => (int)x! == 10, bag));
    }

    [Fact]
    public void AllOf_WithAllMatching_ReturnsTrue()
    {
        var bag = new object[] { 2, 4, 6, 8 };
        Assert.True(Functions.AllOf(x => (int)x! % 2 == 0, bag));
    }

    [Fact]
    public void AllOf_WithSomeNotMatching_ReturnsFalse()
    {
        var bag = new object[] { 2, 3, 4 };
        Assert.False(Functions.AllOf(x => (int)x! % 2 == 0, bag));
    }

    [Fact]
    public void AnyOfAny_WithMatchingPair_ReturnsTrue()
    {
        var bag1 = new object[] { 1, 2, 3 };
        var bag2 = new object[] { 3, 4, 5 };
        Assert.True(Functions.AnyOfAny((x, y) => (int)x! == (int)y!, bag1, bag2));
    }

    [Fact]
    public void AnyOfAny_WithNoMatchingPairs_ReturnsFalse()
    {
        var bag1 = new object[] { 1, 2 };
        var bag2 = new object[] { 3, 4 };
        Assert.False(Functions.AnyOfAny((x, y) => (int)x! == (int)y!, bag1, bag2));
    }

    [Fact]
    public void Not_WithTrue_ReturnsFalse()
    {
        Assert.False(Functions.Not(true));
    }

    [Fact]
    public void Not_WithFalse_ReturnsTrue()
    {
        Assert.True(Functions.Not(false));
    }

    [Fact]
    public void And_WithBothTrue_ReturnsTrue()
    {
        Assert.True(Functions.And(true, true));
    }

    [Fact]
    public void And_WithOneFalse_ReturnsFalse()
    {
        Assert.False(Functions.And(true, false));
    }

    [Fact]
    public void Or_WithOneTrue_ReturnsTrue()
    {
        Assert.True(Functions.Or(false, true));
    }

    [Fact]
    public void Or_WithBothFalse_ReturnsFalse()
    {
        Assert.False(Functions.Or(false, false));
    }

    [Fact]
    public void IntegerAdd_AddsTwoIntegers()
    {
        Assert.Equal(5, Functions.IntegerAdd(2, 3));
    }

    [Fact]
    public void IntegerSubtract_SubtractsTwoIntegers()
    {
        Assert.Equal(2, Functions.IntegerSubtract(5, 3));
    }

    [Fact]
    public void IntegerMultiply_MultipliesTwoIntegers()
    {
        Assert.Equal(15, Functions.IntegerMultiply(5, 3));
    }

    [Fact]
    public void IntegerDivide_DividesTwoIntegers()
    {
        Assert.Equal(3, Functions.IntegerDivide(9, 3));
    }

    [Fact]
    public void IntegerMod_ReturnsRemainder()
    {
        Assert.Equal(1, Functions.IntegerMod(10, 3));
    }

    [Fact]
    public void IntegerAbs_ReturnsAbsoluteValue()
    {
        Assert.Equal(5, Functions.IntegerAbs(-5));
    }

    [Fact]
    public void DoubleAdd_AddsTwoDoubles()
    {
        Assert.Equal(5.5, Functions.DoubleAdd(2.2, 3.3), 2);
    }

    [Fact]
    public void DoubleSubtract_SubtractsTwoDoubles()
    {
        Assert.Equal(1.5, Functions.DoubleSubtract(4.5, 3.0), 2);
    }

    [Fact]
    public void DoubleMultiply_MultipliesTwoDoubles()
    {
        Assert.Equal(6.0, Functions.DoubleMultiply(2.0, 3.0), 2);
    }

    [Fact]
    public void DoubleDivide_DividesTwoDoubles()
    {
        Assert.Equal(2.0, Functions.DoubleDivide(6.0, 3.0), 2);
    }

    [Fact]
    public void DoubleAbs_ReturnsAbsoluteValue()
    {
        Assert.Equal(5.5, Functions.DoubleAbs(-5.5), 2);
    }



    [Fact]
    public void StringContains_WithSubstring_ReturnsTrue()
    {
        Assert.True(Functions.StringContains("Hello World", "World"));
    }

    [Fact]
    public void StringContains_WithoutSubstring_ReturnsFalse()
    {
        Assert.False(Functions.StringContains("Hello", "World"));
    }

    [Fact]
    public void StringStartsWith_WithPrefix_ReturnsTrue()
    {
        Assert.True(Functions.StringStartsWith("Hello World", "Hello"));
    }

    [Fact]
    public void StringEndsWith_WithSuffix_ReturnsTrue()
    {
        Assert.True(Functions.StringEndsWith("Hello World", "World"));
    }

    [Fact]
    public void StringNormalizeToLowerCase_ConvertsToLower()
    {
        Assert.Equal("hello", Functions.StringNormalizeToLowerCase("HELLO"));
    }

    [Fact]
    public void StringNormalizeSpace_TrimsWhitespace()
    {
        Assert.Equal("hello world", Functions.StringNormalizeSpace("  hello   world  "));
    }

}
