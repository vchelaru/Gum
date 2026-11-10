using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

/// <summary>
/// The argument rule every backend's pixel-data texture applier shares: positive size, and exactly
/// width * height * 4 bytes of RGBA. Issue #7885.
/// </summary>
public class PixelDataValidationTests
{
    [Fact]
    public void EnsureRgbaLength_ExactLength_DoesNotThrow()
    {
        Should.NotThrow(() => PixelDataValidation.EnsureRgbaLength(new byte[3 * 2 * 4], 3, 2));
    }

    [Theory]
    [InlineData(8, 6, 6)]
    [InlineData(6 * 6 * 4 - 1, 6, 6)]
    [InlineData(6 * 6 * 4 + 1, 6, 6)]
    [InlineData(6 * 6 * 4 + 4, 6, 6)]
    [InlineData(0, 6, 6)]
    public void EnsureRgbaLength_WrongLength_ThrowsAndNamesBothSizes(int length, int width, int height)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => PixelDataValidation.EnsureRgbaLength(new byte[length], width, height));

        exception.Message.ShouldContain($"{length} bytes");
        exception.Message.ShouldContain($"{width}x{height}");
        exception.Message.ShouldContain("144");
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    [InlineData(-1, 4)]
    [InlineData(4, -1)]
    public void EnsureRgbaLength_ZeroOrNegativeSize_Throws(int width, int height)
    {
        Should.Throw<ArgumentException>(() => PixelDataValidation.EnsureRgbaLength(new byte[0], width, height));
    }

    [Fact]
    public void EnsureRgbaLength_NullPixels_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => PixelDataValidation.EnsureRgbaLength(null!, 4, 4));
    }

    [Fact]
    public void EnsureRgbaLength_SizeWhoseByteCountOverflowsInt_ThrowsInsteadOfWrapping()
    {
        // 32768 * 32768 * 4 wraps to 0 in int arithmetic, which an empty array would then "match".
        Should.Throw<ArgumentException>(() => PixelDataValidation.EnsureRgbaLength(new byte[0], 32768, 32768));
    }
}
