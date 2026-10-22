using System;
using System.IO;
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
/// Issue #6419: BBCode-coloured text keeps its parsed colour runs between frames. For each thing that can
/// change those runs (the text, a colour tag, the font, the wrap width, the BBCode markup itself), a text
/// that was drawn and then changed must draw exactly like an identical text that was never drawn before the
/// change, and must differ from its own earlier drawing.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class BbCodeTextRenderCacheTests : BaseTestClass
{
    private const int CaptureWidth = 320;
    private const int CaptureHeight = 160;
    private const string Wrapped =
        "lorem [Color=Red]ipsum dolor[/Color] sit amet consectetur [Color=Blue]adipiscing[/Color] elit sed do";

    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    [Fact]
    public void ChangingTheText_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text => text.Text = "ab[Color=Red]cdef[/Color]ghij",
            mutate: text => text.Text = "[Color=Red]abcd[/Color]efghij klm [Color=Red]n[/Color]");

        frames.Mutated.ShouldBe(frames.Fresh);
        frames.Mutated.ShouldNotBe(frames.Before);
        Count(frames.Mutated, IsRed).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ChangingAColorTag_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text => text.Text = "ab[Color=Red]cdef[/Color]ghij",
            mutate: text => text.Text = "ab[Color=Blue]cdef[/Color]ghij");

        frames.Mutated.ShouldBe(frames.Fresh);
        Count(frames.Before, IsRed).ShouldBeGreaterThan(0);
        Count(frames.Before, IsBlue).ShouldBe(0);
        Count(frames.Mutated, IsRed).ShouldBe(0);
        Count(frames.Mutated, IsBlue).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ChangingTheFont_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text =>
            {
                UseOrbitron(text, 16);
                text.Text = "ab[Color=Red]cdef[/Color]ghij";
            },
            mutate: text => text.FontSize = 28,
            useKernSmith: true);

        frames.Mutated.ShouldBe(frames.Fresh);
        frames.Mutated.ShouldNotBe(frames.Before);
        Count(frames.Mutated, IsRed).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ChangingTheWrapWidth_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text =>
            {
                text.WidthUnits = DimensionUnitType.Absolute;
                text.Width = 120;
                text.HeightUnits = DimensionUnitType.RelativeToChildren;
                text.Text = Wrapped;
            },
            mutate: text => text.Width = 250);

        frames.Mutated.ShouldBe(frames.Fresh);
        frames.Mutated.ShouldNotBe(frames.Before);
        Count(frames.Mutated, IsRed).ShouldBeGreaterThan(0);
        Count(frames.Mutated, IsBlue).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RemovingTheBbCodeMarkup_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text => text.Text = "ab[Color=Red]cdef[/Color]ghij",
            mutate: text => text.Text = "abcdefghij");

        frames.Mutated.ShouldBe(frames.Fresh);
        Count(frames.Before, IsRed).ShouldBeGreaterThan(0);
        Count(frames.Mutated, IsRed).ShouldBe(0);
    }

    [Fact]
    public void AddingBbCodeMarkup_DrawsLikeAFreshText()
    {
        Frames frames = Capture(
            setup: text => text.Text = "abcdefghij",
            mutate: text => text.Text = "ab[Color=Red]cdef[/Color]ghij");

        frames.Mutated.ShouldBe(frames.Fresh);
        Count(frames.Before, IsRed).ShouldBe(0);
        Count(frames.Mutated, IsRed).ShouldBeGreaterThan(0);
    }

    private static void UseOrbitron(TextRuntime text, int size)
    {
        text.UseCustomFont = true;
        text.CustomFontFile = FontPath;
        text.FontSize = size;
    }

    private static bool IsRed(XnaColor pixel) => pixel.R > 200 && pixel.G < 60 && pixel.B < 60;

    private static bool IsBlue(XnaColor pixel) => pixel.B > 200 && pixel.R < 60 && pixel.G < 60;

    private static int Count(XnaColor[] pixels, Func<XnaColor, bool> predicate)
    {
        int count = 0;
        foreach (XnaColor pixel in pixels)
        {
            if (predicate(pixel))
            {
                count++;
            }
        }
        return count;
    }

    private sealed record Frames(XnaColor[] Before, XnaColor[] Mutated, XnaColor[] Fresh);

    /// <summary>
    /// Draws a text built by <paramref name="setup"/> (Before), changes it with <paramref name="mutate"/> and
    /// draws it again (Mutated), then draws a second text that gets the same setup and change but is never
    /// drawn in between (Fresh).
    /// </summary>
    private static Frames Capture(Action<TextRuntime> setup, Action<TextRuntime> mutate, bool useKernSmith = false)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        TextRenderingMode originalMode = Text.TextRenderingMode;
        IInMemoryFontCreator? originalCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        Text.TextRenderingMode = TextRenderingMode.CharacterByCharacter;
        if (useKernSmith)
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
        }

        TextRuntime drawn = new();
        TextRuntime fresh = new();
        try
        {
            drawn.X = 10;
            drawn.Y = 10;
            setup(drawn);
            drawn.AddToManagers(managers, null);
            XnaColor[] before = Render(game.GraphicsDevice, managers, drawn);

            mutate(drawn);
            XnaColor[] mutated = Render(game.GraphicsDevice, managers, drawn);
            drawn.RemoveFromManagers();

            fresh.X = 10;
            fresh.Y = 10;
            setup(fresh);
            mutate(fresh);
            fresh.AddToManagers(managers, null);
            XnaColor[] freshPixels = Render(game.GraphicsDevice, managers, fresh);

            return new Frames(before, mutated, freshPixels);
        }
        finally
        {
            fresh.RemoveFromManagers();
            drawn.RemoveFromManagers();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = originalCreator;
            Text.TextRenderingMode = originalMode;
        }
    }

    private static XnaColor[] Render(GraphicsDevice device, SystemManagers managers, TextRuntime text)
    {
        text.UpdateLayout();

        using RenderTarget2D capture = new(device, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        // Two passes, so the second one draws with whatever the first one cached.
        for (int i = 0; i < 2; i++)
        {
            device.SetRenderTarget(capture);
            device.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        device.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return data;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() =>
            Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
