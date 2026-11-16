using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Text drawn by the Renderer skips the clip the walk already put on the canvas and, unrotated and unclipped by
/// anything else, draws its cached blobs at its offset with no Save/Translate/Restore (#8148). Every path must
/// give the same bytes as Text clipping itself, which is what a Text drawn outside any walk still does.
/// </summary>
public class TextWalkClipTests
{
    const int SceneSize = 200;
    const int Frames = 3;

    public static IEnumerable<object[]> WalkCases() => new[]
    {
        new object[] { "Unclipped", 1f },
        new object[] { "Unclipped", 1.5f },
        new object[] { "BbCodeColors", 1f },
        new object[] { "Outline", 1.5f },
        new object[] { "ParentClips", 1f },
        new object[] { "ParentClips", 1.5f },
        new object[] { "NestedClips", 1f },
        new object[] { "OwnClip", 1f },
        new object[] { "RotatedText", 1f },
        new object[] { "RotatedClipper", 1f },
        new object[] { "RotatedClipper", 1.5f },
    };

    // Builds a scene whose only drawing is the text; the containers are invisible renderables.
    static (IRenderableIpso Root, Text Text) CreateScene(string kind)
    {
        InvisibleRenderable root = new() { Width = SceneSize, Height = SceneSize };
        Text text = new()
        {
            FontSize = 18,
            Width = 300,
            Height = 60,
            X = 3.37f,
            Y = 5.61f,
            RawText = "WWW overflowing text",
        };
        IRenderableIpso parent = root;
        switch (kind)
        {
            case "BbCodeColors":
                text.RawText = "[Color=Red]Red[/Color] [Color=Green]Green[/Color] [Color=Blue]Blue[/Color]";
                break;
            case "Outline":
                text.OutlineThickness = 2;
                break;
            case "ParentClips":
                parent = AddClipper(root, 10.3f, 20.7f, 60, 40, 0);
                break;
            case "NestedClips":
                parent = AddClipper(AddClipper(root, 10.3f, 20.7f, 90, 50, 0), 20.2f, 4.4f, 80, 80, 0);
                break;
            case "OwnClip":
                text.Width = 45.5f;
                text.Height = 15.25f;
                text.ClipsChildren = true;
                break;
            case "RotatedText":
                parent = AddClipper(root, 10.3f, 20.7f, 120, 80, 0);
                text.Rotation = 17;
                break;
            case "RotatedClipper":
                parent = AddClipper(root, 30.3f, 20.7f, 80, 40, 10);
                break;
        }
        text.Parent = parent;
        return (root, text);
    }

    static InvisibleRenderable AddClipper(IRenderableIpso parent, float x, float y, float width, float height, float rotation)
    {
        InvisibleRenderable clipper = new() { X = x, Y = y, Width = width, Height = height, Rotation = rotation, ClipsChildren = true };
        clipper.Parent = parent;
        return clipper;
    }

    static byte[] Snapshot(SKSurface surface)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    // What the walk drew before #8148: every clip the walk applies (the text's clipping ancestors and the text
    // itself when it clips), then Text.Render outside any walk, which clips itself and translates the canvas.
    static byte[] RenderReference(string kind, float scale, bool nearestClipOnly = false)
    {
        (IRenderableIpso _, Text text) = CreateScene(kind);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(SceneSize * 2, SceneSize * 2));
        surface.Canvas.Scale(scale);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        List<IRenderableIpso> clippers = new();
        for (IRenderableIpso? current = text; current != null; current = current.Parent)
        {
            if (current.ClipsChildren)
            {
                clippers.Insert(0, current);
                if (nearestClipOnly)
                {
                    break;
                }
            }
        }
        for (int i = 0; i < Frames; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.Save();
            foreach (IRenderableIpso clipper in clippers)
            {
                surface.Canvas.ClipTo(clipper);
            }
            text.Render(managers);
            surface.Canvas.Restore();
        }
        return Snapshot(surface);
    }

    static int DifferingBytes(byte[] actual, byte[] expected)
    {
        actual.Length.ShouldBe(expected.Length);
        int count = 0;
        for (int i = 0; i < actual.Length; i++)
        {
            if (actual[i] != expected[i])
            {
                count++;
            }
        }
        return count;
    }

    static SystemManagers CreateManagers(SKSurface surface, float scale)
    {
        surface.Canvas.Scale(scale);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        managers.Initialize();
        return managers;
    }

    [Theory]
    [MemberData(nameof(WalkCases))]
    public void Draw_TextInHierarchyWalk_MatchesTextClippingItself(string kind, float scale)
    {
        byte[] reference = RenderReference(kind, scale);
        (IRenderableIpso root, Text _) = CreateScene(kind);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(SceneSize * 2, SceneSize * 2));
        SystemManagers managers = CreateManagers(surface, scale);

        for (int i = 0; i < Frames; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            managers.Renderer.Draw(new List<IRenderableIpso> { root }, managers);
        }

        reference.Any(b => b != 0).ShouldBeTrue();
        DifferingBytes(Snapshot(surface), reference).ShouldBe(0);
    }

    [Theory]
    [InlineData("ParentClips", false)]
    [InlineData("ParentClips", true)]
    [InlineData("NestedClips", false)]
    [InlineData("RotatedClipper", true)]
    [InlineData("Unclipped", false)]
    public void GumBatchDraw_TextOrItsRoot_MatchesTextClippingItself(string kind, bool drawRoot)
    {
        // Drawing the text alone never walks its ancestors, so only the text's own (nearest) clip applies.
        byte[] reference = RenderReference(kind, 1, nearestClipOnly: !drawRoot);
        (IRenderableIpso root, Text text) = CreateScene(kind);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(SceneSize * 2, SceneSize * 2));
        SystemManagers managers = CreateManagers(surface, 1);

        for (int i = 0; i < Frames; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            managers.Renderer.Begin();
            managers.Renderer.Draw(drawRoot ? root : text);
            managers.Renderer.End();
        }

        DifferingBytes(Snapshot(surface), reference).ShouldBe(0);
    }

    [Theory]
    [InlineData("Unclipped")]
    [InlineData("ParentClips")]
    [InlineData("NestedClips")]
    public void Draw_TextInsideRenderTarget_MatchesTheSameSceneWithoutRenderTarget(string kind)
    {
        byte[] Render(bool isRenderTarget)
        {
            (IRenderableIpso root, Text _) = CreateScene(kind);
            // A clipping ancestor above the render target: the bake's offscreen canvas carries none of its clip.
            InvisibleRenderable outerClip = new() { X = 0, Y = 0, Width = 70, Height = 45, ClipsChildren = true };
            InvisibleRenderable target = new() { X = 0, Y = 0, Width = SceneSize, Height = SceneSize, IsRenderTarget = isRenderTarget };
            target.Parent = outerClip;
            root.Parent = target;
            using SKSurface surface = SKSurface.Create(new SKImageInfo(SceneSize * 2, SceneSize * 2));
            SystemManagers managers = CreateManagers(surface, 1);
            for (int i = 0; i < Frames; i++)
            {
                surface.Canvas.Clear(SKColors.Transparent);
                managers.Renderer.Draw(new List<IRenderableIpso> { outerClip }, managers);
            }
            return Snapshot(surface);
        }

        byte[] live = Render(isRenderTarget: false);
        live.Any(b => b != 0).ShouldBeTrue();
        DifferingBytes(Render(isRenderTarget: true), live).ShouldBe(0);
    }

    [Fact]
    public void Draw_UnrotatedUnclippedTextInWalk_DrawsItsBlobWithNoOtherCanvasCall()
    {
        int RecordedOperations(bool withText)
        {
            InvisibleRenderable root = new() { Width = SceneSize, Height = SceneSize };
            if (withText)
            {
                Text text = new() { FontSize = 18, Width = 300, Height = 60, X = 3.37f, Y = 5.61f, RawText = "42" };
                text.Parent = root;
            }
            using SKSurface surface = SKSurface.Create(new SKImageInfo(SceneSize, SceneSize));
            SystemManagers managers = CreateManagers(surface, 1);
            List<IRenderableIpso> scene = new() { root };
            // The first frame paints through RichTextKit and the second builds the cached blobs.
            managers.Renderer.Draw(scene, managers);
            managers.Renderer.Draw(scene, managers);

            using SKPictureRecorder recorder = new();
            managers.Canvas = recorder.BeginRecording(new SKRect(0, 0, SceneSize, SceneSize));
            managers.Renderer.Draw(scene, managers);
            using SKPicture picture = recorder.EndRecording();
            return picture.ApproximateOperationCount;
        }

        int textOperations = RecordedOperations(withText: true) - RecordedOperations(withText: false);

        textOperations.ShouldBe(1, "because the text draws its one cached blob at its offset, with no Save, Translate or Restore");
    }
}
