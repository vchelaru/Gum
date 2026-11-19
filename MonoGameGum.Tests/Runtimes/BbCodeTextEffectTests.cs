using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

// SPIKE: Godot-style built-in effect tags ([wave amp=50 freq=5]) and proof that existing tags parse unchanged.
public class BbCodeTextEffectTests : BaseTestClass
{
    readonly Func<double> _originalClock = BbCodeTextEffect.TimeSeconds;

    public override void Dispose()
    {
        BbCodeTextEffect.TimeSeconds = _originalClock;
        base.Dispose();
    }

    // Each existing tag form, with values holding spaces or '='. Expected values were read from the
    // parser before the spike (a value with a second '=' was never a tag: the parser keeps the LAST '='
    // as the name/value split, so the name becomes "Link=https://a.com/?q" and is unknown).
    [Theory]
    [InlineData("[Font=Arial Black]x[/Font]", "Font", "Arial Black", "x")]
    [InlineData("[State=My State]x[/State]", "State", "My State", "x")]
    [InlineData("[Custom=Wave]x[/Custom]", "Custom", "Wave", "x")]
    [InlineData("[Color=Red]x[/Color]", "Color", "Red", "x")]
    [InlineData("[Link=shop]x[/Link]", "Link", "shop", "x")]
    [InlineData("[IsBold=true]x[/IsBold]", "IsBold", "true", "x")]
    public void Parse_ExistingTag_IsUnchanged(string text, string name, string argument, string stripped)
    {
        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        tags.Count.ShouldBe(1);
        tags[0].Name.ShouldBe(name);
        tags[0].Open.Argument.ShouldBe(argument);
        tags[0].Open.HasOptions.ShouldBeFalse();
        BbCodeParser.RemoveTags(text, tags).ShouldBe(stripped);
    }

    [Theory]
    [InlineData("[Link=https://a.com/?q=1]x[/Link]")]
    [InlineData("[Color Red]x[/Color]")]
    [InlineData("[note to self]x")]
    public void Parse_NotATagBeforeSpike_IsStillNotATag(string text)
    {
        BbCodeParser.Parse(text, BbCodeParser.KnownTags).ShouldBeEmpty();
    }

    [Fact]
    public void Parse_EffectTagWithOptions_KeepsOptionsAsArgument()
    {
        string text = "a[wave amp=50 freq=5]bc[/wave]";

        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        tags.Count.ShouldBe(1);
        tags[0].Name.ShouldBe("wave");
        tags[0].Open.Argument.ShouldBe("amp=50 freq=5");
        tags[0].Open.HasOptions.ShouldBeTrue();
        BbCodeParser.RemoveTags(text, tags).ShouldBe("abc");
    }

    [Fact]
    public void Parse_EffectTagWithoutOptions_IsATag()
    {
        List<FoundTag> tags = BbCodeParser.Parse("[shake]hi[/shake]", BbCodeParser.KnownTags);

        tags.Count.ShouldBe(1);
        tags[0].Open.Argument.ShouldBeNull();
    }

    [Fact]
    public void AddTags_EffectTagWithOptions_RoundTripsInOptionForm()
    {
        string text = "[wave amp=50 freq=5]bc[/wave]";
        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        BbCodeParser.AddTags("bc", tags).ShouldBe(text);
    }

    [Fact]
    public void Create_Wave_UsesGodotSourceDefaultsAndParsedOptions()
    {
        BbCodeTextEffect defaults = BbCodeTextEffect.Create("wave", null).ShouldNotBeNull();
        defaults.Amplitude.ShouldBe(20f);
        defaults.Frequency.ShouldBe(5f);

        BbCodeTextEffect parsed = BbCodeTextEffect.Create("wave", "amp=50 freq=2.5").ShouldNotBeNull();
        parsed.Amplitude.ShouldBe(50f);
        parsed.Frequency.ShouldBe(2.5f);
    }

    [Fact]
    public void Draw_WaveText_MovesLettersVerticallyOverTime()
    {
        Text text = CreateText("[wave amp=100 freq=5]AV[/wave]");

        BbCodeTextEffect.TimeSeconds = () => 0;
        float[] atZero = DrawnTops(text);
        BbCodeTextEffect.TimeSeconds = () => 0.25;
        float[] later = DrawnTops(text);

        atZero.Length.ShouldBe(2);
        // Godot: sin(freq * t + x / 50) * amp / 10; the first letter is at x = 0.
        atZero[0].ShouldBe(0f, 0.01f);
        // The draw snaps the glyph to a whole pixel, so the offset lands within one pixel of the formula.
        later[0].ShouldBe((float)(Math.Sin(5 * 0.25) * 10), 1f);
        later[1].ShouldNotBe(atZero[1]);
    }

    static Text CreateText(string markup)
    {
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 1000;
        Text text = (Text)runtime.RenderableComponent!;
        text.BitmapFont = CreateStubFont();
        runtime.Text = markup;
        return text;
    }

    // Glyph tops relative to the Text's top.
    static float[] DrawnTops(Text text)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<float> tops = new();
        spriteRenderer.DrawObserver = (position, scale, source) => tops.Add(position.Y);
        text.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        text.DrawCharacterByCharacterForTests(spriteRenderer);
        float origin = text.GetAbsoluteTop();
        return tops.Select(y => MathF.Round(y - origin, 3)).ToArray();
    }

    static BitmapFont CreateStubFont()
    {
        string data =
"info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
"common lineHeight=12 base=10 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
"page id=0 file=\"x.png\"\n" +
"chars count=2\n" +
"char id=65 x=10 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=86 x=40 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n";
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }
}
