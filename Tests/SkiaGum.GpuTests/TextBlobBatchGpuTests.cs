using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #8156: consecutive plain texts drawn as one merged text blob on a GPU canvas give the same bytes as drawing every
/// text on its own, and a static scene reuses its merged blobs without allocating.
/// </summary>
[Collection(GpuCollection.Name)]
public class TextBlobBatchGpuTests : IDisposable
{
    const int Size = 220;

    // Text draws its cached blobs from the second frame on, and a run merges once it has matched that many flushes.
    const int MergedFrames = RenderingLibrary.Graphics.TextBlobBatch.StillFlushesBeforeMerge + 2;

    readonly WglGpuContext _gpu;
    readonly SKBitmap _texture;
    readonly SKFilterMode _previousFilter;

    public TextBlobBatchGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
        _previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        _texture = new SKBitmap(12, 12);
        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                bool isBorder = x < 4 || y < 4 || x > 7 || y > 7;
                _texture.SetPixel(x, y, isBorder ? new SKColor(200, 40, 40) : new SKColor(40, 40, 200, 150));
            }
        }
    }

    public void Dispose()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = _previousFilter;
        _texture.Dispose();
    }

    NineSliceRuntime Frame(float x, float y, float width, float height) => new()
    {
        Texture = _texture,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
    };

    static TextRuntime Label(string text, float x, float y, SKColor? color = null) => new()
    {
        Text = text,
        X = x,
        Y = y,
        Width = 120,
        Height = 14,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        Color = color ?? SKColors.White,
        FontSize = 14,
    };

    static ContainerRuntime Container(float x, float y, float width, float height, bool clips = false) => new()
    {
        X = x,
        Y = y,
        Width = width,
        Height = height,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        ClipsChildren = clips,
    };

    // Draws `frames` frames and returns
    // the last frame's pixels and draw calls. `batch` turns both the text and the NineSlice batches on or off.
    (byte[] Pixels, int DrawCalls) Render(Action<ContainerRuntime> build, bool batch, Action<int>? beforeFrame, int frames, float zoom)
    {
        byte[] pixels = Array.Empty<byte>();
        int drawCalls = 0;
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            managers.Renderer.BatchesNineSlices = batch;
            managers.Renderer.BatchesTexts = batch;
            managers.Renderer.Camera.Zoom = zoom;

            ContainerRuntime root = Container(0, 0, Size, Size);
            build(root);
            root.AddToManagers(managers, null);

            for (int frame = 0; frame < frames; frame++)
            {
                beforeFrame?.Invoke(frame);
                surface.Canvas.Clear(SKColors.Black);
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            drawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
            context.Flush(true, true);

            using SKBitmap bitmap = new SKBitmap(Size, Size);
            surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
            pixels = bitmap.Bytes;
            root.RemoveFromManagers();
        });
        return (pixels, drawCalls);
    }

    static int CountDifferentBytes(byte[] a, byte[] b)
    {
        a.Length.ShouldBe(b.Length);
        int count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                count++;
            }
        }
        return count;
    }

    // Returns the merged and unmerged draw calls of the last frame after asserting both draw the same bytes.
    (int Merged, int Unmerged) AssertMergedMatchesUnmerged(Action<ContainerRuntime> build, Action<int>? beforeFrame = null, int frames = MergedFrames, float zoom = 1)
    {
        (byte[] mergedPixels, int merged) = Render(build, batch: true, beforeFrame, frames, zoom);
        (byte[] unmergedPixels, int unmerged) = Render(build, batch: false, beforeFrame, frames, zoom);
        CountDifferentBytes(mergedPixels, unmergedPixels).ShouldBe(0);
        return (merged, unmerged);
    }

    [Fact]
    public void Draw_TenPlainTexts_RecordOneDrawCall()
    {
        (int merged, int unmerged) = AssertMergedMatchesUnmerged(root =>
        {
            for (int i = 0; i < 10; i++)
            {
                root.AddChild(Label("Item " + i, 10 + (i % 2) * 100, 10 + i * 18));
            }
        });

        unmerged.ShouldBe(10);
        merged.ShouldBe(1);
    }

    [Fact]
    public void Draw_TextsOverNineSliceRows_MergeAfterTheBatch()
    {
        (int merged, int unmerged) = AssertMergedMatchesUnmerged(root =>
        {
            for (int i = 0; i < 6; i++)
            {
                root.AddChild(Frame(10, 10 + i * 30, 150, 26));
                root.AddChild(Label("Row " + i, 16, 16 + i * 30));
            }
        });

        unmerged.ShouldBe(12);
        // One DrawVertices for the frames, one merged blob for the labels.
        merged.ShouldBe(2);
    }

    [Fact]
    public void Draw_ScrollingRun_MatchesEveryFrame()
    {
        ContainerRuntime? list = null;
        AssertMergedMatchesUnmerged(root =>
        {
            list = Container(0, 0, Size, 400);
            for (int i = 0; i < 20; i++)
            {
                list.AddChild(Label("Scrolled " + i, 10, i * 16));
            }
            root.AddChild(list);
        }, frame => list!.Y = -frame * 7.25f, frames: 6);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.37f)]
    public void Draw_FreeFloatingScrollingRun_MatchesEveryFrame(float zoom)
    {
        ContainerRuntime? list = null;
        AssertMergedMatchesUnmerged(root =>
        {
            list = Container(0.3f, 0, Size, 400);
            for (int i = 0; i < 12; i++)
            {
                TextRuntime label = Label("Floating " + i, 10.6f + i * 0.17f, i * 15.4f);
                label.RenderableComponent.ShouldBeOfType<global::SkiaGum.Text>().OverrideTextRenderingPositionMode =
                    global::SkiaGum.TextRenderingPositionMode.FreeFloating;
                list.AddChild(label);
            }
            root.AddChild(list);
        }, frame => list!.Y = -frame * 3.31f, frames: 6, zoom: zoom);
    }

    [Fact]
    public void Draw_ZoomedSnappedRun_Matches()
    {
        AssertMergedMatchesUnmerged(root =>
        {
            for (int i = 0; i < 10; i++)
            {
                root.AddChild(Label("Zoomed " + i, 3.3f + i, 4 + i * 11.7f));
            }
        }, zoom: 1.37f);
    }

    [Fact]
    public void Draw_TextChangedMidRun_DrawsTheNewText()
    {
        TextRuntime? changing = null;
        AssertMergedMatchesUnmerged(root =>
        {
            for (int i = 0; i < 8; i++)
            {
                TextRuntime label = Label("Line " + i, 10, 10 + i * 18);
                if (i == 3)
                {
                    changing = label;
                }
                root.AddChild(label);
            }
        }, frame =>
        {
            if (frame == MergedFrames)
            {
                changing!.Text = "Changed text";
            }
            if (frame == MergedFrames + 2)
            {
                changing!.Text = "Again";
            }
        }, frames: 2 * MergedFrames + 2);
    }

    [Fact]
    public void Draw_MixedColorsAndStyles_KeepOrderAndColors()
    {
        (int merged, int unmerged) = AssertMergedMatchesUnmerged(root =>
        {
            root.AddChild(Label("white a", 10, 10));
            root.AddChild(Label("white b", 10, 26));
            root.AddChild(Label("red a", 10, 42, SKColors.Red));
            root.AddChild(Label("red b", 10, 58, SKColors.Red));
            TextRuntime outlined = Label("outlined", 10, 74);
            outlined.OutlineThickness = 2;
            root.AddChild(outlined);
            root.AddChild(Label("[Color=Lime]lime[/Color] white", 10, 90));
            root.AddChild(Label("translucent", 10, 106, new SKColor(255, 255, 255, 120)));
            root.AddChild(Label("white c", 10, 122));
            root.AddChild(Label("white d", 10, 138));
        });

        unmerged.ShouldBe(9);
        // white a+b, red a+b, outlined, BBCode two colors, translucent, white c+d.
        merged.ShouldBe(6);
    }

    [Fact]
    public void Draw_OverlappingTextsAndNineSliceBetween_KeepZOrder()
    {
        AssertMergedMatchesUnmerged(root =>
        {
            root.AddChild(Label("under under", 10, 10));
            root.AddChild(Label("over over", 14, 13, SKColors.Red));
            root.AddChild(Label("third", 12, 15));
            root.AddChild(Frame(20, 8, 60, 40));
            root.AddChild(Label("on the frame", 22, 12));
            root.AddChild(Label("also on it", 30, 20));
            root.AddChild(Frame(25, 15, 60, 40));
            root.AddChild(Label("last", 30, 25));
        });
    }

    [Fact]
    public void Draw_NestedClips_KeepTextInsideEachClip()
    {
        AssertMergedMatchesUnmerged(root =>
        {
            root.AddChild(Label("before the clip spills", 2, 2));
            ContainerRuntime outer = Container(10, 20, 150, 150, clips: true);
            outer.AddChild(Label("outer text spills far to the right", 4, 4));
            outer.AddChild(Label("outer two", 4, 20));
            ContainerRuntime inner = Container(20, 40, 60, 60, clips: true);
            inner.AddChild(Label("inner text overflows the clip", 2, 2));
            inner.AddChild(Label("inner two overflows", 2, 18));
            outer.AddChild(inner);
            outer.AddChild(Label("after inner", 4, 110));
            root.AddChild(outer);
            root.AddChild(Label("after outer", 10, 190));
        });
    }

    [Fact]
    public void Draw_StaticScene_ReusesMergedBlobsWithoutAllocating()
    {
        int builds = -1;
        long allocated = -1;
        int changedBuilds = -1;
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            ContainerRuntime root = Container(0, 0, Size, Size);
            TextRuntime? first = null;
            for (int i = 0; i < 6; i++)
            {
                root.AddChild(Frame(10, 10 + i * 30, 150, 26));
                TextRuntime label = Label("Row " + i, 16, 16 + i * 30);
                first ??= label;
                root.AddChild(label);
            }
            ContainerRuntime clip = Container(170, 10, 40, 100, clips: true);
            clip.AddChild(Label("a", 0, 0));
            clip.AddChild(Label("b", 0, 20));
            root.AddChild(clip);
            root.AddToManagers(managers, null);
            RenderingLibrary.Graphics.TextBlobBatch batch = managers.Renderer.TextBatch;

            for (int frame = 0; frame < MergedFrames; frame++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            int buildsBefore = batch.MergedBuildCount;
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 50; frame++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            builds = batch.MergedBuildCount - buildsBefore;

            // A changed text paints through RichTextKit its first frame, then draws new blobs, so its run rebuilds
            // once; the clip's run is untouched.
            first!.Text = "Changed";
            for (int frame = 0; frame < MergedFrames; frame++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            changedBuilds = batch.MergedBuildCount - buildsBefore;
            context.Flush(true, true);
            root.RemoveFromManagers();
        });

        builds.ShouldBe(0);
        allocated.ShouldBe(0);
        // The changed run draws each text on its own while it changes, then merges once it has matched for 30 flushes.
        changedBuilds.ShouldBe(1);
    }

    [Fact]
    public void Draw_SteadyScroll_DrawsMovingTextsSinglyWithoutAllocating()
    {
        int scrollDrawCalls = -1;
        int scrollBuilds = -1;
        long allocated = -1;
        int stoppedDrawCalls = -1;
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            ContainerRuntime root = Container(0, 0, Size, Size);
            // Rows 20.7 px apart snap to whole pixels on their own, so their offsets relative to the first row change
            // as the list scrolls.
            ContainerRuntime list = Container(0, 0, 120, 400);
            for (int i = 0; i < 8; i++)
            {
                list.AddChild(Label("Row " + i, 4, i * 20.7f));
            }
            root.AddChild(list);
            root.AddChild(Frame(130, 10, 80, 80));
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Label("Static " + i, 130, 100 + i * 18));
            }
            root.AddToManagers(managers, null);
            RenderingLibrary.Graphics.TextBlobBatch batch = managers.Renderer.TextBatch;
            int frame = 0;
            void DrawFrame()
            {
                list.Y = -frame * 0.37f;
                frame++;
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }

            // Long enough for the static run to merge.
            for (int i = 0; i < MergedFrames; i++)
            {
                DrawFrame();
            }
            int buildsBefore = batch.MergedBuildCount;
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 50; i++)
            {
                DrawFrame();
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            scrollBuilds = batch.MergedBuildCount - buildsBefore;
            scrollDrawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;

            // Stopped: the list merges again once it has held still for 30 flushes.
            frame--;
            for (int i = 0; i < MergedFrames; i++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            stoppedDrawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
            context.Flush(true, true);
            root.RemoveFromManagers();
        });

        // 8 moving rows drawn singly, one NineSlice draw, the static run merged into one.
        scrollDrawCalls.ShouldBe(8 + 1 + 1);
        scrollBuilds.ShouldBe(0);
        allocated.ShouldBe(0);
        stoppedDrawCalls.ShouldBe(1 + 1 + 1);
    }

    // Draws eight rows 20 px apart in one run; drawFrames gets the managers, the fourth row and the batch.
    void WithEightRows(Action<SystemManagers, TextRuntime, RenderingLibrary.Graphics.TextBlobBatch> drawFrames)
    {
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            ContainerRuntime root = Container(0, 0, Size, Size);
            TextRuntime? moving = null;
            for (int i = 0; i < 8; i++)
            {
                TextRuntime label = Label("Row " + i, 4, i * 20);
                if (i == 3)
                {
                    moving = label;
                }
                root.AddChild(label);
            }
            root.AddToManagers(managers, null);
            drawFrames(managers, moving!, managers.Renderer.TextBatch);
            context.Flush(true, true);
            root.RemoveFromManagers();
        });
    }

    static void DrawFrame(SystemManagers managers)
    {
        managers.Renderer.NotifyHostFrameAdvanced();
        managers.Draw();
    }

    [Fact]
    public void Draw_RunChangingEveryTenFlushes_StaysUnmergedWithoutAllocating()
    {
        int builds = -1;
        long allocated = -1;
        int drawCalls = -1;
        WithEightRows((managers, moving, batch) =>
        {
            // A row moves 1 px every 10 frames, the pace of a list scrolling slower than a pixel a frame.
            void DrawFrames(int count)
            {
                for (int frame = 0; frame < count; frame++)
                {
                    if (frame % 10 == 0)
                    {
                        moving.Y = moving.Y == 60 ? 61 : 60;
                    }
                    DrawFrame(managers);
                }
            }

            DrawFrames(40);
            int buildsBefore = batch.MergedBuildCount;
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            DrawFrames(200);
            allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            builds = batch.MergedBuildCount - buildsBefore;
            drawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
        });

        builds.ShouldBe(0);
        allocated.ShouldBe(0);
        drawCalls.ShouldBe(8);
    }

    [Fact]
    public void Draw_RunHeldStillThirtyFlushes_Merges()
    {
        int drawCallsAfter29 = -1;
        int buildsAfter29 = -1;
        int drawCallsAfter30 = -1;
        int buildsAfter30 = -1;
        WithEightRows((managers, moving, batch) =>
        {
            for (int frame = 0; frame < MergedFrames; frame++)
            {
                DrawFrame(managers);
            }
            moving.Y = 61;
            DrawFrame(managers);
            int buildsBefore = batch.MergedBuildCount;
            for (int frame = 0; frame < 29; frame++)
            {
                DrawFrame(managers);
            }
            drawCallsAfter29 = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
            buildsAfter29 = batch.MergedBuildCount - buildsBefore;
            DrawFrame(managers);
            drawCallsAfter30 = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
            buildsAfter30 = batch.MergedBuildCount - buildsBefore;
        });

        drawCallsAfter29.ShouldBe(8);
        buildsAfter29.ShouldBe(0);
        drawCallsAfter30.ShouldBe(1);
        buildsAfter30.ShouldBe(1);
    }

    [Fact]
    public void Draw_HiddenSpriteRotatedTranslucentAndNineSliceInRun_KeepOrder()
    {
        AssertMergedMatchesUnmerged(root =>
        {
            root.AddChild(Label("one one one", 10, 10));
            TextRuntime hidden = Label("HIDDEN", 12, 12, SKColors.Red);
            hidden.Visible = false;
            root.AddChild(hidden);
            root.AddChild(Label("two two two", 12, 14));
            root.AddChild(new SpriteRuntime
            {
                Texture = _texture,
                X = 14,
                Y = 8,
                Width = 30,
                Height = 20,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
            });
            root.AddChild(Label("three over sprite", 14, 12));
            root.AddChild(Label("four", 16, 16));
            TextRuntime rotated = Label("rotated mid", 18, 18);
            rotated.Rotation = 20;
            root.AddChild(rotated);
            root.AddChild(Label("five", 20, 20));
            root.AddChild(Label("translucent", 22, 22, new SKColor(255, 255, 255, 128)));
            root.AddChild(Label("six", 24, 24));
            root.AddChild(Frame(10, 10, 40, 30));
            root.AddChild(Label("seven", 26, 26));
        });
    }
}
