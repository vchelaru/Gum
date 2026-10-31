using System;
using System.Collections.Generic;
using Gum.Bundle;
using Shouldly;

namespace Gum.Bundle.Tests;

public class GumRawTextureTests : IDisposable
{
    private readonly List<string> _warnings = new List<string>();
    private readonly Action<string> _previousReportWarning;

    public GumRawTextureTests()
    {
        _previousReportWarning = GumRawTexture.ReportWarning;
        GumRawTexture.ReportWarning = _warnings.Add;
        GumRawTexture.ResetWarnings();
    }

    public void Dispose()
    {
        GumRawTexture.ReportWarning = _previousReportWarning;
        GumRawTexture.ResetWarnings();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Encode_ThenReadHeader_ShouldRoundTripEveryField(bool isPremultiplied)
    {
        byte[] rgba = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

        byte[] entry = GumRawTexture.Encode(width: 3, height: 2, rgba, isPremultiplied);
        GumRawTextureHeader header = GumRawTexture.ReadHeader(entry);

        GumRawTexture.IsRawTexture(entry).ShouldBeTrue();
        header.Width.ShouldBe(3);
        header.Height.ShouldBe(2);
        header.PixelFormat.ShouldBe(GumRawTexturePixelFormat.Rgba8);
        header.IsPremultiplied.ShouldBe(isPremultiplied);
        entry.Length.ShouldBe(GumRawTexture.HeaderLength + rgba.Length);
        entry.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(rgba);
    }

    [Fact]
    public void IsRawTexture_ShouldBeFalse_ForPngAndShortData()
    {
        byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        GumRawTexture.IsRawTexture(pngSignature).ShouldBeFalse();
        GumRawTexture.IsRawTexture(new byte[] { 0x89 }).ShouldBeFalse();
        GumRawTexture.IsRawTexture(Array.Empty<byte>()).ShouldBeFalse();
    }

    [Fact]
    public void Encode_ShouldThrow_WhenPixelLengthDoesNotMatchSize()
    {
        Should.Throw<ArgumentException>(() => GumRawTexture.Encode(width: 2, height: 2, new byte[12], isPremultiplied: false));
    }

    [Fact]
    public void ReadHeader_ShouldThrow_WhenHeaderIsTruncated()
    {
        byte[] entry = GumRawTexture.Encode(width: 1, height: 1, new byte[4], isPremultiplied: false);

        GumBundleFormatException exception = Should.Throw<GumBundleFormatException>(
            () => GumRawTexture.ReadHeader(entry.AsSpan(0, GumRawTexture.HeaderLength - 1)));

        exception.Message.ShouldContain("truncated");
    }

    [Fact]
    public void ReadHeader_ShouldThrow_WhenPixelDataIsTruncated()
    {
        byte[] entry = GumRawTexture.Encode(width: 2, height: 1, new byte[8], isPremultiplied: false);

        GumBundleFormatException exception = Should.Throw<GumBundleFormatException>(
            () => GumRawTexture.ReadHeader(entry.AsSpan(0, entry.Length - 1)));

        exception.Message.ShouldContain("truncated");
    }

    [Theory]
    [InlineData(4, (byte)99, "header version")]
    [InlineData(5, (byte)42, "pixel format")]
    public void ReadHeader_ShouldThrow_WhenHeaderFieldIsUnknown(int offset, byte value, string expectedMessage)
    {
        byte[] entry = GumRawTexture.Encode(width: 1, height: 1, new byte[4], isPremultiplied: false);
        entry[offset] = value;

        GumBundleFormatException exception = Should.Throw<GumBundleFormatException>(() => GumRawTexture.ReadHeader(entry));

        exception.Message.ShouldContain(expectedMessage);
    }

    [Fact]
    public void ReadHeader_ShouldThrow_WhenMagicIsMissing()
    {
        Should.Throw<GumBundleFormatException>(() => GumRawTexture.ReadHeader(new byte[GumRawTexture.HeaderLength]));
    }

    [Fact]
    public void PrepareForRuntime_ShouldLeavePixelsAlone_WhenAlphaModesMatch()
    {
        byte[] rgba = { 255, 255, 255, 128, 50, 60, 70, 0 };
        byte[] straight = GumRawTexture.Encode(width: 2, height: 1, rgba, isPremultiplied: false);
        byte[] premultiplied = GumRawTexture.Encode(width: 2, height: 1, rgba, isPremultiplied: true);

        GumRawTexture.PrepareForRuntime(straight, "a.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: false, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: true);
        GumRawTexture.PrepareForRuntime(premultiplied, "a.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: true);

        straight.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(rgba);
        premultiplied.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(rgba);
        _warnings.ShouldBeEmpty();
    }

    [Fact]
    public void PrepareForRuntime_ShouldPremultiplyAndWarnOnce_WhenStraightPackageMeetsPremultipliedRuntime()
    {
        byte[] first = GumRawTexture.Encode(width: 2, height: 1,
            new byte[] { 255, 255, 255, 128, 50, 60, 70, 0 }, isPremultiplied: false);
        byte[] second = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 200, 100, 0, 255 }, isPremultiplied: false);

        GumRawTexture.PrepareForRuntime(first, "Sprites/a.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "Renderer.UsePremultipliedAlpha is true",
            bleedTransparentEdges: true);
        GumRawTexture.PrepareForRuntime(second, "Sprites/b.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "Renderer.UsePremultipliedAlpha is true",
            bleedTransparentEdges: true);

        first.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(new byte[] { 128, 128, 128, 128, 0, 0, 0, 0 });
        _warnings.ShouldBe(new[]
        {
            "Gum package 'Game.gumpkg' stores straight-alpha textures, but Renderer.UsePremultipliedAlpha is true, " +
            "so each texture is converted when it loads (first: 'Sprites/a.png'). " +
            "Repack it with 'gumcli pack --premultiplied-alpha' to skip the conversion.",
        });
    }

    [Fact]
    public void PrepareForRuntime_ShouldUnpremultiplyBleedAndWarnOnce_WhenPremultipliedPackageMeetsStraightRuntime()
    {
        // (128,64,0,128) is premultiplied (255,128,0) at half alpha; the transparent texel gets that color bled in.
        byte[] first = GumRawTexture.Encode(width: 2, height: 1,
            new byte[] { 128, 64, 0, 128, 0, 0, 0, 0 }, isPremultiplied: true);
        byte[] second = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 9, 8, 7, 255 }, isPremultiplied: true);

        GumRawTexture.PrepareForRuntime(first, "Sprites/a.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: false, runtimeAlphaReason: "Renderer.UsePremultipliedAlpha is false",
            bleedTransparentEdges: true);
        GumRawTexture.PrepareForRuntime(second, "Sprites/b.png", "Game.gumpkg",
            runtimeUsesPremultipliedAlpha: false, runtimeAlphaReason: "Renderer.UsePremultipliedAlpha is false",
            bleedTransparentEdges: true);

        first.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(new byte[] { 255, 128, 0, 128, 255, 128, 0, 0 });
        second.AsSpan(GumRawTexture.HeaderLength).ToArray().ShouldBe(new byte[] { 9, 8, 7, 255 });
        _warnings.ShouldBe(new[]
        {
            "Gum package 'Game.gumpkg' stores premultiplied-alpha textures, but Renderer.UsePremultipliedAlpha is false, " +
            "so each texture is converted when it loads (first: 'Sprites/a.png'). " +
            "Repack it with 'gumcli pack' without --premultiplied-alpha to skip the conversion.",
        });
    }

    [Fact]
    public void PrepareForRuntime_ShouldWriteTheWarningOnceToConsoleAndTrace_ByDefault()
    {
        byte[] first = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 1, 1, 1, 128 }, isPremultiplied: false);
        byte[] second = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 1, 1, 1, 128 }, isPremultiplied: false);
        System.IO.StringWriter console = new System.IO.StringWriter();
        System.IO.TextWriter previousConsole = Console.Out;
        System.IO.StringWriter trace = new System.IO.StringWriter();
        System.Diagnostics.TextWriterTraceListener listener = new System.Diagnostics.TextWriterTraceListener(trace);
        GumRawTexture.ReportWarning = _previousReportWarning;
        Console.SetOut(console);
        System.Diagnostics.Trace.Listeners.Add(listener);
        try
        {
            GumRawTexture.PrepareForRuntime(first, "Sprites/a.png", "Default.gumpkg",
                runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false);
            GumRawTexture.PrepareForRuntime(second, "Sprites/b.png", "Default.gumpkg",
                runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false);
            listener.Flush();
        }
        finally
        {
            System.Diagnostics.Trace.Listeners.Remove(listener);
            Console.SetOut(previousConsole);
        }

        string expected = "Gum package 'Default.gumpkg' stores straight-alpha textures, but the test runtime, " +
            "so each texture is converted when it loads (first: 'Sprites/a.png'). " +
            "Repack it with 'gumcli pack --premultiplied-alpha' to skip the conversion." + Environment.NewLine;
        console.ToString().ShouldBe(expected);
        trace.ToString().ShouldBe(expected);
    }

    [Fact]
    public void PrepareForRuntime_ShouldWarnOncePerPackage_ForEachPackage()
    {
        byte[] fromA = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 1, 1, 1, 255 }, isPremultiplied: false);
        byte[] fromB = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 1, 1, 1, 255 }, isPremultiplied: false);

        GumRawTexture.PrepareForRuntime(fromA, "a.png", "A.gumpkg", runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false);
        GumRawTexture.PrepareForRuntime(fromB, "a.png", "B.gumpkg", runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false);

        _warnings.Count.ShouldBe(2);
    }

    [Fact]
    public void ReadForRuntime_ShouldReturnNullAndRewind_ForAnEncodedImage()
    {
        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        using System.IO.MemoryStream stream = new System.IO.MemoryStream(png);

        byte[]? result = GumRawTexture.ReadForRuntime(stream, "a.png",
            runtimeUsesPremultipliedAlpha: false, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false, out _);

        result.ShouldBeNull();
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void ReadForRuntime_ShouldReturnOnlyTheConvertedPixels_FromANonSeekableStream()
    {
        byte[] entry = GumRawTexture.Encode(width: 2, height: 1, new byte[] { 255, 128, 0, 128, 9, 8, 7, 255 }, isPremultiplied: false);
        using NonSeekableStream stream = new NonSeekableStream(entry);

        byte[]? pixels = GumRawTexture.ReadForRuntime(GumRawTexture.EnsureSeekable(stream), "a.png",
            runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false,
            out GumRawTextureHeader header);

        pixels.ShouldBe(new byte[] { 128, 64, 0, 128, 9, 8, 7, 255 });
        header.Width.ShouldBe(2);
        header.Height.ShouldBe(1);
        header.IsPremultiplied.ShouldBeFalse();
        _warnings.Count.ShouldBe(1);
        Should.Throw<ArgumentException>(() => GumRawTexture.ReadForRuntime(stream, "a.png",
            runtimeUsesPremultipliedAlpha: true, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false, out _));
    }

    [Theory]
    [InlineData(-1, "truncated")]
    [InlineData(1, "longer than its size")]
    public void ReadForRuntime_ShouldThrow_WhenPixelDataLengthDoesNotMatchSize(int extraBytes, string expectedMessage)
    {
        byte[] entry = GumRawTexture.Encode(width: 1, height: 1, new byte[] { 1, 2, 3, 4 }, isPremultiplied: false);
        byte[] resized = new byte[entry.Length + extraBytes];
        Array.Copy(entry, resized, Math.Min(entry.Length, resized.Length));
        using System.IO.MemoryStream stream = new System.IO.MemoryStream(resized);

        GumBundleFormatException exception = Should.Throw<GumBundleFormatException>(() => GumRawTexture.ReadForRuntime(stream, "a.png",
            runtimeUsesPremultipliedAlpha: false, runtimeAlphaReason: "the test runtime", bleedTransparentEdges: false, out _));

        exception.Message.ShouldContain(expectedMessage);
    }

    private sealed class NonSeekableStream : System.IO.MemoryStream
    {
        public NonSeekableStream(byte[] buffer) : base(buffer) { }

        public override bool CanSeek => false;
    }

    [Fact]
    public void Unpremultiply_ShouldRoundToNearest_AndLeaveTransparentAndOpaqueTexelsAlone()
    {
        byte[] rgba = { 1, 0, 0, 1, 127, 50, 1, 127, 9, 8, 7, 255, 5, 5, 5, 0 };

        global::RenderingLibrary.Content.RgbaPixels.Unpremultiply(rgba);

        rgba.ShouldBe(new byte[] { 255, 0, 0, 1, 255, 100, 2, 127, 9, 8, 7, 255, 5, 5, 5, 0 });
    }
}
