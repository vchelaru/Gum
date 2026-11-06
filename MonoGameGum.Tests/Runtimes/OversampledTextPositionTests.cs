using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// While oversampling pins a measurement font, positions within a line follow the drawn glyphs: the
/// display font's whole-pixel advances times the effective scale, not the measurement font's (#7675).
/// Each test pins a font whose W advances 17 pixels and draws from one whose W advances 30 at 18 / 32.4,
/// as Arial 18 does at a zoom of 1.8, so forty W's are drawn about 666.7 wide while layout measures 680.
/// </summary>
public class OversampledTextPositionTests : BaseTestClass
{
    // Issue #6814: the display font draws at exactly the inverse raster ratio so that each atlas pixel
    // lands on one screen pixel. Its whole-pixel advances then add up to a different width than the
    // native measurement, and the glyphs keep them rather than being fitted to the measured width.
    [Fact]
    public void MeasureDrawnString_WhileOversampled_IsTheDisplayFontAdvancesTimesTheEffectiveScale()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextRuntime textRuntime = CreateText(HorizontalAlignment.Left);
        Text contained = Oversample(textRuntime, measuredAdvance, drawnAdvance, compensation);
        textRuntime.Text = text;

        for (int index = 0; index <= text.Length; index++)
        {
            contained.MeasureDrawnString(text.Substring(0, index), HorizontalMeasurementStyle.Full)
                .ShouldBe(index * drawnAdvance * compensation, tolerance: 0.001f, $"pen position before character {index}");
        }
        contained.UpdateIpsoForRenderingForTests().Width.ShouldBe(text.Length * drawnAdvance * compensation, tolerance: 0.001f,
            "because the drawn text is as wide as the display font's advances, about 666.7, not the measured 680");
        contained.WrappedTextWidth.ShouldBe(text.Length * measuredAdvance,
            "because layout keeps measuring with the pinned font (#4309)");
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void GetCharacterIndexAtPosition_WhileOversampled_ReturnsTheCharacterDrawnAtThePoint(HorizontalAlignment alignment)
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextRuntime textRuntime = CreateText(alignment);
        Text contained = Oversample(textRuntime, measuredAdvance, drawnAdvance, compensation);
        textRuntime.Text = text;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.UpdateIpsoForRenderingForTests().Y + 1;
        float advance = drawnAdvance * compensation;

        for (int character = 0; character < text.Length; character++)
        {
            float start = drawnLeft + character * advance;
            contained.GetCharacterIndexAtPosition(start + advance * 0.25f, y)
                .ShouldBe(character, $"left quarter of character {character}");
            contained.GetCharacterIndexAtPosition(start + advance * 0.75f, y)
                .ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void TextBoxCaret_WhileOversampled_SitsAtTheDrawnPenPosition(HorizontalAlignment alignment)
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextBox textBox = CreateTextBox();
        TextRuntime textInstance = GetTextInstance(textBox);
        textInstance.HorizontalAlignment = alignment;
        Text contained = Oversample(textInstance, measuredAdvance, drawnAdvance, compensation);
        textBox.Text = text;
        GraphicalUiElement caret = ((TextBoxBaseVisual)textBox.Visual).CaretInstance;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;

        for (int index = 0; index <= text.Length; index++)
        {
            textBox.CaretIndex = index;

            (caret.GetAbsoluteLeft() - drawnLeft)
                .ShouldBe(index * drawnAdvance * compensation, tolerance: 0.001f, $"caret index {index}");
        }
    }

    [Fact]
    public void TextBoxSelection_WhileOversampled_CoversTheDrawnGlyphs()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        TextBox textBox = CreateTextBox();
        Text contained = Oversample(GetTextInstance(textBox), measuredAdvance, drawnAdvance, compensation);
        textBox.Text = new string('W', 40);
        textBox.IsFocused = true;
        textBox.SelectionStart = 10;
        textBox.SelectionLength = 20;

        GraphicalUiElement selection = ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Single(child => child.Name == "SelectionInstance" && child.Visible);

        (selection.GetAbsoluteLeft() - contained.GetAbsoluteLeft()).ShouldBe(10 * drawnAdvance * compensation, tolerance: 0.001f);
        selection.AbsoluteWidth.ShouldBe(20 * drawnAdvance * compensation, tolerance: 0.001f);
    }

    [Fact]
    public void TextBoxGetCaretIndexAtPosition_WhileOversampled_ReturnsTheCharacterDrawnAtThePoint()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextBox textBox = CreateTextBox();
        Text contained = Oversample(GetTextInstance(textBox), measuredAdvance, drawnAdvance, compensation);
        textBox.Text = text;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.GetAbsoluteTop() + 1;
        float advance = drawnAdvance * compensation;

        for (int character = 0; character < text.Length; character++)
        {
            float start = drawnLeft + character * advance;
            textBox.GetCaretIndexAtPosition(start + advance * 0.25f, y)
                .ShouldBe(character, $"left quarter of character {character}");
            textBox.GetCaretIndexAtPosition(start + advance * 0.75f, y)
                .ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    // A text made before any font exists, even a default one, has nothing to draw, so it is 0 wide.
    [Fact]
    public void MeasureDrawnString_WithNoFont_IsZero()
    {
        BitmapFont? savedDefault = Text.DefaultBitmapFont;
        try
        {
            Text.DefaultBitmapFont = null;
            Text text = new();
            text.BitmapFont = null;

            text.MeasureDrawnString("WW", HorizontalMeasurementStyle.Full).ShouldBe(0);
            text.GetDrawnCharacterAdvance('W').ShouldBe(0);
        }
        finally
        {
            Text.DefaultBitmapFont = savedDefault;
        }
    }

    // Pins a font of the measured advance for layout and draws from one of the drawn advance at the
    // compensation scale, as TextRuntime.RegenerateOversampledFont does.
    static Text Oversample(TextRuntime textRuntime, int measuredAdvance, int drawnAdvance, float compensation)
    {
        Text contained = (Text)textRuntime.RenderableComponent!;
        contained.BitmapFont = CreateFont(measuredAdvance);
        contained.SetOversampledDisplayFont(CreateFont(drawnAdvance));
        contained.OversampleCompensationScale = compensation;
        return contained;
    }

    // A font whose W advances a whole number of pixels, as a rasterizer gives at one size.
    static BitmapFont CreateFont(int wAdvance)
    {
        string fontData =
$@"info face=""Stub"" size=-{wAdvance} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={wAdvance} base={wAdvance} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=87 x=0 y=0 width={wAdvance} height=13 xoffset=0 yoffset=4 xadvance={wAdvance} page=0 chnl=15
";
        BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
        font.SetFontPattern(256, 256);
        return font;
    }

    static TextRuntime CreateText(HorizontalAlignment alignment)
    {
        TextRuntime textRuntime = new();
        textRuntime.X = 20;
        textRuntime.Y = 30;
        textRuntime.Width = 1000;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.HorizontalAlignment = alignment;
        return textRuntime;
    }

    static TextBox CreateTextBox()
    {
        TextBox textBox = new();
        textBox.Width = 1000;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        return textBox;
    }

    static TextRuntime GetTextInstance(TextBox textBox) => ((TextBoxBaseVisual)textBox.Visual).TextInstance;
}
