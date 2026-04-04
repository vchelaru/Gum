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

    private int _rowCount;

    public PropertyGridVisual()
    {
        this.Width = 0;
        this.WidthUnits = DimensionUnitType.RelativeToParent;
        this.Height = 0;
        this.HeightUnits = DimensionUnitType.RelativeToChildren;
        this.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
    }

    public void AddRow(string label, FrameworkElement control)
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

        this.Children.Add(row);
        _rowCount++;
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
        row.Name = $"Row Container";

        // Alternating row background — fills the row
        var background = new ColoredRectangleRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Color = _rowCount % 2 == 0 ? EvenRowColor : OddRowColor;
        row.Children.Add(background);

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
