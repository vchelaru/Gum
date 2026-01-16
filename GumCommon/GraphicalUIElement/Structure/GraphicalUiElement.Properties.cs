using Gum.Collections;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Properties

        ColorOperation IRenderableIpso.ColorOperation => mContainedObjectAsIpso.ColorOperation;

        public static MissingFileBehavior MissingFileBehavior { get; set; } = MissingFileBehavior.ConsumeSilently;

        public ElementSave ElementSave
        {
            get;
            set;
        }

        public ISystemManagers? Managers => mManagers;

        /// <summary>
        /// Returns this instance's SystemManagers, or climbs up the parent/child relationship
        /// until a non-null SystemsManager is found. Otherwise, returns null.
        /// </summary>
        public ISystemManagers? EffectiveManagers
        {
            get
            {
                if (mManagers != null)
                {
                    return mManagers;
                }
                else
                {
                    return this.ElementGueContainingThis?.EffectiveManagers ??
                        this.EffectiveParentGue?.EffectiveManagers;
                }
            }
        }

        /// <inheritdoc/>
        public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();

        /// <inheritdoc/>
        public bool Visible
        {
            get
            {
                if (mContainedObjectAsIVisible != null)
                {
                    return mContainedObjectAsIVisible.Visible;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                // If this is a Screen, then it doesn't have a contained IVisible:
                if (mContainedObjectAsIVisible != null && value != mContainedObjectAsIVisible.Visible)
                {
                    mContainedObjectAsIVisible.Visible = value;

                    var absoluteVisible = AbsoluteVisible;
                    // See if this has a parent that stacks children. If so, update its layout:

                    var didUpdate = false;
                    if (absoluteVisible)
                    {
                        if (!mIsLayoutSuspended && !GraphicalUiElement.IsAllLayoutSuspended)
                        {
                            // resume layout:
                            // This does need to be recursive because contained objects may have been 
                            // updated while the parent was invisible, becoming dirty, and waiting for
                            // the resume
                            didUpdate = ResumeLayoutUpdateIfDirtyRecursive();

                            //if (isFontDirty)
                            //{
                            //    if (!IsAllLayoutSuspended)
                            //    {
                            //        this.UpdateToFontValues();
                            //        isFontDirty = false;
                            //    }
                            //}
                            //if (currentDirtyState != null)
                            //{
                            //    UpdateLayout(currentDirtyState.ParentUpdateType,
                            //        currentDirtyState.ChildrenUpdateDepth,
                            //        currentDirtyState.XOrY);
                            //}

                            if (this.WidthUnits == DimensionUnitType.Ratio || this.HeightUnits == DimensionUnitType.Ratio)
                            {
                                // If this is a width or height ratio and we're made visible, then the parent needs to update if it stacks:
                                this.UpdateLayout(ParentUpdateType.IfParentStacks | ParentUpdateType.IfParentIsAutoGrid |

                                    // if there are ratio sized children, then flipping visibility on this can update the widths of the ratio'ed children
                                    ParentUpdateType.IfParentHasRatioSizedChildren,
                                    // If something is made visible, that shouldn't update the children, right?
                                    //int.MaxValue/2, 
                                    0,
                                    null);
                                didUpdate = true;
                            }
                        }
                    }

                    if (!didUpdate)
                    {
                        // This will make this dirty:
                        this.UpdateLayout(ParentUpdateType.IfParentStacks | ParentUpdateType.IfParentWidthHeightDependOnChildren | ParentUpdateType.IfParentIsAutoGrid |
                            ParentUpdateType.IfParentHasRatioSizedChildren,
                            // If something is made visible, that shouldn't update the children, right?
                            //int.MaxValue/2, 
                            0,
                            null);
                    }

                    if (!absoluteVisible && (GetIfParentStacks() || GetIfParentIsAutoGrid()))
                    {
                        // This updates the parent right away:
                        (Parent as GraphicalUiElement)?.UpdateLayout(ParentUpdateType.IfParentStacks | ParentUpdateType.IfParentIsAutoGrid, int.MaxValue / 2, null);

                    }
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <inheritdoc/>
        IVisible? IVisible.Parent
        {
            get
            {
                return ((IRenderableIpso)this).Parent as IVisible;
            }
        }

        /// <summary>
        /// The X "world units" that the entire gum rendering system uses. This is essentially the "top level" container's width.
        /// For a game which renders at 1:1, this will match the game's resolution. 
        /// </summary>
        public static float CanvasWidth
        {
            get;
            set;
        }

        /// <summary>
        /// The Y "world units" that the entire gum rendering system uses. This is essentially the "top level" container's height.
        /// For a game which renders at 1:1, this will match the game's resolution. 
        /// </summary>
        public static float CanvasHeight
        {
            get;
            set;
        }



        public float Z
        {
            get
            {
                if (mContainedObjectAsIpso == null)
                {
                    return 0;
                }
                else
                {
                    return mContainedObjectAsIpso.Z;
                }
            }
            set
            {
                mContainedObjectAsIpso.Z = value;
            }
        }

        #region IRenderable properties


        BlendState IRenderable.BlendState
        {
            get
            {
#if FULL_DIAGNOSTICS
                if (mContainedObjectAsIpso == null)
                {
                    throw new NullReferenceException("This GraphicalUiElemente has not had its visual set, so it does not have a blend operation. This can happen if a GraphicalUiElement was added as a child without its contained renderable having been set.");
                }
#endif
                return mContainedObjectAsIpso.BlendState;
            }
        }


        bool IRenderable.Wrap
        {
            get { return mContainedObjectAsIpso.Wrap; }
        }

        public virtual void Render(ISystemManagers managers)
        {
            mContainedObjectAsIpso.Render(managers);
        }

        public virtual string BatchKey => mContainedObjectAsIpso?.BatchKey ?? string.Empty;

        public virtual void StartBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.StartBatch(systemManagers);
        public virtual void EndBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.EndBatch(systemManagers);

        Layer? mLayer;

        public Layer? Layer => mLayer;

        #endregion

        public bool IsRenderTarget => mContainedObjectAsIpso?.IsRenderTarget == true;
        int IRenderableIpso.Alpha => mContainedObjectAsIpso?.Alpha ?? 255;

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
        /// The number of pixels spacing between each child if this has a ChildrenLayout of 
        /// TopToBottomStack or LeftToRightStack. This has no affect on other types of ChildrenLayout, 
        /// including AutoGridHorizontal or AutoGridVertical.
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
        /// is primraily used for performance reasons as it can make layouts for large collections of stacked children faster.
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

                    var parentGue = Parent as GraphicalUiElement;
                    var skipLayout = false;
                    // special case:
                    if (XUnits == GeneralUnitType.PixelsFromSmall && XOrigin == HorizontalAlignment.Left)
                    {
                        if (parentGue == null)
                        {
                            skipLayout = true;
                        }
                        else
                        {
                            // WE might be able to get away with more changes here to suppress layouts, but this is a start...
                            if (parentGue.WidthUnits.GetDependencyType() != HierarchyDependencyType.DependsOnChildren &&
                                parentGue.ChildrenLayout != ChildrenLayout.LeftToRightStack &&
                                parentGue.ChildrenLayout != ChildrenLayout.TopToBottomStack)
                            {
                                skipLayout = true;
                            }
                        }

                        this.mContainedObjectAsIpso.X = mX;
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


                    if (Parent as GraphicalUiElement == null && YUnits == GeneralUnitType.PixelsFromSmall && YOrigin == VerticalAlignment.Top)
                    {
                        this.mContainedObjectAsIpso.Y = mY;
                    }
                    else
                    {
                        var refreshParent = IgnoredByParentSize == false;
                        UpdateLayout(refreshParent, 0);
                    }
                }
            }
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
                        throw new ArgumentException();
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
                        throw new ArgumentException();
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

        public GraphicalUiElement? Parent
        {
            get { return _parent; }
            set
            {
#if FULL_DIAGNOSTICS
                if (value == this)
                {
                    throw new InvalidOperationException("Cannot attach an object to itself");
                }
#endif
                if (_parent != value)
                {
                    var oldParent = _parent;
                    if (_parent?.Children?.Contains(this) == true)
                    {
                        _parent.Children.Remove(this);
                        oldParent?.UpdateLayout();
                    }
                    _parent = value;

                    // In case the object was added explicitly 
                    if (_parent?.Children != null && _parent.Children.Contains(this) == false)
                    {
                        _parent.Children.Add(this);

                    }

                    // If layout is suppressed, the parent may not get set
                    // and it's possible to have a floating visible=true object
                    // that gets rendered without a parent:
                    mContainedObjectAsIpso?.SetParentDirect(value);

                    UpdateLayout();
                    ParentChanged?.Invoke(this, new ParentChangedEventArgs()
                    {
                        OldValue = oldParent,
                        NewValue = value
                    });
                }
            }
        }

        IRenderableIpso? IRenderableIpso.Parent { get => Parent; set => this.Parent = value as GraphicalUiElement; }

        // Made obsolete November 4, 2017
        [Obsolete("Use ElementGueContainingThis instead - it more clearly indicates the relationship, " +
            "as the ParentGue may not actually be the parent. If the effective parent is desired, use EffectiveParentGue")]
        public GraphicalUiElement ParentGue
        {
            get { return ElementGueContainingThis; }
            set { ElementGueContainingThis = value; }
        }

        /// <summary>
        /// The ScreenSave or Component which contains this instance.
        /// </summary>
        public GraphicalUiElement? ElementGueContainingThis
        {
            get
            {
                return mWhatContainsThis;
            }
            set
            {
                if (mWhatContainsThis != value)
                {
                    if (mWhatContainsThis != null)
                    {
                        mWhatContainsThis.mWhatThisContains.Remove(this); ;
                    }

                    mWhatContainsThis = value;

                    if (mWhatContainsThis != null)
                    {
                        mWhatContainsThis.mWhatThisContains.Add(this);
                    }
                }
            }
        }

        public GraphicalUiElement? EffectiveParentGue
        {
            get
            {
                if (Parent != null && Parent is GraphicalUiElement)
                {
                    return Parent as GraphicalUiElement;
                }
                else
                {
                    return ElementGueContainingThis;
                }
            }
        }

        public IRenderable RenderableComponent
        {
            get
            {
                if (mContainedObjectAsIpso is GraphicalUiElement)
                {
                    return ((GraphicalUiElement)mContainedObjectAsIpso).RenderableComponent;
                }
                else
                {
                    return mContainedObjectAsIpso;
                }

            }
        }


        /// <summary>
        /// A flat list of all GraphicalUiElements contained by this element. For example, if this GraphicalUiElement
        /// is a Screen, this list is all GraphicalUielements for every instance contained regardless of hierarchy.
        /// </summary>
        /// <remarks>
        /// Since this is an interface using ContainedElements in a foreach allocates memory
        /// and this can actually be significant in a game that updates its UI frequently.
        /// </remarks>
        public IList<GraphicalUiElement> ContainedElements
        {
            get
            {
                return mWhatThisContains;
            }
        }

        string name;
        public string Name
        {
            get => name;
            set
            {
                if (mContainedObjectAsIpso != null)
                {
                    mContainedObjectAsIpso.Name = value;
                }
                name = value;
            }
        }

        /// <summary>
        /// Returns the direct hierarchical children of this. 
        /// Note that this does not return all objects contained in the element, only direct children. 
        /// </summary>

        ObservableCollection<IRenderableIpso>? IRenderableIpso.Children
        {
            get
            {
                return mContainedObjectAsIpso?.Children;
            }
        }

        private GraphicalUiElementCollection _childrenWrapper = GraphicalUiElementCollection.Empty;

        public ObservableCollection<GraphicalUiElement> Children => _childrenWrapper;



        object mTagIfNoContainedObject;
        public object? Tag
        {
            get
            {
                if (mContainedObjectAsIpso != null)
                {
                    return mContainedObjectAsIpso.Tag;
                }
                else
                {
                    return mTagIfNoContainedObject;
                }
            }
            set
            {
                if (mContainedObjectAsIpso != null)
                {
                    mContainedObjectAsIpso.Tag = value;
                }
                else
                {
                    mTagIfNoContainedObject = value;
                }
            }
        }

        public IPositionedSizedObject? Component => mContainedObjectAsIpso;

        /// <summary>
        /// Returns the absolute (screen space) X of the origin of the GraphicalUiElement. Note that
        /// this considers the XOrigin, and will apply rotation.
        /// </summary>
        public float AbsoluteX
        {
            get
            {
                float toReturn = this.GetAbsoluteX();

                var originOffset = Vector2.Zero;

                switch (XOrigin)
                {
                    case HorizontalAlignment.Center:
                        originOffset.X = ((IPositionedSizedObject)this).Width / 2;

                        break;
                    case HorizontalAlignment.Right:
                        originOffset.X = ((IPositionedSizedObject)this).Width;
                        break;
                }

                switch (YOrigin)
                {
                    case VerticalAlignment.TextBaseline:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        if (mContainedObjectAsIpso is IText text)
                        {
                            originOffset.Y -= text.DescenderHeight * text.FontScale;
                        }
                        break;
                    case VerticalAlignment.Center:
                        originOffset.Y = ((IPositionedSizedObject)this).Height / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        break;
                }

                var matrix = this.GetAbsoluteRotationMatrix();
                originOffset = Vector2.Transform(originOffset, matrix);
                return toReturn + originOffset.X;
            }
        }

        /// <summary>
        /// Returns the absolute X (in screen space) of the left edge of the GraphicalUielement.
        /// </summary>
        public float AbsoluteLeft => this.GetAbsoluteX();

        /// <summary>
        /// Returns the absolute Y (screen space) of the origin of the GraphicalUiElement. Note that
        /// this considers the YOrigin, and will apply rotation
        /// </summary>
        public float AbsoluteY
        {
            get
            {
                float toReturn = this.GetAbsoluteY();

                var originOffset = Vector2.Zero;

                switch (XOrigin)
                {
                    case HorizontalAlignment.Center:
                        originOffset.X = ((IPositionedSizedObject)this).Width / 2;

                        break;
                    case HorizontalAlignment.Right:
                        originOffset.X = ((IPositionedSizedObject)this).Width;
                        break;
                }

                switch (YOrigin)
                {
                    case VerticalAlignment.TextBaseline:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        if (mContainedObjectAsIpso is IText text)
                        {
                            originOffset.Y -= text.DescenderHeight * text.FontScale;
                        }
                        break;
                    case VerticalAlignment.Center:
                        originOffset.Y = ((IPositionedSizedObject)this).Height / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        break;
                }
                var matrix = this.GetAbsoluteRotationMatrix();
                originOffset = Vector2.Transform(originOffset, matrix);

                return toReturn + originOffset.Y;
            }
        }

        /// <summary>
        /// Returns the absolute Y (in screen space) of the top edge of the GraphicalUiElement.
        /// </summary>
        public float AbsoluteTop => this.GetAbsoluteY();

        /// <summary>
        /// Returns the right side in absolute pixel coordinates
        /// </summary>
        public float AbsoluteRight => AbsoluteLeft + this.GetAbsoluteWidth();

        /// <summary>
        /// Returns the bottom side in absolute pixel coordinates
        /// </summary>
        public float AbsoluteBottom => AbsoluteTop + this.GetAbsoluteHeight();

        public IVisible ExplicitIVisibleParent
        {
            get;
            set;
        }

        /// <summary>
        /// The pixel coorinate of the top of the displayed region.
        /// </summary>
        public int TextureTop
        {
            get => mTextureTop;
            set
            {
                if (mTextureTop != value)
                {
                    mTextureTop = value;
                    // changing the texture top won't update the dimensions, just
                    // the contained graphical object. 
                    UpdateLayout(updateParent: false, updateChildren: false);

                }
            }
        }

        /// <summary>
        /// The pixel coordinate of the left of the displayed region.
        /// </summary>
        public int TextureLeft
        {
            get => mTextureLeft;
            set
            {
                if (mTextureLeft != value)
                {
                    mTextureLeft = value;
                    UpdateLayout(updateParent: false, updateChildren: false);
                }
            }
        }

        /// <summary>
        /// The pixel width of the source rectangle on the referenced texture.
        /// </summary>
        public int TextureWidth
        {
            get
            {
                return mTextureWidth;
            }
            set
            {
                if (mTextureWidth != value)
                {
                    mTextureWidth = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// The pixel height of the source rectangle on the referenced texture.
        /// </summary>
        public int TextureHeight
        {
            get
            {
                return mTextureHeight;
            }
            set
            {
                if (mTextureHeight != value)
                {
                    mTextureHeight = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// The width scale to apply to the texture width when using TextureAddress.DimensionsBased.
        /// If TextureAddress.DimensionsBased is not used, this value is ignored.
        /// </summary>
        public float TextureWidthScale
        {
            get
            {
                return mTextureWidthScale;
            }
            set
            {
                if (mTextureWidthScale != value)
                {
                    mTextureWidthScale = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// The height scale to apply to the texture width when using TextureAddress.DimensionsBased.
        /// If TextureAddress.DimensionsBased is not used, this value is ignored.
        /// </summary>
        public float TextureHeightScale
        {
            get
            {
                return mTextureHeightScale;
            }
            set
            {
                if (mTextureHeightScale != value)
                {
                    mTextureHeightScale = value;
                    UpdateLayout();
                }
            }
        }

        public TextureAddress TextureAddress
        {
            get
            {
                return mTextureAddress;
            }
            set
            {
                if (mTextureAddress != value)
                {
                    mTextureAddress = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Whether the texture address should wrap.
        /// </summary>
        public bool Wrap
        {
            get
            {
                return mWrap;
            }
            set
            {
                if (mWrap != value)
                {
                    mWrap = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Whether contained children should wrap. This only applies if ChildrenLayout is set to 
        /// ChildrenLayout.LeftToRightStack or ChildrenLayout.TopToBottomStack.
        /// </summary>
        public bool WrapsChildren
        {
            get { return mWrapsChildren; }
            set
            {
                if (mWrapsChildren != value)
                {
                    mWrapsChildren = value; UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Whether the rendering of this object's children should be clipped to the bounds of this object. If false
        /// then children can render outside of the bounds of this object.
        /// </summary>
        public bool ClipsChildren
        {
            get => mContainedObjectAsIpso?.ClipsChildren == true;
            set
            {
                if (mContainedObjectAsIpso is ISetClipsChildren clipsChildrenChild)
                {
                    clipsChildrenChild.ClipsChildren = value;
                }
            }
        }

#if !FRB
        public List<AnimationRuntime>? Animations { get; set; }

        AnimationRuntime? currentAnimation;
        double currentAnimationTime;

        /// <summary>
        /// Starts playing the specified AnimationRuntime.
        /// </summary>
        /// <param name="animation">the AnimationRuntime object</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void PlayAnimation(AnimationRuntime animation)
        {
            if (animation != null)
            {
                currentAnimation = animation;
                currentAnimationTime = 0;
            }
            else
            {
                throw new ArgumentNullException(nameof(animation), "the animation cannot be null");
            }
        }


        /// <summary>
        /// Stops the currently playing animation.
        /// </summary>
        public void StopAnimation()
        {
            currentAnimation = null;
        }
#endif

        #endregion
    }
}
