using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6134: a host that draws through <see cref="GumBatch"/> (FlatRedBall2) never adds its
/// roots to a Gum <see cref="Layer"/>, and <see cref="Renderer.Draw(IRenderableIpso)"/> does not
/// bake render targets. <see cref="Renderer.PreRenderRoots"/> is the host's once-per-frame hook
/// that bakes <c>IsRenderTarget</c> containers and the targets that
/// <c>RenderTargetTextureSource</c> sprites reference.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumBatchRenderTargetBakeTests : BaseTestClass
{
    [Fact]
    public void PreRenderRoots_WithRootsNotInAnyLayer_BakesContainerAndReferencingSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime source = CreateRedRenderTargetContainer();
        source.X = 64;
        source.Y = 0;

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = source;

        source.UpdateLayout();
        sprite.UpdateLayout();

        Color[] pixels = DrawFrameThroughGumBatch(
            game, gd, managers, new List<IRenderableIpso> { sprite, source }, preRender: true);

        Color spritePixel = pixels[(32 * 128) + 32];
        Color containerPixel = pixels[(32 * 128) + 96];

        spritePixel.R.ShouldBeGreaterThan((byte)150);
        ((int)spritePixel.R - spritePixel.G).ShouldBeGreaterThan(80);
        containerPixel.R.ShouldBeGreaterThan((byte)150);
        ((int)containerPixel.R - containerPixel.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void PreRenderRoots_CalledOncePerRootAcrossSeveralBeginEndCycles_BakesEveryRoot()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime left = CreateRedRenderTargetContainer();
        left.X = 0;
        ContainerRuntime right = CreateRedRenderTargetContainer();
        right.X = 64;
        left.UpdateLayout();
        right.UpdateLayout();

        GumBatch batch = new();
        Color[] pixels = new Color[128 * 128];
        using RenderTarget2D capture = new(gd, 128, 128, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int frame = 0; frame < 2; frame++)
        {
            managers.Activity(frame + 1.0);

            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);

            renderer.PreRenderRoots(new List<IRenderableIpso> { left });
            renderer.PreRenderRoots(new List<IRenderableIpso> { right });

            batch.Begin();
            batch.Draw(left);
            batch.End();

            batch.Begin();
            batch.Draw(right);
            batch.End();

            gd.SetRenderTarget(null);
        }

        capture.GetData(pixels);

        pixels[(32 * 128) + 32].R.ShouldBeGreaterThan((byte)150);
        pixels[(32 * 128) + 96].R.ShouldBeGreaterThan((byte)150);
    }

    [Fact]
    public void PreRenderRoots_WithInvisibleSourceContainer_BakesItForTheReferencingSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime source = CreateRedRenderTargetContainer();
        source.Visible = false;

        SpriteRuntime sprite = new();
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = source;

        source.UpdateLayout();
        sprite.UpdateLayout();

        Color[] pixels = DrawFrameThroughGumBatch(
            game, gd, managers, new List<IRenderableIpso> { sprite, source }, preRender: true);

        Color spritePixel = pixels[(32 * 128) + 32];
        spritePixel.R.ShouldBeGreaterThan((byte)150);
        ((int)spritePixel.R - spritePixel.G).ShouldBeGreaterThan(80);
    }

    private static Color[] DrawFrameThroughGumBatch(
        MinimalGame game, GraphicsDevice gd, SystemManagers managers, List<IRenderableIpso> roots, bool preRender)
    {
        Renderer renderer = managers.Renderer;
        GumBatch batch = new();
        Color[] pixels = new Color[128 * 128];
        using RenderTarget2D capture = new(gd, 128, 128, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int frame = 0; frame < 2; frame++)
        {
            managers.Activity(frame + 1.0);

            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);

            if (preRender)
            {
                renderer.PreRenderRoots(roots);
            }

            batch.Begin();
            foreach (IRenderableIpso root in roots)
            {
                batch.Draw(root);
            }
            batch.End();

            gd.SetRenderTarget(null);
        }

        capture.GetData(pixels);
        return pixels;
    }

    private static ContainerRuntime CreateRedRenderTargetContainer()
    {
        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        return container;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => global::Gum.GumService.Default.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
