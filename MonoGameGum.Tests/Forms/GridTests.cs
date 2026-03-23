using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
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
    public void AddChild_ShouldClampToLastRow_WhenRowDefinitionRemoved()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        grid.AddChild(child, row: 1, column: 0);

        // Remove the second row — child must be clamped to row 0
        grid.RowDefinitions.RemoveAt(1);

        GraphicalUiElement row0Container = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)row0Container.Children[0];

        cell.Children.Contains(child.Visual).ShouldBeTrue();
    }

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
    public void AddChild_FrameworkElementRendersBeforeGue_WhenAddedFirst()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel panel = new Panel();
        ColoredRectangleRuntime gue = new ColoredRectangleRuntime();

        grid.AddChild(panel, row: 0, column: 0);
        grid.AddChild(gue, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Children.IndexOf(panel.Visual).ShouldBe(0);
        cell.Children.IndexOf(gue).ShouldBe(1);
    }

    [Fact]
    public void AddChild_FrameworkElement_WithNullChild_ShouldThrow()
    {
        Grid grid = new Grid();
        FrameworkElement child = null!;
        Should.Throw<ArgumentNullException>(() => grid.AddChild(child, row: 0, column: 0));
    }

    [Fact]
    public void AddChild_GraphicalUiElement_ShouldClampColumnToLastValid_WhenColumnExceedsDefinitions()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        grid.AddChild(gue, row: 0, column: 10);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement lastCell = (GraphicalUiElement)rowContainer.Children[1];

        lastCell.Children.Contains(gue).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_GraphicalUiElement_ShouldClampRowToLastValid_WhenRowExceedsDefinitions()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        grid.AddChild(gue, row: 10, column: 0);

        GraphicalUiElement lastRow = (GraphicalUiElement)grid.Visual.Children[1];
        GraphicalUiElement cell = (GraphicalUiElement)lastRow.Children[0];

        cell.Children.Contains(gue).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_GraphicalUiElement_ShouldClampToLastRow_WhenRowDefinitionRemoved()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        grid.AddChild(gue, row: 1, column: 0);

        grid.RowDefinitions.RemoveAt(1);

        GraphicalUiElement row0Container = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)row0Container.Children[0];

        cell.Children.Contains(gue).ShouldBeTrue();
    }

    [Fact]
    public void AddChild_GraphicalUiElement_ShouldPlaceChildInCorrectCell()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        grid.AddChild(gue, row: 0, column: 1);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[1];

        cell.Children.Contains(gue).ShouldBeTrue();
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
    public void AddColumn_ShouldAddColumnDefinitionWithCorrectWidth()
    {
        Grid grid = new Grid();
        grid.AddColumn(new GridLength(150));

        grid.ColumnDefinitions.Count.ShouldBe(1);
        grid.ColumnDefinitions[0].Width.IsAbsolute.ShouldBeTrue();
        grid.ColumnDefinitions[0].Width.Value.ShouldBe(150);
    }

    [Fact]
    public void AddRow_ShouldAddRowDefinitionWithCorrectHeight()
    {
        Grid grid = new Grid();
        grid.AddRow(new GridLength(2, GridUnitType.Star));

        grid.RowDefinitions.Count.ShouldBe(1);
        grid.RowDefinitions[0].Height.IsStar.ShouldBeTrue();
        grid.RowDefinitions[0].Height.Value.ShouldBe(2);
    }

    [Fact]
    public void AddChild_GraphicalUiElement_WithNegativeColumnIndex_ShouldThrow()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Should.Throw<ArgumentOutOfRangeException>(() => grid.AddChild(new InteractiveGue(new InvisibleRenderable()), row: 0, column: -1));
    }

    [Fact]
    public void AddChild_GraphicalUiElement_WithNegativeRowIndex_ShouldThrow()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Should.Throw<ArgumentOutOfRangeException>(() => grid.AddChild(new InteractiveGue(new InvisibleRenderable()), row: -1, column: 0));
    }

    [Fact]
    public void AddChild_GraphicalUiElement_WithNullChild_ShouldThrow()
    {
        Grid grid = new Grid();
        GraphicalUiElement child = null!;
        Should.Throw<ArgumentNullException>(() => grid.AddChild(child, row: 0, column: 0));
    }

    [Fact]
    public void AddChild_GueRendersBeforeFrameworkElement_WhenAddedFirst()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        ColoredRectangleRuntime gue = new ColoredRectangleRuntime();
        Panel panel = new Panel();

        grid.AddChild(gue, row: 0, column: 0);
        grid.AddChild(panel, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Children.IndexOf(gue).ShouldBe(0);
        cell.Children.IndexOf(panel.Visual).ShouldBe(1);
    }

    [Fact]
    public void AddChild_MultipleFrameworkElementsInCell_PreservesInsertionOrder()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel panel1 = new Panel();
        Panel panel2 = new Panel();
        Panel panel3 = new Panel();

        grid.AddChild(panel1, row: 0, column: 0);
        grid.AddChild(panel2, row: 0, column: 0);
        grid.AddChild(panel3, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Children.IndexOf(panel1.Visual).ShouldBe(0);
        cell.Children.IndexOf(panel2.Visual).ShouldBe(1);
        cell.Children.IndexOf(panel3.Visual).ShouldBe(2);
    }

    [Fact]
    public void AddChild_MultipleGuesInCell_PreservesInsertionOrder()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        ColoredRectangleRuntime gue1 = new ColoredRectangleRuntime();
        ColoredRectangleRuntime gue2 = new ColoredRectangleRuntime();
        ColoredRectangleRuntime gue3 = new ColoredRectangleRuntime();

        grid.AddChild(gue1, row: 0, column: 0);
        grid.AddChild(gue2, row: 0, column: 0);
        grid.AddChild(gue3, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Children.IndexOf(gue1).ShouldBe(0);
        cell.Children.IndexOf(gue2).ShouldBe(1);
        cell.Children.IndexOf(gue3).ShouldBe(2);
    }

    [Fact]
    public void AddChild_WithNegativeColumnIndex_ShouldThrow()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Should.Throw<ArgumentOutOfRangeException>(() => grid.AddChild(new Panel(), row: 0, column: -1));
    }

    [Fact]
    public void AddChild_WithNegativeRowIndex_ShouldThrow()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Should.Throw<ArgumentOutOfRangeException>(() => grid.AddChild(new Panel(), row: -1, column: 0));
    }

    [Fact]
    public void AddChild_WithNoRowColumn_FrameworkElement_ShouldThrowNotSupportedException()
    {
        Grid grid = new Grid();

        Should.Throw<NotSupportedException>(() => grid.AddChild(new Panel()));
    }

    [Fact]
    public void AddChild_WithNoRowColumn_GraphicalUiElement_ShouldThrowNotSupportedException()
    {
        Grid grid = new Grid();

        Should.Throw<NotSupportedException>(() => grid.AddChild(new InteractiveGue(new InvisibleRenderable())));
    }

    [Fact]
    public void AutoColumn_MultipleChildren_ShouldReflectWidestChild()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(100f);
    }

    [Fact]
    public void AutoColumn_ShouldNotCorruptLayout_AfterChildRemovedViaRemoveChild_AndSizeChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        grid.RemoveChild(child1);

        // Size change on removed child must not corrupt layout
        child1.Width = 200;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(60f);
    }

    [Fact]
    public void AutoColumn_ShouldNotCorruptLayout_AfterChildReparentedExternally_AndSizeChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        InteractiveGue otherGue = new InteractiveGue(new InvisibleRenderable());
        child1.Visual.Parent = otherGue;

        child1.Width = 200;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(60f);
    }

    [Fact]
    public void AutoColumn_ShouldNotCorruptLayout_AfterChildReparentedToNull_AndSizeChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        child1.Visual.Parent = null;

        child1.Width = 200;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(60f);
    }

    [Fact]
    public void AutoColumn_ShouldNotCorruptLayout_AfterGueRemovedViaRemoveChild_AndSizeChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        gue.Width = 100;
        gue.WidthUnits = DimensionUnitType.Absolute;
        gue.Height = 20;
        gue.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(gue, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        grid.RemoveChild(gue);

        gue.Width = 200;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(60f);
    }

    [Fact]
    public void AutoColumn_ShouldNotDoubleSubscribe_WhenChildMoved()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        // Move child to column 1
        grid.AddChild(child, row: 0, column: 1);

        child.Width = 100;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell0 = (GraphicalUiElement)rowContainer.Children[0];
        GraphicalUiElement cell1 = (GraphicalUiElement)rowContainer.Children[1];

        cell0.Children.Contains(child.Visual).ShouldBeFalse();
        cell1.Width.ShouldBe(100f);
    }

    [Fact]
    public void AutoColumn_RelativeToChildrenGrid_ShouldSetRowContainerToRelativeToChildren()
    {
        Grid grid = new Grid();
        grid.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;

        grid.AddRow(new GridLength(1, GridUnitType.Star));
        grid.AddColumn(GridLength.Auto);

        InteractiveGue child = new InteractiveGue(new InvisibleRenderable());
        child.Width = 80;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.WidthUnits.ShouldBe(DimensionUnitType.RelativeToChildren);

        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];
        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(80f);
    }

    [Fact]
    public void AutoColumn_ShouldReflectWidestChild_WhenMultipleChildrenInSameCell()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 120;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(120f);
    }

    [Fact]
    public void AutoColumn_ShouldRemainCorrect_WhenChildInStarColumnChangesHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child0 = new Panel();
        child0.Width = 100;
        child0.WidthUnits = DimensionUnitType.Absolute;
        child0.Height = 20;
        child0.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child0, row: 0, column: 0);

        Panel child1 = new Panel();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 1);

        // Verify initial state
        GraphicalUiElement rowContainer0 = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell0 = (GraphicalUiElement)rowContainer0.Children[0];
        cell0.Width.ShouldBe(100f);

        // Simulate text wrapping in the star column by changing height
        child1.Height = 80;

        // Re-grab references after relayout
        GraphicalUiElement rowContainerAfter = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell0After = (GraphicalUiElement)rowContainerAfter.Children[0];

        cell0After.Width.ShouldBe(100f);
    }

    [Fact]
    public void AutoColumn_ShouldShrink_WhenWidestChildShrinks()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 150;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 20;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        // Shrink the widest child — cell should shrink to child2's width
        child1.Width = 50;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(80f);
    }

    [Fact]
    public void AutoColumn_ShouldShrinkWhenWidestChildHeightChanges_TriggeringRelayout()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child1 = new Panel();
        child1.Width = 150;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 0);

        Panel child2 = new Panel();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 30;
        child2.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child2, row: 0, column: 0);

        GraphicalUiElement rowContainer0 = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell0 = (GraphicalUiElement)rowContainer0.Children[0];
        cell0.Width.ShouldBe(150f);

        // Height change should trigger RefreshLayout without corrupting widths
        child1.Height = 60;

        // Re-grab references after potential relayout
        GraphicalUiElement rowContainerAfter = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cellAfter = (GraphicalUiElement)rowContainerAfter.Children[0];

        // Widths haven't changed — auto column should still reflect the widest child
        cellAfter.Width.ShouldBe(150f);
    }

    [Fact]
    public void AutoColumn_ShouldStillUpdateAfterDefinitionsChanged()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        // Adding a second column triggers RefreshLayout, which internally reparents children
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // SizeChanged subscription must survive the internal reparenting done by RefreshLayout
        child.Width = 120;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(120f);
    }

    [Fact]
    public void AutoColumn_ShouldUpdateWidth_WhenChildWidthShrinks()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child = new Panel();
        child.Width = 150;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        child.Width = 50;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(50f);
    }

    [Fact]
    public void AutoRow_ShouldUpdateHeight_WhenChildHeightChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 30;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        child.Height = 80;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        // Auto rows use RelativeToChildren — Gum's layout system handles the sizing naturally.
        // Verify HeightUnits remains RelativeToChildren (not stale Absolute from RefreshLayout).
        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
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
    public void AutoColumn_ShouldUpdateWidth_AfterChildRemovedAndReAdded()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        grid.RemoveChild(child);
        grid.AddChild(child, row: 0, column: 0);

        // After re-add, SizeChanged must be re-subscribed
        child.Width = 120;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(120f);
    }

    [Fact]
    public void AutoColumn_ShouldUpdateWidth_WhenChildWidthChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        // Change child width after it has already been added — no refresh is called
        child.Width = 150;

        // Re-grab references from the live visual tree (do not reuse pre-change references)
        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(150f);
    }

    [Fact]
    public void AutoRow_ShouldComputeHeight_WhenChildHasAbsoluteHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        InteractiveGue child = new InteractiveGue(new InvisibleRenderable());
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 60;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.UpdateLayout();

        rowContainer.GetAbsoluteHeight().ShouldBe(60f);
    }

    [Fact]
    public void AutoRow_ShouldRemainRelativeToChildren_AfterChildHeightChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 30;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        GraphicalUiElement rowContainer0 = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer0.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);

        // Simulate text wrapping by changing the child's height
        child.Height = 80;

        // Re-grab reference after potential relayout
        GraphicalUiElement rowContainerAfter = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainerAfter.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
    }

    [Fact]
    public void AutoRow_ShouldRemainRelativeToChildren_AfterChildInStarColumnChangesHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child0 = new Panel();
        child0.Width = 50;
        child0.WidthUnits = DimensionUnitType.Absolute;
        child0.Height = 20;
        child0.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child0, row: 0, column: 0);

        Panel child1 = new Panel();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 20;
        child1.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child1, row: 0, column: 1);

        // Simulate text wrapping in the star column
        child1.Height = 90;

        // Re-grab reference after potential relayout
        GraphicalUiElement rowContainerAfter = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainerAfter.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
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
    public void CellContainer_InAbsoluteRow_ShouldHaveRelativeToParentHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.HeightUnits.ShouldBe(DimensionUnitType.RelativeToParent);
    }

    [Fact]
    public void CellContainer_InAutoRow_ShouldHaveRelativeToChildrenHeight()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
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
    public void ColumnDefinition_Constructor_GridLength_ShouldSetWidth()
    {
        ColumnDefinition columnDef = new ColumnDefinition(new GridLength(200));

        columnDef.Width.IsAbsolute.ShouldBeTrue();
        columnDef.Width.Value.ShouldBe(200);
    }

    [Fact]
    public void ColumnDefinition_MaxWidth_AbsoluteColumn_ClampsToMaximum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300), MaxWidth = 200 });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(200);
    }

    [Fact]
    public void ColumnDefinition_MaxWidth_AutoColumn_ClampsWideContent()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MaxWidth = 100 });

        Panel wide = new Panel();
        wide.Width = 200;
        wide.WidthUnits = DimensionUnitType.Absolute;
        wide.Height = 20;
        wide.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(wide, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(100);
    }

    [Fact]
    public void ColumnDefinition_MinWidth_AbsoluteColumn_ClampsToMinimum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50), MinWidth = 100 });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(100);
    }

    [Fact]
    public void ColumnDefinition_MinWidth_AutoColumn_ClampsNarrowContent()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 150 });

        Panel narrow = new Panel();
        narrow.Width = 50;
        narrow.WidthUnits = DimensionUnitType.Absolute;
        narrow.Height = 20;
        narrow.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(narrow, row: 0, column: 0);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.WidthUnits.ShouldBe(DimensionUnitType.Absolute);
        cell.Width.ShouldBe(150);
    }

    [Fact]
    public void ColumnDefinition_MinWidth_AutoColumn_ClampsEmptyCell()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
        // No child added — content width is 0, MinWidth should still win

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.Width.ShouldBe(80f);
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
    public void RemoveChild_FrameworkElement_WithNullChild_ShouldThrow()
    {
        Grid grid = new Grid();
        FrameworkElement child = null!;
        Should.Throw<ArgumentNullException>(() => grid.RemoveChild(child));
    }

    [Fact]
    public void RemoveChild_GraphicalUiElement_WithNullChild_ShouldThrow()
    {
        Grid grid = new Grid();
        GraphicalUiElement child = null!;
        Should.Throw<ArgumentNullException>(() => grid.RemoveChild(child));
    }

    [Fact]
    public void RemoveChild_GraphicalUiElement_ShouldRemoveFromVisualTree()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        InteractiveGue gue = new InteractiveGue(new InvisibleRenderable());
        grid.AddChild(gue, row: 0, column: 0);
        grid.RemoveChild(gue);

        bool found = false;
        foreach (GraphicalUiElement rowContainer in grid.Visual.Children.Cast<GraphicalUiElement>())
        {
            foreach (GraphicalUiElement cell in rowContainer.Children.Cast<GraphicalUiElement>())
            {
                if (cell.Children.Contains(gue))
                {
                    found = true;
                }
            }
        }

        found.ShouldBeFalse();
    }

    [Fact]
    public void RemoveChild_ShouldBeNoOp_WhenChildNotInGrid()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        Panel panel = new Panel();

        // panel was never added — RemoveChild must not throw
        Should.NotThrow(() => grid.RemoveChild(panel));
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
    public void RowContainer_ShouldHaveRelativeToChildrenWidth_WhenGridIsRelativeToChildren()
    {
        Grid grid = new Grid();
        grid.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        grid.AddRow(new GridLength(1, GridUnitType.Star));
        grid.AddColumn(GridLength.Auto);

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.WidthUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
    }

    [Fact]
    public void RowContainer_ShouldHaveRelativeToParentWidth_WhenGridIsRelativeToParent()
    {
        Grid grid = new Grid();
        grid.AddRow(new GridLength(1, GridUnitType.Star));

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.WidthUnits.ShouldBe(DimensionUnitType.RelativeToParent);
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
    public void RowDefinitions_ShouldUpdateLayout_WhenModifiedAfterChildrenAdded()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);

        // Add a second row — layout should rebuild
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

        grid.Visual.Children.Count.ShouldBe(2);

        GraphicalUiElement row1Container = (GraphicalUiElement)grid.Visual.Children[1];
        row1Container.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        row1Container.Height.ShouldBe(80);

        // Original child should still be in row 0, cell 0
        GraphicalUiElement row0Container = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)row0Container.Children[0];
        cell.Children.Contains(child.Visual).ShouldBeTrue();
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

    [Fact]
    public void RowDefinition_Constructor_GridLength_ShouldSetHeight()
    {
        RowDefinition rowDef = new RowDefinition(GridLength.Auto);

        rowDef.Height.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void RowDefinition_MaxHeight_AutoRow_ClampsToMaximum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MaxHeight = 100 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 200;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        // Cells in auto rows are now RelativeToChildren, so the child's height (200) propagates
        // up to the row container. ApplyMinMaxAutoRowHeights then clamps it to MaxHeight (100).
        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        rowContainer.Height.ShouldBe(100f);
    }

    [Fact]
    public void RowDefinition_MaxHeight_AbsoluteRow_ClampsToMaximum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300), MaxHeight = 200 });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        rowContainer.Height.ShouldBe(200);
    }

    [Fact]
    public void RowDefinition_MinHeight_AutoRow_ClampsToMinimum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 50 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        grid.AddChild(child, row: 0, column: 0);

        // In unit test context, GetAbsoluteHeight() returns 0.
        // MinHeight (50) > computed (0), so clamping fires and sets Absolute height.
        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        rowContainer.Height.ShouldBe(50f);
    }

    [Fact]
    public void RowDefinition_MinHeight_AbsoluteRow_ClampsToMinimum()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50), MinHeight = 100 });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
        rowContainer.Height.ShouldBe(100);
    }

    [Fact]
    public void StarColumn_MaxWidth_ShouldBeSetOnCellContainer()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MaxWidth = 100 });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.MaxWidth.ShouldBe(100f);
    }

    [Fact]
    public void StarColumn_MinWidth_ShouldBeSetOnCellContainer()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 50 });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.MinWidth.ShouldBe(50f);
    }

    [Fact]
    public void StarColumn_NoMinWidth_ShouldLeaveMinWidthNull()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        cell.MinWidth.ShouldBeNull();
    }

    [Fact]
    public void StarColumn_ShouldNotChangeWidthUnits_WhenChildSizeChanges()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Panel child = new Panel();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.Absolute;
        grid.AddChild(child, row: 0, column: 0);

        child.Width = 150;

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];
        GraphicalUiElement cell = (GraphicalUiElement)rowContainer.Children[0];

        // Star columns should remain Ratio — no Auto logic should run
        cell.WidthUnits.ShouldBe(DimensionUnitType.Ratio);
    }

    [Fact]
    public void StarRow_MaxHeight_ShouldBeSetOnRowContainer()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MaxHeight = 200 });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.MaxHeight.ShouldBe(200f);
    }

    [Fact]
    public void StarRow_MinHeight_ShouldBeSetOnRowContainer()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 40 });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.MinHeight.ShouldBe(40f);
    }

    [Fact]
    public void StarRow_NoMinHeight_ShouldLeaveMinHeightNull()
    {
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        GraphicalUiElement rowContainer = (GraphicalUiElement)grid.Visual.Children[0];

        rowContainer.MinHeight.ShouldBeNull();
    }
}
