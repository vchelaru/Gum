#if BRUTAL
#nullable enable

using SkiaSharp;
using System;

namespace Gum;

/// <summary>Draws a pass straight into the window's next image (Gum.Brutal.Vulkan's swapchain) and puts it back for presenting.</summary>
internal interface IBrutalDirectPass
{
    /// <summary>The canvas of the pass in progress.</summary>
    SKCanvas Canvas { get; }

    /// <summary>Acquires the window's next image and starts drawing into it.</summary>
    void Begin(int width, int height);

    /// <summary>Ends the pass and leaves the image ready to present.</summary>
    void End();
}

/// <summary>
/// A target that draws straight into the window while the backend presents and the pass is one Draw announced with
/// <see cref="IBrutalGpuBackend.BeginFrame"/>, and otherwise into a texture of Gum's own, created at the first pass that
/// needs it. A capture pass while presenting touches neither and has no canvas. Entering a direct pass frees the texture.
/// </summary>
internal sealed class PresentOrTextureTarget : IBrutalTarget
{
    private readonly Func<bool> _presentToWindow;
    private readonly Func<bool> _takeFrame;
    private readonly IBrutalDirectPass? _direct;
    private readonly Func<IBrutalTarget> _createTexture;
    private readonly Action _waitForGpu;
    private readonly int _width;
    private readonly int _height;
    private bool _capturePass;

    /// <param name="presentToWindow">Reads whether the backend presents.</param>
    /// <param name="takeFrame">Returns, and clears, whether Draw announced this pass.</param>
    /// <param name="direct">The direct pass, or null when the build has none.</param>
    /// <param name="createTexture">Creates Gum's own texture at this target's size.</param>
    /// <param name="waitForGpu">Blocks until frames in flight are done, before the texture is freed.</param>
    /// <param name="width">The target's width in pixels.</param>
    /// <param name="height">The target's height in pixels.</param>
    public PresentOrTextureTarget(
        Func<bool> presentToWindow, Func<bool> takeFrame, IBrutalDirectPass? direct,
        Func<IBrutalTarget> createTexture, Action waitForGpu, int width, int height)
    {
        _presentToWindow = presentToWindow;
        _takeFrame = takeFrame;
        _direct = direct;
        _createTexture = createTexture;
        _waitForGpu = waitForGpu;
        _width = width;
        _height = height;
    }

    /// <summary>Gum's own texture, or null while none exists.</summary>
    public IBrutalTarget? Texture { get; private set; }

    /// <summary>True when the current or last pass drew straight into the window.</summary>
    public bool IsDirectPass { get; private set; }

    // A capture pass while presenting has no image to draw into; Gum gets its canvas at the first drawn pass.
    public SKCanvas Canvas => IsDirectPass ? _direct!.Canvas : _capturePass ? null! : Texture!.Canvas;

    public void Begin()
    {
        bool isFrame = _takeFrame();
        IsDirectPass = false;
        _capturePass = false;
        if (_direct != null && _presentToWindow())
        {
            if (isFrame)
            {
                if (Texture != null)
                {
                    // Back to presenting after compositing: a frame in flight may still sample the texture.
                    _waitForGpu();
                    Texture.Dispose();
                    Texture = null;
                }
                _direct.Begin(_width, _height);
                IsDirectPass = true;
            }
            else
            {
                // The holder's capture pass at create time: acquiring the window's image here would leave it unpresented.
                _capturePass = true;
            }
            return;
        }
        Texture ??= _createTexture();
        Texture.Begin();
    }

    public void End()
    {
        if (IsDirectPass)
        {
            _direct!.End();
        }
        else if (!_capturePass)
        {
            Texture!.End();
        }
    }

    public void EndWithoutDrawing()
    {
        if (IsDirectPass)
        {
            _direct!.End();
        }
        else if (!_capturePass)
        {
            Texture!.EndWithoutDrawing();
        }
    }

    public void Dispose()
    {
        Texture?.Dispose();
        Texture = null;
    }
}
#endif
