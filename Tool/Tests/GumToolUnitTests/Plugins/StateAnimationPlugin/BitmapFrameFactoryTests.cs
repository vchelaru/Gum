using System.Windows.Media.Imaging;
using Shouldly;
using StateAnimationPlugin.Managers;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning <see cref="BitmapFrameFactory"/>, the WPF-side decode step added
/// when <see cref="IBitmapLoader"/> was sealed to a neutral <c>byte[]</c> return type (issue #3225).
/// </summary>
public class BitmapFrameFactoryTests : BaseTestClass
{
    [Fact]
    public void Create_EmptyBytes_ReturnsNull()
    {
        // Mirrors what Mock.Of<IBitmapLoader>() hands back in other tests -- Moq defaults an
        // unconfigured byte[]-returning method to Array.Empty<byte>(), not null.
        BitmapFrame? frame = BitmapFrameFactory.Create([]);

        frame.ShouldBeNull();
    }

    [Fact]
    public void Create_NullBytes_ReturnsNull()
    {
        BitmapFrame? frame = BitmapFrameFactory.Create(null);

        frame.ShouldBeNull();
    }

    [Fact]
    public void Create_ValidImageBytes_DecodesToFrameWithPositiveDimensions()
    {
        byte[] bytes = new BitmapLoader().LoadImage("PlayIcon.png");

        BitmapFrame? frame = BitmapFrameFactory.Create(bytes);

        frame.ShouldNotBeNull();
        frame.PixelWidth.ShouldBeGreaterThan(0);
        frame.PixelHeight.ShouldBeGreaterThan(0);
    }
}
