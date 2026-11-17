using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

// Issue #8208: a Text wraps, hit-tests and places its TextBox caret at the kerned pen positions it draws.
public class TextKerningTests : BaseTestClass
{
    // 'L', 'T', 'A', 'V' and space all advance 10; glyphs are 8 wide with xoffset 1.
    static BitmapFont CreateFont(string kerningLines)
    {
        string data =
"info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
"common lineHeight=12 base=10 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
"page id=0 file=\"x.png\"\n" +
"chars count=5\n" +
"char id=32 x=0  y=0 width=0 height=0  xoffset=0 yoffset=10 xadvance=10 page=0 chnl=15\n" +
"char id=65 x=10 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=76 x=20 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=84 x=30 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=86 x=40 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
kerningLines;
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }

    [Fact]
    public void WrappedText_KeepsAWordOnTheLine_WhenThePairAtTheJoinMakesItFit()
    {
        // Space then 'A' kerns by -3: "LT AV " measures 10 + 10 + 10 - 3 + 10 + 10 = 47 (its trailing space trims to 0).
        BitmapFont font = CreateFont("kernings count=1\nkerning first=32 second=65 amount=-3\n");
        font.MeasureString("LT AV ").ShouldBe(47);
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.Absolute;
        textRuntime.Width = 47;
        Text text = (Text)textRuntime.RenderableComponent!;
        text.BitmapFont = font;

        textRuntime.Text = "LT AV LT";

        text.WrappedText.Count.ShouldBe(2);
        text.WrappedText[0].ShouldBe("LT AV ");
    }

    [Fact]
    public void GetCharacterIndexAtPosition_UsesTheKernedPenPosition()
    {
        // Kerned, V spans 7 to 17 so 13 is past its middle; unkerned it spans 10 to 20.
        TextRuntime textRuntime = new();
        textRuntime.X = 20;
        textRuntime.Y = 30;
        textRuntime.WidthUnits = DimensionUnitType.Absolute;
        textRuntime.Width = 1000;
        Text text = (Text)textRuntime.RenderableComponent!;
        text.BitmapFont = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");
        textRuntime.Text = "AV";
        float left = text.UpdateIpsoForRenderingForTests().X;

        text.GetCharacterIndexAtPosition(left + 13, text.GetAbsoluteTop() + 1).ShouldBe(2);
    }

    [Fact]
    public void TextBox_PlacesTheCaretAndHitTestsAtTheKernedPenPosition()
    {
        TextBox textBox = new();
        textBox.Width = 1000;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime textInstance = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        Text text = (Text)textInstance.RenderableComponent!;
        text.BitmapFont = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");
        textBox.Text = "AV";
        float left = text.UpdateIpsoForRenderingForTests().X;
        float y = text.GetAbsoluteTop() + 1;

        textBox.GetCaretIndexAtPosition(left + 13, y).ShouldBe(2);

        // Between A and V the caret sits where V's pen starts: A's advance 10 plus the pair -3.
        textBox.CaretIndex = 1;
        GraphicalUiElement caret = textBox.Visual.GetGraphicalUiElementByName("CaretInstance")!;
        (caret.GetAbsoluteLeft() - left).ShouldBe(7, tolerance: 0.001f);
    }
}
