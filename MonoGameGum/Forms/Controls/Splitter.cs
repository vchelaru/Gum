using Gum.DataTypes;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace MonoGameGum.Forms.Controls;
#endif


#if !FRB
namespace Gum.Forms.Controls;


#endif

public enum ResizeBehavior
{
    Rows,
    Columns
}

/// <summary>
/// Control which allows the user to resize two adjacent siblings.
/// </summary>
public class Splitter :
#if RAYLIB
    FrameworkElement
#else
    MonoGameGum.Forms.Controls.FrameworkElement
#endif
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
                if (parentAsPanel.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                {
                    return Controls.ResizeBehavior.Columns;
                }
                else if (parentAsPanel.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack)
                {
                    return Controls.ResizeBehavior.Rows;
                }
            }

            if (Visual.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToParent ||
                Visual.WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfParent)
            {
                // it's stretched horizontally, so it is probably resizing rows:
                return Controls.ResizeBehavior.Rows;
            }
            if (Visual.HeightUnits == Gum.DataTypes.DimensionUnitType.RelativeToParent ||
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
        if (Visual != null)
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

        if(changeInPixels != 0)
        {
            ApplyResizeChangeInPixels(changeInPixels);
        }

    }

    /// <summary>
    /// Resizes the previous and next siblings based on the changeInPixels parameter.
    /// </summary>
    /// <param name="changeInPixels">The number of pixels to resize, where positive is to the right or down dependin gon the effective resize behavior.</param>
    public void ApplyResizeChangeInPixels(float changeInPixels)
    {
        var parent = this.Visual.Parent as GraphicalUiElement;

        var index = parent.Children.IndexOf(this.Visual);

        GraphicalUiElement? firstVisual = null;
        GraphicalUiElement? secondVisual = null;

        if (index > 0)
        {
            firstVisual = parent.Children[index - 1] as GraphicalUiElement;
        }
        if (index < parent.Children.Count - 1)
        {
            secondVisual = parent.Children[index + 1] as GraphicalUiElement;
        }

        var resizeBehavior = EffectiveResizeBehavior;
        if(firstVisual != null)
        {
            if(resizeBehavior == Controls.ResizeBehavior.Rows)
            {
                var absoluteHeight = firstVisual.GetAbsoluteHeight();
                if(firstVisual.MinHeight != null)
                {
                    absoluteHeight -= firstVisual.MinHeight.Value;
                }
                changeInPixels = Math.Max(-absoluteHeight, changeInPixels);
            }
            else
            {
                var absoluteWidth = firstVisual.GetAbsoluteWidth();
                if(firstVisual.MinWidth != null)
                {
                    absoluteWidth -= firstVisual.MinWidth.Value;
                }
                changeInPixels = Math.Max(-absoluteWidth, changeInPixels);
            }
        }
        if (secondVisual != null)
        {
            if(resizeBehavior == Controls.ResizeBehavior.Rows)
            {
                var absoluteHeight = secondVisual.GetAbsoluteHeight();
                if(secondVisual.MinHeight != null)
                {
                    absoluteHeight -= secondVisual.MinHeight.Value;
                }
                changeInPixels = Math.Min(absoluteHeight, changeInPixels);
            }
            else
            {
                var absoluteWidth = secondVisual.GetAbsoluteWidth();
                if(secondVisual.MinWidth != null)
                {
                    absoluteWidth -= secondVisual.MinWidth.Value;
                }
                changeInPixels = Math.Min(absoluteWidth, changeInPixels);
            }
        }

        DimensionUnitType? firstUnits;
        DimensionUnitType? secondUnits;


        if (resizeBehavior == Controls.ResizeBehavior.Rows)
        {
            firstUnits = firstVisual?.HeightUnits;
            secondUnits = secondVisual?.HeightUnits;
        }
        else
        {
            firstUnits = firstVisual?.WidthUnits;
            secondUnits = secondVisual?.WidthUnits;
        }

        var isFirstAbsolute =
            firstUnits != DimensionUnitType.PercentageOfParent &&
            firstUnits != DimensionUnitType.Ratio &&
            firstUnits != DimensionUnitType.PercentageOfSourceFile &&
            firstUnits != DimensionUnitType.PercentageOfOtherDimension &&
            firstUnits != DimensionUnitType.MaintainFileAspectRatio;

        var isSecondAbsolute =
            secondUnits != DimensionUnitType.PercentageOfParent &&
            secondUnits != DimensionUnitType.Ratio &&
            secondUnits != DimensionUnitType.PercentageOfSourceFile &&
            secondUnits != DimensionUnitType.PercentageOfOtherDimension &&
            secondUnits != DimensionUnitType.MaintainFileAspectRatio;

        var wasFirstLayoutSuspended = firstVisual?.IsLayoutSuspended;
        var wasSecondLayoutSuspended = secondVisual?.IsLayoutSuspended;

        if (firstVisual != null && firstVisual.IsLayoutSuspended == false)
        {
            firstVisual.SuspendLayout();
        }
        if (secondVisual != null && secondVisual.IsLayoutSuspended == false)
        {
            secondVisual.SuspendLayout();
        }

        // Ratios have to be handled together
        if (firstUnits == DimensionUnitType.Ratio && secondUnits == DimensionUnitType.Ratio)
        {
            if (resizeBehavior == Controls.ResizeBehavior.Rows)
            {
                var firstHeightInPixels = firstVisual.GetAbsoluteHeight();

                var ratioToAdd = (changeInPixels / firstHeightInPixels) * firstVisual.Height;
                firstVisual.Height += ratioToAdd;
                secondVisual.Height -= ratioToAdd;
            }
            else
            {
                var firstWidthInPixels = firstVisual.GetAbsoluteWidth();

                var ratioToAdd = (changeInPixels / firstWidthInPixels) * firstVisual.Width;
                firstVisual.Width += ratioToAdd;
                secondVisual.Width -= ratioToAdd;
            }
        }
        else
        {
            if (firstUnits == DimensionUnitType.PercentageOfParent)
            {
                if (resizeBehavior == Controls.ResizeBehavior.Rows)
                {
                    var heightInPixels = firstVisual.GetAbsoluteHeight();
                    var newAbsoluteHeight = heightInPixels + changeInPixels;

                    // convert considering the parent's absolute height
                    firstVisual.Height =
                        (newAbsoluteHeight / parent.GetAbsoluteHeight()) * 100.0f;

                }
                else // columns
                {
                    var widthInPixels = firstVisual.GetAbsoluteWidth();
                    var newAbsoluteWidth = widthInPixels + changeInPixels;

                    // convert considering the parent's absolute width
                    firstVisual.Width =
                        (newAbsoluteWidth / parent.GetAbsoluteWidth()) * 100.0f;
                }
            }
            else if (isFirstAbsolute)
            {
                if (resizeBehavior == Controls.ResizeBehavior.Rows)
                {
                    firstVisual.Height += changeInPixels;
                }
                else // columns
                {
                    firstVisual.Width += changeInPixels;
                }
            }
            else if(firstUnits == DimensionUnitType.Ratio)
            {
                var firstHeightInPixels = firstVisual.GetAbsoluteHeight();

                var ratioToAdd = (changeInPixels / firstHeightInPixels) * firstVisual.Height;
                firstVisual.Height += ratioToAdd;
            }


            if (secondUnits == DimensionUnitType.PercentageOfParent)
            {
                if (resizeBehavior == Controls.ResizeBehavior.Rows)
                {
                    var heightInPixels = secondVisual.GetAbsoluteHeight();
                    var newAbsoluteHeight = heightInPixels - changeInPixels;
                    // convert considering the parent's absolute height
                    secondVisual.Height =
                        (newAbsoluteHeight / parent.GetAbsoluteHeight()) * 100.0f;
                }
                else // columns
                {
                    var widthInPixels = secondVisual.GetAbsoluteWidth();
                    var newAbsoluteWidth = widthInPixels - changeInPixels;
                    // convert considering the parent's absolute width
                    secondVisual.Width =
                        (newAbsoluteWidth / parent.GetAbsoluteWidth()) * 100.0f;
                }
            }
            else if (isSecondAbsolute)
            {
                if (resizeBehavior == Controls.ResizeBehavior.Rows)
                {
                    secondVisual.Height -= changeInPixels;
                }
                else // columns
                {
                    secondVisual.Width -= changeInPixels;
                }
            }
            else if(secondUnits == DimensionUnitType.Ratio)
            {
                var secondHeightInPixels = secondVisual.GetAbsoluteHeight();

                var ratioToAdd = (changeInPixels / secondHeightInPixels) * secondVisual.Height;
                secondVisual.Height -= ratioToAdd;
            }
        }

        if(wasFirstLayoutSuspended == false)
        {
            firstVisual?.ClearDirtyLayoutState();
            firstVisual?.ResumeLayout();
        }
        if(wasSecondLayoutSuspended == false)
        {
            secondVisual?.ClearDirtyLayoutState();
            secondVisual?.ResumeLayout();
        }

        // If we do not take extra precautions on layouts, then resizing the splitter causes:
        // 1. The first item to resize, which can propagate upward to the parent object recursively, causing
        //    a top-level layout
        // 2. The second item to resize, which can also propagate upward to the parent object recursively, causing
        //    a top-level layout
        // This can be very expensive. When a resize happens, the net size should be the same before and after
        // the resize, so nothing above the splitter's parent should need a resize. Therefore, we can
        // suppress and clear all layouts on the first/second, then do a single layout on the parent.
        // This can reduce layout calls considerably, especially if the parent is itself in a complex layout.
        parent.UpdateLayout(updateParent: false, updateChildren: true);
    }
}
