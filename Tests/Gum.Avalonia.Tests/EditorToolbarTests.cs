using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.EditorTab;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The editor tab's toolbar above the canvas.</summary>
public class EditorToolbarTests
{
    [AvaloniaFact]
    public void PlusMinusButtons_WidenWithTheUiBaseFontSize()
    {
        EditorToolbar toolbar = new EditorToolbar();
        toolbar.SizedButtons.Count.ShouldBe(4);

        toolbar.UpdateButtonSizes(18);

        foreach (Button button in toolbar.SizedButtons)
        {
            button.Width.ShouldBe(30);
        }

        toolbar.UpdateButtonSizes(12);

        foreach (Button button in toolbar.SizedButtons)
        {
            button.Width.ShouldBe(20);
        }
    }
}
