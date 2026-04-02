using Shouldly;
using WpfDataUi.Controls;

namespace GumToolUnitTests.Controls;

public class TextBoxDisplayLogicTests
{
    public static IEnumerable<object[]> MathExpressionData()
    {
        yield return new object[] { "2+3", typeof(float), 5f };
        yield return new object[] { "10-4", typeof(int), 6 };
        yield return new object[] { "3*7", typeof(double), 21.0 };
        yield return new object[] { "10/2", typeof(decimal), 5m };
        yield return new object[] { "(2+3)*4", typeof(float), 20f };
    }

    public static IEnumerable<object[]> NonMathStringData()
    {
        yield return new object[] { "hello", typeof(float) };
        yield return new object[] { "abc", typeof(int) };
        yield return new object[] { "foo", typeof(double) };
        yield return new object[] { "bar", typeof(decimal) };
    }

    public static IEnumerable<object[]> NonNumericTypeData()
    {
        yield return new object[] { "hello", typeof(string) };
        yield return new object[] { "2+3", typeof(string) };
    }

    public static IEnumerable<object[]> PlainNumberData()
    {
        yield return new object[] { "42", typeof(float) };
        yield return new object[] { "100", typeof(int) };
        yield return new object[] { "-5", typeof(double) };
    }

    public static IEnumerable<object[]> NullableMathData()
    {
        yield return new object[] { "2+3", typeof(float?) };
        yield return new object[] { "10-4", typeof(int?) };
        yield return new object[] { "3*7", typeof(double?) };
    }

    [Theory]
    [MemberData(nameof(MathExpressionData))]
    public void TryHandleMathOperation_ShouldEvaluateExpression(
        string input, Type targetType, object expected)
    {
        object result = TextBoxDisplayLogic.TryHandleMathOperation(input, targetType);

        result.ShouldNotBeNull();
        result.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(NonMathStringData))]
    public void TryHandleMathOperation_ShouldReturnNullForNonMathStrings(
        string input, Type targetType)
    {
        object result = TextBoxDisplayLogic.TryHandleMathOperation(input, targetType);

        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(NonNumericTypeData))]
    public void TryHandleMathOperation_ShouldReturnNullForNonNumericTypes(
        string input, Type targetType)
    {
        object result = TextBoxDisplayLogic.TryHandleMathOperation(input, targetType);

        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(PlainNumberData))]
    public void TryHandleMathOperation_ShouldReturnNullForPlainNumbers(
        string input, Type targetType)
    {
        // Plain numbers have no math operators, so should return null
        // (the caller handles plain number parsing via TryParse/ConvertFromString).
        object result = TextBoxDisplayLogic.TryHandleMathOperation(input, targetType);

        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(NullableMathData))]
    public void TryHandleMathOperation_ShouldWorkWithNullableNumericTypes(
        string input, Type targetType)
    {
        object result = TextBoxDisplayLogic.TryHandleMathOperation(input, targetType);

        result.ShouldNotBeNull();
    }
}
