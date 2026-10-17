using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// <see cref="DataUiGrid.RowPairs"/>: two members that belong together share one row, each cell still
/// a full row of its own (frame, marker, detail text), and a pair whose partner is missing or filtered
/// away shows as ordinary single rows.
/// </summary>
public class PairedRowsTests
{
    private sealed class PositionFixture
    {
        public float X { get; set; } = 1;
        public float Y { get; set; } = 2;
        public string Units { get; set; } = "px";
    }

    private static (DataUiGrid Grid, Window Window, DataUiCategoryView View) ShowGrid(params string[] memberNames)
    {
        PositionFixture fixture = new PositionFixture();
        MemberCategory category = new MemberCategory("Position");
        foreach (string memberName in memberNames)
        {
            category.Members.Add(new InstanceMember(memberName, fixture));
        }

        DataUiGrid grid = new DataUiGrid
        {
            RowPairs = new[] { new RowPair("X", "Y") },
            RowDecorator = editor => new Border { Tag = "frame", Child = editor },
        };
        grid.SetCategories(new List<MemberCategory> { category });
        Window window = new Window { Content = grid, Width = 500, Height = 400 };
        window.Show();
        window.UpdateLayout();
        return (grid, window, window.GetVisualDescendantsOfType<DataUiCategoryView>().Single());
    }

    private static SingleDataUiContainer Container(DataUiGrid grid, string memberName) =>
        grid.LiveContainers.Single(container => container.Member!.Name == memberName);

    private static double TopOf(Visual visual, Visual relativeTo) => visual.TranslatePoint(new Point(0, 0), relativeTo)!.Value.Y;

    [AvaloniaFact]
    public void PairedMembers_ShareOneRow_AtTheFirstMembersPlace()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView view) = ShowGrid("X", "Units", "Y");

        view.Rows.ItemCount.ShouldBe(2);
        SingleDataUiContainer x = Container(grid, "X");
        SingleDataUiContainer y = Container(grid, "Y");
        SingleDataUiContainer units = Container(grid, "Units");
        TopOf(y, window).ShouldBe(TopOf(x, window), 0.5);
        x.TranslatePoint(new Point(0, 0), window)!.Value.X
            .ShouldBeLessThan(y.TranslatePoint(new Point(0, 0), window)!.Value.X);
        TopOf(units, window).ShouldBeGreaterThan(TopOf(x, window));
        window.Close();
    }

    [AvaloniaFact]
    public void EachCellOfAPair_IsFramedLikeARow()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Y");

        Container(grid, "X").Parent.ShouldBeOfType<Border>().Tag.ShouldBe("frame");
        Container(grid, "Y").Parent.ShouldBeOfType<Border>().Tag.ShouldBe("frame");
        window.Close();
    }

    [AvaloniaFact]
    public void CellsOfAPair_UseNarrowLabels()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Units", "Y");

        double pairedLabel = ((TextBoxDisplay)Container(grid, "X").Displayer!).LabelHost.Bounds.Width;
        double unpairedLabel = ((TextBoxDisplay)Container(grid, "Units").Displayer!).LabelHost.Bounds.Width;
        pairedLabel.ShouldBeLessThan(unpairedLabel / 2);
        window.Close();
    }

    [AvaloniaFact]
    public void TabFromTheFirstCell_ReachesTheSecond()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Units", "Y");
        TextBox xBox = ((TextBoxDisplay)Container(grid, "X").Displayer!).TextBox;
        TextBox yBox = ((TextBoxDisplay)Container(grid, "Y").Displayer!).TextBox;
        xBox.Focus();

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);

        yBox.IsFocused.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void DetailText_ShowsUnderItsOwnCell()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Y");
        InstanceMember y = Container(grid, "Y").Member!;
        y.DetailText = "Set by Header.Height";
        window.UpdateLayout();

        SingleDataUiContainer xContainer = Container(grid, "X");
        SingleDataUiContainer yContainer = Container(grid, "Y");
        TextBlock hint = yContainer.GetVisualDescendantsOfType<TextBlock>().Single(text => text.IsVisible && text.Text == "Set by Header.Height");
        xContainer.GetVisualDescendantsOfType<TextBlock>().Any(text => text.IsVisible && text.Text == "Set by Header.Height").ShouldBeFalse();
        hint.TranslatePoint(new Point(hint.Bounds.Width, 0), window)!.Value.X
            .ShouldBeLessThanOrEqualTo(window.Bounds.Width);
        hint.TranslatePoint(new Point(0, 0), window)!.Value.X
            .ShouldBeGreaterThanOrEqualTo(yContainer.TranslatePoint(new Point(0, 0), window)!.Value.X);
        window.Close();
    }

    [AvaloniaFact]
    public void PanelTooNarrowForBothCells_StacksThePairIntoTwoRows_WithUsableFields()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Y");

        grid.Width = 100;
        window.UpdateLayout();
        window.UpdateLayout();

        SingleDataUiContainer x = Container(grid, "X");
        SingleDataUiContainer y = Container(grid, "Y");
        TopOf(y, window).ShouldBeGreaterThan(TopOf(x, window));
        TextBox xBox = ((TextBoxDisplay)x.Displayer!).TextBox;
        TextBox yBox = ((TextBoxDisplay)y.Displayer!).TextBox;
        xBox.Bounds.Width.ShouldBeGreaterThanOrEqualTo(44);
        yBox.Bounds.Width.ShouldBeGreaterThanOrEqualTo(44);

        grid.Width = 500;
        window.UpdateLayout();
        window.UpdateLayout();
        TopOf(y, window).ShouldBe(TopOf(x, window), 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void LongDetailText_DoesNotStackAPairThatFits()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView _) = ShowGrid("X", "Y");
        Container(grid, "Y").Member!.DetailText = "Set by a source with a very long name that would never fit on one line in half the row";
        window.UpdateLayout();

        TopOf(Container(grid, "Y"), window).ShouldBe(TopOf(Container(grid, "X"), window), 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void MemberWithoutItsPartner_ShowsAsASingleRow()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView view) = ShowGrid("X", "Units");

        view.Rows.ItemCount.ShouldBe(2);
        TopOf(Container(grid, "Units"), window).ShouldBeGreaterThan(TopOf(Container(grid, "X"), window));
        ((TextBoxDisplay)Container(grid, "X").Displayer!).LabelHost.Bounds.Width
            .ShouldBe(((TextBoxDisplay)Container(grid, "Units").Displayer!).LabelHost.Bounds.Width, 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void FilteringAwayOneOfAPair_LeavesTheOtherAsASingleRow_AndClearingRestoresThePair()
    {
        (DataUiGrid grid, Window window, DataUiCategoryView view) = ShowGrid("X", "Y");

        grid.ApplyMemberFilter(member => member.Name == "Y");
        window.UpdateLayout();
        view.Rows.ItemCount.ShouldBe(1);
        grid.LiveContainers.Select(container => container.Member!.Name).ShouldBe(new[] { "Y" });
        double filteredLabel = ((TextBoxDisplay)Container(grid, "Y").Displayer!).LabelHost.Bounds.Width;

        grid.ApplyMemberFilter(null);
        window.UpdateLayout();
        view.Rows.ItemCount.ShouldBe(1);
        TopOf(Container(grid, "Y"), window).ShouldBe(TopOf(Container(grid, "X"), window), 0.5);
        ((TextBoxDisplay)Container(grid, "Y").Displayer!).LabelHost.Bounds.Width.ShouldBeLessThan(filteredLabel);
        window.Close();
    }
}
