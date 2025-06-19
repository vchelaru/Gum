using Gum.DataTypes;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls;

public enum ResizeBehavior
{
    Rows,
    Columns
}

/// <summary>
/// Control which allows the user to resize two adjacent siblings.
/// </summary>
public class Splitter : FrameworkElement
{
    /// <summary>
    /// The explicitly-set resize behavior. If null, resize behavior is determined
    /// by layout.
    /// </summary>
    public ResizeBehavior? ResizeBehavior
    {
        get;
        set;
    } 

    ResizeBehavior EffectiveResizeBehavior
    {
        get
        {
            if (ResizeBehavior.HasValue)
            {
                return ResizeBehavior.Value;
            }

            var parentAsPanel = Visual.Parent as GraphicalUiElement;
            if (parentAsPanel != null)
            {
                if(parentAsPanel.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                {
                    return Controls.ResizeBehavior.Columns;
                }
                else if(parentAsPanel.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack)
                {
                    return Controls.ResizeBehavior.Rows;
                }
            }

            if(Visual.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToParent || 
                Visual.WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfParent)
            {
                // it's stretched horizontally, so it is probably resizing rows:
                return Controls.ResizeBehavior.Rows;
            }
            if(Visual.HeightUnits == Gum.DataTypes.DimensionUnitType.RelativeToParent ||
                Visual.HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfParent)
            {
                // it's stretched vertically, so it is probably resizing columns:
                return Controls.ResizeBehavior.Columns;
            }

            return Controls.ResizeBehavior.Rows;
        }
    }

    /// <summary>
    /// Creates a new Splitter using default visuals.
    /// </summary>
    public Splitter() : base() { }

    /// <summary>
    /// Creates a new Splitter using the specified visual.
    /// </summary>
    /// <param name="visual">The visual to use.</param>
    public Splitter(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        if(Visual != null)
        {
            Visual.RollOn += HandleRollOn;
            Visual.Push += HandleVisualPush;
            Visual.Dragging += HandleVisualDragging;
        }
        base.ReactToVisualChanged();
    }

    float? leftGrabbedInOffset;
    float? topGrabbedInOffset;

    private void HandleRollOn(object? sender, EventArgs e)
    {
        CustomCursor = EffectiveResizeBehavior == Controls.ResizeBehavior.Columns ?
            Cursors.SizeWE :
            Cursors.SizeNS;
    }

    private void HandleVisualPush(object? sender, EventArgs e)
    {

        var cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
        var cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();

        leftGrabbedInOffset = null;
        topGrabbedInOffset = null;

        var relativeToLeftIn = cursorX - Visual.AbsoluteLeft;
        var relativeToTopIn = cursorY - Visual.AbsoluteTop;

        leftGrabbedInOffset = relativeToLeftIn;
        topGrabbedInOffset = relativeToTopIn;
    }

    private void HandleVisualDragging(object? sender, EventArgs e)
    {
        var parent = this.Visual.Parent as GraphicalUiElement;

        if (parent == null || topGrabbedInOffset == null || leftGrabbedInOffset == null)
        {
            // can't do anything without a parent
            return;
        }

        float changeInPixels = 0;

        if (EffectiveResizeBehavior == Controls.ResizeBehavior.Rows)
        {
            var cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();
            changeInPixels = cursorY - (Visual.AbsoluteTop + topGrabbedInOffset!.Value);

        }
        else
        {
            var cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
            changeInPixels = cursorX - (Visual.AbsoluteLeft + leftGrabbedInOffset!.Value);
        }
        ApplyResizeChangeInPixels(changeInPixels);

    }

    /// <summary>
    /// Resizes the previous and next siblings based on the changeInPixels parameter.
    /// </summary>
    /// <param name="changeInPixels">The number of pixels to resize, where positive is to the right or down dependin gon the effective resize behavior.</param>
    public void ApplyResizeChangeInPixels(float changeInPixels)
    {
        var parent = this.Visual.Parent as GraphicalUiElement;

        var index = parent.Children.IndexOf(this.Visual);

        GraphicalUiElement? visualBefore = null;
        GraphicalUiElement? visualAfter = null;

        if (index > 0)
        {
            visualBefore = parent.Children[index - 1] as GraphicalUiElement;
        }
        if (index < parent.Children.Count - 1)
        {
            visualAfter = parent.Children[index + 1] as GraphicalUiElement;
        }

        DimensionUnitType? unitsBefore;
        DimensionUnitType? unitsAfter;

        var resizeBehavior = EffectiveResizeBehavior;

        if (resizeBehavior == Controls.ResizeBehavior.Rows)
        {
            unitsBefore = visualBefore?.HeightUnits;
            unitsAfter = visualAfter?.HeightUnits;
        }
        else
        {
            unitsBefore = visualBefore?.WidthUnits;
            unitsAfter = visualAfter?.WidthUnits;
        }

        var areAllAbsolute =
            unitsBefore != DimensionUnitType.PercentageOfParent &&
            unitsBefore != DimensionUnitType.Ratio &&
            unitsBefore != DimensionUnitType.PercentageOfSourceFile &&
            unitsBefore != DimensionUnitType.PercentageOfOtherDimension &&
            unitsBefore != DimensionUnitType.MaintainFileAspectRatio &&

            unitsAfter != DimensionUnitType.PercentageOfParent &&
            unitsAfter != DimensionUnitType.Ratio &&
            unitsAfter != DimensionUnitType.PercentageOfSourceFile &&
            unitsAfter != DimensionUnitType.PercentageOfOtherDimension &&
            unitsAfter != DimensionUnitType.MaintainFileAspectRatio
            ;

        if (areAllAbsolute)
        {
            if (resizeBehavior == Controls.ResizeBehavior.Rows)
            {
                if (visualBefore != null)
                {
                    visualBefore.Height += changeInPixels;
                }
                if (visualAfter != null)
                {
                    visualAfter.Height -= changeInPixels;
                }
            }
            else // columns
            {
                if (visualBefore != null)
                {
                    visualBefore.Width += changeInPixels;
                }
                if (visualAfter != null)
                {
                    visualAfter.Width -= changeInPixels;
                }
            }
        }

        if(unitsBefore == DimensionUnitType.Ratio && unitsAfter == DimensionUnitType.Ratio
    }
}
