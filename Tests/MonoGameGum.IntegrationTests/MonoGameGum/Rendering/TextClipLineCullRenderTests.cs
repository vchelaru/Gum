using System.Linq;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A Text partly inside a clip draws only the lines that reach the clip (#8503). Each scene renders a
/// long scrolled Text through the real Renderer with <see cref="CameraScissorExtensions.CullOffscreenWhenClipped"/>
/// off and on: the pixels must be identical and fewer sprites must be submitted with it on.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextClipLineCullRenderTests : BaseTestClass
{
    private const int CaptureWidth = 320;
    private const int CaptureHeight = 240;

    private static readonly string TestFontPath = System.IO.Path.GetFullPath(
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "SkiaGum.Tests", "Assets", "Fonts", "TestFont.ttf"));

    public enum Scene
    {
        Plain,
        CameraZoom2,
        CameraZoom2FractionalScroll,
        CameraZoomHalfFractionalScroll,
        ScrolledCamera,
        LayerWithItsOwnZoom,
        NestedClips,
        DropShadowCrossingTheClipEdge,
        InsideRenderTargetBake,
        ReplaceAlphaInsideRenderTargetBake,
        MinAlphaInsideRenderTargetBake,
    }

    [Theory]
    [InlineData(Scene.Plain)]
    [InlineData(Scene.CameraZoom2)]
    [InlineData(Scene.CameraZoom2FractionalScroll)]
    [InlineData(Scene.CameraZoomHalfFractionalScroll)]
    [InlineData(Scene.ScrolledCamera)]
    [InlineData(Scene.LayerWithItsOwnZoom)]
    [InlineData(Scene.NestedClips)]
    [InlineData(Scene.DropShadowCrossingTheClipEdge)]
    [InlineData(Scene.InsideRenderTargetBake)]
    [InlineData(Scene.ReplaceAlphaInsideRenderTargetBake)]
    [InlineData(Scene.MinAlphaInsideRenderTargetBake)]
    public void ClippedText_CullingLines_LeavesEveryPixelUnchangedAndSubmitsFewerSprites(Scene scene)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Camera camera = managers.Renderer.Camera;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        Layer? layer = null;
        GraphicalUiElement? root = null;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            if (scene == Scene.CameraZoom2)
            {
                camera.Zoom = 2;
            }
            else if (scene == Scene.CameraZoom2FractionalScroll)
            {
                camera.Zoom = 2;
                camera.X = 7.3f;
                camera.Y = 7.3f;
            }
            else if (scene == Scene.CameraZoomHalfFractionalScroll)
            {
                camera.Zoom = 0.5f;
                camera.X = -20.7f;
                camera.Y = -20.7f;
            }
            else if (scene == Scene.ScrolledCamera)
            {
                camera.X = 13;
                camera.Y = 27;
            }
            else if (scene == Scene.LayerWithItsOwnZoom)
            {
                layer = managers.Renderer.AddLayer();
                layer.LayerCameraSettings = new LayerCameraSettings { Zoom = 1.5f };
            }

            root = BuildScene(scene, out TextRuntime text, lineCount: 200);
            root.AddToManagers(managers, layer);
            root.UpdateLayout();

            if (scene == Scene.DropShadowCrossingTheClipEdge)
            {
                // Draw once so the font (and its shadow variant) is created before checking it.
                Render(game.GraphicsDevice, managers);
                ((Text)text.RenderableComponent!).BitmapFont!.ShadowFont.ShouldNotBeNull();
            }

            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            (XnaColor[] withoutCull, long spritesWithoutCull) = Render(game.GraphicsDevice, managers);
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            (XnaColor[] withCull, long spritesWithCull) = Render(game.GraphicsDevice, managers);

            withoutCull.Any(c => c != XnaColor.Black).ShouldBeTrue("because some text must be inside the clip");
            int firstDifference = FirstDifference(withoutCull, withCull);
            firstDifference.ShouldBe(-1, firstDifference < 0 ? "" :
                $"pixel ({firstDifference % CaptureWidth},{firstDifference / CaptureWidth}) is {withCull[firstDifference]} culled, {withoutCull[firstDifference]} not");
            spritesWithCull.ShouldBeLessThan(spritesWithoutCull / 4);
        }
        finally
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            root?.RemoveFromManagers();
            if (layer != null)
            {
                managers.Renderer.RemoveLayer(layer);
            }
            camera.Zoom = 1;
            camera.X = 0;
            camera.Y = 0;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // The scissor is built without GumBatch's matrix, so it says nothing about where lines land through it.
    [Fact]
    public void ClippedText_DrawnThroughAForcedMatrix_DrawsEveryLine()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            GraphicalUiElement root = BuildScene(Scene.Plain, out _, lineCount: 200);
            root.UpdateLayout();

            long SpritesDrawn(Microsoft.Xna.Framework.Matrix? matrix)
            {
                long before = game.GraphicsDevice.Metrics.SpriteCount;
                renderer.Begin(matrix);
                renderer.Draw(root);
                renderer.End();
                return game.GraphicsDevice.Metrics.SpriteCount - before;
            }

            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            long allLines = SpritesDrawn(Microsoft.Xna.Framework.Matrix.Identity);
            CameraScissorExtensions.CullOffscreenWhenClipped = true;

            SpritesDrawn(Microsoft.Xna.Framework.Matrix.Identity).ShouldBe(allLines);
            SpritesDrawn(null).ShouldBeLessThan(allLines / 4);
        }
        finally
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // #8503's measurement: a 500-line Text with about five lines inside its clip.
    [Fact]
    public void FiveHundredLineText_FewLinesInsideTheClip_SubmitsOnlyThoseLines()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        GraphicalUiElement? root = null;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            root = BuildScene(Scene.Plain, out _, lineCount: 500);
            root.AddToManagers(managers, null);
            root.UpdateLayout();

            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            long withoutCull = Render(game.GraphicsDevice, managers).Sprites;
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            long withCull = Render(game.GraphicsDevice, managers).Sprites;

            withCull.ShouldBeLessThan(withoutCull / 20);
        }
        finally
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            root?.RemoveFromManagers();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private static int FirstDifference(XnaColor[] expected, XnaColor[] actual)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                return i;
            }
        }
        return -1;
    }

    // A clip 60 rows tall showing a few lines of a long Text scrolled up past its top edge.
    private static GraphicalUiElement BuildScene(Scene scene, out TextRuntime text, int lineCount)
    {
        ContainerRuntime clip = new();
        clip.X = 20;
        // Fractional edges, so the scissor's rounding is part of what the pixels check.
        clip.Y = 20.4f;
        clip.Width = 200;
        clip.Height = 60.3f;
        clip.WidthUnits = DimensionUnitType.Absolute;
        clip.HeightUnits = DimensionUnitType.Absolute;
        clip.ClipsChildren = true;

        ContainerRuntime textParent = clip;
        if (scene == Scene.NestedClips)
        {
            ContainerRuntime inner = new();
            inner.Y = 25;
            inner.Width = 200;
            inner.Height = 100;
            inner.WidthUnits = DimensionUnitType.Absolute;
            inner.HeightUnits = DimensionUnitType.Absolute;
            inner.ClipsChildren = true;
            clip.AddChild(inner);
            textParent = inner;
        }

        bool alphaOnly = scene == Scene.ReplaceAlphaInsideRenderTargetBake || scene == Scene.MinAlphaInsideRenderTargetBake;
        if (alphaOnly)
        {
            ColoredRectangleRuntime background = new();
            background.Width = 200;
            background.Height = 60;
            background.Color = new XnaColor(200, 40, 40);
            textParent.AddChild(background);
        }

        text = new TextRuntime();
        text.UseCustomFont = true;
        text.CustomFontFile = TestFontPath;
        text.FontSize = 14;
        text.Width = 190;
        text.WidthUnits = DimensionUnitType.Absolute;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Text = string.Join("\n", Enumerable.Range(0, lineCount).Select(i => "Line " + i + " quick brown"));
        // Scrolled so the clip shows lines from the middle, with lines cut by both clip edges.
        text.Y = -403.5f;
        if (scene == Scene.DropShadowCrossingTheClipEdge)
        {
            text.HasDropshadow = true;
            text.DropshadowOffsetX = 3;
            text.DropshadowOffsetY = 9;
            text.DropshadowColor = new XnaColor(0, 120, 255);
        }
        else if (scene == Scene.ReplaceAlphaInsideRenderTargetBake)
        {
            text.Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha;
        }
        else if (scene == Scene.MinAlphaInsideRenderTargetBake)
        {
            text.Blend = Gum.RenderingLibrary.Blend.MinAlpha;
        }
        textParent.AddChild(text);

        if (scene == Scene.InsideRenderTargetBake || alphaOnly)
        {
            ContainerRuntime renderTarget = new();
            renderTarget.X = 10;
            renderTarget.Y = 10;
            renderTarget.Width = 280;
            renderTarget.Height = 200;
            renderTarget.WidthUnits = DimensionUnitType.Absolute;
            renderTarget.HeightUnits = DimensionUnitType.Absolute;
            renderTarget.IsRenderTarget = true;
            renderTarget.AddChild(clip);
            return renderTarget;
        }

        return clip;
    }

    private static (XnaColor[] Pixels, long Sprites) Render(GraphicsDevice graphicsDevice, SystemManagers managers)
    {
        using RenderTarget2D capture = new(graphicsDevice, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        long sprites = 0;
        // The first draw creates the font; the second is the one measured.
        for (int i = 0; i < 2; i++)
        {
            graphicsDevice.SetRenderTarget(capture);
            graphicsDevice.Clear(XnaColor.Black);
            long before = graphicsDevice.Metrics.SpriteCount;
            managers.Renderer.Draw(managers);
            sprites = graphicsDevice.Metrics.SpriteCount - before;
        }
        graphicsDevice.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return (data, sprites);
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
