using Gum;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>An in-memory stand-in for the OpenGL or Vulkan backend: raster surfaces as targets, calls recorded.</summary>
internal sealed class FakeBrutalGpuBackend : IBrutalGpuBackend
{
    public List<FakeBrutalTarget> Targets { get; } = new();
    public List<(FakeBrutalTarget Target, int Width, int Height)> Presents { get; } = new();
    public bool Disposed { get; private set; }

    /// <summary>Reads whether the host's queue lock is held; set by the queue-lock tests and recorded at each WaitForGpu, target Dispose and backend Dispose.</summary>
    public Func<bool>? IsLockHeld { get; set; }
    public List<bool> WaitForGpuLockStates { get; } = new();
    public bool? DisposedWhileLocked { get; private set; }

    /// <summary>Stand-ins for SkiaGameRendering taking the host lock in Begin and releasing it in End.</summary>
    public Action? OnTargetBegin { get; set; }
    public Action? OnTargetEnd { get; set; }

    public IBrutalTarget CreateTarget(int width, int height)
    {
        FakeBrutalTarget target = new FakeBrutalTarget(width, height) { IsLockHeld = IsLockHeld, OnBegin = OnTargetBegin, OnEnd = OnTargetEnd };
        Targets.Add(target);
        return target;
    }

    public int WaitForGpuCount { get; private set; }

    public void WaitForGpu()
    {
        WaitForGpuCount++;
        WaitForGpuLockStates.Add(IsLockHeld?.Invoke() ?? false);
    }

    public void Present(IBrutalTarget target, int width, int height) => Presents.Add(((FakeBrutalTarget)target, width, height));

    public void Dispose()
    {
        Disposed = true;
        DisposedWhileLocked = IsLockHeld?.Invoke() ?? false;
    }
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
    public Func<bool>? IsLockHeld { get; init; }
    public Action? OnBegin { get; init; }
    public Action? OnEnd { get; init; }
    public bool? DisposedWhileLocked { get; private set; }

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
        OnBegin?.Invoke();
        _surface.Canvas.Clear(SKColors.Transparent);
    }

    public void End()
    {
        EndCount++;
        OnEnd?.Invoke();
    }

    public void EndWithoutDrawing() => OnEnd?.Invoke();

    public void Dispose()
    {
        Disposed = true;
        DisposedWhileLocked = IsLockHeld?.Invoke() ?? false;
        _surface.Dispose();
    }
}
