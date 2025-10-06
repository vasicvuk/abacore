using System.Text.RegularExpressions;

namespace ABACore.Runtime;

/// <summary>
/// Provides runtime function implementations for ALFA function calls.
/// These functions are referenced by the compiled policy code.
/// </summary>
public static class Functions
{
    // String functions

    /// <summary>
    /// Compares two strings for equality.
    /// </summary>
    public static bool StringEqual(object? str1, object? str2)
    {
        return string.Equals(str1?.ToString(), str2?.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Compares two strings for equality (case-insensitive).
    /// </summary>
    public static bool StringEqualIgnoreCase(object? str1, object? str2)
    {
        return string.Equals(str1?.ToString(), str2?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a string starts with another string.
    /// </summary>
    public static bool StringStartsWith(object? str, object? prefix)
    {
        string? s = str?.ToString();
        string? p = prefix?.ToString();
        return s != null && p != null && s.StartsWith(p, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if a string ends with another string.
    /// </summary>
    public static bool StringEndsWith(object? str, object? suffix)
    {
        string? s = str?.ToString();
        string? suf = suffix?.ToString();
        return s != null && suf != null && s.EndsWith(suf, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if a string contains another string.
    /// </summary>
    public static bool StringContains(object? str, object? substring)
    {
        string? s = str?.ToString();
        string? sub = substring?.ToString();
        return s != null && sub != null && s.Contains(sub, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if a string matches a regular expression.
    /// </summary>
    public static bool StringRegexMatch(object? str, object? pattern)
    {
        string? s = str?.ToString();
        string? p = pattern?.ToString();
        return s != null && p != null && Regex.IsMatch(s, p);
    }

    /// <summary>
    /// Normalizes whitespace in a string.
    /// </summary>
    public static string StringNormalizeSpace(object? str)
    {
        string? s = str?.ToString();
        return s == null ? string.Empty : Regex.Replace(s.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Converts a string to lowercase.
    /// </summary>
    public static string StringNormalizeToLowerCase(object? str)
    {
        return str?.ToString()?.ToLowerInvariant() ?? string.Empty;
    }

    // Integer arithmetic functions

    /// <summary>
    /// Adds two integers.
    /// </summary>
    public static int IntegerAdd(object? a, object? b)
    {
        return Convert.ToInt32(a) + Convert.ToInt32(b);
    }

    /// <summary>
    /// Subtracts two integers.
    /// </summary>
    public static int IntegerSubtract(object? a, object? b)
    {
        return Convert.ToInt32(a) - Convert.ToInt32(b);
    }

    /// <summary>
    /// Multiplies two integers.
    /// </summary>
    public static int IntegerMultiply(object? a, object? b)
    {
        return Convert.ToInt32(a) * Convert.ToInt32(b);
    }

    /// <summary>
    /// Divides two integers.
    /// </summary>
    public static int IntegerDivide(object? a, object? b)
    {
        return Convert.ToInt32(a) / Convert.ToInt32(b);
    }

    /// <summary>
    /// Computes the modulus of two integers.
    /// </summary>
    public static int IntegerMod(object? a, object? b)
    {
        return Convert.ToInt32(a) % Convert.ToInt32(b);
    }

    /// <summary>
    /// Computes the absolute value of an integer.
    /// </summary>
    public static int IntegerAbs(object? a)
    {
        return Math.Abs(Convert.ToInt32(a));
    }

    // Double arithmetic functions

    /// <summary>
    /// Adds two doubles.
    /// </summary>
    public static double DoubleAdd(object? a, object? b)
    {
        return Convert.ToDouble(a) + Convert.ToDouble(b);
    }

    /// <summary>
    /// Subtracts two doubles.
    /// </summary>
    public static double DoubleSubtract(object? a, object? b)
    {
        return Convert.ToDouble(a) - Convert.ToDouble(b);
    }

    /// <summary>
    /// Multiplies two doubles.
    /// </summary>
    public static double DoubleMultiply(object? a, object? b)
    {
        return Convert.ToDouble(a) * Convert.ToDouble(b);
    }

    /// <summary>
    /// Divides two doubles.
    /// </summary>
    public static double DoubleDivide(object? a, object? b)
    {
        return Convert.ToDouble(a) / Convert.ToDouble(b);
    }

    /// <summary>
    /// Computes the absolute value of a double.
    /// </summary>
    public static double DoubleAbs(object? a)
    {
        return Math.Abs(Convert.ToDouble(a));
    }

    // DateTime functions

    /// <summary>
    /// Adds a duration to a DateTime.
    /// </summary>
    public static DateTime DateTimeAddDayTimeDuration(object? dateTime, object? duration)
    {
        var dt = Convert.ToDateTime(dateTime);
        var ts = TimeSpan.Parse(duration?.ToString() ?? "0");
        return dt.Add(ts);
    }

    /// <summary>
    /// Subtracts a duration from a DateTime.
    /// </summary>
    public static DateTime DateTimeSubtractDayTimeDuration(object? dateTime, object? duration)
    {
        var dt = Convert.ToDateTime(dateTime);
        var ts = TimeSpan.Parse(duration?.ToString() ?? "0");
        return dt.Subtract(ts);
    }

    /// <summary>
    /// Compares two DateTimes for greater than.
    /// </summary>
    public static bool DateTimeGreaterThan(object? dt1, object? dt2)
    {
        return Convert.ToDateTime(dt1) > Convert.ToDateTime(dt2);
    }

    /// <summary>
    /// Compares two DateTimes for less than.
    /// </summary>
    public static bool DateTimeLessThan(object? dt1, object? dt2)
    {
        return Convert.ToDateTime(dt1) < Convert.ToDateTime(dt2);
    }

    /// <summary>
    /// Compares two DateTimes for equality.
    /// </summary>
    public static bool DateTimeEqual(object? dt1, object? dt2)
    {
        return Convert.ToDateTime(dt1) == Convert.ToDateTime(dt2);
    }

    /// <summary>
    /// Compares two Dates for greater than.
    /// </summary>
    public static bool DateGreaterThan(object? d1, object? d2)
    {
        return Convert.ToDateTime(d1).Date > Convert.ToDateTime(d2).Date;
    }

    /// <summary>
    /// Compares two Dates for less than.
    /// </summary>
    public static bool DateLessThan(object? d1, object? d2)
    {
        return Convert.ToDateTime(d1).Date < Convert.ToDateTime(d2).Date;
    }

    /// <summary>
    /// Compares two Dates for equality.
    /// </summary>
    public static bool DateEqual(object? d1, object? d2)
    {
        return Convert.ToDateTime(d1).Date == Convert.ToDateTime(d2).Date;
    }

    /// <summary>
    /// Compares two Times for greater than.
    /// </summary>
    public static bool TimeGreaterThan(object? t1, object? t2)
    {
        return TimeSpan.Parse(t1?.ToString() ?? "0") > TimeSpan.Parse(t2?.ToString() ?? "0");
    }

    /// <summary>
    /// Compares two Times for less than.
    /// </summary>
    public static bool TimeLessThan(object? t1, object? t2)
    {
        return TimeSpan.Parse(t1?.ToString() ?? "0") < TimeSpan.Parse(t2?.ToString() ?? "0");
    }

    /// <summary>
    /// Compares two Times for equality.
    /// </summary>
    public static bool TimeEqual(object? t1, object? t2)
    {
        return TimeSpan.Parse(t1?.ToString() ?? "0") == TimeSpan.Parse(t2?.ToString() ?? "0");
    }

    // Higher-order bag functions

    /// <summary>
    /// Checks if any element in a bag matches a condition.
    /// </summary>
    public static bool AnyOf(Func<object?, bool> predicate, params object?[] bag)
    {
        return bag.Any(predicate);
    }

    /// <summary>
    /// Checks if all elements in a bag match a condition.
    /// </summary>
    public static bool AllOf(Func<object?, bool> predicate, params object?[] bag)
    {
        return bag.All(predicate);
    }

    /// <summary>
    /// Checks if any element in bag1 matches any element in bag2.
    /// </summary>
    public static bool AnyOfAny(Func<object?, object?, bool> predicate, object?[] bag1, object?[] bag2)
    {
        return bag1.Any(x => bag2.Any(y => predicate(x, y)));
    }

    /// <summary>
    /// Checks if all elements in bag1 match any element in bag2.
    /// </summary>
    public static bool AllOfAny(Func<object?, object?, bool> predicate, object?[] bag1, object?[] bag2)
    {
        return bag1.All(x => bag2.Any(y => predicate(x, y)));
    }

    /// <summary>
    /// Checks if any element in bag1 matches all elements in bag2.
    /// </summary>
    public static bool AnyOfAll(Func<object?, object?, bool> predicate, object?[] bag1, object?[] bag2)
    {
        return bag1.Any(x => bag2.All(y => predicate(x, y)));
    }

    /// <summary>
    /// Checks if all elements in bag1 match all elements in bag2.
    /// </summary>
    public static bool AllOfAll(Func<object?, object?, bool> predicate, object?[] bag1, object?[] bag2)
    {
        return bag1.All(x => bag2.All(y => predicate(x, y)));
    }

    // Logical functions

    /// <summary>
    /// Logical NOT.
    /// </summary>
    public static bool Not(bool value)
    {
        return !value;
    }

    /// <summary>
    /// Logical AND.
    /// </summary>
    public static bool And(bool a, bool b)
    {
        return a && b;
    }

    /// <summary>
    /// Logical OR.
    /// </summary>
    public static bool Or(bool a, bool b)
    {
        return a || b;
    }
}
