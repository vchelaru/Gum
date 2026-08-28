using System;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// KernSmith's default rasterizer backend (FreeType) is native code and cannot run on browser-wasm.
/// A consumer who forgets to pass <see cref="KernSmith.RasterizerBackend.StbTrueType"/> explicitly
/// used to hit whatever opaque failure mode KernSmith itself produced on wasm (an uncaught exception,
/// per a real BlazorGL repro, or a silent fallback to Gum's built-in font per the docs) -- see the
/// try/catch added around <c>TextRuntime.RegenerateOversampledFont</c>'s <c>TryCreateFont</c> calls for
/// the crash half of that bug. This covers the other half: fail fast with an actionable message instead
/// of leaving the diagnosis to KernSmith. <see cref="GumFontGenerator.IsBrowserPlatform"/> is a test
/// seam for <see cref="OperatingSystem.IsBrowser"/>, since a normal desktop test run is never actually
/// browser-wasm.
/// </summary>
public class GumFontGeneratorBrowserBackendTests : IDisposable
{
    private readonly Func<bool> _previousIsBrowserPlatform = GumFontGenerator.IsBrowserPlatform;

    public void Dispose() => GumFontGenerator.IsBrowserPlatform = _previousIsBrowserPlatform;

    [Fact]
    public void Generate_OnBrowserWithNoExplicitBackend_ThrowsActionableException()
    {
        GumFontGenerator.IsBrowserPlatform = () => true;
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 18, Ranges = "65" };

        PlatformNotSupportedException exception = Should.Throw<PlatformNotSupportedException>(
            () => GumFontGenerator.Generate(bmfcSave));

        exception.Message.ShouldContain("StbTrueType");
    }

    [Fact]
    public void Generate_OnBrowserWithExplicitStbTrueTypeBackend_DoesNotThrowForMissingBackend()
    {
        GumFontGenerator.IsBrowserPlatform = () => true;
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 18, Ranges = "65" };

        // StbTrueType itself isn't registered in this desktop test process, so generation still fails --
        // but it must fail with KernSmith's own error, not our "no backend chosen" guard, proving the
        // guard only fires when backend is null.
        Exception exception = Record.Exception(
            () => GumFontGenerator.Generate(bmfcSave, KernSmith.RasterizerBackend.StbTrueType));

        (exception is PlatformNotSupportedException).ShouldBeFalse(
            "because an explicit backend must skip the browser-wasm guard entirely");
    }
}
