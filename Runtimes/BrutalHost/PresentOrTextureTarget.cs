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
/// <see cref="IBrutalGpuBackend.BeginFrame"/>, and otherwise into textures of Gum's own: as many as the image count, drawn
/// into in turn, one a pass Draw announced, each created at the first pass that needs it. A capture pass uses the current
/// texture without moving on, and while presenting touches neither and has no canvas. Entering a direct pass frees every
/// texture, and a smaller image count frees the extra ones, both after waiting for the GPU.
/// </summary>
internal sealed class PresentOrTextureTarget : IBrutalTarget
{
    private readonly Func<bool> _presentToWindow;
    private readonly Func<int> _imageCount;
    private readonly Func<bool> _takeFrame;
    private readonly IBrutalDirectPass? _direct;
    private readonly Func<IBrutalTarget> _createTexture;
    private readonly Action _waitForGpu;
    private readonly int _width;
    private readonly int _height;
    private readonly IBrutalTarget?[] _textures;
    private int _slot;
    private bool _slotDrawn;
    private bool _capturePass;

    /// <param name="presentToWindow">Reads whether the backend presents.</param>
    /// <param name="imageCount">Reads how many textures Gum draws into in turn.</param>
    /// <param name="takeFrame">Returns, and clears, whether Draw announced this pass.</param>
    /// <param name="direct">The direct pass, or null when the build has none.</param>
    /// <param name="createTexture">Creates Gum's own texture at this target's size.</param>
    /// <param name="waitForGpu">Blocks until frames in flight are done, before a texture is freed.</param>
    /// <param name="width">The target's width in pixels.</param>
    /// <param name="height">The target's height in pixels.</param>
    public PresentOrTextureTarget(
        Func<bool> presentToWindow, Func<int> imageCount, Func<bool> takeFrame, IBrutalDirectPass? direct,
        Func<IBrutalTarget> createTexture, Action waitForGpu, int width, int height)
    {
        _presentToWindow = presentToWindow;
        _imageCount = imageCount;
        _takeFrame = takeFrame;
        _direct = direct;
        _createTexture = createTexture;
        _waitForGpu = waitForGpu;
        _width = width;
        _height = height;
        _textures = new IBrutalTarget?[MaxImageCount];
    }

    /// <summary>The most textures Gum draws into in turn.</summary>
    public const int MaxImageCount = 8;

    /// <summary>Throws, as a property setter would, when <paramref name="value"/> is not a texture count Gum supports.</summary>
    public static void CheckImageCount(int value)
    {
        if (value < 1 || value > MaxImageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"RenderTargetImageCount must be from 1 to {MaxImageCount}.");
        }
    }

    /// <summary>The texture of Gum's own the last pass drew into, or null while none exists.</summary>
    public IBrutalTarget? Texture => _textures[_slot];

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
                if (HasTexturesFrom(0))
                {
                    // Back to presenting after compositing: a frame in flight may still sample a texture.
                    _waitForGpu();
                    FreeTexturesFrom(0);
                    _slot = 0;
                    _slotDrawn = false;
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
        if (isFrame)
        {
            int count = _imageCount();
            if (HasTexturesFrom(count))
            {
                // Fewer images: a frame in flight may still sample one of the extra textures.
                _waitForGpu();
                FreeTexturesFrom(count);
            }
            if (_slotDrawn)
            {
                _slot = (_slot + 1) % count;
            }
            _slotDrawn = true;
        }
        IBrutalTarget texture = _textures[_slot] ??= _createTexture();
        texture.Begin();
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
        FreeTexturesFrom(0);
    }

    private bool HasTexturesFrom(int first)
    {
        for (int i = first; i < _textures.Length; i++)
        {
            if (_textures[i] != null)
            {
                return true;
            }
        }
        return false;
    }

    private void FreeTexturesFrom(int first)
    {
        for (int i = first; i < _textures.Length; i++)
        {
            _textures[i]?.Dispose();
            _textures[i] = null;
        }
    }
}
#endif
