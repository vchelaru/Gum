using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

// SPIKE #8116 (sort3): every sorting/batching mode must draw the same bytes as the plain walk wherever draws
// overlap. Modes: 1 shared BatchKeyGroupedOrderer copy, 2 SkiaRunSorter; each with and without the NineSlice
// DrawVertices merge, plus the NSBATCH text deferral alone.
[Collection("SpikeSort")]
public class SpikeSortZOrderTests : IDisposable
{
    const int Size = 200;
    readonly SKSurface _surface;
    readonly SKBitmap _texture;
    static bool Fuse = true; // run sorter pre-renders in its own walk (GUM_SORT_FUSE)

    public SpikeSortZOrderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(Size, Size));
        GumService.Default.Initialize(_surface.Canvas, Size, Size);
        _texture = new SKBitmap(12, 12);
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
                _texture.SetPixel(x, y, (x < 4 || y < 4 || x > 7 || y > 7) ? new SKColor(200, 40, 40) : new SKColor(40, 40, 200, 180));
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        RenderingLibrary.Graphics.Renderer.SortMode = 0;
        NineSliceBatcher.Enabled = false;
        NineSliceBatcher.DeferText = true;
        _surface.Dispose();
        _texture.Dispose();
    }

    NineSliceRuntime Frame(float x, float y, float w, float h) => new()
    {
        Texture = _texture, X = x, Y = y, Width = w, Height = h,
        WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
    };

    static TextRuntime Label(string text, float x, float y, float w = 40, float h = 12) => new()
    {
        Text = text, X = x, Y = y, Width = w, Height = h,
        WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        Color = SKColors.White,
    };

    byte[] Draw(int sortMode, bool nsBatch, bool deferText)
    {
        RenderingLibrary.Graphics.Renderer.SortMode = sortMode;
        RenderingLibrary.Graphics.SkiaRunSorter.Fuse = Fuse;
        NineSliceBatcher.Enabled = nsBatch;
        NineSliceBatcher.DeferText = deferText;
        _surface.Canvas.Clear(SKColors.Black);
        GumService.Default.Draw();
        _surface.Canvas.Flush();
        using SKImage image = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    static int Diff(byte[] a, byte[] b) { int n = 0; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) n++; return n; }

    void AssertAllModesMatchWalk()
    {
        byte[] walk = Draw(0, false, true);
// The DrawVertices merge itself is not byte-identical to lattices on the raster backend (semi-transparent        // texels; it is on the BRUTAL GPU capture), so merged modes are compared with the unsorted merged walk.        byte[] merged = Draw(0, true, false);        string report = $"orderer {Diff(Draw(1, false, true), walk)}, runSorter {Diff(Draw(2, false, true), walk)}, deferral {Diff(Draw(0, true, true), merged)}, orderer+merge {Diff(Draw(1, true, false), merged)}, runSorter+merge {Diff(Draw(2, true, false), merged)}";        report.ShouldBe("orderer 0, runSorter 0, deferral 0, orderer+merge 0, runSorter+merge 0");
    }

    [Fact]
    public void Draw_OverlappingSiblings_KeepsZOrder()
    {
        var root = GumService.Default.Root;
        root.Children.Add(Frame(10, 10, 80, 80));
        root.Children.Add(Label("Over A", 20, 20, 120, 40));
        root.Children.Add(Frame(50, 30, 80, 80));   // covers part of the text
        root.Children.Add(Label("Over B", 60, 60));
        root.Children.Add(Frame(120, 120, 40, 40)); // free to batch with the first two
        AssertAllModesMatchWalk();
    }

    [Fact]
    public void Draw_TextOverflowingItsCell_StaysUnderTheNextCell()
    {
        var root = GumService.Default.Root;
        for (int i = 0; i < 3; i++)
        {
            root.Children.Add(Frame(10, 10 + i * 30, 100, 26));
            // Layout height 8, but three wrapped lines spill into the next cell's frame.
            root.Children.Add(Label("one two three four five six", 14, 14 + i * 30, 40, 8));
        }
        AssertAllModesMatchWalk();
// Negative control: the shared orderer with only layout bounds moves the spilled text above the next frame.        RenderingLibrary.Graphics.Renderer.SortHook = "off";        try { Diff(Draw(1, false, true), Draw(0, false, true)).ShouldBeGreaterThan(0); }        finally { RenderingLibrary.Graphics.Renderer.SortHook = null; }
    }

    [Fact]
    public void Draw_NestedClips_KeepZOrderInsideEachClip()
    {
        var outer = new ContainerRuntime
        {
            X = 5, Y = 5, Width = 150, Height = 150, ClipsChildren = true,
            WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        };
        outer.AddChild(Frame(0, 0, 100, 40));
        outer.AddChild(Label("outer text spills", 4, 4, 160, 30));
        var inner = new ContainerRuntime
        {
            X = 20, Y = 20, Width = 90, Height = 90, ClipsChildren = true,
            WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
        };
        inner.AddChild(Frame(0, 0, 60, 60));
        inner.AddChild(Label("inner text overflows the clip", 10, 10, 200, 20));
        inner.AddChild(Frame(30, 5, 60, 30));
        outer.AddChild(inner);
        outer.AddChild(Frame(60, 60, 80, 80)); // after the inner clip, overlapping it
        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.Children.Add(Frame(100, 100, 90, 90)); // outside the outer clip's window
        AssertAllModesMatchWalk();
    }
}
