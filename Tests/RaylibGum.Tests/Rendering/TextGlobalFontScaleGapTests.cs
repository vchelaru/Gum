using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6827 gaps: wrapping and AbsoluteMultipliedByFontScale under SystemManagers.GlobalFontScale, matching MonoGame,
/// where the global scale enlarges glyphs against an unscaled Width and scales font-scale units.
/// </summary>
public class TextGlobalFontScaleGapTests : BaseTestClass
{
    const string Words = "aaaa bbbb cccc dddd eeee ffff gggg hhhh";

    static TextRuntime CreateWrapped(float width)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.Absolute;
        text.Width = width;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.Text = Words;
        return text;
    }

    static int LineCount(TextRuntime runtime) => ((Text)runtime.RenderableComponent).WrappedText.Count;

    [Fact]
    public void Wrap_GlobalFontScaleTwo_WrapsLikeHalfTheWidthAndDoublesHeight()
    {
        float saved = SystemManagers.GlobalFontScale;
        try
        {
            SystemManagers.GlobalFontScale = 1;
            TextRuntime narrowAtOne = CreateWrapped(60);
            int linesAtOne = LineCount(narrowAtOne);

            SystemManagers.GlobalFontScale = 2;
            TextRuntime wideAtTwo = CreateWrapped(120);
            TextRuntime narrowAtTwo = CreateWrapped(60);

            linesAtOne.ShouldBeGreaterThan(1, "because the text must wrap for this test to mean anything");
            LineCount(wideAtTwo).ShouldBe(linesAtOne, "because doubling glyphs and width wraps at the same words");
            LineCount(narrowAtTwo).ShouldBeGreaterThan(linesAtOne, "because doubled glyphs fit fewer words per line");
            wideAtTwo.AbsoluteHeight.ShouldBe(2 * narrowAtOne.AbsoluteHeight, 2, "because each line is twice as tall");
        }
        finally
        {
            SystemManagers.GlobalFontScale = saved;
        }
    }

    [Fact]
    public void AbsoluteMultipliedByFontScale_SystemManagersGlobalFontScaleTwo_DoublesSize()
    {
        float saved = SystemManagers.GlobalFontScale;
        try
        {
            SystemManagers.GlobalFontScale = 2;
            ContainerRuntime container = new();
            container.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
            container.HeightUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
            container.Width = 10;
            container.Height = 15;

            container.AbsoluteWidth.ShouldBe(20);
            container.AbsoluteHeight.ShouldBe(30);
        }
        finally
        {
            SystemManagers.GlobalFontScale = saved;
        }
    }
}
