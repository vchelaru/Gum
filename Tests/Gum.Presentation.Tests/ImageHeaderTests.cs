using Gum.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;

namespace Gum.Presentation.Tests;

public class ImageHeaderTests : BaseTestClass
{
    private readonly string _directory;
    private readonly List<IImageDecoder> _registered = new();

    public ImageHeaderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ImageHeaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public override void Dispose()
    {
        foreach (IImageDecoder decoder in _registered)
        {
            LoaderManager.Self.ImageDecoders.Remove(decoder);
        }
        Directory.Delete(_directory, true);
        base.Dispose();
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        public int Width = 7;
        public int Height = 3;
        public int Calls;
        public bool Throws;
        public IEnumerable<string>? SupportedExtensions => new[] { "fake" };

        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            image = null;
            if (!fileName.EndsWith(".fake", StringComparison.OrdinalIgnoreCase)) return false;
            Calls++;
            if (Throws) throw new InvalidDataException("bad fake");
            image = new DecodedImage(new byte[Width * Height * 4], Width, Height);
            return true;
        }
    }

    private FakeDecoder Register(FakeDecoder? decoder = null)
    {
        decoder ??= new FakeDecoder();
        LoaderManager.Self.ImageDecoders.Add(decoder);
        _registered.Add(decoder);
        return decoder;
    }

    private string Write(string name, byte[] bytes)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string WriteImage(string name, SKEncodedImageFormat format, int w, int h)
    {
        using var bitmap = new SKBitmap(w, h);
        using var data = SKImage.FromBitmap(bitmap).Encode(format, 90);
        return Write(name, data.ToArray());
    }

    [Fact]
    public void GetDimensions_ShouldUseDecoder_WhenExtensionIsDecoderProvided()
    {
        Register();
        string path = Write("a.fake", new byte[] { 1, 2, 3 });

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(7);
        size.Value.Height.ShouldBe(3);
    }

    [Fact]
    public void GetDimensions_ShouldPreferDecoder_WhenFileIsAlsoAValidPng()
    {
        Register(new FakeDecoder { Width = 5, Height = 6 });
        string png = WriteImage("a.png", SKEncodedImageFormat.Png, 2, 2);
        string path = Path.Combine(_directory, "b.fake");
        File.Copy(png, path);

        ImageHeader.GetDimensions(path)!.Value.Width.ShouldBe(5);
    }

    [Fact]
    public void GetDimensions_ShouldAskDecoderOnce_WhenDecoderHandlesFile()
    {
        FakeDecoder decoder = Register();
        string path = Write("a.fake", new byte[] { 1 });

        ImageHeader.GetDimensions(path);

        decoder.Calls.ShouldBe(1);
    }

    [Fact]
    public void GetDimensions_ShouldReadPng_WhenNoDecoderRegistered()
    {
        string path = WriteImage("a.png", SKEncodedImageFormat.Png, 12, 9);

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(12);
        size.Value.Height.ShouldBe(9);
    }

    [Fact]
    public void GetDimensions_ShouldReadPng_WhenDecoderDeclines()
    {
        Register();
        string path = WriteImage("a.png", SKEncodedImageFormat.Png, 12, 9);

        ImageHeader.GetDimensions(path)!.Value.Width.ShouldBe(12);
    }

    [Fact]
    public void GetDimensions_ShouldReadJpeg()
    {
        string path = WriteImage("a.jpg", SKEncodedImageFormat.Jpeg, 20, 10);

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(20);
        size.Value.Height.ShouldBe(10);
    }

    [Fact]
    public void GetDimensions_ShouldReadWebpThroughSkia_WhenHeaderTableDoesNotKnowIt()
    {
        string path = WriteImage("a.webp", SKEncodedImageFormat.Webp, 16, 8);

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(16);
        size.Value.Height.ShouldBe(8);
    }

    [Fact]
    public void GetDimensions_ShouldReturnNull_WhenFileDoesNotExist()
    {
        ImageHeader.GetDimensions(Path.Combine(_directory, "missing.png")).ShouldBeNull();
    }

    [Fact]
    public void GetDimensions_ShouldReturnNull_WhenNothingRecognizesTheFile()
    {
        string path = Write("a.fake", new byte[] { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 });

        ImageHeader.GetDimensions(path).ShouldBeNull();
    }

    [Fact]
    public void GetDimensions_ShouldReturnNull_WhenFileIsEmpty()
    {
        string path = Write("a.png", Array.Empty<byte>());

        ImageHeader.GetDimensions(path).ShouldBeNull();
    }

    [Fact]
    public void GetDimensions_ShouldPropagateDecoderException_WhenDecoderThrows()
    {
        Register(new FakeDecoder { Throws = true });
        string path = Write("a.fake", new byte[] { 1 });

        Should.Throw<InvalidDataException>(() => ImageHeader.GetDimensions(path));
    }

    [Fact]
    public void GetDimensions_ShouldNotLockFile_AfterReading()
    {
        Register();
        string path = Write("a.fake", new byte[] { 1 });

        ImageHeader.GetDimensions(path);

        File.Delete(path);
        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void GetDimensions_ShouldReadGif()
    {
        byte[] bytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 14, 0, 6, 0 };
        string path = Write("a.gif", bytes);

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(14);
        size.Value.Height.ShouldBe(6);
    }

    [Fact]
    public void GetDimensions_ShouldReadBmp()
    {
        byte[] bytes = new byte[26];
        bytes[0] = 0x42;
        bytes[1] = 0x4D;
        bytes[18] = 11;
        bytes[22] = 5;
        string path = Write("a.bmp", bytes);

        var size = ImageHeader.GetDimensions(path);

        size!.Value.Width.ShouldBe(11);
        size.Value.Height.ShouldBe(5);
    }

    [Fact]
    public void GetDimensions_ShouldUseDecoder_WhenExtensionIsUpperCase()
    {
        Register();
        string path = Write("a.FAKE", new byte[] { 1 });

        ImageHeader.GetDimensions(path)!.Value.Width.ShouldBe(7);
    }

    [Fact]
    public void GetDimensions_ShouldReadPng_WhenFileHasWrongExtension()
    {
        string png = WriteImage("a.png", SKEncodedImageFormat.Png, 4, 9);
        string path = Path.Combine(_directory, "misnamed.jpg");
        File.Copy(png, path);

        ImageHeader.GetDimensions(path)!.Value.Height.ShouldBe(9);
    }

    [Fact]
    public void GetDimensions_ShouldReturnNull_WhenPngIsTruncated()
    {
        string png = WriteImage("a.png", SKEncodedImageFormat.Png, 4, 9);
        string path = Write("cut.png", File.ReadAllBytes(png).Take(10).ToArray());

        Should.NotThrow(() => ImageHeader.GetDimensions(path));
    }
}
