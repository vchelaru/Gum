using Gum.Forms.Controls;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// Skia twin of LabelTests.Text_ShouldParseBbCodeTags. Skia keeps the tags in RawText and parses them into styled runs (#6511).
/// </summary>
public class LabelSkiaTests : SkiaFormsTestBase
{
    [Fact]
    public void Text_ShouldParseBbCodeTags_IntoStyledRuns()
    {
        Label label = new();
        label.Text = "This is [IsBold=true]bold[/IsBold] and [IsItalic=true]italic[/IsItalic] text.";

        label.TextComponent.ShouldNotBeNull();
        Text text = (Text)label.TextComponent.RenderableComponent;
        List<Text.StyledTextRun> runs = text.GetStyledRuns();
        string.Concat(runs.Select(r => r.Text)).ShouldBe("This is bold and italic text.");
        runs.Single(r => r.Text == "bold").Style.FontWeight.ShouldBe(700);
        runs.Single(r => r.Text == "italic").Style.FontItalic.ShouldBeTrue();
    }
}
