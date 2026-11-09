using Gum;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>An in-memory stand-in for the OpenGL or Vulkan backend: raster surfaces as targets, calls recorded.</summary>
internal sealed class FakeBrutalGpuBackend : IBrutalGpuBackend
{
    public List<FakeBrutalTarget> Targets { get; } = new();
    public List<(FakeBrutalTarget Target, int Width, int Height)> Presents { get; } = new();
    public bool Disposed { get; private set; }

    public IBrutalTarget CreateTarget(int width, int height)
    {
        FakeBrutalTarget target = new FakeBrutalTarget(width, height);
        Targets.Add(target);
        return target;
    }

    public int WaitForGpuCount { get; private set; }

    public void WaitForGpu() => WaitForGpuCount++;

    public void Present(IBrutalTarget target, int width, int height) => Presents.Add(((FakeBrutalTarget)target, width, height));

    public void Dispose() => Disposed = true;
}

internal sealed class FakeBrutalTarget : IBrutalTarget
{
    private readonly SKSurface _surface;

    public FakeBrutalTarget(int width, int height)
    {
        Width = width;
        Height = height;
        _surface = SKSurface.Create(new SKImageInfo(width, height));
    }

    public int Width { get; }
    public int Height { get; }
    public int BeginCount { get; private set; }
    public int EndCount { get; private set; }
    public bool Disposed { get; private set; }

    public SKCanvas Canvas => _surface.Canvas;

    public SKColor PixelAt(int x, int y)
    {
        using SKBitmap bitmap = new SKBitmap(Width, Height);
        _surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
        return bitmap.GetPixel(x, y);
    }

    public void Begin()
    {
        BeginCount++;
        _surface.Canvas.Clear(SKColors.Transparent);
    }

    public void End() => EndCount++;

    public void EndWithoutDrawing() { }

    public void Dispose()
    {
        Disposed = true;
        _surface.Dispose();
    }
}
