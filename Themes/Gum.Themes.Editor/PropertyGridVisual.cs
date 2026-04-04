using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Editor;

/// <summary>
/// A two-column layout control that displays label/control pairs in rows
/// with alternating background colors, similar to egui's Widget Gallery.
/// </summary>
public class PropertyGridVisual : ContainerRuntime
{
    // Fixed label column width in pixels. Future: make this configurable
    // (percentage-based, auto-size, user-draggable splitter, etc.)
    private const float LabelColumnWidth = 120;

    private static readonly Color EvenRowColor = new Color(35, 35, 35);
    private static readonly Color OddRowColor = new Color(45, 45, 45);

    /// <summary>
    /// Whether rows are created with alternating background colors.
    /// Must be set before calling <see cref="AddRow"/>. Defaults to true.
    /// When false, no background rectangles are created, reducing object count.
    /// </summary>
    public bool AlternatingRowColorsEnabled { get; set; } = true;

    public PropertyGridVisual()
    {
        this.Width = 0;
        this.WidthUnits = DimensionUnitType.RelativeToParent;
        this.Height = 0;
        this.HeightUnits = DimensionUnitType.RelativeToChildren;
        this.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
    }

    /// <summary>
    /// Adds a labeled row containing the specified control to the property grid.
    /// Returns the row container so callers can toggle its Visible property;
    /// alternating row colors are automatically recalculated when any row's visibility changes.
    /// </summary>
    public ContainerRuntime AddRow(string label, FrameworkElement control)
    {
        var (row, content) = CreateRow();

        var labelText = new Label();
        labelText.Text = label;
        labelText.Width = LabelColumnWidth;
        labelText.WidthUnits = DimensionUnitType.Absolute;
        labelText.Height = 0;
        labelText.HeightUnits = DimensionUnitType.RelativeToParent;
        labelText.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        labelText.Y = 0;
        labelText.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        content.AddChild(labelText.Visual);

        var controlVisual = control.Visual;
        controlVisual.Width = -(LabelColumnWidth);
        controlVisual.WidthUnits = DimensionUnitType.RelativeToParent;

        content.AddChild(controlVisual);

        if (AlternatingRowColorsEnabled)
        {
            row.VisibleChanged += (_, _) => RestripeRows();
        }

        this.Children.Add(row);

        if (AlternatingRowColorsEnabled)
        {
            RestripeRows();
        }

        return row;
    }

    private void RestripeRows()
    {
        int visibleIndex = 0;
        foreach (var child in this.Children)
        {
            if (!child.Visible)
            {
                continue;
            }

            var background = child.Children[0] as ColoredRectangleRuntime;
            if (background != null)
            {
                background.Color = visibleIndex % 2 == 0 ? EvenRowColor : OddRowColor;
            }

            visibleIndex++;
        }
    }

    private (ContainerRuntime row, ContainerRuntime content) CreateRow()
    {
        // Row is the outer container — sizes to its children, holds background + content
        var row = new ContainerRuntime();
        row.Width = 0;
        row.WidthUnits = DimensionUnitType.RelativeToParent;
        row.Height = 4;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.MinHeight = 34;
        row.Name = "Row Container";

        if (AlternatingRowColorsEnabled)
        {
            var background = new ColoredRectangleRuntime();
            background.Dock(Gum.Wireframe.Dock.Fill);
            row.Children.Add(background);
        }

        // Content container — stacks label and control left to right.
        var content = new ContainerRuntime();
        content.Width = 0;
        content.WidthUnits = DimensionUnitType.RelativeToParent;
        content.Height = 0;
        content.HeightUnits = DimensionUnitType.RelativeToChildren;
        content.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        content.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        content.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        content.Name = "Row Content";
        row.Children.Add(content);

        return (row, content);
    }
}
