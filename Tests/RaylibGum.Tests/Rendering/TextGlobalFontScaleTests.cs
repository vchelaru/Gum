using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6827: SystemManagers.GlobalFontScale must scale drawn text and its reported size together, as on MonoGame.
/// </summary>
public class TextGlobalFontScaleTests : BaseTestClass
{
    [Theory]
    [InlineData("HHHH")]
    [InlineData("[FontScale=3]HH[/FontScale]HH")]
    public void Draw_GlobalFontScaleTwo_DrawsAndReportsTwiceTheSize(string markup)
    {
        float saved = SystemManagers.GlobalFontScale;
        try
        {
            SystemManagers.GlobalFontScale = 1;
            TextInkRender.Bounds inkAtOne = Render(markup, out float widthAtOne, out float heightAtOne);

            SystemManagers.GlobalFontScale = 2;
            TextInkRender.Bounds inkAtTwo = Render(markup, out float widthAtTwo, out float heightAtTwo);

            inkAtTwo.Width.ShouldBe(2 * inkAtOne.Width, 3, "because the drawn text doubles");
            inkAtTwo.Height.ShouldBe(2 * inkAtOne.Height, 3, "because the drawn text doubles");
            widthAtTwo.ShouldBe(2 * widthAtOne, 2, "because the reported width doubles with the drawn text");
            heightAtTwo.ShouldBe(2 * heightAtOne, 2, "because the reported height doubles with the drawn text");
        }
        finally
        {
            SystemManagers.GlobalFontScale = saved;
        }
    }

    [Fact]
    public void NominalFontScale_ShouldIncludeGlobalFontScale()
    {
        float saved = SystemManagers.GlobalFontScale;
        try
        {
            SystemManagers.GlobalFontScale = 2;
            Text text = new() { FontScale = 1.5f };

            text.NominalFontScale.ShouldBe(3);
        }
        finally
        {
            SystemManagers.GlobalFontScale = saved;
        }
    }

    private static TextInkRender.Bounds Render(string markup, out float width, out float height)
    {
        TextRuntime? runtime = null;
        Color[] pixels = TextInkRender.Render(markup, 4, 4, text =>
        {
            text.FontScale = 1;
            runtime = text;
        });
        width = runtime!.AbsoluteWidth;
        height = runtime.AbsoluteHeight;
        return TextInkRender.InkBounds(pixels);
    }
}
