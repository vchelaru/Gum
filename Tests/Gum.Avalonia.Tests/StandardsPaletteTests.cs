using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Standards chip palette under the element tree: chips keep their text inside their edge.</summary>
public class StandardsPaletteTests
{
    [AvaloniaFact]
    public void Chips_ClipTheirText_AndShowOnlyTheIcon_WhenNarrow()
    {
        AvaloniaStandardsPalette palette = new AvaloniaStandardsPalette();
        Border host = new Border { Width = 130, Child = palette, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top };
        Window window = new Window { Content = host, Width = 300, Height = 400 };
        window.Show();
        palette.RefreshChips(new[] { "ColoredRectangle", "Container" });
        window.UpdateLayout();

        TextBlock label = palette.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "ColoredRectangle");
        Border chip = label.GetVisualAncestors().OfType<Border>().First(border => border.ClipToBounds);
        label.IsVisible.ShouldBeTrue();
        // The long name is trimmed inside the chip rather than drawn past its edge (#4694).
        label.TranslatePoint(new global::Avalonia.Point(label.Bounds.Width, 0), chip)!.Value.X.ShouldBeLessThanOrEqualTo(chip.Bounds.Width);

        host.Width = 60;
        window.UpdateLayout();

        chip.Bounds.Width.ShouldBeLessThan(AvaloniaStandardsPalette.IconOnlyBelowWidth);
        label.IsVisible.ShouldBeFalse();
        window.Close();
    }
}
