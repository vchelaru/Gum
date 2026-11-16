using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #8153: same-texture NineSlices drawn as one DrawVertices batch on a GPU canvas, with non-overlapping text
/// drawn after the batch, give the same bytes as drawing every NineSlice with its own lattice.
/// </summary>
[Collection(GpuCollection.Name)]
public class NineSliceVertexBatchGpuTests : IDisposable
{
    const int Size = 220;

    readonly WglGpuContext _gpu;
    readonly SKBitmap _textureA;
    readonly SKBitmap _textureB;
    readonly SKFilterMode _previousFilter;

    public NineSliceVertexBatchGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
        _previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        _textureA = Texture(new SKColor(200, 40, 40), new SKColor(40, 40, 200, 150));
        _textureB = Texture(new SKColor(40, 180, 40, 120), new SKColor(230, 230, 40));
    }

    public void Dispose()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = _previousFilter;
        _textureA.Dispose();
        _textureB.Dispose();
    }

    // 12x12, a 4-texel border around a semi-transparent or opaque middle.
    static SKBitmap Texture(SKColor border, SKColor middle)
    {
        SKBitmap bitmap = new(12, 12);
        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                bool isBorder = x < 4 || y < 4 || x > 7 || y > 7;
                SKColor color = isBorder ? border : middle;
                bitmap.SetPixel(x, y, color);
            }
        }
        return bitmap;
    }

    static NineSliceRuntime Frame(SKBitmap texture, float x, float y, float width, float height) => new()
    {
        Texture = texture,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
    };

    static TextRuntime Label(string text, float x, float y, float width = 40, float height = 12, float fontSize = 14) => new()
    {
        Text = text,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        Color = SKColors.White,
        FontSize = fontSize,
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

    // Draws three frames (text draws its cached blobs from the second frame on, and only those defer) and returns
    // the last frame's pixels and draw calls.
    (byte[] Pixels, int DrawCalls) Render(Action<ContainerRuntime, SystemManagers> build, bool batch, Action<int>? beforeFrame)
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

            ContainerRuntime root = Container(0, 0, Size, Size);
            build(root, managers);
            root.AddToManagers(managers, null);

            for (int frame = 0; frame < 3; frame++)
            {
                beforeFrame?.Invoke(frame);
                surface.Canvas.Clear(SKColors.Black);
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

    // Returns the batched and unbatched draw calls after asserting both draw the same bytes.
    (int Batched, int Unbatched) AssertBatchedMatchesUnbatched(Action<ContainerRuntime> build) =>
        AssertBatchedMatchesUnbatched((root, _) => build(root));

    // beforeFrame runs before each of the three frames, given the frame index.
    (int Batched, int Unbatched) AssertBatchedMatchesUnbatched(Action<ContainerRuntime, SystemManagers> build, Action<int>? beforeFrame = null)
    {
        (byte[] batchedPixels, int batched) = Render(build, batch: true, beforeFrame);
        (byte[] unbatchedPixels, int unbatched) = Render(build, batch: false, beforeFrame);
        CountDifferentBytes(batchedPixels, unbatchedPixels).ShouldBe(0);
        return (batched, unbatched);
    }

    [Fact]
    public void Draw_FiveSameTextureNineSlices_RecordOneDrawCall()
    {
        (int batched, int unbatched) = AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 5; i++)
            {
                root.AddChild(Frame(_textureA, 10 + i * 40, 10, 36, 30));
            }
        });

        unbatched.ShouldBe(5);
        batched.ShouldBe(1);
    }

    [Fact]
    public void Draw_NineSliceAlone_MatchesLattice()
    {
        (int batched, int unbatched) = AssertBatchedMatchesUnbatched(root => root.AddChild(Frame(_textureA, 17, 23, 61, 47)));

        batched.ShouldBe(1);
        unbatched.ShouldBe(1);
    }

    [Fact]
    public void Draw_TwoTexturesInterleaved_KeepsOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 6; i++)
            {
                root.AddChild(Frame(i % 2 == 0 ? _textureA : _textureB, 10 + i * 20, 10 + i * 10, 60, 50));
            }
        });
    }

    [Fact]
    public void Draw_TextBetweenNineSlices_DrawsAfterTheBatchWhereNothingOverlaps()
    {
        (int batched, int unbatched) = AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 40, 100, 36));
                root.AddChild(Label("Item " + i, 16, 18 + i * 40));
            }
        });

        // Four frames in one batch plus four texts, against eight separate draws.
        unbatched.ShouldBe(8);
        batched.ShouldBe(5);
    }

    [Fact]
    public void Draw_OverlappingSiblings_KeepsZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 80));
            root.AddChild(Label("Over A", 20, 20, 120, 40));
            // Covers part of the text, so it cannot join the batch the first frame is in.
            root.AddChild(Frame(_textureA, 50, 30, 80, 80));
            root.AddChild(Label("Over B", 60, 60));
            root.AddChild(Frame(_textureA, 150, 150, 40, 40));
        });
    }

    [Fact]
    public void Draw_TextOverflowingItsCell_StaysUnderTheNextCell()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 30, 100, 26));
                // Layout height 8, but the wrapped lines spill into the next cell's frame.
                root.AddChild(Label("one two three four five six", 14, 14 + i * 30, 40, 8));
            }
        });
    }

    [Fact]
    public void Draw_TextOutlineReachingTheNextCell_StaysUnderIt()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            // The descenders' glyph bounds stay inside their own row; only the thick outline reaches the next
            // row's frame.
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 24, 120, 20));
                TextRuntime label = Label("gyp qjg", 20, 12 + i * 24, 110, 14);
                label.OutlineThickness = 8;
                root.AddChild(label);
            }
        });
    }

    [Fact]
    public void Draw_TextShadowReachingTheNextCell_StaysUnderIt()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 26, 120, 22));
                TextRuntime label = Label("Shadowed", 14, 12 + i * 26, 110, 18);
                label.HasDropshadow = true;
                label.DropshadowOffsetX = 2;
                label.DropshadowOffsetY = 14;
                label.DropshadowBlurX = 3;
                label.DropshadowBlurY = 3;
                label.DropshadowColor = SKColors.Lime;
                root.AddChild(label);
            }
        });
    }

    [Fact]
    public void Draw_RotatedItemsInTheWalk_KeepZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 60));
            NineSliceRuntime rotatedFrame = Frame(_textureA, 60, 40, 80, 60);
            rotatedFrame.Rotation = 30;
            root.AddChild(rotatedFrame);
            TextRuntime rotatedText = Label("Rotated text", 20, 30, 120, 20);
            rotatedText.Rotation = -20;
            root.AddChild(rotatedText);
            root.AddChild(Frame(_textureA, 40, 50, 60, 60));
            ContainerRuntime rotatedParent = Container(120, 120, 80, 80);
            rotatedParent.Rotation = 15;
            rotatedParent.AddChild(Frame(_textureA, 0, 0, 60, 40));
            rotatedParent.AddChild(Label("Inside", 4, 4));
            root.AddChild(rotatedParent);
            root.AddChild(Frame(_textureA, 130, 130, 50, 50));
        });
    }

    [Fact]
    public void Draw_NestedClips_KeepZOrderInsideEachClip()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            ContainerRuntime outer = Container(5, 5, 150, 150, clips: true);
            outer.AddChild(Frame(_textureA, 0, 0, 100, 40));
            outer.AddChild(Label("outer text spills", 4, 4, 160, 30));
            ContainerRuntime inner = Container(20, 20, 90, 90, clips: true);
            inner.AddChild(Frame(_textureA, 2, 2, 60, 60));
            inner.AddChild(Label("inner text overflows the clip", 10, 10, 200, 20));
            inner.AddChild(Frame(_textureA, 30, 5, 50, 30));
            outer.AddChild(inner);
            // After the inner clip, overlapping it.
            outer.AddChild(Frame(_textureA, 60, 60, 80, 80));
            root.AddChild(outer);
            root.AddChild(Frame(_textureA, 100, 100, 90, 90));
        });
    }

    [Fact]
    public void GumBatch_HostDrawBetweenNineSlices_LandsBetweenThem()
    {
        SKColor underSecond = default;
        SKColor overFirst = default;
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            NineSliceRuntime first = Frame(_textureB, 10, 10, 60, 60);
            NineSliceRuntime second = Frame(_textureB, 40, 40, 60, 60);
            using SKPaint blue = new() { Color = SKColors.Blue };

            surface.Canvas.Clear(SKColors.Black);
            RenderingLibrary.Graphics.Renderer renderer = managers.Renderer;
            renderer.Begin();
            renderer.Draw(first);
            surface.Canvas.DrawRect(30, 30, 40, 40, blue);
            renderer.Draw(second);
            renderer.End();
            context.Flush(true, true);

            using SKBitmap bitmap = new SKBitmap(Size, Size);
            surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
            // (35, 35) is inside the first frame's border and the blue rectangle, outside the second frame.
            overFirst = bitmap.GetPixel(35, 35);
            // (42, 60) is in the second frame's left border, semi-transparent green over the blue.
            underSecond = bitmap.GetPixel(42, 60);
        });

        overFirst.ShouldBe(SKColors.Blue);
        underSecond.Blue.ShouldBeGreaterThan((byte)0);
        underSecond.Green.ShouldBeGreaterThan((byte)0);
    }

    [Fact]
    public void Draw_RenderTargetBetweenNineSlices_KeepsZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 80));
            ContainerRuntime target = Container(40, 40, 100, 100);
            target.IsRenderTarget = true;
            target.AddChild(Frame(_textureA, 0, 0, 60, 60));
            target.AddChild(Label("baked", 4, 4));
            target.AddChild(Frame(_textureB, 30, 30, 60, 60));
            root.AddChild(target);
            root.AddChild(Frame(_textureA, 80, 80, 80, 80));
            root.AddChild(Frame(_textureA, 150, 10, 60, 60));
        });
    }
    [Fact]
    public void Draw_OtherTextureOverPendingBatchAndDeferredText_KeepsZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 80));
            root.AddChild(Label("Deferred", 20, 20, 100, 30));
            root.AddChild(Frame(_textureB, 15, 15, 80, 80));
            root.AddChild(Frame(_textureA, 120, 10, 80, 80));
            root.AddChild(Label("Second", 125, 20, 100, 30));
            root.AddChild(Frame(_textureB, 110, 15, 80, 80));
        });
    }

    [Fact]
    public void Draw_SpriteAndShapesAfterPendingBatch_KeepZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 80));
            root.AddChild(Label("t", 20, 20));
            root.AddChild(new SpriteRuntime
            {
                Texture = _textureB, X = 30, Y = 30, Width = 60, Height = 60,
                WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
            });
            root.AddChild(Frame(_textureA, 50, 50, 80, 80));
            root.AddChild(new ColoredRectangleRuntime
            {
                X = 70, Y = 70, Width = 60, Height = 60, Color = new SKColor(0, 255, 255, 128),
                WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
            });
            root.AddChild(Frame(_textureA, 100, 100, 80, 80));
            root.AddChild(new ColoredCircleRuntime
            {
                X = 120, Y = 120, Width = 60, Height = 60, Color = SKColors.Magenta,
                WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
            });
        });
    }

    [Fact]
    public void Draw_BoldItalicGlyphsOverhangingIntoNextCell_StayUnderIt()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 5; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 30, 150, 26));
                root.AddChild(Label("[IsItalic=true][IsBold=true]fjWfjW[/IsBold][/IsItalic]", 12, 4 + i * 30, 150, 8, fontSize: 40));
            }
        });
    }

    [Fact]
    public void Draw_BBCodeLargerFontRunIntoNextCell_StaysUnderIt()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            for (int i = 0; i < 5; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 30, 150, 26));
                root.AddChild(Label("a [FontSize=60]Big[/FontSize] gj", 12, 12 + i * 30, 150, 8));
            }
        });
    }

    [Fact]
    public void Draw_ChildrenOutsideNineSliceParent_KeepZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            NineSliceRuntime parent = Frame(_textureA, 10, 10, 60, 60);
            parent.AddChild(Label("child text far out", 70, 70, 120, 20));
            parent.AddChild(Frame(_textureA, 80, 60, 60, 60));
            root.AddChild(parent);
            root.AddChild(Frame(_textureA, 60, 60, 60, 60));
        });
    }

    [Fact]
    public void Draw_ClipEndingWhileBatchPending_KeepsZOrder()
    {
        AssertBatchedMatchesUnbatched(root =>
        {
            root.AddChild(Frame(_textureA, 5, 5, 60, 60));
            ContainerRuntime clip = Container(20, 20, 60, 60, clips: true);
            clip.AddChild(Frame(_textureA, 0, 0, 100, 100));
            clip.AddChild(Label("clipped text spills far", 5, 5, 200, 40));
            root.AddChild(clip);
            root.AddChild(Label("after clip", 30, 30, 120, 20));
            root.AddChild(Frame(_textureA, 50, 50, 100, 100));
        });
    }

    [Fact]
    public void Draw_TextureSwappedBetweenFrames_DrawsTheNewTexture()
    {
        NineSliceRuntime? first = null;
        NineSliceRuntime? second = null;
        AssertBatchedMatchesUnbatched((root, _) =>
        {
            first = Frame(_textureA, 10, 10, 80, 80);
            second = Frame(_textureA, 50, 50, 80, 80);
            root.AddChild(first);
            root.AddChild(Label("x", 60, 60));
            root.AddChild(second);
        }, frame =>
        {
            if (frame == 1)
            {
                first!.Texture = _textureB;
            }
            if (frame == 2)
            {
                second!.Texture = _textureB;
                first!.Texture = _textureA;
            }
        });
    }

    [Fact]
    public void GumBatch_NineSliceUnderCallerMatrixThenOutsideCycle_MatchesLattice()
    {
        byte[] Draw(bool batch)
        {
            byte[] result = Array.Empty<byte>();
            _gpu.WithSurface(Size, Size, (surface, context) =>
            {
                GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
                SystemManagers managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = surface.Canvas;
                managers.Renderer.BatchesNineSlices = batch;
                NineSliceRuntime frame = Frame(_textureB, 10, 10, 60, 60);
                surface.Canvas.Clear(SKColors.Black);
                managers.Renderer.Begin(SKMatrix.CreateScale(1.5f, 1.25f));
                managers.Renderer.Draw(frame);
                managers.Renderer.End();
                managers.Renderer.Draw(frame);
                context.Flush(true, true);
                using SKBitmap bitmap = new SKBitmap(Size, Size);
                surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
                result = bitmap.Bytes;
            });
            return result;
        }

        CountDifferentBytes(Draw(true), Draw(false)).ShouldBe(0);
    }

    [Fact]
    public void Draw_TwoOverlappingLayers_KeepZOrder()
    {
        AssertBatchedMatchesUnbatched((root, managers) =>
        {
            root.AddChild(Frame(_textureA, 10, 10, 80, 80));
            root.AddChild(Label("under layer", 20, 20, 100, 20));
            RenderingLibrary.Graphics.Layer top = managers.Renderer.AddLayer();
            NineSliceRuntime overlay = Frame(_textureA, 30, 30, 80, 80);
            overlay.AddToManagers(managers, top);
            TextRuntime overlayText = Label("layer text", 35, 35, 100, 20);
            overlayText.AddToManagers(managers, top);
            root.AddChild(Frame(_textureA, 60, 60, 80, 80));
        });
    }
    [Fact]
    public void Draw_StaticScene_ReusesBatchVerticesWithoutAllocating()
    {
        int builds = -1;
        long allocated = -1;
        int movedBuilds = -1;
        _gpu.WithSurface(Size, Size, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            ContainerRuntime root = Container(0, 0, Size, Size);
            for (int i = 0; i < 4; i++)
            {
                root.AddChild(Frame(_textureA, 10, 10 + i * 40, 100, 36));
                root.AddChild(Label("Item " + i, 16, 18 + i * 40));
            }
            ContainerRuntime clip = Container(120, 10, 90, 90, clips: true);
            clip.AddChild(Frame(_textureB, 4, 4, 60, 60));
            root.AddChild(clip);
            root.AddToManagers(managers, null);
            RenderingLibrary.Graphics.NineSliceVertexBatch batch = managers.Renderer.NineSliceBatch;

            for (int frame = 0; frame < 5; frame++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            int buildsBefore = batch.VerticesBuildCount;
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 50; frame++)
            {
                managers.Renderer.NotifyHostFrameAdvanced();
                managers.Draw();
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            builds = batch.VerticesBuildCount - buildsBefore;

            root.Y = 3;
            managers.Renderer.NotifyHostFrameAdvanced();
            managers.Draw();
            movedBuilds = batch.VerticesBuildCount - buildsBefore;
            context.Flush(true, true);
            root.RemoveFromManagers();
        });

        builds.ShouldBe(0);
        allocated.ShouldBe(0);
        // Moving the scene changes both batches' vertices, so both are rebuilt.
        movedBuilds.ShouldBe(2);
    }
}
