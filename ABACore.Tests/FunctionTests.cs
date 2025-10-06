using ABACore.Runtime;

namespace ABACore.Tests;

/// <summary>
/// Tests for runtime function implementations.
/// </summary>
public class FunctionTests
{
    #region String Function Tests

    [Fact]
    public void StringEqual_MatchingStrings_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringEqual("hello", "hello");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringEqual_DifferentStrings_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringEqual("hello", "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringEqual_CaseSensitive_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringEqual("Hello", "hello");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringEqual_NullValues_ShouldHandleGracefully()
    {
        // Act
        bool result1 = Functions.StringEqual(null, null);
        bool result2 = Functions.StringEqual("hello", null);
        bool result3 = Functions.StringEqual(null, "hello");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void StringEqualIgnoreCase_DifferentCase_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringEqualIgnoreCase("Hello", "hello");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringEqualIgnoreCase_DifferentStrings_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringEqualIgnoreCase("Hello", "World");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringStartsWith_ValidPrefix_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringStartsWith("hello world", "hello");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringStartsWith_InvalidPrefix_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringStartsWith("hello world", "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringStartsWith_EmptyString_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringStartsWith("hello", "");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringEndsWith_ValidSuffix_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringEndsWith("hello world", "world");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringEndsWith_InvalidSuffix_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringEndsWith("hello world", "hello");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringContains_ValidSubstring_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringContains("hello world", "lo wo");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_InvalidSubstring_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringContains("hello world", "xyz");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringRegexMatch_ValidPattern_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.StringRegexMatch("test@example.com", @"^[\w\.-]+@[\w\.-]+\.\w+$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringRegexMatch_InvalidPattern_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.StringRegexMatch("not-an-email", @"^[\w\.-]+@[\w\.-]+\.\w+$");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringNormalizeSpace_MultipleSpaces_ShouldNormalize()
    {
        // Act
        string result = Functions.StringNormalizeSpace("  hello   world  ");

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StringNormalizeSpace_Tabs_ShouldNormalize()
    {
        // Act
        string result = Functions.StringNormalizeSpace("hello\t\tworld");

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StringNormalizeToLowerCase_MixedCase_ShouldConvert()
    {
        // Act
        string result = Functions.StringNormalizeToLowerCase("Hello World");

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StringNormalizeToLowerCase_AlreadyLowerCase_ShouldRemainSame()
    {
        // Act
        string result = Functions.StringNormalizeToLowerCase("hello world");

        // Assert
        Assert.Equal("hello world", result);
    }

    #endregion

    #region Integer Arithmetic Function Tests

    [Fact]
    public void IntegerAdd_PositiveNumbers_ShouldReturnSum()
    {
        // Act
        int result = Functions.IntegerAdd(10, 20);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void IntegerAdd_NegativeNumbers_ShouldReturnSum()
    {
        // Act
        int result = Functions.IntegerAdd(-10, -20);

        // Assert
        Assert.Equal(-30, result);
    }

    [Fact]
    public void IntegerSubtract_PositiveNumbers_ShouldReturnDifference()
    {
        // Act
        int result = Functions.IntegerSubtract(30, 10);

        // Assert
        Assert.Equal(20, result);
    }

    [Fact]
    public void IntegerSubtract_ResultNegative_ShouldReturnNegative()
    {
        // Act
        int result = Functions.IntegerSubtract(10, 30);

        // Assert
        Assert.Equal(-20, result);
    }

    [Fact]
    public void IntegerMultiply_PositiveNumbers_ShouldReturnProduct()
    {
        // Act
        int result = Functions.IntegerMultiply(5, 6);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void IntegerMultiply_WithZero_ShouldReturnZero()
    {
        // Act
        int result = Functions.IntegerMultiply(100, 0);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void IntegerDivide_ExactDivision_ShouldReturnQuotient()
    {
        // Act
        int result = Functions.IntegerDivide(20, 5);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void IntegerDivide_WithRemainder_ShouldReturnTruncatedQuotient()
    {
        // Act
        int result = Functions.IntegerDivide(23, 5);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void IntegerMod_PositiveNumbers_ShouldReturnRemainder()
    {
        // Act
        int result = Functions.IntegerMod(23, 5);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void IntegerMod_ExactDivision_ShouldReturnZero()
    {
        // Act
        int result = Functions.IntegerMod(20, 5);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void IntegerAbs_PositiveNumber_ShouldReturnSame()
    {
        // Act
        int result = Functions.IntegerAbs(42);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void IntegerAbs_NegativeNumber_ShouldReturnPositive()
    {
        // Act
        int result = Functions.IntegerAbs(-42);

        // Assert
        Assert.Equal(42, result);
    }

    #endregion

    #region Double Arithmetic Function Tests

    [Fact]
    public void DoubleAdd_PositiveNumbers_ShouldReturnSum()
    {
        // Act
        double result = Functions.DoubleAdd(10.5, 20.3);

        // Assert
        Assert.Equal(30.8, result, precision: 10);
    }

    [Fact]
    public void DoubleAdd_NegativeNumbers_ShouldReturnSum()
    {
        // Act
        double result = Functions.DoubleAdd(-10.5, -20.3);

        // Assert
        Assert.Equal(-30.8, result, precision: 10);
    }

    [Fact]
    public void DoubleSubtract_PositiveNumbers_ShouldReturnDifference()
    {
        // Act
        double result = Functions.DoubleSubtract(30.5, 10.2);

        // Assert
        Assert.Equal(20.3, result, precision: 10);
    }

    [Fact]
    public void DoubleMultiply_PositiveNumbers_ShouldReturnProduct()
    {
        // Act
        double result = Functions.DoubleMultiply(5.5, 2.0);

        // Assert
        Assert.Equal(11.0, result, precision: 10);
    }

    [Fact]
    public void DoubleDivide_ExactDivision_ShouldReturnQuotient()
    {
        // Act
        double result = Functions.DoubleDivide(10.0, 2.0);

        // Assert
        Assert.Equal(5.0, result, precision: 10);
    }

    [Fact]
    public void DoubleDivide_WithRemainder_ShouldReturnPreciseQuotient()
    {
        // Act
        double result = Functions.DoubleDivide(10.0, 3.0);

        // Assert
        Assert.Equal(3.333333, result, precision: 5);
    }

    [Fact]
    public void DoubleAbs_PositiveNumber_ShouldReturnSame()
    {
        // Act
        double result = Functions.DoubleAbs(42.5);

        // Assert
        Assert.Equal(42.5, result);
    }

    [Fact]
    public void DoubleAbs_NegativeNumber_ShouldReturnPositive()
    {
        // Act
        double result = Functions.DoubleAbs(-42.5);

        // Assert
        Assert.Equal(42.5, result);
    }

    #endregion

    #region DateTime Function Tests

    [Fact]
    public void DateTimeAddDayTimeDuration_AddDays_ShouldReturnFutureDate()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1, 12, 0, 0);
        var duration = TimeSpan.FromDays(5);

        // Act
        DateTime result = Functions.DateTimeAddDayTimeDuration(baseDate, duration.ToString());

        // Assert
        Assert.Equal(new DateTime(2024, 1, 6, 12, 0, 0), result);
    }

    [Fact]
    public void DateTimeAddDayTimeDuration_AddHours_ShouldReturnFutureTime()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1, 12, 0, 0);
        var duration = TimeSpan.FromHours(3);

        // Act
        DateTime result = Functions.DateTimeAddDayTimeDuration(baseDate, duration.ToString());

        // Assert
        Assert.Equal(new DateTime(2024, 1, 1, 15, 0, 0), result);
    }

    [Fact]
    public void DateTimeSubtractDayTimeDuration_SubtractDays_ShouldReturnPastDate()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 10, 12, 0, 0);
        var duration = TimeSpan.FromDays(5);

        // Act
        DateTime result = Functions.DateTimeSubtractDayTimeDuration(baseDate, duration.ToString());

        // Assert
        Assert.Equal(new DateTime(2024, 1, 5, 12, 0, 0), result);
    }

    [Fact]
    public void DateTimeGreaterThan_LaterDate_ShouldReturnTrue()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 10);
        DateTime date2 = new(2024, 1, 5);

        // Act
        bool result = Functions.DateTimeGreaterThan(date1, date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DateTimeGreaterThan_EarlierDate_ShouldReturnFalse()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 5);
        DateTime date2 = new(2024, 1, 10);

        // Act
        bool result = Functions.DateTimeGreaterThan(date1, date2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DateTimeLessThan_EarlierDate_ShouldReturnTrue()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 5);
        DateTime date2 = new(2024, 1, 10);

        // Act
        bool result = Functions.DateTimeLessThan(date1, date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DateTimeEqual_SameDate_ShouldReturnTrue()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 10, 12, 30, 45);
        DateTime date2 = new(2024, 1, 10, 12, 30, 45);

        // Act
        bool result = Functions.DateTimeEqual(date1, date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DateEqual_SameDate_ShouldReturnTrue()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 10, 12, 30, 45);
        DateTime date2 = new(2024, 1, 10, 18, 45, 30);

        // Act
        bool result = Functions.DateEqual(date1, date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DateEqual_DifferentDate_ShouldReturnFalse()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 10);
        DateTime date2 = new(2024, 1, 11);

        // Act
        bool result = Functions.DateEqual(date1, date2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TimeGreaterThan_LaterTime_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.TimeGreaterThan("15:30:00", "10:00:00");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TimeLessThan_EarlierTime_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.TimeLessThan("10:00:00", "15:30:00");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TimeEqual_SameTime_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.TimeEqual("12:30:45", "12:30:45");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Higher-Order Bag Function Tests

    [Fact]
    public void AnyOf_HasMatchingElement_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag = [1, 2, 3, 4, 5];

        // Act
        bool result = Functions.AnyOf(x => Convert.ToInt32(x) > 3, bag);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AnyOf_NoMatchingElement_ShouldReturnFalse()
    {
        // Arrange
        object?[] bag = [1, 2, 3];

        // Act
        bool result = Functions.AnyOf(x => Convert.ToInt32(x) > 10, bag);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AllOf_AllMatch_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag = [10, 20, 30, 40];

        // Act
        bool result = Functions.AllOf(x => Convert.ToInt32(x) >= 10, bag);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AllOf_NotAllMatch_ShouldReturnFalse()
    {
        // Arrange
        object?[] bag = [10, 20, 5, 40];

        // Act
        bool result = Functions.AllOf(x => Convert.ToInt32(x) >= 10, bag);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AnyOfAny_HasMatch_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag1 = [1, 2, 3];
        object?[] bag2 = [3, 4, 5];

        // Act
        bool result = Functions.AnyOfAny((x, y) => Convert.ToInt32(x) == Convert.ToInt32(y), bag1, bag2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AnyOfAny_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        object?[] bag1 = [1, 2, 3];
        object?[] bag2 = [4, 5, 6];

        // Act
        bool result = Functions.AnyOfAny((x, y) => Convert.ToInt32(x) == Convert.ToInt32(y), bag1, bag2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AllOfAny_AllMatch_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag1 = [3, 4, 5];
        object?[] bag2 = [3, 4, 5];

        // Act
        bool result = Functions.AllOfAny((x, y) => Convert.ToInt32(x) == Convert.ToInt32(y), bag1, bag2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AnyOfAll_AllMatchForOne_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag1 = [10, 20];
        object?[] bag2 = [5, 6, 7];

        // Act
        bool result = Functions.AnyOfAll((x, y) => Convert.ToInt32(x) > Convert.ToInt32(y), bag1, bag2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AllOfAll_AllMatch_ShouldReturnTrue()
    {
        // Arrange
        object?[] bag1 = [10, 20];
        object?[] bag2 = [5, 6];

        // Act
        bool result = Functions.AllOfAll((x, y) => Convert.ToInt32(x) > Convert.ToInt32(y), bag1, bag2);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Logical Function Tests

    [Fact]
    public void Not_True_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.Not(true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Not_False_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.Not(false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void And_TrueAndTrue_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.And(true, true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void And_TrueAndFalse_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.And(true, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void And_FalseAndFalse_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.And(false, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Or_TrueOrFalse_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.Or(true, false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Or_FalseOrFalse_ShouldReturnFalse()
    {
        // Act
        bool result = Functions.Or(false, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Or_TrueOrTrue_ShouldReturnTrue()
    {
        // Act
        bool result = Functions.Or(true, true);

        // Assert
        Assert.True(result);
    }

    #endregion
}
