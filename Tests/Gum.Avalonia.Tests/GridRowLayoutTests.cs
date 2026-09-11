using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The property grid's rows fit their panel, as the WPF grid's do.</summary>
public class GridRowLayoutTests
{
    private class NullableSettings
    {
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
    }

    private class TextSettings
    {
        public string Name { get; set; } = "";
        public int X { get; set; }
    }

    [AvaloniaFact]
    public void TextRow_IsAsTallAsTheWpfRow()
    {
        DataUiGrid grid = new DataUiGrid { Instance = new TextSettings() };
        Window window = new Window { Content = grid, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        // The WPF grid's text rows are 24px: a 22px field with a pixel above and below.
        List<double> rowHeights = grid.GetVisualDescendants().OfType<SingleDataUiContainer>().Select(row => row.Bounds.Height).ToList();
        rowHeights.Count.ShouldBe(2);
        rowHeights.ShouldAllBe(height => height <= 24, string.Join(", ", rowHeights));
        window.Close();
    }

    [AvaloniaFact]
    public void NullableRow_KeepsItsIsNullCheckBox_InsideThePanel_BesideTheScrollBar()
    {
        DataUiGrid grid = new DataUiGrid { Instance = new NullableSettings() };
        // Shorter than the rows, so the vertical scroll bar shows and takes its width from them.
        Window window = new Window { Content = grid, Width = 400, Height = 70 };
        window.Show();
        window.UpdateLayout();

        // The grid's own bar, not one inside a text box.
        ScrollViewer viewer = grid.GetVisualDescendants().OfType<ScrollViewer>().First();
        ScrollBar scrollBar = viewer.GetVisualDescendants().OfType<ScrollBar>().First(bar => bar.TemplatedParent == viewer && bar.Orientation == Orientation.Vertical);
        scrollBar.IsVisible.ShouldBeTrue();
        CheckBox isNull = window.GetVisualDescendants().OfType<CheckBox>().First(box => Equals(box.Content, "Is Null"));
        isNull.Bounds.Width.ShouldBeGreaterThan(0);
        double right = isNull.Bounds.Width;
        for (global::Avalonia.Visual? visual = isNull; visual != null && visual != window; visual = visual.GetVisualParent())
        {
            right += visual.Bounds.X;
        }
        right.ShouldBeLessThanOrEqualTo(400 - scrollBar.Bounds.Width);
        window.Close();
    }
}
