using System.Collections.Generic;
using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// What a BBCode <see cref="Text"/> with MaxLettersToShow draws on MonoGame. Each case renders the text and
/// compares every pixel with a control whose markup holds only the letters that should be visible (tags are
/// not letters, an explicit newline counts as one), so the expectation is the spec, not a recorded image.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextMaxLettersToShowBbCodeDrawTests : BaseTestClass
{
    private const int CaptureWidth = 200;
    private const int CaptureHeight = 120;

    [Theory]
    [InlineData("ab[Color=Red]cdef[/Color]gh", 0, "")]
    [InlineData("ab[Color=Red]cdef[/Color]gh", 1, "a")]
    [InlineData("ab[Color=Red]cdef[/Color]gh", 3, "ab[Color=Red]c[/Color]")]
    [InlineData("ab[Color=Red]cdef[/Color]gh", 6, "ab[Color=Red]cdef[/Color]")]
    [InlineData("ab[Color=Red]cdef[/Color]gh", 7, "ab[Color=Red]cdef[/Color]g")]
    [InlineData("[Color=Red]ab[/Color][Color=Blue]cd[/Color]", 3, "[Color=Red]ab[/Color][Color=Blue]c[/Color]")]
    [InlineData("[Color=Green][FontScale=1.5]abcd[/FontScale]ef[/Color]", 5, "[Color=Green][FontScale=1.5]abcd[/FontScale]e[/Color]")]
    [InlineData("a[Glyph=0x42]cd", 2, "aB")]
    [InlineData("[Color=Red]a[Glyph=0x42]c[/Color]d", 3, "[Color=Red]aBc[/Color]")]
    public void Draw_StyledSingleLine_ShowsOnlyTheFirstLetters(string markup, int count, string visibleMarkup)
    {
        AssertDrawsLikeControl(markup, count, visibleMarkup);
    }

    [Theory]
    [InlineData("[Color=Red]ab[/Color]c\ndef", 5, "[Color=Red]ab[/Color]c\nd")]
    [InlineData("[Color=Red]ab[/Color]c\ndef\nghi", 9, "[Color=Red]ab[/Color]c\ndef\ng")]
    public void Draw_StyledFirstLineThenPlainLines_ShowsOnlyTheFirstLetters(string markup, int count, string visibleMarkup)
    {
        AssertDrawsLikeControl(markup, count, visibleMarkup);
    }

    // The line before the styled one has no tags, so it takes the untagged branch of the inline draw.
    [Theory]
    [InlineData("abc\n[Color=Red]def[/Color]", 2, "ab")]
    [InlineData("abc\n[Color=Red]def[/Color]", 5, "abc\n[Color=Red]d[/Color]")]
    [InlineData("abc\nxyz\n[Color=Red]def[/Color]", 9, "abc\nxyz\n[Color=Red]d[/Color]")]
    public void Draw_PlainFirstLineThenStyledLine_ShowsOnlyTheFirstLetters(string markup, int count, string visibleMarkup)
    {
        AssertDrawsLikeControl(markup, count, visibleMarkup);
    }

    [Theory]
    [InlineData("ab[Color=Red]cd[/Color]")]
    [InlineData("abc\n[Color=Red]def[/Color]")]
    public void Draw_StyledText_DoesNotChangeMaxLettersToShow(string markup)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        TextRuntime text = new();
        text.X = 10;
        text.Y = 10;
        text.Text = markup;
        text.MaxLettersToShow = 5;
        text.AddToManagers(SystemManagers.Default, null);
        try
        {
            text.UpdateLayout();
            Render(game.GraphicsDevice, SystemManagers.Default);

            text.MaxLettersToShow.ShouldBe(5);
            ((Text)text.RenderableComponent!).MaxLettersToShow.ShouldBe(5);
        }
        finally
        {
            text.RemoveFromManagers();
        }
    }

    private static void AssertDrawsLikeControl(string markup, int count, string visibleMarkup)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        XnaColor[] actual = RenderText(game.GraphicsDevice, markup, count);
        XnaColor[] control = RenderText(game.GraphicsDevice, visibleMarkup, null);

        int actualInk = InkCount(actual);
        if (visibleMarkup.Length == 0)
        {
            actualInk.ShouldBe(0, "because zero letters are shown");
        }
        else
        {
            InkCount(control).ShouldBeGreaterThan(0, "because the control draws something");
        }

        int differing = actual.Zip(control).Count(pair => pair.First != pair.Second);
        differing.ShouldBe(0, $"MaxLettersToShow={count} on '{markup}' should draw like '{visibleMarkup}'");
    }

    private static XnaColor[] RenderText(GraphicsDevice gd, string markup, int? maxLetters)
    {
        SystemManagers managers = SystemManagers.Default;
        TextRuntime text = new();
        text.X = 10;
        text.Y = 10;
        text.Text = markup;
        text.MaxLettersToShow = maxLetters;
        text.AddToManagers(managers, null);
        try
        {
            text.UpdateLayout();
            return Render(gd, managers);
        }
        finally
        {
            text.RemoveFromManagers();
        }
    }

    private static int InkCount(IEnumerable<XnaColor> pixels) =>
        pixels.Count(p => p.R > 0 || p.G > 0 || p.B > 0);

    private static XnaColor[] Render(GraphicsDevice gd, SystemManagers managers)
    {
        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        // Two frames, so a draw that changes state for the next one shows up.
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return data;
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
