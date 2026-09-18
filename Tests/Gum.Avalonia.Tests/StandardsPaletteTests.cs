using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Standards chip palette under the element tree: chips keep their text inside their edge.</summary>
public class StandardsPaletteTests
{
    [AvaloniaFact]
    public void CtrlClick_RaisesAddAsChildOfSelectionRequested_NotAddToCurrentRequested()
    {
        // #4837 follow-up: plain Ctrl+click and Ctrl+Shift+click on a chip must route through the
        // same branch (add as a child of the current selection). A prior split - only Ctrl+Shift
        // took that branch - left plain Ctrl+click still doing the old root-level add.
        AvaloniaStandardsPalette palette = new AvaloniaStandardsPalette();
        Window window = new Window { Content = palette, Width = 300, Height = 400 };
        window.Show();
        palette.RefreshChips(new[] { "Container" });
        window.UpdateLayout();

        string? addAsChildTypeName = null;
        string? addToCurrentTypeName = null;
        palette.AddAsChildOfSelectionRequested = typeName => addAsChildTypeName = typeName;
        palette.AddToCurrentRequested = typeName => addToCurrentTypeName = typeName;

        Border chip = palette.GetVisualDescendants().OfType<Border>().First(border => border.ClipToBounds);
        Point point = chip.TranslatePoint(new Point(chip.Bounds.Width / 2, chip.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.Control);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();

        addAsChildTypeName.ShouldBe("Container");
        addToCurrentTypeName.ShouldBeNull();
        window.Close();
    }

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
