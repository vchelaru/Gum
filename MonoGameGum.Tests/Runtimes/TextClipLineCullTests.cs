using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A Text with an active clip draws only the lines whose glyphs can reach the clip (#8503). Every test
/// compares the clipped draw with the full one: each glyph that touches the visible rows is drawn exactly
/// where the full draw puts it, and glyphs wholly outside are skipped. The stub font has a line height of
/// 10 and glyphs covering rows 2 to 10 of their line.
/// </summary>
public class TextClipLineCullTests : BaseTestClass
{
    const int LineHeight = 10;

    record struct Glyph(float X, float Y, float ScaleX, float ScaleY, int SourceY, int SourceHeight)
    {
        public float Bottom => Y + SourceHeight * ScaleY;
    }

    static string Lines(int count, string line = "abc") => string.Join("\n", Enumerable.Repeat(line, count));

    static TextRuntime CreateRuntime(string text, float height = 1000, VerticalAlignment verticalAlignment = VerticalAlignment.Top,
        float fontScale = 1, float lineHeightMultiplier = 1)
    {
        TextRuntime runtime = new();
        runtime.Width = 1000;
        runtime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        runtime.Height = height;
        runtime.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        runtime.VerticalAlignment = verticalAlignment;
        runtime.FontScale = fontScale;
        runtime.LineHeightMultiplier = lineHeightMultiplier;
        ((Text)runtime.RenderableComponent!).BitmapFont = CreateFont();
        runtime.Text = text;
        return runtime;
    }

    static List<Glyph> Draw(Text text, float? visibleTop = null, float? visibleBottom = null)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<Glyph> glyphs = new();
        spriteRenderer.DrawObserver = (position, scale, source) =>
            glyphs.Add(new Glyph(position.X, position.Y, scale.X, scale.Y, source?.Y ?? 0, source?.Height ?? 0));
        if (visibleTop == null)
        {
            text.DrawCharacterByCharacterForTests(spriteRenderer);
        }
        else
        {
            text.DrawCharacterByCharacterForTests(spriteRenderer, visibleTop.Value, visibleBottom!.Value);
        }
        return glyphs;
    }

    // The clipped draw holds every glyph of the full draw that touches the visible rows, unchanged and in order,
    // and nothing else that the full draw does not have.
    static void ShouldDrawEveryGlyphTouching(List<Glyph> full, List<Glyph> clipped, float visibleTop, float visibleBottom)
    {
        List<Glyph> touching = full.Where(g => g.Bottom >= visibleTop && g.Y <= visibleBottom).ToList();
        touching.Count.ShouldBeGreaterThan(0);
        clipped.Where(g => g.Bottom >= visibleTop && g.Y <= visibleBottom).ShouldBe(touching);
        clipped.All(full.Contains).ShouldBeTrue();
    }

    [Fact]
    public void Draw_ClipCuttingTopAndBottom_DrawsOnlyTheLinesInsideTheClip()
    {
        Text text = (Text)CreateRuntime(Lines(100)).RenderableComponent!;

        List<Glyph> full = Draw(text);
        List<Glyph> clipped = Draw(text, 205, 255);

        full.Count.ShouldBe(399);
        // Lines 20 (rows 202-210) to 25 (rows 252-260) reach the clip.
        clipped.Count.ShouldBe(6 * 4);
        ShouldDrawEveryGlyphTouching(full, clipped, 205, 255);
    }

    [Fact]
    public void Draw_ClipCoveringTheWholeText_DrawsEveryLine()
    {
        Text text = (Text)CreateRuntime(Lines(10)).RenderableComponent!;

        Draw(text, -500, 500).ShouldBe(Draw(text));
    }

    [Fact]
    public void Draw_ClipBetweenTwoLinesGlyphRows_DrawsNothing()
    {
        Text text = (Text)CreateRuntime(Lines(10)).RenderableComponent!;

        // Line 0 covers rows 2-10 and line 1 rows 12-20.
        Draw(text, 10.5f, 11.5f).ShouldBeEmpty();
    }

    [Fact]
    public void Draw_ClipAboveOrBelowTheText_DrawsNothing()
    {
        Text text = (Text)CreateRuntime(Lines(10)).RenderableComponent!;

        Draw(text, -100, -1).ShouldBeEmpty();
        Draw(text, 101, 200).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void Draw_TextOverflowingItsHeight_DrawsTheLinesInsideTheClipWhereverTheAlignmentPutsThem(VerticalAlignment alignment)
    {
        Text text = (Text)CreateRuntime(Lines(100), height: 20, verticalAlignment: alignment).RenderableComponent!;

        List<Glyph> full = Draw(text);
        List<Glyph> clipped = Draw(text, 0, 20);

        clipped.Count.ShouldBeLessThan(full.Count / 10);
        ShouldDrawEveryGlyphTouching(full, clipped, 0, 20);
    }

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(0.5f, 0.7f)]
    public void Draw_ScaledOrSpacedLines_DrawsOnlyTheLinesInsideTheClip(float fontScale, float lineHeightMultiplier)
    {
        Text text = (Text)CreateRuntime(Lines(100), fontScale: fontScale, lineHeightMultiplier: lineHeightMultiplier).RenderableComponent!;

        List<Glyph> full = Draw(text);
        List<Glyph> clipped = Draw(text, 203, 241);

        clipped.Count.ShouldBeLessThan(full.Count / 5);
        ShouldDrawEveryGlyphTouching(full, clipped, 203, 241);
    }

    [Fact]
    public void Draw_TypewriterPastTheSkippedLines_ShowsTheSameLettersOfTheVisibleLines()
    {
        TextRuntime runtime = CreateRuntime(Lines(100));
        Text text = (Text)runtime.RenderableComponent!;
        // Lines 0-20 in full and the first letter of line 21.
        text.MaxLettersToShow = 21 * 4 + 1;

        List<Glyph> full = Draw(text);
        List<Glyph> clipped = Draw(text, 205, 255);

        full.Count.ShouldBe(85);
        // Line 20 (three letters and its line break) and the first letter of line 21.
        clipped.Count.ShouldBe(5);
        ShouldDrawEveryGlyphTouching(full, clipped, 205, 255);
    }

    [Fact]
    public void Draw_TypewriterEndingAboveTheClip_DrawsNothing()
    {
        Text text = (Text)CreateRuntime(Lines(100)).RenderableComponent!;
        text.MaxLettersToShow = 10 * 4;

        Draw(text, 205, 255).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(40f)]
    [InlineData(-40f)]
    public void Draw_DropShadow_DrawsEachShadowLineWhoseShadowIsInsideTheClip(float shadowOffsetY)
    {
        TextRuntime runtime = CreateRuntime(Lines(100));
        Text text = (Text)runtime.RenderableComponent!;
        text.BitmapFont!.ShadowFont = CreateFont();
        text.HasDropshadow = true;
        text.DropshadowOffsetY = shadowOffsetY;

        List<Glyph> full = Draw(text);
        List<Glyph> clipped = Draw(text, 205, 255);

        full.Count.ShouldBe(2 * 399);
        // Six main lines and six shadow lines drawn 40 rows away, whose own lines are outside the clip.
        clipped.Count.ShouldBe(12 * 4);
        ShouldDrawEveryGlyphTouching(full, clipped, 205, 255);
    }

    [Fact]
    public void Draw_StyledLinesOfDifferentHeights_DrawsTheLinesInsideTheClipAtTheirWalkedPositions()
    {
        // Every third line has a double-size run, so lines step by 10 or 20.
        string text = string.Join("\n", Enumerable.Range(0, 60).Select(i =>
            i % 3 == 0 ? "[FontScale=2]ab[/FontScale]c" : i % 3 == 1 ? "[Color=Red]abc[/Color]" : "abc"));
        Text styled = (Text)CreateRuntime(text).RenderableComponent!;

        List<Glyph> full = Draw(styled);
        // 300 falls inside a double-size line (lines 21-24 span rows 280-320).
        List<Glyph> clipped = Draw(styled, 305, 345);

        clipped.Count.ShouldBeLessThan(full.Count / 5);
        ShouldDrawEveryGlyphTouching(full, clipped, 305, 345);
    }

    [Fact]
    public void Draw_StyledTypewriterPastSkippedLines_ShowsTheSameLettersOfTheVisibleLines()
    {
        string text = string.Join("\n", Enumerable.Range(0, 60).Select(i => i % 2 == 0 ? "[Color=Red]abc[/Color]" : "abc"));
        Text styled = (Text)CreateRuntime(text).RenderableComponent!;
        styled.MaxLettersToShow = 21 * 4 + 1;

        List<Glyph> full = Draw(styled);
        List<Glyph> clipped = Draw(styled, 205, 255);

        clipped.Count.ShouldBeLessThan(10);
        ShouldDrawEveryGlyphTouching(full, clipped, 205, 255);
    }

    [Fact]
    public void Draw_TypewriterOverStyledLinesWithAClip_DrawsOnlyThatManyLettersAndKeepsTheLimit()
    {
        Text styled = (Text)CreateRuntime("[Color=Red]ab[/Color]\ncd\nef").RenderableComponent!;
        styled.MaxLettersToShow = 4;

        Draw(styled, -100, 100).Count.ShouldBe(4);
        Draw(styled, -100, 100).Count.ShouldBe(4);
        styled.MaxLettersToShow.ShouldBe(4);
    }

    [Theory]
    [InlineData(40f)]
    [InlineData(-40f)]
    public void Draw_StyledLinesWithADropShadow_DrawsEachShadowLineWhoseShadowIsInsideTheClip(float shadowOffsetY)
    {
        string text = string.Join("\n", Enumerable.Range(0, 100).Select(i => i % 2 == 0 ? "[Color=Red]abc[/Color]" : "abc"));
        Text styled = (Text)CreateRuntime(text).RenderableComponent!;
        styled.BitmapFont!.ShadowFont = CreateFont();
        styled.HasDropshadow = true;
        styled.DropshadowOffsetY = shadowOffsetY;

        List<Glyph> full = Draw(styled);
        List<Glyph> clipped = Draw(styled, 205, 255);

        // Six lines and six shadow lines 40 rows away reach the clip, out of 100 of each.
        clipped.Count.ShouldBeLessThan(full.Count / 5);
        ShouldDrawEveryGlyphTouching(full, clipped, 205, 255);
    }

    [Fact]
    public void Draw_StyledLineWithAWaveEffect_IsAlwaysDrawn()
    {
        // Gum has no [Y] tag; vertical letter offsets come from [Custom] functions and the built-in effects.
        System.Func<double> savedClock = BbCodeTextEffect.TimeSeconds;
        try
        {
            ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = (int)GumProjectSave.GumxVersions.BbCodeTextEffects };
            BbCodeTextEffect.TimeSeconds = () => 0.25;
            Text styled = (Text)CreateRuntime("abc\n[Wave amp=300]abc[/Wave]\nabc").RenderableComponent!;
            styled.RawText.ShouldBe("abc\nabc\nabc");

            List<Glyph> full = Draw(styled);
            // Line 0 is the first four draws (three letters and the line break), line 2 the last three.
            List<Glyph> waveLine = full.Skip(4).Take(full.Count - 7).ToList();

            // The clip is far below every line's own rows, but the wave can carry its letters anywhere.
            Draw(styled, 400, 500).ShouldBe(waveLine);
        }
        finally
        {
            BbCodeTextEffect.TimeSeconds = savedClock;
        }
    }

    [Fact]
    public void Draw_RotatedText_DrawsEveryLine()
    {
        TextRuntime runtime = CreateRuntime(Lines(100));
        runtime.Rotation = 30;
        Text text = (Text)runtime.RenderableComponent!;

        Draw(text, 205, 255).ShouldBe(Draw(text));
    }

    [Fact]
    public void Draw_TextUnderARotatedParent_DrawsEveryLine()
    {
        ContainerRuntime parent = new();
        parent.Rotation = 30;
        TextRuntime runtime = CreateRuntime(Lines(100));
        parent.AddChild(runtime);
        Text text = (Text)runtime.RenderableComponent!;

        Draw(text, 205, 255).ShouldBe(Draw(text));
    }

    // Every printable character is the same 6x8 glyph 2 rows below its line top.
    static BitmapFont CreateFont()
    {
        System.Text.StringBuilder builder = new();
        builder.AppendLine("info face=\"Stub\" size=-10 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
        builder.AppendLine($"common lineHeight={LineHeight} base=8 scaleW=512 scaleH=512 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
        builder.AppendLine("page id=0 file=\"x.png\"");
        builder.AppendLine($"chars count={'~' - ' ' + 1}");
        for (char c = ' '; c <= '~'; c++)
        {
            int glyphWidth = c == ' ' ? 0 : 6;
            int glyphHeight = c == ' ' ? 0 : 8;
            builder.AppendLine($"char id={(int)c} x=0 y=0 width={glyphWidth} height={glyphHeight} xoffset=0 yoffset=2 xadvance=7 page=0 chnl=15");
        }

        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, builder.ToString());
        font.SetFontPattern(512, 512);
        return font;
    }
}
