using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
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
/// Caret, selection and click positions of a TextBox whose glyphs have different widths, with font
/// oversampling off and on, at every horizontal alignment. The stub fonts advance i, W, m and space by
/// different amounts, so a position computed from the wrong glyph or the wrong font shows up (the other
/// oversampled tests use only W).
/// </summary>
public class TextBoxMixedGlyphPositionTests : BaseTestClass
{
    internal const string Content = "iWm mWi imW Wi";
    const float Compensation = 18f / 32.4f;

    internal static readonly Dictionary<char, int> MeasuredAdvances = new() { ['i'] = 6, ['W'] = 17, ['m'] = 14, [' '] = 5 };
    static readonly Dictionary<char, int> DrawnAdvances = new() { ['i'] = 10, ['W'] = 30, ['m'] = 24, [' '] = 9 };

    public static IEnumerable<object[]> Cases()
    {
        foreach (bool oversampled in new[] { false, true })
        {
            foreach (HorizontalAlignment alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
            {
                yield return new object[] { oversampled, alignment };
            }
        }
    }

    // The pen position before the character at index, in screen pixels from the drawn text's left.
    internal static float Pen(string text, int index, bool oversampled)
    {
        Dictionary<char, int> advances = oversampled ? DrawnAdvances : MeasuredAdvances;
        float scale = oversampled ? Compensation : 1f;
        return text.Take(index).Sum(character => advances[character]) * scale;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Caret_MixedGlyphs_SitsAtTheDrawnPenPosition(bool oversampled, HorizontalAlignment alignment)
    {
        (TextBox textBox, Text contained) = Create(oversampled, alignment);
        GraphicalUiElement caret = ((TextBoxBaseVisual)textBox.Visual).CaretInstance;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;

        for (int index = 0; index <= Content.Length; index++)
        {
            textBox.CaretIndex = index;

            (caret.GetAbsoluteLeft() - drawnLeft).ShouldBe(Pen(Content, index, oversampled), tolerance: 0.01f, $"caret index {index}");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Click_MixedGlyphs_ReturnsTheCharacterDrawnAtThePoint(bool oversampled, HorizontalAlignment alignment)
    {
        (TextBox textBox, Text contained) = Create(oversampled, alignment);
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.GetAbsoluteTop() + 1;

        for (int character = 0; character < Content.Length; character++)
        {
            float start = drawnLeft + Pen(Content, character, oversampled);
            float advance = Pen(Content, character + 1, oversampled) - Pen(Content, character, oversampled);
            textBox.GetCaretIndexAtPosition(start + advance * 0.25f, y).ShouldBe(character, $"left quarter of character {character}");
            textBox.GetCaretIndexAtPosition(start + advance * 0.75f, y).ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Selection_MixedGlyphs_StartsAtTheDrawnPenPosition(bool oversampled, HorizontalAlignment alignment)
    {
        (TextBox textBox, Text contained) = Create(oversampled, alignment);
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;

        foreach (int start in new[] { 0, 1, 3, 4, 9 })
        {
            textBox.SelectionStart = start;
            textBox.SelectionLength = 3;
            GraphicalUiElement selection = ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
                .OfType<GraphicalUiElement>()
                .Single(child => child.Name == "SelectionInstance" && child.Visible);

            (selection.GetAbsoluteLeft() - drawnLeft).ShouldBe(Pen(Content, start, oversampled), tolerance: 0.01f, $"selection starting at {start}");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_MixedGlyphs_IsAsWideAsTheSumOfTheDrawnAdvances(bool oversampled, HorizontalAlignment alignment)
    {
        (_, Text contained) = Create(oversampled, alignment);

        contained.MeasureDrawnString(Content, HorizontalMeasurementStyle.Full)
            .ShouldBe(Pen(Content, Content.Length, oversampled), tolerance: 0.01f);
    }

    internal static (TextBox TextBox, Text Contained) Create(bool oversampled, HorizontalAlignment alignment)
    {
        TextBox textBox = new();
        textBox.Width = 1000;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime textInstance = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        textInstance.HorizontalAlignment = alignment;
        Text contained = (Text)textInstance.RenderableComponent!;
        contained.BitmapFont = CreateFont(MeasuredAdvances);
        if (oversampled)
        {
            contained.SetOversampledDisplayFont(CreateFont(DrawnAdvances));
            contained.OversampleCompensationScale = Compensation;
        }
        textBox.Text = Content;
        textBox.IsFocused = true;
        return (textBox, contained);
    }

    // A font whose glyphs advance a whole number of pixels, as a rasterizer gives at one size.
    internal static BitmapFont CreateFont(Dictionary<char, int> advances)
    {
        int lineHeight = advances['W'];
        string chars = string.Concat(advances.OrderBy(pair => pair.Key).Select(pair =>
            $"char id={(int)pair.Key} x=0 y=0 width={pair.Value} height=13 xoffset=0 yoffset=4 xadvance={pair.Value} page=0 chnl=15\n"));
        string fontData =
$@"info face=""Stub"" size=-{lineHeight} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={lineHeight} base={lineHeight} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count={advances.Count}
{chars}";
        BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
        font.SetFontPattern(256, 256);
        return font;
    }
}
