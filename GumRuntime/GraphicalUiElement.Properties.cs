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

    ColorOperation IRenderableIpso.ColorOperation => RequiredContainedObject.ColorOperation;

    public static MissingFileBehavior MissingFileBehavior { get; set; } = MissingFileBehavior.ConsumeSilently;

    /// <summary>
    /// The element this was created from, or null for an element created in code.
    /// </summary>
    public ElementSave? ElementSave
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

    /// <summary>
    /// A transform applied to the cursor's raw screen position during hit-testing on this
    /// element and its descendants (resolved through <see cref="EffectiveHitTestTransformMatrix"/>).
    /// Set it to map a window pixel back into the coordinate space this subtree was drawn in —
    /// e.g. the inverse of the scale/letterbox blit used to composite a subtree that was rendered
    /// into an externally-managed RenderTarget2D. Null (the default) means no transform. This is
    /// consumed by hit-testing only and is never read by rendering or layout (issue #4096).
    /// </summary>
    public System.Numerics.Matrix3x2? HitTestTransformMatrix { get; set; }

    /// <summary>
    /// Returns this instance's <see cref="HitTestTransformMatrix"/>, or climbs the parent/child
    /// relationship to the nearest ancestor that set one. Returns null when none is set. Mirrors
    /// <see cref="EffectiveManagers"/>: nearest ancestor wins, null means "no transform."
    /// </summary>
    public System.Numerics.Matrix3x2? EffectiveHitTestTransformMatrix
    {
        get
        {
            if (HitTestTransformMatrix != null)
            {
                return HitTestTransformMatrix;
            }
            else
            {
                return this.ElementGueContainingThis?.EffectiveHitTestTransformMatrix ??
                    this.EffectiveParentGue?.EffectiveHitTestTransformMatrix;
            }
        }
    }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();

    /// <inheritdoc/>
    public bool Visible
    {
        get => mContainedObjectAsIVisible?.Visible ?? false;
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
                                // Update January 27, 2026: Yes, children should update if we are dealing with ratios:
                                //0,
                                ShouldUpdateChildren() ? int.MaxValue/2 : 0, 
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
                        // See call above on why we check if children should be updated
                        //0,
                        ShouldUpdateChildren() ? int.MaxValue / 2 : 0,
                        null);
                }

                if (!absoluteVisible && (GetIfParentStacks() || GetIfParentIsAutoGrid() || GetIfParentWidthHeightDependOnChildren()))
                {
                    // This updates the parent right away. #3066: gate the climb on the parent's size
                    // delta so hiding a descendant that doesn't change the item's size doesn't relayout
                    // every sibling in a stacking parent.
                    Parent?.UpdateLayout(ParentUpdateType.IfParentStacks | ParentUpdateType.IfParentWidthHeightDependOnChildren | ParentUpdateType.IfParentIsAutoGrid, int.MaxValue / 2, null, gateClimbOnSizeChange: true);

                }
                VisibleChanged?.Invoke(this, EventArgs.Empty);
                ReportTypedPropertyChanged("Visible", value);
            }

            bool ShouldUpdateChildren()
            {
                // If this or siblings are ratio, then changing this width can result in changing the width of children, so we need to update recursively.
                if (this.WidthUnits == DimensionUnitType.Ratio || this.HeightUnits == DimensionUnitType.Ratio) return true;

                if(this.Parent != null)
                {
                    foreach(var child in this.Parent.Children)
                    {
                        if(child.WidthUnits == DimensionUnitType.Ratio || child.HeightUnits == DimensionUnitType.Ratio)
                        {
                            return true;
                        }
                    }
                }
                return false;
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

    #region IPSO properties

    /// <summary>
    /// The X position of this object as an IPositionedSizedObject. This does not consider origins
    /// so it will use the default origin, which is top-left for most types.
    /// </summary>
    float IPositionedSizedObject.X
    {
        get
        {
            // this used to throw an exception, but 
            // the screen is an IPSO which may be considered
            // the effective parent of an element.
            if (mContainedObjectAsIpso == null)
            {
                return 0;
            }
            else
            {
                return mContainedObjectAsIpso.X;
            }
        }
        set
        {
            throw new InvalidOperationException("This is a GraphicalUiElement. You must cast the instance to GraphicalUiElement to set its X so that its XUnits apply.");
        }
    }

    /// <summary>
    /// The Y position of this object as an IPositionedSizedObject. This does not consider origins
    /// so it will use the default origin, which is top-left for most types.
    /// </summary>
    float IPositionedSizedObject.Y
    {
        get
        {
            if (mContainedObjectAsIpso == null)
            {
                return 0;
            }
            else
            {
                return mContainedObjectAsIpso.Y;
            }
        }
        set
        {
            throw new InvalidOperationException("This is a GraphicalUiElement. You must cast the instance to GraphicalUiElement to set its Y so that its YUnits apply.");
        }
    }

    float IPositionedSizedObject.Rotation
    {
        get => mContainedObjectAsIpso?.Rotation ?? 0;
        set
        {
            throw new InvalidOperationException(
                "This is a GraphicalUiElement. You must cast the instance to GraphicalUiElement to set its Rotation so that its layout apply.");

        }
    }

    float IPositionedSizedObject.Width
    {
        get
        {
            if (mContainedObjectAsIpso == null)
            {
                return GraphicalUiElement.CanvasWidth;
            }
            else
            {
                return mContainedObjectAsIpso.Width;
            }
        }
        set
        {
            RequiredContainedObject.Width = value;
        }
    }

    float IPositionedSizedObject.Height
    {
        get
        {
            if (mContainedObjectAsIpso == null)
            {
                return GraphicalUiElement.CanvasHeight;
            }
            else
            {
                return mContainedObjectAsIpso.Height;
            }
        }
        set
        {
            RequiredContainedObject.Height = value;
        }
    }

    /// <summary>
    /// Returns the absolute width of the GraphicalUiElement in pixels (as opposed to using its WidthUnits)
    /// </summary>
    /// <returns>The absolute width in pixels.</returns>
    [Obsolete("Use AbsoluteWidth instead.")]
    public float GetAbsoluteWidth() => AbsoluteWidth;

    /// <summary>
    /// Returns the absolute height of the GraphicalUiElement in pixels (as opposed to using its HeightUnits)
    /// </summary>
    /// <returns>The absolute height in pixels.</returns>
    [Obsolete("Use AbsoluteHeight instead.")]
    public float GetAbsoluteHeight() => AbsoluteHeight;

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        RequiredContainedObject.SetParentDirect(parent);
    }

    #endregion

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
            // Without a renderable there is nothing to hold Z, and the getter reports 0.
            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.Z = value;
            }
        }
    }

    #region IRenderable properties


    BlendState? IRenderable.BlendState
    {
        get
        {
            return RequiredContainedObject.BlendState;
        }
    }


    bool IRenderable.Wrap
    {
        get { return RequiredContainedObject.Wrap; }
    }

    public virtual void Render(ISystemManagers managers)
    {
        RequiredContainedObject.Render(managers);
    }

    public virtual string BatchKey => mContainedObjectAsIpso?.BatchKey ?? string.Empty;

    public virtual object? BatchSortKey => mContainedObjectAsIpso?.BatchSortKey;

    public virtual void StartBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.StartBatch(systemManagers);
    public virtual void EndBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.EndBatch(systemManagers);

    Layer? mLayer;

    public Layer? Layer => mLayer;

    #endregion

    public bool IsRenderTarget => mContainedObjectAsIpso?.IsRenderTarget == true;
    int IRenderableIpso.Alpha => mContainedObjectAsIpso?.Alpha ?? 255;

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
                _isSettingParent = true;
                try
                {
                    if (_parent?.Children?.Contains(this) == true)
                    {
                        _parent.Children.Remove(this);
                    }
                    _parent = value;

                    // In case the object was added explicitly
                    if (_parent?.Children != null && _parent.Children.Contains(this) == false)
                    {
                        _parent.Children.Add(this);

                    }
                }
                finally
                {
                    _isSettingParent = false;
                }

                // If layout is suppressed, the parent may not get set
                // and it's possible to have a floating visible=true object
                // that gets rendered without a parent:
                mContainedObjectAsIpso?.SetParentDirect(value);

                // Runs for every removal path (Parent = null, RemoveChild, Children.Remove/Clear).
                // A suspended parent only records the layout as dirty.
                oldParent?.UpdateLayoutAfterChildRemoved();
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

    // A removed child only moves its old parent's layout when the parent stacks, places children
    // in a grid, sizes from them, or shares space between Ratio children.
    void UpdateLayoutAfterChildRemoved()
    {
        var dependsOnChildren = ChildrenLayout != ChildrenLayout.Regular || GetIfDimensionsDependOnChildren();
        if (!dependsOnChildren)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].WidthUnits == DimensionUnitType.Ratio || Children[i].HeightUnits == DimensionUnitType.Ratio)
                {
                    dependsOnChildren = true;
                    break;
                }
            }
        }
        if (dependsOnChildren)
        {
            UpdateLayout();
        }
    }

    // Made obsolete November 4, 2017
    [Obsolete("Use ElementGueContainingThis instead - it more clearly indicates the relationship, " +
        "as the ParentGue may not actually be the parent. If the effective parent is desired, use EffectiveParentGue")]
    public GraphicalUiElement? ParentGue
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

    /// <summary>
    /// The renderable this element displays, or null if it has none (such as a Screen).
    /// </summary>
    public IRenderable? RenderableComponent
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

    string? name;
    public string? Name
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

    private static readonly ObservableCollection<IRenderableIpso> EmptyIpsoChildren =
        new FrozenObservableCollection<IRenderableIpso>();

    ObservableCollection<IRenderableIpso> IRenderableIpso.Children
    {
        get
        {
            return mContainedObjectAsIpso?.Children ?? EmptyIpsoChildren;
        }
    }

    private GraphicalUiElementCollection _childrenWrapper = GraphicalUiElementCollection.Empty;

    public ObservableCollection<GraphicalUiElement> Children => _childrenWrapper;



    object? mTagIfNoContainedObject;
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
    /// Returns the absolute width of the GraphicalUiElement in pixels (as opposed to using its WidthUnits).
    /// </summary>
    public float AbsoluteWidth => ((IPositionedSizedObject)this).Width;

    /// <summary>
    /// Returns the absolute height of the GraphicalUiElement in pixels (as opposed to using its HeightUnits).
    /// </summary>
    public float AbsoluteHeight => ((IPositionedSizedObject)this).Height;

    /// <summary>
    /// Returns the right side in absolute pixel coordinates
    /// </summary>
    public float AbsoluteRight => AbsoluteLeft + this.AbsoluteWidth;

    /// <summary>
    /// Returns the bottom side in absolute pixel coordinates
    /// </summary>
    public float AbsoluteBottom => AbsoluteTop + this.AbsoluteHeight;

    public IVisible? ExplicitIVisibleParent
    {
        get;
        set;
    }

    /// <summary>
    /// The pixel coordinate of the top of the displayed region.
    /// Ignored unless <see cref="TextureAddress"/> is Custom or DimensionsBased.
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
    /// Ignored unless <see cref="TextureAddress"/> is Custom or DimensionsBased.
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
    /// Only applied when <see cref="TextureAddress"/> is Custom; ignored for EntireTexture and
    /// DimensionsBased (which derives width from <see cref="TextureWidthScale"/> instead).
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
    /// Only applied when <see cref="TextureAddress"/> is Custom; ignored for EntireTexture and
    /// DimensionsBased (which derives height from <see cref="TextureHeightScale"/> instead).
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

    /// <summary>
    /// Controls how the source rectangle on the texture is determined. Defaults to EntireTexture, which
    /// ignores <see cref="TextureLeft"/>, <see cref="TextureTop"/>, <see cref="TextureWidth"/>, and
    /// <see cref="TextureHeight"/>. Must be set to Custom (or DimensionsBased) for those values to take effect.
    /// </summary>
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
    /// <summary>
    /// The list of <see cref="AnimationRuntime"/> objects available on this element.
    /// Animations are typically populated when loading a Gum project. Use
    /// <see cref="PlayAnimation(AnimationRuntime)"/> or the extension methods
    /// <c>PlayAnimation(int)</c> / <c>PlayAnimation(string)</c> to start playback,
    /// or access <see cref="AnimationController"/> directly for full control.
    /// </summary>
    public List<AnimationRuntime>? Animations { get; set; }

    /// <summary>
    /// Gets the AnimationController that manages animation playback for this element.
    /// Use this to control animations (play, pause, stop), check playback state, and subscribe to animation events.
    /// </summary>
    public AnimationController AnimationController { get; private set; } = new();

    /// <summary>
    /// Convenience wrapper for <see cref="AnimationController.Play(AnimationRuntime)"/>.
    /// Starts playing the specified <see cref="AnimationRuntime"/> from the beginning.
    /// <para>
    /// Only one animation can play at a time. Calling this while an animation is already
    /// playing will replace the current animation. To play multiple animations in sequence
    /// or to access playback state and events (pause, resume, <see cref="AnimationController.OnCompleted"/>),
    /// use <see cref="AnimationController"/> directly.
    /// </para>
    /// </summary>
    /// <param name="animation">The AnimationRuntime object to play.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public void PlayAnimation(AnimationRuntime animation)
    {
        AnimationController.Play(animation);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="AnimationController.PlayAnimationAsync(AnimationRuntime, System.Threading.CancellationToken)"/>.
    /// Starts playing the specified <see cref="AnimationRuntime"/> and returns a task that completes
    /// when it finishes.
    /// <para>
    /// If the animation is stopped or replaced by another <see cref="PlayAnimation(AnimationRuntime)"/>/
    /// <c>PlayAnimationAsync</c> call before it finishes, the returned task is cancelled
    /// (<see cref="System.Threading.Tasks.TaskCanceledException"/>) rather than completing. Callers awaiting
    /// this method must handle that case, since code after the <c>await</c> should only run once the
    /// animation actually finished.
    /// </para>
    /// </summary>
    /// <param name="animation">The AnimationRuntime object to play.</param>
    /// <param name="cancellationToken">A token used to stop the animation and cancel the task early.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public System.Threading.Tasks.Task PlayAnimationAsync(AnimationRuntime animation, System.Threading.CancellationToken cancellationToken = default)
    {
        return AnimationController.PlayAnimationAsync(animation, cancellationToken);
    }


    /// <summary>
    /// Convenience wrapper for <see cref="AnimationController.Stop()"/>.
    /// Stops the currently playing animation and resets the playback time to zero.
    /// <see cref="AnimationController"/> plays one animation at a time, so this stops
    /// whichever animation is currently active.
    /// </summary>
    public void StopAnimation()
    {
        AnimationController.Stop();
    }
#endif
}
