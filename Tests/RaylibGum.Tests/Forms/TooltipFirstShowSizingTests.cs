using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Issue #4853 — a Tooltip's nine-slice background rendered too small on the very first
/// <see cref="Tooltip.Show"/> when its content was assigned while
/// <see cref="GraphicalUiElement.IsAllLayoutSuspended"/> was true (the common pattern for batch UI
/// setup). Text.RawText assignment is not suspend-aware, so it measured against raylib's native
/// placeholder font before the real font resolved; nothing invalidated that stale measurement once
/// the real font arrived (see the root-cause fix and pinning test in
/// TextFontReassignmentRefreshesWrapTests.cs). A second Show() happened to force an extra layout pass
/// that re-triggered the wrap, masking the bug as "fixes itself the second time".
/// </summary>
public class TooltipFirstShowSizingTests : BaseTestClass
{
    [Fact]
    public void Show_AfterContentAssignedUnderSuspension_SizesBackgroundToTextOnTheFirstShow()
    {
        Button button = new Button();
        GraphicalUiElement.IsAllLayoutSuspended = true;
        button.AddToRoot();
        button.ToolTip = "The quick brown fox jumps over the lazy dog";
        GraphicalUiElement.IsAllLayoutSuspended = false;

        Tooltip tooltip = ToolTipService.GetTooltip(button)!;
        var visual = (Gum.Forms.DefaultVisuals.V3.TooltipVisual)tooltip.Visual;

        tooltip.Show(cursorX: 10, cursorY: 10);

        visual.TextInstance.AbsoluteWidth.ShouldBeGreaterThan(300,
            "a single-line rendering of this string should be well over 300px wide; a value near 190px means it wrapped against a stale, too-narrow measurement");
        visual.Background.AbsoluteWidth.ShouldBe(visual.AbsoluteWidth,
            "the background should exactly track the container on the very first Show(), not lag behind the text");
    }
}
