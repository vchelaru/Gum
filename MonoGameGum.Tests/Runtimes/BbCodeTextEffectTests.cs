using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

public class BbCodeTextEffectTests : BaseTestClass
{
    readonly Func<double> _originalClock = BbCodeTextEffect.TimeSeconds;
    readonly GumProjectSave? _originalProject = ObjectFinder.Self.GumProjectSave;

    public override void Dispose()
    {
        BbCodeTextEffect.TimeSeconds = _originalClock;
        ObjectFinder.Self.GumProjectSave = _originalProject;
        base.Dispose();
    }

    #region Parse

    // Each existing tag form, with values holding spaces or '='.
    [Theory]
    [InlineData("[Font=Arial Black]x[/Font]", "Font", "Arial Black", "x")]
    [InlineData("[State=My State]x[/State]", "State", "My State", "x")]
    [InlineData("[Custom=Wave]x[/Custom]", "Custom", "Wave", "x")]
    [InlineData("[Color=Red]x[/Color]", "Color", "Red", "x")]
    [InlineData("[Link=https://a.com/?q=1]x[/Link]", "Link", "https://a.com/?q=1", "x")]
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
    [InlineData("[Color Red]x[/Color]")]
    [InlineData("[note to self]x")]
    public void Parse_SpaceInANonEffectTag_IsNotATag(string text)
    {
        BbCodeParser.Parse(text, BbCodeParser.KnownTags).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Wave")]
    [InlineData("Shake")]
    [InlineData("Tornado")]
    [InlineData("Fade")]
    [InlineData("Rainbow")]
    [InlineData("Pulse")]
    public void Parse_EachEffectTag_IsATagIgnoringCase(string name)
    {
        string lower = name.ToLowerInvariant();
        string text = $"a[{lower} x=1]bc[/{name.ToUpperInvariant()}]";

        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        tags.Count.ShouldBe(1);
        tags[0].Name.ShouldBe(name);
        tags[0].Open.Argument.ShouldBe("x=1");
        tags[0].Open.HasOptions.ShouldBeTrue();
        BbCodeParser.RemoveTags(text, tags).ShouldBe("abc");
    }

    [Fact]
    public void Parse_EffectTagWithoutParameters_IsATag()
    {
        List<FoundTag> tags = BbCodeParser.Parse("[Shake]hi[/Shake]", BbCodeParser.KnownTags);

        tags.Count.ShouldBe(1);
        tags[0].Open.Argument.ShouldBeNull();
    }

    // The single-value form is not supported: it is not a tag, so it stays in the text like any malformed tag.
    [Fact]
    public void Parse_EffectEqualsForm_IsNotATag()
    {
        string text = "[Wave=5]x[/Wave]";

        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        tags.ShouldBeEmpty();
        BbCodeParser.RemoveTags(text, tags).ShouldBe(text);
    }

    [Fact]
    public void AddTags_EffectTagWithParameters_RoundTripsInParameterForm()
    {
        string text = "[Wave amp=50 freq=5]bc[/Wave]";
        List<FoundTag> tags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        BbCodeParser.AddTags("bc", tags).ShouldBe(text);
    }

    #endregion

    #region Version gate

    [Fact]
    public void KnownTags_ProjectBelowEffectsVersion_LeavesEffectTagsLiteral()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = (int)GumProjectSave.GumxVersions.RaiseChildrenEventsExpansion };

        BbCodeParser.Parse("[Wave]x[/Wave]", BbCodeParser.KnownTags).ShouldBeEmpty();
        CreateText("[Wave]AV[/Wave]").RawText.ShouldBe("[Wave]AV[/Wave]");
    }

    [Fact]
    public void KnownTags_ProjectAtEffectsVersion_RecognizesEffectTags()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = (int)GumProjectSave.GumxVersions.BbCodeTextEffects };

        BbCodeParser.Parse("[Wave]x[/Wave]", BbCodeParser.KnownTags).Count.ShouldBe(1);
        CreateText("[Wave]AV[/Wave]").RawText.ShouldBe("AV");
    }

    [Fact]
    public void KnownTags_NoProject_RecognizesEffectTags()
    {
        ObjectFinder.Self.GumProjectSave = null;

        BbCodeParser.Parse("[Wave]x[/Wave]", BbCodeParser.KnownTags).Count.ShouldBe(1);
    }

    #endregion

    #region Parameters

    [Theory]
    [InlineData("Wave", null, 20f, 5f)]
    [InlineData("Wave", "amp=50 freq=2.5", 50f, 2.5f)]
    [InlineData("Wave", "amp=abc freq=", 20f, 5f)]
    [InlineData("Wave", "AMP=7", 7f, 5f)]
    public void Create_Wave_UsesGodotDefaultsAndParsedParameters(string name, string? options, float amp, float freq)
    {
        BbCodeTextEffect effect = BbCodeTextEffect.Create(name, options).ShouldNotBeNull();

        effect.Kind.ShouldBe(BbCodeTextEffectKind.Wave);
        effect.Amplitude.ShouldBe(amp);
        effect.Frequency.ShouldBe(freq);
    }

    [Fact]
    public void Create_EachEffect_HasGodotSourceDefaults()
    {
        BbCodeTextEffect shake = BbCodeTextEffect.Create("Shake", null)!;
        shake.Rate.ShouldBe(20f);
        shake.Level.ShouldBe(5f);
        shake.Connected.ShouldBeTrue();

        BbCodeTextEffect tornado = BbCodeTextEffect.Create("Tornado", null)!;
        tornado.Radius.ShouldBe(10f);
        tornado.Frequency.ShouldBe(1f);

        BbCodeTextEffect fade = BbCodeTextEffect.Create("Fade", null)!;
        fade.Start.ShouldBe(0);
        fade.Length.ShouldBe(10);

        BbCodeTextEffect rainbow = BbCodeTextEffect.Create("Rainbow", null)!;
        rainbow.Frequency.ShouldBe(1f);
        rainbow.Saturation.ShouldBe(0.8f);
        rainbow.Value.ShouldBe(0.8f);
        rainbow.Speed.ShouldBe(1f);

        BbCodeTextEffect pulse = BbCodeTextEffect.Create("Pulse", null)!;
        pulse.Frequency.ShouldBe(1f);
        pulse.Ease.ShouldBe(-2f);
        pulse.PulseColor.ShouldBe(System.Drawing.Color.FromArgb(64, 255, 255, 255));
    }

    [Fact]
    public void Create_Pulse_ParsesColorLikeTheColorTag()
    {
        BbCodeTextEffect.Create("Pulse", "color=Red")!.PulseColor.ToArgb().ShouldBe(System.Drawing.Color.Red.ToArgb());
        BbCodeTextEffect.Create("Pulse", "color=0x80FF0000")!.PulseColor.ShouldBe(System.Drawing.Color.FromArgb(0x80, 255, 0, 0));
        BbCodeTextEffect.Create("Pulse", "color=notacolor")!.PulseColor.ShouldBe(System.Drawing.Color.FromArgb(64, 255, 255, 255));
    }

    [Fact]
    public void Create_Shake_ParsesConnectedButKeepsItsDefaultOnBadInput()
    {
        BbCodeTextEffect.Create("Shake", "connected=0")!.Connected.ShouldBeFalse();
        BbCodeTextEffect.Create("Shake", "connected=maybe")!.Connected.ShouldBeTrue();
    }

    #endregion

    #region Draw (MonoGame)

    [Fact]
    public void Draw_Wave_MovesLettersByGodotFormula()
    {
        Text text = CreateText("[Wave amp=100 freq=5]AV[/Wave]");

        BbCodeTextEffect.TimeSeconds = () => 0;
        float[] atZero = DrawnTops(text);
        BbCodeTextEffect.TimeSeconds = () => 0.25;
        float[] later = DrawnTops(text);

        atZero.Length.ShouldBe(2);
        // sin(freq * t + x / 50) * amp / 10. The first letter is at x = 0; the draw snaps to whole pixels.
        atZero[0].ShouldBe(0f, 1f);
        later[0].ShouldBe((float)(Math.Sin(5 * 0.25) * 10), 1f);
        // The second letter is 7 pixels along (10 advance, -3 kerning).
        later[1].ShouldBe((float)(Math.Sin(5 * 0.25 + 7 / 50.0) * 10), 1f);
    }

    [Fact]
    public void Draw_Tornado_MovesLettersInACircle()
    {
        Text text = CreateText("[Tornado radius=20 freq=1]A[/Tornado]");
        float plainLeft = DrawnLefts(CreateText("A"))[0];
        float plainTop = DrawnTops(CreateText("A"))[0];

        BbCodeTextEffect.TimeSeconds = () => Math.PI / 2;

        // sin(phase) * radius across, cos(phase) * radius down; phase = pi / 2 at x = 0.
        (DrawnLefts(text)[0] - plainLeft).ShouldBe(20f, 1f);
        (DrawnTops(text)[0] - plainTop).ShouldBe(0f, 1f);
    }

    [Fact]
    public void Draw_Shake_StaysWithinLevelOverTenAndChanges()
    {
        Text text = CreateText("[Shake level=50 rate=10]A[/Shake]");
        float plainLeft = DrawnLefts(CreateText("A"))[0];
        List<float> lefts = new();

        for (int frame = 0; frame < 20; frame++)
        {
            double time = frame * 0.05;
            BbCodeTextEffect.TimeSeconds = () => time;
            lefts.Add(DrawnLefts(text)[0] - plainLeft);
        }

        lefts.ShouldAllBe(left => Math.Abs(left) <= 6f);
        lefts.Distinct().Count().ShouldBeGreaterThan(1);
    }

    [Fact]
    public void Draw_Fade_LowersAlphaAlongTheSpan()
    {
        Text text = CreateText("A[Fade length=2]AVA[/Fade]");
        BbCodeTextEffect.TimeSeconds = () => 0;

        byte[] alphas = DrawnColors(text).Select(color => color.A).ToArray();

        // 1 - (index in span - start) / length, clamped: 1, 0.5, 0.
        alphas.ShouldBe(new byte[] { 255, 255, 128, 0 });
    }

    [Fact]
    public void Draw_FadeInsideAlpha_MultipliesAlpha()
    {
        Text text = CreateText("[Alpha=128][Fade length=2]AV[/Fade][/Alpha]");
        BbCodeTextEffect.TimeSeconds = () => 0;

        DrawnColors(text).Select(color => color.A).ToArray().ShouldBe(new byte[] { 128, 64 });
    }

    // The same table runs on raylib and Skia (BbCodeEffectParityCases), so every runtime draws the same offsets and colors.
    [Theory]
    [MemberData(nameof(BbCodeEffectParityCases.All), MemberType = typeof(BbCodeEffectParityCases))]
    public void Draw_Effect_MatchesTheParityTable(string open, string close, double time, float dx, float dy,
        int red, int green, int blue, int alpha)
    {
        Text.Customizations[BbCodeEffectParityCases.ShiftFunctionName] = (_, _) =>
            new LetterCustomization { YOffset = BbCodeEffectParityCases.ShiftYOffset, Color = System.Drawing.Color.Red };
        try
        {
            float plainLeft = DrawnLefts(CreateText("A"))[0];
            float plainTop = DrawnTops(CreateText("A"))[0];
            Text text = CreateText($"{open}A{close}");
            BbCodeTextEffect.TimeSeconds = () => time;

            // The draw snaps each glyph to a whole pixel.
            (DrawnLefts(text)[0] - plainLeft).ShouldBe(dx, 1f);
            (DrawnTops(text)[0] - plainTop).ShouldBe(dy, 1f);
            System.Drawing.Color color = DrawnColors(text)[0];
            ((int)color.R).ShouldBe(red);
            ((int)color.G).ShouldBe(green);
            ((int)color.B).ShouldBe(blue);
            ((int)color.A).ShouldBe(alpha);
        }
        finally
        {
            Text.Customizations.Remove(BbCodeEffectParityCases.ShiftFunctionName);
        }
    }

    [Fact]
    public void Draw_PausedGameTime_FreezesEffects()
    {
        IGumService? original = IGumService.Default;
        try
        {
            var service = new Moq.Mock<IGumService>();
            service.SetupGet(item => item.GameTime).Returns(0.1f);
            IGumService.Default = service.Object;
            BbCodeTextEffect.TimeSeconds = BbCodeTextEffect.DefaultTimeSeconds;
            Text text = CreateText("[Wave amp=100 freq=5]A[/Wave]");

            float[] first = DrawnTops(text);
            System.Threading.Thread.Sleep(30);
            float[] second = DrawnTops(text);

            second.ShouldBe(first);
            first[0].ShouldBe((float)(Math.Sin(5 * 0.1f) * 10), 1f);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    [Fact]
    public void DefaultTimeSeconds_NoGameTime_FollowsTheWallClock()
    {
        IGumService? original = IGumService.Default;
        try
        {
            IGumService.Default = null;

            double first = BbCodeTextEffect.DefaultTimeSeconds();
            System.Threading.Thread.Sleep(30);
            double second = BbCodeTextEffect.DefaultTimeSeconds();

            (second - first).ShouldBeGreaterThanOrEqualTo(0.02);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    // As on Skia, where the effect is applied at draw time: a context function sees the letter before the effect,
    // and the effect adds to the offset it returns.
    [Fact]
    public void Draw_ContextCustomizationInsideAnEffect_SeesNoEffectAndTheEffectAddsToIt()
    {
        float? seenYOffset = null;
        Text.ContextCustomizations["Probe"] = (_, _, context) =>
        {
            seenYOffset = context.YOffset;
            return new LetterCustomization { YOffset = 3 };
        };
        try
        {
            Text text = CreateText("[Wave amp=100 freq=5][Custom=Probe]A[/Custom][/Wave]");
            BbCodeTextEffect.TimeSeconds = () => 0.1;

            float top = DrawnTops(text)[0];

            seenYOffset.ShouldBe(0f);
            top.ShouldBe(DrawnTops(CreateText("A"))[0] + 3 + (float)(Math.Sin(0.5) * 10), 1f);
        }
        finally
        {
            Text.ContextCustomizations.Remove("Probe");
        }
    }

    [Fact]
    public void Draw_EffectSpanCutByTruncateLineWithEllipsis_DrawsTheKeptLettersMoved()
    {
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 45;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = 12;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.IsTruncatingWithEllipsisOnLastLine = true;
        Text text = (Text)runtime.RenderableComponent!;
        text.BitmapFont = CreateStubFont();
        runtime.Text = "[Wave amp=100 freq=5]AVAV AVAV AVAV[/Wave]";
        BbCodeTextEffect.TimeSeconds = () => 0.1;

        float[] tops = DrawnTops(text);

        text.WrappedText.Count.ShouldBe(1);
        tops.Length.ShouldBeGreaterThan(0);
        tops[0].ShouldBe((float)(Math.Sin(0.5) * 10), 1f);
    }

    [Fact]
    public void TextBox_EffectMarkup_StaysLiteral()
    {
        Gum.Forms.Controls.TextBox textBox = new();

        textBox.Text = "[Wave amp=5]x[/Wave]";

        textBox.Text.ShouldBe("[Wave amp=5]x[/Wave]");
    }

    [Theory]
    [InlineData(0.0, 255)]
    [InlineData(0.5, 160)]
    [InlineData(1.0, 64)]
    public void Draw_Pulse_BlendsTowardItsColorOverTime(double time, int expectedAlpha)
    {
        Text text = CreateText("[Pulse]A[/Pulse]");
        BbCodeTextEffect.TimeSeconds = () => time;

        System.Drawing.Color color = DrawnColors(text)[0];

        // The default color is white at 25% alpha, so only alpha changes on white text.
        color.R.ShouldBe((byte)255);
        color.A.ShouldBe((byte)expectedAlpha);
    }

    [Theory]
    [InlineData("AV", 7f)]
    [InlineData("[Wave amp=0]AV[/Wave]", 7f)]
    [InlineData("A[Wave amp=0]V[/Wave]", 7f)]
    [InlineData("A[Rainbow]V[/Rainbow]", 7f)]
    [InlineData("A[Custom=Nothing]V[/Custom]", 10f)]
    public void Draw_BuiltInEffect_KeepsKerning(string markup, float expectedSecondX)
    {
        Text.Customizations["Nothing"] = (_, _) => new LetterCustomization();
        try
        {
            Text text = CreateText(markup);
            BbCodeTextEffect.TimeSeconds = () => 0;

            float[] lefts = DrawnLefts(text);

            lefts.Length.ShouldBe(2);
            (lefts[1] - lefts[0]).ShouldBe(expectedSecondX, 0.01f);
        }
        finally
        {
            Text.Customizations.Remove("Nothing");
        }
    }

    [Fact]
    public void Draw_WaveWithMaxLettersToShow_DrawsOnlyThoseLetters()
    {
        Text text = CreateText("[Wave]AVAV[/Wave]");
        text.MaxLettersToShow = 3;

        DrawnLefts(text).Length.ShouldBe(3);
    }

    [Fact]
    public void DefaultTimeSeconds_UsesGumServiceGameTime_WhenItHasOne()
    {
        IGumService? original = IGumService.Default;
        try
        {
            var service = new Moq.Mock<IGumService>();
            service.SetupGet(item => item.GameTime).Returns(12.5f);
            IGumService.Default = service.Object;

            BbCodeTextEffect.DefaultTimeSeconds().ShouldBe(12.5);

            service.SetupGet(item => item.GameTime).Returns((float?)null);
            BbCodeTextEffect.DefaultTimeSeconds().ShouldNotBe(12.5);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    #endregion

    static float[] DrawnLefts(Text text)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<float> lefts = new();
        spriteRenderer.DrawObserver = (position, scale, source) => lefts.Add(position.X);
        text.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        text.DrawCharacterByCharacterForTests(spriteRenderer);
        return lefts.ToArray();
    }

    static System.Drawing.Color[] DrawnColors(Text text)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<System.Drawing.Color> colors = new();
        spriteRenderer.DrawObserver = (position, scale, source) => { };
        spriteRenderer.DrawColorObserver = colors.Add;
        text.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        text.DrawCharacterByCharacterForTests(spriteRenderer);
        return colors.ToArray();
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

    static BitmapFont CreateStubFont()
    {
        string data =
"info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
"common lineHeight=12 base=10 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
"page id=0 file=\"x.png\"\n" +
"chars count=2\n" +
"char id=65 x=10 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=86 x=40 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"kernings count=1\n" +
"kerning first=65 second=86 amount=-3\n";
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }
}
