using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #7585 (and #7213): Gum runs on a straight-alpha pipeline and on a premultiplied one
/// (<see cref="Renderer.UsePremultipliedAlpha"/>). Text and sprites must draw the same pixels in both.
/// The embedded default font atlas used to stay straight alpha on the premultiplied pipeline and drew
/// as solid rectangles. Each test renders on a real GL device over an opaque backdrop and reads the
/// pixels back. The straight-alpha tests pin the behavior that must not change.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class EmbeddedFontAlphaModeRenderTests : BaseTestClass
{
    private const int Width = 96;
    private const int Height = 48;
    private const string TextToDraw = "HOUZ 0123";

    private static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    [Fact]
    public void EmbeddedFontText_Straight_DrawsGlyphsNotASolidBlock()
    {
        TextStats stats = MeasureText(RenderEmbeddedText(premultiplied: false));

        stats.BrightFraction.ShouldBeInRange(0.05f, 0.22f);
    }

    [Fact]
    public void EmbeddedFontText_Premultiplied_DrawsGlyphsNotASolidBlock()
    {
        TextStats stats = MeasureText(RenderEmbeddedText(premultiplied: true));

        stats.BrightFraction.ShouldBeInRange(0.05f, 0.22f);
    }

    [Fact]
    public void EmbeddedFontText_Premultiplied_MatchesStraight()
    {
        XnaColor[] straight = RenderEmbeddedText(premultiplied: false);
        XnaColor[] premultiplied = RenderEmbeddedText(premultiplied: true);

        MaxChannelDifference(straight, premultiplied).ShouldBeLessThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BakedStyleFontText_DrawsGlyphsNotASolidBlock(bool premultiplied)
    {
        // A KernSmith or baked atlas: white glyphs with black transparent texels, premultiplied by the
        // font creator when the pipeline is premultiplied. Must be unaffected by any embedded-font change.
        TextStats stats = MeasureText(RenderBakedStyleText(premultiplied));

        stats.BrightFraction.ShouldBeInRange(0.05f, 0.22f);
    }

    [Fact]
    public void BakedStyleFontText_Premultiplied_MatchesStraight()
    {
        XnaColor[] straight = RenderBakedStyleText(premultiplied: false);
        XnaColor[] premultiplied = RenderBakedStyleText(premultiplied: true);

        MaxChannelDifference(straight, premultiplied).ShouldBeLessThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sprite_HalfAlphaTexture_BlendsOverBackdrop(bool premultiplied)
    {
        // Source (200, 100, 50) at alpha 128 over the backdrop; the texture holds premultiplied
        // data when the pipeline is premultiplied, which is what a loaded texture holds.
        XnaColor[] pixels = Render(premultiplied, (managers, device) =>
        {
            XnaColor source = new((byte)200, (byte)100, (byte)50, (byte)128);
            Texture2D texture = new(device, 4, 4, false, SurfaceFormat.Color);
            XnaColor stored = premultiplied
                ? new XnaColor((byte)100, (byte)50, (byte)25, (byte)128)
                : source;
            texture.SetData(Enumerable.Repeat(stored, 16).ToArray());

            SpriteRuntime sprite = new();
            sprite.Texture = texture;
            sprite.WidthUnits = DimensionUnitType.Absolute;
            sprite.HeightUnits = DimensionUnitType.Absolute;
            sprite.Width = 32;
            sprite.Height = 32;
            sprite.X = 10;
            sprite.Y = 8;
            return sprite;
        });

        XnaColor pixel = pixels[(20 * Width) + 20];
        Math.Abs(pixel.R - 135).ShouldBeLessThanOrEqualTo(3);
        Math.Abs(pixel.G - 85).ShouldBeLessThanOrEqualTo(3);
        Math.Abs(pixel.B - 70).ShouldBeLessThanOrEqualTo(3);
    }

    private static XnaColor[] RenderEmbeddedText(bool premultiplied) =>
        Render(premultiplied, (managers, device) => CreateText());

    private static XnaColor[] RenderBakedStyleText(bool premultiplied) =>
        Render(premultiplied, (managers, device) =>
        {
            using Stream atlasStream = OpenResource("Font18Arial_0.png");
            using Texture2D embedded = Texture2D.FromStream(device, atlasStream);
            XnaColor[] atlas = new XnaColor[embedded.Width * embedded.Height];
            embedded.GetData(atlas);
            for (int i = 0; i < atlas.Length; i++)
            {
                XnaColor c = atlas[i];
                // Baked style: transparent texels are black, glyph texels are white at the glyph alpha.
                byte value = premultiplied ? c.A : (c.A == 0 ? (byte)0 : (byte)255);
                atlas[i] = new XnaColor(value, value, value, c.A);
            }

            Texture2D baked = new(device, embedded.Width, embedded.Height, false, SurfaceFormat.Color);
            baked.SetData(atlas);

            using Stream fntStream = OpenResource("Font18Arial.fnt");
            string fnt = new StreamReader(fntStream).ReadToEnd();

            TextRuntime text = CreateText();
            text.Typeface = new BitmapFont(baked, fnt);
            return text;
        });

    private static TextRuntime CreateText()
    {
        TextRuntime text = new();
        text.X = 4;
        text.Y = 4;
        text.Color = XnaColor.White;
        text.Text = TextToDraw;
        return text;
    }

    private static XnaColor[] Render(bool premultiplied,
        Func<SystemManagers, GraphicsDevice, Gum.Wireframe.GraphicalUiElement> create)
    {
        // The switch must be set before Gum initializes, since the embedded font loads then.
        using MinimalGame game = new(premultiplied);
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = Width;
        root.Height = Height;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest opaque solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Backdrop });
#pragma warning restore CS0618
        root.AddChild(create(managers, gd));
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D target = new(gd, Width, Height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(target);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Width * Height];
        target.GetData(pixels);
        root.RemoveFromManagers();
        return pixels;
    }

    private static TextStats MeasureText(XnaColor[] pixels)
    {
        // Count pixels that moved far from the backdrop inside the text's row band.
        int bright = 0;
        int total = 0;
        for (int y = 4; y < 26; y++)
        {
            for (int x = 4; x < 80; x++)
            {
                XnaColor p = pixels[(y * Width) + x];
                total++;
                if (p.R > 160)
                {
                    bright++;
                }
            }
        }
        return new TextStats(bright / (float)total);
    }

    private static int MaxChannelDifference(XnaColor[] a, XnaColor[] b)
    {
        int max = 0;
        for (int i = 0; i < a.Length; i++)
        {
            max = Math.Max(max, Math.Abs(a[i].R - b[i].R));
            max = Math.Max(max, Math.Abs(a[i].G - b[i].G));
            max = Math.Max(max, Math.Abs(a[i].B - b[i].B));
        }
        return max;
    }

    private static Stream OpenResource(string fileName)
    {
        Assembly assembly = typeof(Text).Assembly;
        string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("." + fileName, StringComparison.Ordinal));
        return assembly.GetManifestResourceStream(name)!;
    }

    private readonly record struct TextStats(float BrightFraction);

    private sealed class MinimalGame : SharedGameLease
    {
        private readonly bool _premultiplied;

        public MinimalGame(bool premultiplied) : base(Width, Height)
        {
            _premultiplied = premultiplied;
        }

        protected override void OnInitialize()
        {
            Renderer.UsePremultipliedAlpha = _premultiplied;
            Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            Renderer.UsePremultipliedAlpha = false;
        }
    }
}
