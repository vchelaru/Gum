using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.SettingsWindow;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>The read-only Settings row (<see cref="SettingsRows.ReadOnly"/>) trims a long value with an ellipsis.</summary>
public class ReadOnlyValueWithChipDisplayTests
{
    private const string LongPath = @"C:\Users\someone\AppData\Roaming\Gum\a\very\long\folder\path\that\will\never\fit\in\the\narrow\row";

    private static (Window Window, TextBlock Value) Show(string path, double width)
    {
        InstanceMember member = SettingsRows.ReadOnly("p", "Settings folder", "hint", () => path, "default");
        ReadOnlyValueWithChipDisplay display = new ReadOnlyValueWithChipDisplay { InstanceMember = member, Width = width };
        Window window = new Window { Content = display, Width = width, Height = 120 };
        window.Show();
        display.Refresh();
        window.UpdateLayout();
        TextBlock value = display.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == path);
        return (window, value);
    }

    [AvaloniaFact]
    public void LongValue_TrimsWithCharacterEllipsis_AndTooltipHoldsFullPath()
    {
        (Window window, TextBlock value) = Show(LongPath, 300);

        value.TextTrimming.ShouldBe(TextTrimming.CharacterEllipsis);
        value.Bounds.Width.ShouldBeLessThan(300);
        value.TextLayout.TextLines.Any(l => l.HasCollapsed).ShouldBeTrue();
        ToolTip.GetTip(value).ShouldBe(LongPath);
        window.Close();
    }

    [AvaloniaFact]
    public void ShortValue_IsNotTrimmed()
    {
        (Window window, TextBlock value) = Show("C:/Gum", 600);

        value.TextLayout.TextLines.Any(l => l.HasCollapsed).ShouldBeFalse();
        value.Text.ShouldBe("C:/Gum");
        window.Close();
    }
}
