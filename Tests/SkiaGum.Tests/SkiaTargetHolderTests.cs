using SkiaGum.Helpers;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

public class SkiaTargetHolderTests : IDisposable
{
    private class FakeTarget : ISkiaHostTarget
    {
        public readonly SKBitmap Bitmap;
        public readonly SKCanvas Backing;
        public readonly List<string> Calls = new();
        public bool IsDisposed;

        public FakeTarget(int width, int height)
        {
            Bitmap = new SKBitmap(width, height);
            Backing = new SKCanvas(Bitmap);
        }

        public SKCanvas Canvas => Backing;
        public void Begin() => Calls.Add("Begin");
        public void EndWithoutDrawing() => Calls.Add("EndWithoutDrawing");
        public void Dispose()
        {
            IsDisposed = true;
            Backing.Dispose();
            Bitmap.Dispose();
        }
    }

    private readonly List<FakeTarget> _created = new();
    private readonly List<SKCanvas> _appliedCanvases = new();
    private readonly List<(int, int)> _resizes = new();

    private SkiaTargetHolder<FakeTarget> CreateHolder() =>
        new SkiaTargetHolder<FakeTarget>(
            (w, h) =>
            {
                var target = new FakeTarget(w, h);
                _created.Add(target);
                return target;
            },
            canvas => _appliedCanvases.Add(canvas),
            (w, h) => _resizes.Add((w, h)));

    public void Dispose()
    {
        foreach (var target in _created) target.Dispose();
    }

    [Fact]
    public void Create_CapturesCanvasAndSize_WithoutResizeCallback()
    {
        var holder = CreateHolder();

        holder.Create(100, 50).ShouldBeTrue();

        holder.Width.ShouldBe(100);
        holder.Height.ShouldBe(50);
        holder.Canvas.ShouldBeSameAs(_created[0].Backing);
        _created[0].Calls.ShouldBe(new[] { "Begin", "EndWithoutDrawing" });
        _resizes.ShouldBeEmpty();
    }

    [Fact]
    public void Create_NonPositiveSize_CreatesNothing()
    {
        var holder = CreateHolder();

        holder.Create(0, 50).ShouldBeFalse();

        holder.Target.ShouldBeNull();
        _created.ShouldBeEmpty();
    }

    [Fact]
    public void EnsureSize_SizeChanged_RecreatesDisposesOldAndSwapsCanvas()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);

        holder.EnsureSize(200, 80).ShouldBeTrue();

        _created.Count.ShouldBe(2);
        _created[0].IsDisposed.ShouldBeTrue();
        holder.Target.ShouldBeSameAs(_created[1]);
        _appliedCanvases.ShouldBe(new[] { _created[1].Backing });
        _resizes.ShouldBe(new[] { (200, 80) });
        holder.Width.ShouldBe(200);
        holder.Height.ShouldBe(80);
    }

    [Fact]
    public void EnsureSize_SizeUnchanged_DoesNothing()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);

        holder.EnsureSize(100, 50).ShouldBeFalse();

        _created.Count.ShouldBe(1);
        _created[0].IsDisposed.ShouldBeFalse();
        _resizes.ShouldBeEmpty();
        _appliedCanvases.ShouldBeEmpty();
    }

    [Fact]
    public void EnsureSize_SameNewSizeTwice_ResizesOnce()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);

        holder.EnsureSize(200, 80);
        holder.EnsureSize(200, 80);

        _created.Count.ShouldBe(2);
        _resizes.Count.ShouldBe(1);
    }

    [Fact]
    public void EnsureSize_NonPositiveSize_KeepsExistingTarget()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);

        holder.EnsureSize(0, 0).ShouldBeFalse();

        _created.Count.ShouldBe(1);
        _created[0].IsDisposed.ShouldBeFalse();
        _resizes.ShouldBeEmpty();
    }

    [Fact]
    public void BeginFrame_BeginsTargetAndAppliesItsCanvas()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);
        _created[0].Calls.Clear();

        SKCanvas canvas = holder.BeginFrame()!;

        canvas.ShouldBeSameAs(_created[0].Backing);
        _created[0].Calls.ShouldBe(new[] { "Begin" });
        _appliedCanvases.ShouldBe(new[] { _created[0].Backing });
    }

    [Fact]
    public void BeginFrame_BeforeCreate_ReturnsNull()
    {
        CreateHolder().BeginFrame().ShouldBeNull();
    }

    [Fact]
    public void Dispose_DisposesTargetAndClearsIt()
    {
        var holder = CreateHolder();
        holder.Create(100, 50);

        holder.Dispose();

        _created[0].IsDisposed.ShouldBeTrue();
        holder.Target.ShouldBeNull();
        holder.Canvas.ShouldBeNull();
    }
}
