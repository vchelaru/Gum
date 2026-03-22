using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tests for the experimental Grid control, verifying layout, row/column definitions,
/// and child placement behavior.
/// </summary>
public class GridTests : BaseTestClass
{
    [Fact]
    public void AddChild_ShouldPlaceChildInCorrectCell()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 1);

        child.Visual.Parent.ShouldNotBeNull();
    }

    [Fact]
    public void AddChild_ShouldClampColumnToLastValid_WhenColumnExceedsDefinitions()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 10);

        // Should be clamped to last column (index 1)
        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement lastCell = (GraphicalUiElement)rowContainer.Children[1];

        lastCell.Children.Contains(child.Visual).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_ShouldClampRowToLastValid_WhenRowExceedsDefinitions()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 10, column: 0);

        // Should be clamped to last row (index 1)
        GraphicalUiElement lastRow = (GraphicalUiElement)grid.Visual.Children[1];
        GraphicalUiElement cell = (GraphicalUiElement)lastRow.Children[0];

        cell.Children.Contains(child.Visual).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_ShouldPlaceMultipleChildrenInSameCell()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child1 = new Panel();
        Panel child2 = new Panel();
        grid.AddChild(child1, row: 0, column: 0);
        grid.AddChild(child2, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Children.Contains(child1.Visual).ShouldBeTrue();
        cell.Children.Contains(child2.Visual).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_ShouldUpdatePlacement_WhenCalledTwiceForSameChild()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);
        grid.AddChild(child, row: 1, column: 1);

        // The child should only appear once in the entire visual tree
        int childCount = 0;
        foreach (GraphicalUiElement rowContainer in grid.Visual.Children.Cast<GraphicalUiElement>())
        {
            foreach (GraphicalUiElement cell in rowContainer.Children.Cast<GraphicalUiElement>())
            {
                if (cell.Children.Contains(child.Visual))
                {
                    childCount++;
                }
            }
        }

        childCount.ShouldBe(1);
    }

    [Fact]
    public void AutoColumnWidth_ShouldUnifyToWidestContent()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Row 0 has a 80px wide child in the Auto column
        Panel narrow = new Panel();
        narrow.Width = 80;
        narrow.WidthUnits = DimensionUnitType.Absolute;
        narrow.Height = 20;
        narrow.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(narrow, row: 0, column: 0);

        // Row 1 has a 150px wide child in the Auto column
        Panel wide = new Panel();
        wide.Width = 150;
        wide.WidthUnits = DimensionUnitType.Absolute;
        wide.Height = 20;
        wide.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(wide, row: 1, column: 0);

        // Both cells in column 0 should be unified to 150 (the wider content)
        GraphicalUiElement row0 = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement row1 = (GraphicalUiElement)grid.Visual.Children[1];
        GraphicalUiElement cell0_0 = (GraphicalUiElement)row0.Children[0];
        GraphicalUiElement cell1_0 = (GraphicalUiElement)row1.Children[0];

        cell0_0.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell1_0.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell0_0.Width.ShouldBe(150);
        cell1_0.Width.ShouldBe(150);
    }

    [Fact]
    public void AutoRowHeight_ShouldSizeToContent()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 75;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
        rowContainer.Height.ShouldBe(0);
    }

    [Fact]
    public void CellContainer_ShouldFillRowHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.HeightUnits.ShouldBe(DimensionUnitType.RelativeToParent);
        cell.Height.ShouldBe(0);
    }

    [Fact]
    public void ColumnDefinitions_AbsoluteWidth_ShouldSetAbsoluteWidthUnits()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(200);
    }

    [Fact]
    public void ColumnDefinitions_StarWidth_ShouldSetRatioWidthUnits()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell0 = (GraphicalUiElement)rowContainer.Children[0];
        GraphicalUiElement cell1 = (GraphicalUiElement)rowContainer.Children[1];

        cell0.WidthUnits.ShouldBe(DimensionUnitType.Ratio);
        cell0.Width.ShouldBe(1);
        cell1.WidthUnits.ShouldBe(DimensionUnitType.Ratio);
        cell1.Width.ShouldBe(2);
    }

    [Fact]
    public void ColumnDefinition_DefaultWidth_ShouldBeStar()
    {
        ColumnDefinition columnDef = new ColumnDefinition();

        columnDef.Width.IsStar.ShouldBeTrue();
        columnDef.Width.Value.ShouldBe(1);
    }

    [Fact]
    public void ColumnDefinitions_ShouldUpdateLayout_WhenModifiedAfterChildrenAdded()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);

        // Now add a second column — layout should rebuild
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.Children.Count.ShouldBe(2);

        GraphicalUiElement cell1 = (GraphicalUiElement)rowContainer.Children[1];
        cell1.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell1.Width.ShouldBe(200);

        // Original child should still be in first cell
        GraphicalUiElement cell0 = (GraphicalUiElement)rowContainer.Children[0];
        cell0.Children.Contains(child.Visual).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateImplicit1x1Grid_WhenNoDefinitions()
    {
        Grid grid = new Grid();

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);

        // Should have 1 implicit row with 1 implicit cell
        grid.Visual.Children.Count.ShouldBe(1);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.Children.Count.ShouldBe(1);

        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];
        cell.Children.Contains(child.Visual).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateVisual()
    {
        Grid grid = new Grid();

        grid.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetTopToBottomStack()
    {
        Grid grid = new Grid();

        grid.Visual.ChildrenLayout.ShouldBe(ChildrenLayout.TopToBottomStack);
    }

    [Fact]
    public void GridLength_DefaultConstructor_ShouldBeAbsolute()
    {
        GridLength length = new GridLength(100);

        length.IsAbsolute.ShouldBeTrue();
        length.Value.ShouldBe(100);
    }

    [Fact]
    public void GridLength_StarConstructor_ShouldBeStar()
    {
        GridLength length = new GridLength(2, GridUnitType.Star);

        length.IsStar.ShouldBeTrue();
    }

    [Fact]
    public void RefreshLayout_ShouldCreateCorrectNumberOfCellsPerRow()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        foreach (GraphicalUiElement rowContainer in grid.Visual.Children.Cast<GraphicalUiElement>())
        {
            rowContainer.Children.Count.ShouldBe(3);
        }
    }

    [Fact]
    public void RefreshLayout_ShouldCreateCorrectNumberOfRowContainers_AfterRemovingDefinition()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        grid.Visual.Children.Count.ShouldBe(3);

        grid.RowDefinitions.RemoveAt(2);

        grid.Visual.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void RefreshLayout_ShouldCreateCorrectNumberOfRowContainers()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        grid.Visual.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void RemoveChild_ShouldRemoveFromVisualTree()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);
        grid.RemoveChild(child);

        bool found = false;
        foreach (GraphicalUiElement rowContainer in grid.Visual.Children.Cast<GraphicalUiElement>())
        {
            foreach (GraphicalUiElement cell in rowContainer.Children.Cast<GraphicalUiElement>())
            {
                if (cell.Children.Contains(child.Visual))
                {
                    found = true;
                }
            }
        }

        found.ShouldBeFalse();
    }

    [Fact]
    public void RowContainer_ShouldFillGridWidth()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.WidthUnits.ShouldBe(DimensionUnitType.RelativeToParent);
        rowContainer.Width.ShouldBe(0);
    }

    [Fact]
    public void RowDefinitions_AbsoluteHeight_ShouldSetAbsoluteHeightUnits()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        rowContainer.Height.ShouldBe(150);
    }

    [Fact]
    public void RowDefinitions_StarHeight_ShouldSetRatioHeightUnits()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        GraphicalUiElement row0 = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement row1 = (GraphicalUiElement)grid.Visual.Children[1];

        row0.HeightUnits.ShouldBe(DimensionUnitType.Ratio);
        row1.HeightUnits.ShouldBe(DimensionUnitType.Ratio);
    }

    [Fact]
    public void RowDefinition_DefaultHeight_ShouldBeStar()
    {
        RowDefinition rowDef = new RowDefinition();

        rowDef.Height.IsStar.ShouldBeTrue();
        rowDef.Height.Value.ShouldBe(1);
    }
}
