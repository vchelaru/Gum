using RenderingLibrary.Math;
using Shouldly;

namespace Gum.Presentation.Tests.Math;

/// <summary>
/// Pins <see cref="MathFunctions.RoundToInt(float)"/>, the double and decimal overloads. The oracles
/// below are literal copies of the original formula, so any rewrite of the bodies must match them.
/// </summary>
public class RoundToIntTests
{
    private static int OldRoundToInt(float x) =>
        (int)(System.Math.Round(x, MidpointRounding.AwayFromZero) + (System.Math.Sign(x) * .5f));

    private static int OldRoundToInt(double x) =>
        (int)(System.Math.Round(x, MidpointRounding.AwayFromZero) + (System.Math.Sign(x) * .5f));

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(-0f, 0)]
    [InlineData(0.49999997f, 0)]
    [InlineData(-0.49999997f, 0)]
    [InlineData(0.5f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(2.5f, 3)]
    [InlineData(-0.5f, -1)]
    [InlineData(-1.5f, -2)]
    [InlineData(-2.5f, -3)]
    [InlineData(3.99999f, 4)]
    [InlineData(-3.99999f, -4)]
    [InlineData(8388607.5f, 8388608)]
    [InlineData(-8388607.5f, -8388608)]
    [InlineData(16777216f, 16777216)]
    [InlineData(1000000000f, 1000000000)]
    [InlineData(-1000000000f, -1000000000)]
    [InlineData(2147483520f, 2147483520)]
    [InlineData(-2147483648f, int.MinValue)]
    public void RoundToInt_Float_ReturnsPinnedValue(float input, int expected)
    {
        MathFunctions.RoundToInt(input).ShouldBe(expected);
    }

    [Fact]
    public void RoundToInt_Float_MatchesOldFormulaForRandomBitPatterns()
    {
        Random random = new Random(6388);
        byte[] bytes = new byte[4];
        int compared = 0;

        for (int i = 0; i < 400; i++)
        {
            random.NextBytes(bytes);
            float value = BitConverter.ToSingle(bytes, 0);
            if (float.IsNaN(value))
            {
                continue;
            }

            MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"bits 0x{BitConverter.SingleToInt32Bits(value):X8}");
            compared++;
        }

        compared.ShouldBeGreaterThan(300);
    }

    [Fact]
    public void RoundToInt_Float_MatchesOldFormulaForPixelRangeValues()
    {
        Random random = new Random(6388);

        for (int i = 0; i < 400; i++)
        {
            float value = (float)((random.NextDouble() * 2 - 1) * 5000);
            MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"value {value:R}");
        }

        // Exact midpoints and their float neighbors.
        for (int quarter = -400; quarter <= 400; quarter++)
        {
            float midpoint = quarter / 4f;
            foreach (float value in new[] { midpoint, MathF.BitDecrement(midpoint), MathF.BitIncrement(midpoint) })
            {
                MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"value {value:R}");
            }
        }
    }

    [Fact]
    public void RoundToInt_FloatNaN_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => MathFunctions.RoundToInt(float.NaN));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(-0.0, 0)]
    [InlineData(0.49999999999999994, 0)]
    [InlineData(0.5, 1)]
    [InlineData(0.5000000000000001, 1)]
    [InlineData(1.5, 2)]
    [InlineData(2.5, 3)]
    [InlineData(-0.5, -1)]
    [InlineData(-1.5, -2)]
    [InlineData(-2.5, -3)]
    [InlineData(3.9999999999, 4)]
    [InlineData(-3.9999999999, -4)]
    [InlineData(1000000000.5, 1000000001)]
    [InlineData(2147483646.5, 2147483647)]
    [InlineData(-2147483647.5, int.MinValue)]
    [InlineData(-2147483648.0, int.MinValue)]
    public void RoundToInt_Double_ReturnsPinnedValue(double input, int expected)
    {
        MathFunctions.RoundToInt(input).ShouldBe(expected);
    }

    [Fact]
    public void RoundToInt_Double_MatchesOldFormulaForRandomBitPatterns()
    {
        Random random = new Random(6388);
        byte[] bytes = new byte[8];
        int compared = 0;

        for (int i = 0; i < 400; i++)
        {
            random.NextBytes(bytes);
            double value = BitConverter.ToDouble(bytes, 0);
            if (double.IsNaN(value))
            {
                continue;
            }

            MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"bits 0x{BitConverter.DoubleToInt64Bits(value):X16}");
            compared++;
        }

        compared.ShouldBeGreaterThan(300);
    }

    [Fact]
    public void RoundToInt_Double_MatchesOldFormulaForPixelRangeValues()
    {
        Random random = new Random(6388);

        for (int i = 0; i < 400; i++)
        {
            double value = (random.NextDouble() * 2 - 1) * 5000;
            MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"value {value:R}");
        }

        for (int quarter = -400; quarter <= 400; quarter++)
        {
            double midpoint = quarter / 4.0;
            foreach (double value in new[] { midpoint, System.Math.BitDecrement(midpoint), System.Math.BitIncrement(midpoint) })
            {
                MathFunctions.RoundToInt(value).ShouldBe(OldRoundToInt(value), $"value {value:R}");
            }
        }
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.49", 0)]
    [InlineData("0.5", 1)]
    [InlineData("1.5", 2)]
    [InlineData("2.5", 3)]
    [InlineData("-0.49", 0)]
    [InlineData("-0.5", -1)]
    [InlineData("-1.5", -2)]
    [InlineData("-2.5", -3)]
    [InlineData("3.99999", 4)]
    [InlineData("-3.99999", -4)]
    public void RoundToInt_Decimal_ReturnsPinnedValue(string input, int expected)
    {
        decimal value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

        MathFunctions.RoundToInt(value).ShouldBe(expected);
    }
}
