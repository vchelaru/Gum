using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    public GeneralUnitType XUnits
    {
        get => mXUnits;
        set
        {
            if (value != mXUnits)
            {
                mXUnits = value;
                UpdateLayout();
            }
        }
    }

    public GeneralUnitType YUnits
    {
        get { return mYUnits; }
        set
        {
            if (mYUnits != value)
            {
                mYUnits = value;
                UpdateLayout();
            }
        }
    }

    public HorizontalAlignment XOrigin
    {
        get { return mXOrigin; }
        set
        {
            if (mXOrigin != value)
            {
                mXOrigin = value; UpdateLayout();
            }
        }
    }

    public VerticalAlignment YOrigin
    {
        get { return mYOrigin; }
        set
        {
            if (mYOrigin != value)
            {
                mYOrigin = value; UpdateLayout();
            }
        }
    }

    public DimensionUnitType WidthUnits
    {
        get { return mWidthUnit; }
        set
        {
            if (mWidthUnit != value)
            {
                mWidthUnit = value; UpdateLayout();
            }
        }
    }

    public DimensionUnitType HeightUnits
    {
        get { return mHeightUnit; }
        set
        {
            if (mHeightUnit != value)
            {
                mHeightUnit = value;

                if (mContainedObjectAsIpso is IText)
                {
                    RefreshTextOverflowVerticalMode();
                }

                UpdateLayout();
            }
        }
    }


    bool ignoredByParentSize;
    public bool IgnoredByParentSize
    {
        get => ignoredByParentSize;
        set
        {
            if (ignoredByParentSize != value)
            {
                ignoredByParentSize = value;
                // todo - could be smarter here?
                UpdateLayout();
            }
        }
    }

    ChildrenLayout childrenLayout;
    public ChildrenLayout ChildrenLayout
    {
        get => childrenLayout;
        set
        {
            if (value != childrenLayout)
            {
                childrenLayout = value; UpdateLayout();
            }
        }
    }

    int autoGridHorizontalCells = 4;
    /// <summary>
    /// The number of columns in an auto-grid. For <see cref="ChildrenLayout.AutoGridHorizontal"/> this is a fixed
    /// column count. For <see cref="ChildrenLayout.AutoGridVertical"/> it is a minimum: extra children add columns,
    /// which shrink the cells only when <see cref="WidthUnits"/> is <see cref="DimensionUnitType.RelativeToChildren"/>.
    /// Otherwise cells keep their size and extra columns overflow past the right edge.
    /// </summary>
    public int AutoGridHorizontalCells
    {
        get => autoGridHorizontalCells;
        set
        {
            if (autoGridHorizontalCells != value)
            {
                autoGridHorizontalCells = value; UpdateLayout();
            }
        }
    }

    int autoGridVerticalCells = 4;
    /// <summary>
    /// The number of rows in an auto-grid. For <see cref="ChildrenLayout.AutoGridVertical"/> this is a fixed
    /// row count. For <see cref="ChildrenLayout.AutoGridHorizontal"/> it is a minimum: extra children add rows,
    /// which shrink the cells only when <see cref="HeightUnits"/> is <see cref="DimensionUnitType.RelativeToChildren"/>.
    /// Otherwise cells keep their size and extra rows overflow below the bottom edge.
    /// </summary>
    public int AutoGridVerticalCells
    {
        get => autoGridVerticalCells;
        set
        {
            if (autoGridVerticalCells != value)
            {
                autoGridVerticalCells = value; UpdateLayout();
            }
        }
    }

    TextOverflowVerticalMode textOverflowVerticalMode;
    // we have to store this locally because we are going to effectively assign the overflow mode based on the height units and this value
    //
    // Intentionally NOT #if FRB-gated like the other FRB1-only font properties (Font, FontSize, IsBold,
    // etc.) further down this class -- this one isn't FRB1-specific plumbing, it's read directly by this
    // class's own layout coordination on every backend (RefreshTextOverflowVerticalMode's
    // RelativeToChildren-forces-SpillOver override, DialogBox page-splitting, RelativeToChildren sizing).
    // GraphicalUiElement genuinely owns this value; it isn't leaking a FRB1-only concern (#3708).
    public TextOverflowVerticalMode TextOverflowVerticalMode
    {
        get => textOverflowVerticalMode;
        set
        {
            if (textOverflowVerticalMode != value)
            {
                if (this.RenderableComponent is IText text)
                {
                    text.TextOverflowVerticalMode = value;
                }
                textOverflowVerticalMode = value;
            }
        }
    }

    float stackSpacing;
    /// <summary>
    /// The number of pixels between children when ChildrenLayout is TopToBottomStack or
    /// LeftToRightStack, and between cells when it is AutoGridHorizontal or AutoGridVertical.
    /// It has no effect on Regular.
    /// </summary>
    public float StackSpacing
    {
        get => stackSpacing;
        set
        {
            if (stackSpacing != value)
            {
                stackSpacing = value;
                if (ChildrenLayout != ChildrenLayout.Regular)
                {
                    UpdateLayout();
                }
            }
        }
    }

    bool useFixedStackChildrenSize;
    /// <summary>
    /// Whether to use the same spacing for all children. If true then the size of the first element is used as the height for all other children. This option
    /// is primarily used for performance reasons as it can make layouts for large collections of stacked children faster.
    /// In a TopToBottomStack that does not wrap, each later child is placed by its index after the first child, so a later
    /// child's own Y offset moves only that child rather than the children after it.
    /// </summary>
    public bool UseFixedStackChildrenSize
    {
        get => useFixedStackChildrenSize;
        set
        {
            if (useFixedStackChildrenSize != value)
            {
                useFixedStackChildrenSize = value;
                if (ChildrenLayout != ChildrenLayout.Regular)
                {
                    UpdateLayout();
                }
            }
        }
    }

    /// <summary>
    /// Rotation in degrees. Positive value rotates counterclockwise.
    /// </summary>
    public float Rotation
    {
        get
        {
            return mRotation;
        }
        set
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(value) || float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
            {
                throw new Exception($"Invalid Rotation value set: {value}");
            }
#endif
            if (mRotation != value)
            {
                mRotation = value;

                UpdateLayout();
            }
        }
    }

    public bool FlipHorizontal
    {
        get => mContainedObjectAsIpso?.FlipHorizontal ?? false;
        set
        {
            if (mContainedObjectAsIpso != null)
            {
                if (mContainedObjectAsIpso.FlipHorizontal != value)
                {
                    mContainedObjectAsIpso.FlipHorizontal = value;
                    UpdateLayout();
                }
            }
        }
    }

    public float X
    {
        get
        {
            return mX;
        }
        set
        {
            if (mX != value && mContainedObjectAsIpso != null)
            {
#if FULL_DIAGNOSTICS
                if (float.IsNaN(value))
                {
                    throw new ArgumentException("Not a Number (NAN) not allowed");
                }
                if (float.IsPositiveInfinity(value))
                {
                    throw new ArgumentException("Positive Infinity not allowed");
                }
                if (float.IsNegativeInfinity(value))
                {
                    throw new ArgumentException("Negative Infinity not allowed");
                }
#endif
                mX = value;

                var skipLayout = XUnits == GeneralUnitType.PixelsFromSmall && XOrigin == HorizontalAlignment.Left &&
                    CanPlacePositionDirectly(XOrY.X);
                if (skipLayout)
                {
                    var oldX = this.mContainedObjectAsIpso.X;
                    this.mContainedObjectAsIpso.X = mX;
                    if (oldX != mX)
                    {
                        PositionChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                if (!skipLayout)
                {
                    var refreshParent = IgnoredByParentSize == false;
                    UpdateLayout(refreshParent, 0);
                }
            }
        }
    }

    public float Y
    {
        get
        {
            return mY;
        }
        set
        {
            if (mY != value && mContainedObjectAsIpso != null)
            {
#if FULL_DIAGNOSTICS
                if (float.IsNaN(value))
                {
                    throw new ArgumentException("Not a Number (NAN) not allowed");
                }
                if (float.IsPositiveInfinity(value))
                {
                    throw new ArgumentException("Positive Infinity not allowed");
                }
                if (float.IsNegativeInfinity(value))
                {
                    throw new ArgumentException("Negative Infinity not allowed");
                }
#endif
                mY = value;


                if (Parent as GraphicalUiElement == null && YUnits == GeneralUnitType.PixelsFromSmall && YOrigin == VerticalAlignment.Top &&
                    CanPlacePositionDirectly(XOrY.Y))
                {
                    var oldY = this.mContainedObjectAsIpso.Y;
                    this.mContainedObjectAsIpso.Y = mY;
                    if (oldY != mY)
                    {
                        PositionChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    var refreshParent = IgnoredByParentSize == false;
                    UpdateLayout(refreshParent, 0);
                }
            }
        }
    }


    // Whether a top-left, PixelsFromSmall position can be copied straight to the renderable instead of
    // running layout: true only when no parent logic (stacking, grid cells, content sizing, flip or
    // rotation) would move it or depend on it.
    bool CanPlacePositionDirectly(XOrY axis)
    {
        var effectiveParent = EffectiveParentGue;
        if (effectiveParent == null)
        {
            return true;
        }
        if (effectiveParent.ChildrenLayout != ChildrenLayout.Regular ||
            effectiveParent.GetAbsoluteRotation() != 0 ||
            effectiveParent.GetAbsoluteFlipHorizontal())
        {
            return false;
        }
        var parentUnits = axis == XOrY.X ? effectiveParent.WidthUnits : effectiveParent.HeightUnits;
        return parentUnits.GetDependencyType() != HierarchyDependencyType.DependsOnChildren &&
            parentUnits != DimensionUnitType.RelativeToMaxParentOrChildren;
    }

    float? _maxWidth;
    public float? MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (_maxWidth != value)
            {
                _maxWidth = value;

                UpdateLayout();
            }

        }
    }

    float? _minWidth;
    public float? MinWidth
    {
        get => _minWidth;
        set
        {
            if (_minWidth != value)
            {
                _minWidth = value;

                UpdateLayout();
            }

        }
    }

    public float Width
    {
        get => mWidth;
        set
        {
            if (mWidth != value)
            {
#if FULL_DIAGNOSTICS
                if (float.IsPositiveInfinity(value) ||
                    float.IsNegativeInfinity(value) ||
                    float.IsNaN(value))
                {
                    throw new ArgumentException("Invalid Width: " + value);
                }
#endif
                mWidth = value;

                if (Rotation == 0)
                {
                    UpdateLayout(
                        ParentUpdateType.IfParentWidthHeightDependOnChildren |
                        ParentUpdateType.IfParentStacks |
                        ParentUpdateType.IfParentHasRatioSizedChildren,
                        int.MaxValue / 2, XOrY.X
                        );
                }
                else
                {
                    UpdateLayout(
                        ParentUpdateType.IfParentWidthHeightDependOnChildren |
                        ParentUpdateType.IfParentStacks |
                        ParentUpdateType.IfParentHasRatioSizedChildren,
                        int.MaxValue / 2
                        );
                }
            }
        }
    }


    float? _maxHeight;
    public float? MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (_maxHeight != value)
            {
                _maxHeight = value;

                UpdateLayout();
            }

        }
    }
    float? _minHeight;
    public float? MinHeight
    {
        get => _minHeight;
        set
        {
            if (_minHeight != value)
            {
                _minHeight = value;

                UpdateLayout();
            }

        }
    }

    public float Height
    {
        get => mHeight;
        set
        {
            if (mHeight != value)
            {
#if FULL_DIAGNOSTICS
                if (float.IsPositiveInfinity(value) ||
                    float.IsNegativeInfinity(value) ||
                    float.IsNaN(value))
                {
                    throw new ArgumentException("Invalid height: " + value);
                }
#endif
                mHeight = value;

                // If this height changes, then we should only update the parent if the height change can actually affect the parent:
                if (Rotation == 0)
                {
                    // only update Y if unrotated:
                    UpdateLayout(
                        ParentUpdateType.IfParentWidthHeightDependOnChildren |
                        ParentUpdateType.IfParentStacks |
                        ParentUpdateType.IfParentHasRatioSizedChildren,
                        int.MaxValue / 2, XOrY.Y
                        );
                }
                else
                {
                    UpdateLayout(
                        ParentUpdateType.IfParentWidthHeightDependOnChildren |
                        ParentUpdateType.IfParentStacks |
                        ParentUpdateType.IfParentHasRatioSizedChildren,
                        int.MaxValue / 2
                        );
                }
            }
        }
    }
}
