using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins issue #6154: with <see cref="Renderer.UsePremultipliedAlpha"/> on, a texture loaded from a
/// file is premultiplied and the edge bleed does not run; with it off, loading is unchanged.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PremultipliedTextureLoadTests : BaseTestClass
{
    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        Renderer.BleedTransparentTextureEdgesOnLoad = true;
        base.Dispose();
    }

    [Fact]
    public void LoadedPng_ShouldBePremultiplied_WhenSwitchOn_AndBleedShouldNotRun()
    {
        Renderer.UsePremultipliedAlpha = true;
        Renderer.BleedTransparentTextureEdgesOnLoad = true;

        XnaColor[] pixels = LoadTwoPixelPng(
            new XnaColor((byte)255, (byte)0, (byte)0, (byte)128),
            new XnaColor((byte)255, (byte)0, (byte)0, (byte)0));

        pixels[0].ShouldBe(new XnaColor((byte)128, (byte)0, (byte)0, (byte)128));
        // The bleed would copy the neighbor's color into this texel, leaving color above alpha.
        pixels[1].ShouldBe(new XnaColor((byte)0, (byte)0, (byte)0, (byte)0));
    }

    [Fact]
    public void LoadedPng_ShouldBeUnchanged_WhenSwitchOff()
    {
        Renderer.UsePremultipliedAlpha = false;
        Renderer.BleedTransparentTextureEdgesOnLoad = false;

        XnaColor[] pixels = LoadTwoPixelPng(
            new XnaColor((byte)255, (byte)0, (byte)0, (byte)128),
            new XnaColor((byte)255, (byte)0, (byte)0, (byte)0));

        pixels[0].R.ShouldBe((byte)255);
        pixels[0].A.ShouldBe((byte)128);
    }

    [Fact]
    public void LoadedPng_ShouldStillBleed_WhenSwitchOff()
    {
        Renderer.UsePremultipliedAlpha = false;
        Renderer.BleedTransparentTextureEdgesOnLoad = true;

        XnaColor[] pixels = LoadTwoPixelPng(
            new XnaColor((byte)255, (byte)255, (byte)255, (byte)255),
            new XnaColor((byte)0, (byte)0, (byte)0, (byte)0));

        pixels[1].R.ShouldBe((byte)255);
        pixels[1].A.ShouldBe((byte)0);
    }

    private static XnaColor[] LoadTwoPixelPng(XnaColor first, XnaColor second)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        string pngPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".png");
        try
        {
            using (Texture2D source = new(game.GraphicsDevice, 2, 1, false, SurfaceFormat.Color))
            {
                source.SetData(new[] { first, second });
                using System.IO.FileStream fs = System.IO.File.Create(pngPath);
                source.SaveAsPng(fs, 2, 1);
            }

            Texture2D loaded = LoaderManager.Self.LoadContent<Texture2D>(pngPath);
            XnaColor[] pixels = new XnaColor[2];
            loaded.GetData(pixels);
            return pixels;
        }
        finally
        {
            try { System.IO.File.Delete(pngPath); } catch { /* best effort */ }
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override Microsoft.Xna.Framework.Color ClearColor => XnaColor.CornflowerBlue;

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
