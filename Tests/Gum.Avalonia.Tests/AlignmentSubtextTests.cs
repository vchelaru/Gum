using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Alignment tab's Anchor and Dock labels say what they change; labels sit at the top of their rows.</summary>
public class AlignmentSubtextTests
{
    private static Control LabelCell(AlignmentView view, int row) =>
        view.Children.Single(c => Grid.GetRow(c) == row && Grid.GetColumn(c) == 0);

    private static string[] TextsIn(Control cell) =>
        cell.GetSelfAndVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();

    [AvaloniaFact]
    public void AnchorAndDockLabels_HaveSubtext_MarginHasNone()
    {
        AlignmentView view = new AlignmentView();

        TextsIn(LabelCell(view, 1)).ShouldBe(new[] { "Margin" });
        TextsIn(LabelCell(view, 2)).ShouldBe(new[] { "Anchor", "position only" });
        TextsIn(LabelCell(view, 4)).ShouldBe(new[] { "Dock", "position and size" });
    }

    [AvaloniaFact]
    public void RowLabels_AlignToTopOfRow()
    {
        AlignmentView view = new AlignmentView();

        foreach (int row in new[] { 1, 2, 4 })
        {
            LabelCell(view, row).VerticalAlignment.ShouldBe(VerticalAlignment.Top, $"row {row}");
        }
    }
}
