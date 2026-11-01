using Gum.Managers;
using RenderingLibrary.Content;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ImageFileExtensionsTests : IDisposable
{
    private readonly List<IImageDecoder> _registered = new();

    public void Dispose()
    {
        foreach (IImageDecoder decoder in _registered)
        {
            LoaderManager.Self.ImageDecoders.Remove(decoder);
        }
    }

    private sealed class Decoder : IImageDecoder
    {
        public IEnumerable<string>? Extensions;
        public IEnumerable<string>? SupportedExtensions => Extensions;
        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            image = null;
            return false;
        }
    }

    private void Register(params string[] extensions)
    {
        var decoder = new Decoder { Extensions = extensions };
        LoaderManager.Self.ImageDecoders.Add(decoder);
        _registered.Add(decoder);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("PNG")]
    [InlineData("jpeg")]
    [InlineData("gif")]
    [InlineData("bmp")]
    public void Contains_ShouldBeTrue_ForBuiltInFormat(string extension)
    {
        ImageFileExtensions.Contains(extension).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ShouldBeFalse_ForDecoderFormat_WhenNoDecoderRegistered()
    {
        ImageFileExtensions.Contains("fake").ShouldBeFalse();
    }

    [Theory]
    [InlineData("fake")]
    [InlineData("FAKE")]
    public void Contains_ShouldBeTrue_ForDecoderFormat_RegardlessOfCase(string extension)
    {
        Register("fake");

        ImageFileExtensions.Contains(extension).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ShouldBeTrue_WhenDecoderDeclaresExtensionWithLeadingDotAndUpperCase()
    {
        Register(".FAKE");

        ImageFileExtensions.Contains("fake").ShouldBeTrue();
    }

    [Fact]
    public void Contains_ShouldBeFalse_AfterDecoderIsRemoved()
    {
        Register("fake");
        LoaderManager.Self.ImageDecoders.Clear();

        ImageFileExtensions.Contains("fake").ShouldBeFalse();
    }

    [Fact]
    public void All_ShouldListDecoderExtensionOnce_WhenTwoDecodersDeclareIt()
    {
        Register("fake");
        Register("fake");

        ImageFileExtensions.All.Count(x => x == "fake").ShouldBe(1);
    }

    [Fact]
    public void All_ShouldIgnoreBlankExtensions()
    {
        Register("", "  ");

        ImageFileExtensions.All.ShouldNotContain("");
        ImageFileExtensions.All.ShouldNotContain("  ");
    }
}
