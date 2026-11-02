using Gum.Managers;
using Moq.AutoMock;
using RenderingLibrary.Content;
using Shouldly;

namespace Gum.Presentation.Tests;

public class DragDropManagerFileDropFormatTests : IDisposable
{
    private readonly DragDropManager _sut = new AutoMocker().CreateInstance<DragDropManager>();
    private readonly Decoder _decoder = new();

    public DragDropManagerFileDropFormatTests()
    {
        LoaderManager.Self.ImageDecoders.Add(_decoder);
    }

    public void Dispose() => LoaderManager.Self.ImageDecoders.Remove(_decoder);

    private sealed class Decoder : IImageDecoder
    {
        public IEnumerable<string>? SupportedExtensions => new[] { "fake" };
        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            image = null;
            return false;
        }
    }

    [Theory]
    [InlineData("c:/a/pic.png")]
    [InlineData("c:/a/pic.fake")]
    [InlineData("c:/a/pic.FAKE")]
    [InlineData("c:/a/dir.with.dots/pic.fake")]
    [InlineData("c:/a/pic.svg")]
    [InlineData("c:/a/anim.achx")]
    public void IsValidExtensionForFileDrop_ShouldBeTrue_ForTextureFormats(string file)
    {
        _sut.IsValidExtensionForFileDrop(file).ShouldBeTrue();
    }

    [Theory]
    [InlineData("c:/a/pic.xyz")]
    [InlineData("c:/a/pic")]
    [InlineData("c:/a/fake")]
    public void IsValidExtensionForFileDrop_ShouldBeFalse_ForUnknownFormats(string file)
    {
        _sut.IsValidExtensionForFileDrop(file).ShouldBeFalse();
    }

    [Fact]
    public void ValidTextureExtensions_ShouldIncludeDecoderFormat()
    {
        _sut.ValidTextureExtensions.ShouldContain("fake");
    }

    [Fact]
    public void IsValidExtensionForFileDrop_ShouldBeFalse_ForDecoderFormat_AfterDecoderRemoved()
    {
        LoaderManager.Self.ImageDecoders.Remove(_decoder);

        _sut.IsValidExtensionForFileDrop("c:/a/pic.fake").ShouldBeFalse();
    }
}
