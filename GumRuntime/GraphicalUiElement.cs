using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;
using GumRuntime;

#if MONOGAME || XNA4
using RenderingLibrary.Math.Geometry;
#endif

using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;

#if SKIA
using SkiaGum;
using SkiaGum.GueDeriving;
using SkiaGum.Managers;
using SkiaGum.Renderables;
using SkiaSharp;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
#if UWP
using System.Reflection;
#endif

namespace Gum.Wireframe
{
    #region Enums

    public enum MissingFileBehavior
    {
        ConsumeSilently,
        ThrowException
    }

    #endregion

    /// <summary>
    /// The base object for all Gum runtime objects. It contains functionality for
    /// setting variables, states, and perofrming layout. The GraphicalUiElement can
    /// wrap an underlying rendering object.
    /// </summary>
    public partial class GraphicalUiElement : IRenderableIpso, IVisible
    {
        #region Enums/Internal Classes

        enum ChildType
        {
            Absolute = 1,
            Relative = 1 << 1,
            BothAbsoluteAndRelative = Absolute | Relative,
            StackedWrapped = 1 << 2,
            All = Absolute | Relative | StackedWrapped
        }

        class DirtyState
        {
            public ParentUpdateType ParentUpdateType;
            public int ChildrenUpdateDepth;
            public XOrY? XOrY;
        }

        public enum ParentUpdateType
        {
            None = 0,
            IfParentStacks = 1,
            IfParentWidthHeightDependOnChildren = 2,
            All = 4

        }

        #endregion

        #region Fields

        private DirtyState currentDirtyState;
        bool isFontDirty = false;

        public static int UpdateLayoutCallCount;
        public static int ChildrenUpdatingParentLayoutCalls;

        public static bool ShowLineRectangles = true;

        // to save on casting:
        IRenderableIpso mContainedObjectAsIpso;
        IVisible mContainedObjectAsIVisible;

        GraphicalUiElement mWhatContainsThis;

        /// <summary>
        /// A flat list of all GraphicalUiElements contained by this element. For example, if this GraphicalUiElement
        /// is a Screen, this list is all GraphicalUielements for every instance contained regardless of hierarchy.
        /// </summary>
        List<GraphicalUiElement> mWhatThisContains = new List<GraphicalUiElement>();

        Dictionary<string, string> mExposedVariables = new Dictionary<string, string>();

        GeneralUnitType mXUnits;
        GeneralUnitType mYUnits;
        HorizontalAlignment mXOrigin;
        VerticalAlignment mYOrigin;
        DimensionUnitType mWidthUnit;
        DimensionUnitType mHeightUnit;

        SystemManagers mManagers;

        int mTextureTop;
        int mTextureLeft;
        int mTextureWidth;
        int mTextureHeight;
        bool mWrap;

        bool mWrapsChildren = false;

        float mTextureWidthScale;
        float mTextureHeightScale;

        TextureAddress mTextureAddress;

        float mX;
        float mY;
        protected float mWidth;
        protected float mHeight;
        float mRotation;

        IRenderableIpso mParent;

        protected bool mIsLayoutSuspended = false;
        public bool IsLayoutSuspended => mIsLayoutSuspended;

        // We need ThreadStatic in case screens are being loaded
        // in the background - we don't want to interrupt the foreground
        // layout behavior.
        [ThreadStatic]
        public static bool IsAllLayoutSuspended = false;

        Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
            new Dictionary<string, DataTypes.Variables.StateSave>();

        Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
            new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

        // the row or column index when anobject is sorted.
        // This is used by the stacking logic to properly sort objects
        public int StackedRowOrColumnIndex { get; set; } = -1;

        // null by default, non-null if an object uses
        // stacked layout for its children.
        public List<float> StackedRowOrColumnDimensions { get; private set; }
        #endregion

        #region Properties

        ColorOperation IRenderableIpso.ColorOperation => mContainedObjectAsIpso.ColorOperation;

        public static MissingFileBehavior MissingFileBehavior { get; set; } = MissingFileBehavior.ConsumeSilently;

        public ElementSave ElementSave
        {
            get;
            set;
        }

        public SystemManagers Managers
        {
            get
            {
                return mManagers;
            }
        }

        /// <summary>
        /// Returns this instance's SystemManagers, or climbs up the parent/child relationship
        /// until a non-null SystemsManager is found. Otherwise, returns null.
        /// </summary>
        public SystemManagers EffectiveManagers
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

                    var absoluteVisible = ((IVisible)this).AbsoluteVisible;
                    // See if this has a parent that stacks children. If so, update its layout:

                    var didUpdate = false;
                    if(absoluteVisible)
                    {
                        if(!mIsLayoutSuspended && !GraphicalUiElement.IsAllLayoutSuspended)
                        {
                            // resume layout:
                            // This does need to be recursive because contained objects may have been 
                            // updated while the parent was invisible, becoming dirty, and waiting for
                            // the resume
                            ResumeLayoutUpdateIfDirtyRecursive();

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
                            didUpdate = true;
                        }
                    }
                    
                    if(!didUpdate)
                    {
                        // This will make this dirty:
                        this.UpdateLayout(ParentUpdateType.IfParentStacks, 
                            // If something is made visible, that shouldn't update the children, right?
                            //int.MaxValue/2, 
                            0,
                            null);
                    }

                    if(!absoluteVisible && GetIfParentStacks())
                    {
                        // This updates the parent right away:
                        (Parent as GraphicalUiElement)?.UpdateLayout(ParentUpdateType.IfParentStacks, int.MaxValue / 2, null);

                    }
                }
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
                mContainedObjectAsIpso.Width = value;
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
                mContainedObjectAsIpso.Height = value;
            }
        }

        /// <summary>
        /// Returns the absolute width of the GraphicalUiElement in pixels (as opposed to using its WidthUnits)
        /// </summary>
        /// <returns>The absolute width in pixels.</returns>
        public float GetAbsoluteWidth() => ((IPositionedSizedObject)this).Width;

        /// <summary>
        /// Returns the absolute height of the GraphicalUiElement in pixels (as opposed to using its HeightUnits)
        /// </summary>
        /// <returns>The absolute height in pixels.</returns>
        public float GetAbsoluteHeight() => ((IPositionedSizedObject)this).Height;

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mContainedObjectAsIpso.SetParentDirect(parent);
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
                mContainedObjectAsIpso.Z = value;
            }
        }

        #region IRenderable properties


#if MONOGAME || XNA4
        Microsoft.Xna.Framework.Graphics.BlendState IRenderable.BlendState
        {
            get
            {
#if DEBUG
                if (mContainedObjectAsIpso == null)
                {
                    throw new NullReferenceException("This GraphicalUiElemente has not had its visual set, so it does not have a blend operation. This can happen if a GraphicalUiElement was added as a child without its contained renderable having been set.");
                }
#endif
                return mContainedObjectAsIpso.BlendState;
            }
        }
#endif

        bool IRenderable.Wrap
        {
            get { return mContainedObjectAsIpso.Wrap; }
        }

#if MONOGAME || XNA4
        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            mContainedObjectAsIpso.Render(spriteRenderer, managers);
        }
#endif

#if SKIA
        public virtual void Render(SKCanvas canvas)
        {
            mContainedObjectAsIpso.Render(canvas);

            if (ClipsChildren)
            {
                var absoluteX = this.GetAbsoluteX();
                var absoluteY = this.GetAbsoluteY();
                var rect = new SKRect(absoluteX, absoluteY, absoluteX + mContainedObjectAsIpso.Width, absoluteY + mContainedObjectAsIpso.Height);

                canvas.Save();
                canvas.ClipRect(rect);
            }

            // todo - this may allocate slightly due to foreach. Consider changing to a for loop
            foreach (var child in this.Children)
            {
                child.Render(canvas);
            }

            if (ClipsChildren)
            {
                canvas.Restore();
            }
        }
#endif


        Layer mLayer;

        #endregion

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
                    mHeightUnit = value; UpdateLayout();
                }
            }
        }


        bool ignoredByParentSize;
        public bool IgnoredByParentSize
        {
            get => ignoredByParentSize;
            set
            {
                if(ignoredByParentSize != value)
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
                if(value != childrenLayout)
                {
                    childrenLayout = value; UpdateLayout();
                }
            }
        }

        float stackSpacing;
        /// <summary>
        /// The number of pixels spacing between each child if this is has a ChildrenLayout of 
        /// TopToBottomStack or LeftToRightStack.
        /// </summary>
        public float StackSpacing
        {
            get => stackSpacing;
            set
            {
                if(stackSpacing != value)
                {
                    stackSpacing = value; 
                    if(ChildrenLayout != ChildrenLayout.Regular)
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
#if DEBUG
                if (float.IsNaN(value) || float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
                {
                    throw new Exception($"Invalid Rotaiton value set: {value}");
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
#if DEBUG
                    if (float.IsNaN(value))
                    {
                        throw new ArgumentException("Not a Number (NAN) not allowed");
                    }
#endif
                    mX = value;

                    var parentGue = Parent as GraphicalUiElement;
                    // special case:
                    if (Parent as GraphicalUiElement == null && XUnits == GeneralUnitType.PixelsFromSmall && XOrigin == HorizontalAlignment.Left)
                    {
                        this.mContainedObjectAsIpso.X = mX;
                    }
                    else
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
#if DEBUG
                    if (float.IsNaN(value))
                    {
                        throw new ArgumentException("Not a Number (NAN) not allowed");
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

        public float Width
        {
            get { return mWidth; }
            set
            {
#if DEBUG
                if (float.IsPositiveInfinity(value) ||
                    float.IsNegativeInfinity(value) ||
                    float.IsNaN(value))
                {
                    throw new ArgumentException();
                }
#endif
                if (mWidth != value)
                {
                    mWidth = value; UpdateLayout();
                }
            }
        }

        public float Height
        {
            get { return mHeight; }
            set
            {
                if (mHeight != value)
                {
#if DEBUG
                    if (float.IsPositiveInfinity(value) ||
                        float.IsNegativeInfinity(value) ||
                        float.IsNaN(value))
                    {
                        throw new ArgumentException();
                    }
#endif
                    mHeight = value; UpdateLayout();
                }
            }
        }

        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
#if DEBUG
                if (value == this)
                {
                    throw new InvalidOperationException("Cannot attach an object to itself");
                }
#endif
                if (mParent != value)
                {
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Remove(this);
                        (mParent as GraphicalUiElement)?.UpdateLayout();
                    }
                    mParent = value;

                    // In case the object was added explicitly 
                    if (mParent?.Children != null && mParent.Children.Contains(this) == false)
                    {
                        mParent.Children.Add(this);
                    }
                    UpdateLayout();

                    ParentChanged?.Invoke(this, null);
                }
            }
        }

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
        public GraphicalUiElement ElementGueContainingThis
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

        public GraphicalUiElement EffectiveParentGue
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
        /// Returns an enumerable for all GraphicalUiElements that this contains.
        /// </summary>
        /// <remarks>
        /// Since this is an interface using ContainedElements in a foreach allocates memory
        /// and this can actually be significant in a game that updates its UI frequently.
        /// </remarks>
        public IEnumerable<GraphicalUiElement> ContainedElements
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
        /// Returns the direct hierarchical children of this. Note that this does not return all objects contained in the element. 
        /// </summary>
        public ObservableCollection<IRenderableIpso> Children
        {
            get
            {
                return mContainedObjectAsIpso?.Children;
            }
        }

        object mTagIfNoContainedObject;
        public object Tag
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

        public IPositionedSizedObject Component { get { return mContainedObjectAsIpso; } }

        /// <summary>
        /// Returns the absolute X of the origin of the GraphicalUiElement. Note that
        /// this considers the XOrigin, and will apply rotation
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
                        if (mContainedObjectAsIpso is Text text)
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
        /// Returns the absolute Y of the origin of the GraphicalUiElement. Note that
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
                        if (mContainedObjectAsIpso is Text text)
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
            get
            {
                return mTextureTop;
            }
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
        /// The pixel coorinate of the left of the displayed region.
        /// </summary>
        public int TextureLeft
        {
            get
            {
                return mTextureLeft;
            }
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
        /// The pixel width of the displayed region.
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
        /// The pixel height of the displayed region.
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

        public bool ClipsChildren
        {
            get;
            set;
        }


        #endregion

        #region Events

        // It's possible that a size change could result in a layout which 
        // results in a further size change. This recursive call of size changes
        // could happen indefinitely so we only want to do this one time.
        // This prevents the size change from happening over and over:
        bool isInSizeChange;
        public event EventHandler SizeChanged;
        public event EventHandler PositionChanged;
        public event EventHandler ParentChanged;

        #endregion

        #region Constructor

#if MONOGAME || XNA4
        public GraphicalUiElement()
            : this(null, null)
        {

        }
#endif

#if SKIA
        public GraphicalUiElement()
        {
            mIsLayoutSuspended = true;
            Width = 32;
            Height = 32;
            mIsLayoutSuspended = false;

        }
#endif

        public GraphicalUiElement(IRenderable containedObject, GraphicalUiElement whatContainsThis)
        {
            SetContainedObject(containedObject);

            mWhatContainsThis = whatContainsThis;
            if (mWhatContainsThis != null)
            {
                mWhatContainsThis.mWhatThisContains.Add(this);

                // I don't think we want to do this. 
                if (whatContainsThis.mContainedObjectAsIpso != null)
                {
                    this.Parent = whatContainsThis;
                }
            }
        }

        public void SetContainedObject(IRenderable containedObject)
        {
            if (containedObject == this)
            {
                throw new ArgumentException("The argument containedObject cannot be 'this'");
            }


            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.Children.CollectionChanged -= HandleCollectionChanged;
                if (string.IsNullOrEmpty(this.Name) && !string.IsNullOrEmpty(mContainedObjectAsIpso.Name))
                {
                    Name = mContainedObjectAsIpso.Name;
                }
            }

            mContainedObjectAsIpso = containedObject as IRenderableIpso;
            mContainedObjectAsIVisible = containedObject as IVisible;

            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.Children.CollectionChanged += HandleCollectionChanged;
            }

            if (containedObject != null)
            {
                UpdateLayout();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the default state.
        /// </summary>
        /// <remarks>
        /// This function is virtual so that derived classes can override it
        /// and provide a quicker method for setting default states
        /// </remarks>
        public virtual void SetInitialState()
        {
            var elementSave = this.Tag as ElementSave;
            this.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        }

        public void UpdateLayout()
        {
            UpdateLayout(true, true);
        }

        public void UpdateLayout(bool updateParent, bool updateChildren)
        {
            int value = int.MaxValue / 2;
            if (!updateChildren)
            {
                value = 0;
            }
            UpdateLayout(updateParent, value);
        }

        string NameOrType => !string.IsNullOrEmpty(Name) ? Name : $"<{GetType().Name}>";
        
        string ParentQualifiedName => Parent as GraphicalUiElement == null ? NameOrType : (Parent as GraphicalUiElement).ParentQualifiedName + "." + NameOrType;

        public void UpdateLayout(bool updateParent, int childrenUpdateDepth, XOrY? xOrY = null)
        {
            if (updateParent)
            {
                UpdateLayout(ParentUpdateType.All, childrenUpdateDepth, xOrY);
            }
            else
            {
                UpdateLayout(ParentUpdateType.None, childrenUpdateDepth, xOrY);
            }

        }

        public static bool AreUpdatesAppliedWhenInvisible { get; set; } = false;

        public void UpdateLayout(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY = null)
        {
            var updateParent =
                (parentUpdateType & ParentUpdateType.All) == ParentUpdateType.All ||
                (parentUpdateType & ParentUpdateType.IfParentStacks) == ParentUpdateType.IfParentStacks && GetIfParentStacks() ||
                (parentUpdateType & ParentUpdateType.IfParentWidthHeightDependOnChildren) == ParentUpdateType.IfParentWidthHeightDependOnChildren && (Parent as GraphicalUiElement)?.GetIfDimensionsDependOnChildren() == true;

            #region Early Out - Suspended

            var asIVisible = this as IVisible;

            var isSuspended = mIsLayoutSuspended || IsAllLayoutSuspended;
            if (!isSuspended)
            {
                isSuspended = !AreUpdatesAppliedWhenInvisible && mContainedObjectAsIVisible != null && asIVisible.AbsoluteVisible == false;
            }

            if (isSuspended)
            {
                MakeDirty(parentUpdateType, childrenUpdateDepth, xOrY);
                return;
            }

            if(!AreUpdatesAppliedWhenInvisible)
            {
                var parentAsIVisible = Parent as IVisible;
                if (Visible == false && parentAsIVisible?.AbsoluteVisible == false )
                {
                    return;
                }
            }

            #endregion

            #region Early Out - Update Parent and exit

            currentDirtyState = null;


            // May 15, 2014
            // Parent needs to be
            // set before we start
            // doing the updates because
            // we use foreaches internally
            // in the updates.
            if (mContainedObjectAsIpso != null)
            {
                // If we assign the Parent, then the Parent will have the 
                // mContainedObjectAsIpso added to its children, which will
                // result in it being rendered. But this GraphicalUiElement is
                // already a child of the Parent, so adding the mContainedObjectAsIpso
                // as well would result in a double-render. Instead, we'll set the parent
                // direct, so the parent doesn't know about this child:
                //mContainedObjectAsIpso.Parent = mParent;
                mContainedObjectAsIpso.SetParentDirect(mParent);
            }


            // Not sure why we use the ParentGue and not the Parent itself...
            // We want to do it on the actual Parent so that objects attached to components
            // should update the components
            if (updateParent && GetIfShouldCallUpdateOnParent())
            {
                var asGue = this.Parent as GraphicalUiElement;
                // Just climb up one and update from there
                asGue.UpdateLayout(parentUpdateType, childrenUpdateDepth + 1);
                ChildrenUpdatingParentLayoutCalls++;
                return;
            }
            // This should be *after* the return when updating the parent otherwise we double-count layouts
            UpdateLayoutCallCount++;

            #endregion

            float widthBeforeLayout = 0;
            float heightBeforeLayout = 0;
            float xBeforeLayout = 0;
            float yBeforeLayout = 0;
            // Victor Chelaru
            // March 1, 2015
            // We tested not doing "deep" UpdateLayouts
            // if the object doesn't actually need it. This
            // is the case if the if-statement below evaluates to true. But in practice
            // we got very minor reduction in calls, but we incurred a lot of if-checks, so I don't
            // think this is worth it at this time.
            //if(this.mXOrigin == HorizontalAlignment.Left && mXUnits == GeneralUnitType.PixelsFromSmall &&
            //    this.mYOrigin == VerticalAlignment.Top && mYUnits == GeneralUnitType.PixelsFromSmall &&
            //    this.mWidthUnit == DimensionUnitType.Absolute && this.mWidth > 0 &&
            //    this.mHeightUnit == DimensionUnitType.Absolute && this.mHeight > 0)
            //{
            //    var parent = EffectiveParentGue;
            //    if (parent == null || parent.ChildrenLayout == Gum.Managers.ChildrenLayout.Regular)
            //    {
            //        UnnecessaryUpdateLayouts++;
            //    }
            //}

            float parentWidth;
            float parentHeight;

            GetParentDimensions(out parentWidth, out parentHeight);

            float absoluteParentRotation = 0;
            bool isParentFlippedHorizontally = false;
            if (this.Parent != null)
            {
                absoluteParentRotation = this.Parent.GetAbsoluteRotation();
                isParentFlippedHorizontally = Parent.GetAbsoluteFlipHorizontal();
            }
            else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
            {
                parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
                parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;

                absoluteParentRotation = this.ElementGueContainingThis.GetAbsoluteRotation();
            }

            if (mContainedObjectAsIpso != null)
            {
#if MONOGAME || XNA4
                if (mContainedObjectAsIpso is LineRectangle)
                {
                    (mContainedObjectAsIpso as LineRectangle).ClipsChildren = ClipsChildren;
                }
                else
#endif
                if (mContainedObjectAsIpso is InvisibleRenderable)
                {
                    (mContainedObjectAsIpso as InvisibleRenderable).ClipsChildren = ClipsChildren;
                }

                if (this.mContainedObjectAsIpso != null)
                {
                    widthBeforeLayout = mContainedObjectAsIpso.Width;
                    heightBeforeLayout = mContainedObjectAsIpso.Height;

                    xBeforeLayout = mContainedObjectAsIpso.X;
                    yBeforeLayout = mContainedObjectAsIpso.Y;
                }

                // The texture dimensions may need to be set before
                // updating width if we are using % of texture width/height.
                // However, if the texture coordinates depend on the dimensions
                // (like for a tiling background) then this also needs to be set
                // after UpdateDimensions. 
                if (mContainedObjectAsIpso is Sprite
#if MONOGAME || XNA4
                    || mContainedObjectAsIpso is NineSlice
#endif
                    )
                {
                    UpdateTextureCoordinatesNotDimensionBased();
                }

                // August 12, 2021
                // If we can update one
                // of the dimensions first
                // (if it doesn't depend on
                // any children), we should, since
                // it can make the children update have
                // the real width/height set properly
                var widthDependencyType = this.WidthUnits.GetDependencyType();
                var heightDependencyType = this.HeightUnits.GetDependencyType();

                var hasChildDependency = widthDependencyType == HierarchyDependencyType.DependsOnChildren ||
                    heightDependencyType == HierarchyDependencyType.DependsOnChildren;

                if (hasChildDependency && widthDependencyType != heightDependencyType)
                {
                    // we can do one of them first
                    if (widthDependencyType != HierarchyDependencyType.DependsOnChildren)
                    {
                        UpdateDimensions(parentWidth, parentHeight, XOrY.X, considerWrappedStacked: false);
                    }
                    else if (heightDependencyType != HierarchyDependencyType.DependsOnChildren)
                    {
                        UpdateDimensions(parentWidth, parentHeight, XOrY.Y, considerWrappedStacked: false);
                    }
                }

                if (hasChildDependency && childrenUpdateDepth > 0)
                {
                    UpdateChildren(childrenUpdateDepth, ChildType.Absolute, skipIgnoreByParentSize:true);
                }

                // This will update according to all absolute children
                UpdateDimensions(parentWidth, parentHeight, xOrY, considerWrappedStacked: false);

                if (this.WrapsChildren && (this.ChildrenLayout == ChildrenLayout.LeftToRightStack || this.ChildrenLayout == ChildrenLayout.TopToBottomStack))
                {
                    // Now we can update all children that are wrapped:
                    UpdateChildren(childrenUpdateDepth, ChildType.StackedWrapped, skipIgnoreByParentSize: false);
                    if (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
                        this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren)
                    {
                        UpdateDimensions(parentWidth, parentHeight, xOrY, considerWrappedStacked: true);
                    }
                }

                if (mContainedObjectAsIpso is Sprite
#if MONOGAME || XNA4
                    || mContainedObjectAsIpso is NineSlice
#endif
                    )
                {
                    UpdateTextureCoordinatesDimensionBased();
                }

                // If the update is "deep" then we want to refresh the text texture.
                // Otherwise it may have been something shallow like a reposition.
                // -----------------------------------------------------------------------------
                // Update December 3, 2022 - This if-check causes lots of performance issues
                // If a text object is updating itself and its parent needs to update, then if
                // children depth > 0, then the parent update will cause all other children to update
                // which is very expensive. We now do enough checks at the property level to prevent the
                // text from updating unnecessarily, so let's change this to prevent parents from updating
                // all of their children:
                //if (mContainedObjectAsIpso is Text asText && childrenUpdateDepth > 0)
                if (mContainedObjectAsIpso is Text asText)
                {
                    // Only if the width or height have changed:
                    if (mContainedObjectAsIpso.Width != widthBeforeLayout ||
                        mContainedObjectAsIpso.Height != heightBeforeLayout)
                    {
#if MONOGAME || XNA4
                        asText.SetNeedsRefreshToTrue();
                        asText.UpdatePreRenderDimensions();
#endif
                    }
                }

                // See the above call to UpdateTextureCoordiantes
                // on why this is called both before and after UpdateDimensions
                if (mContainedObjectAsIpso is Sprite)
                {
                    UpdateTextureCoordinatesNotDimensionBased();
                }


                UpdatePosition(parentWidth, parentHeight, xOrY, absoluteParentRotation, isParentFlippedHorizontally);

                if (GetIfParentStacks())
                {
                    RefreshParentRowColumnDimensionForThis();
                }

                if (this.Parent == null)
                {
                    mContainedObjectAsIpso.Rotation = mRotation;
                }
                else
                {
                    if (isParentFlippedHorizontally)
                    {
                        mContainedObjectAsIpso.Rotation =
                            -mRotation;// + Parent.GetAbsoluteRotation();
                    }
                    else
                    {
                        mContainedObjectAsIpso.Rotation =
                            mRotation;// + Parent.GetAbsoluteRotation();
                    }
                }

            }

            if (childrenUpdateDepth > 0)
            {
                UpdateChildren(childrenUpdateDepth, ChildType.All, skipIgnoreByParentSize:false);

                var sizeDependsOnChildren = this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    this.HeightUnits == DimensionUnitType.RelativeToChildren;

                var canOneDimensionChangeOtherDimension = false;

                if (this.mContainedObjectAsIpso == null)
                {
                    for(int i = 0; i < this.mWhatThisContains.Count; i++)
                    {
                        canOneDimensionChangeOtherDimension = GetIfOneDimensionCanChangeOtherDimension(mWhatThisContains[i]);

                        if (canOneDimensionChangeOtherDimension)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < this.Children.Count; i++)
                    {
                        var uncastedChild = Children[i];

                        if (uncastedChild is GraphicalUiElement child)
                        {
                            canOneDimensionChangeOtherDimension = GetIfOneDimensionCanChangeOtherDimension(child);

                            if (canOneDimensionChangeOtherDimension)
                            {
                                break;
                            }

                        }
                    }
                }

                if (sizeDependsOnChildren && canOneDimensionChangeOtherDimension)
                {
                    float widthBeforeSecondLayout = mContainedObjectAsIpso.Width;
                    float heightBeforeSecondLayout = mContainedObjectAsIpso.Height;

                    UpdateDimensions(parentWidth, parentHeight, xOrY, considerWrappedStacked: true);

                    if (widthBeforeSecondLayout != mContainedObjectAsIpso.Width ||
                        heightBeforeSecondLayout != mContainedObjectAsIpso.Height)
                    {
                        UpdateChildren(childrenUpdateDepth, ChildType.BothAbsoluteAndRelative, skipIgnoreByParentSize:true);
                    }

                }
            }

            // Eventually add more conditions here to make it fire less often
            // like check the width/height of the parent to see if they're 0
            if (updateParent && GetIfShouldCallUpdateOnParent())
            {
                (this.Parent as GraphicalUiElement).UpdateLayout(false, false);
                ChildrenUpdatingParentLayoutCalls++;
            }
            if (this.mContainedObjectAsIpso != null)
            {
                if (widthBeforeLayout != mContainedObjectAsIpso.Width ||
                    heightBeforeLayout != mContainedObjectAsIpso.Height)
                {
                    if(!isInSizeChange)
                    {
                        isInSizeChange = true;
                        SizeChanged?.Invoke(this, null);
                        isInSizeChange = false;
                    }
                }

                if (xBeforeLayout != mContainedObjectAsIpso.X ||
                        yBeforeLayout != mContainedObjectAsIpso.Y)
                {
                    PositionChanged?.Invoke(this, null);
                }
            }

        }

        ChildType GetChildLayoutType(GraphicalUiElement parent)
        {
            var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);


            var isAbsolute = mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                            mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                            mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings &&
                            mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings &&

                (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
                    mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted) &&
                (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
                    mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted ||
                    mYUnits == GeneralUnitType.PixelsFromBaseline);

            if (doesParentWrapStack)
            {
                return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
            }
            else
            {
                return isAbsolute ? ChildType.Absolute : ChildType.Relative;
            }
        }

        ChildType GetChildLayoutType(XOrY xOrY, GraphicalUiElement parent)
        {
            bool isAbsolute;
            var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);

            if (xOrY == XOrY.X)
            {
                isAbsolute = mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                    (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
                        mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted);

            }
            else // Y
            {
                isAbsolute = mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                    (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
                        mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted &&
                        mYUnits == GeneralUnitType.PixelsFromBaseline);

            }

            if (doesParentWrapStack)
            {
                return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
            }
            else
            {
                return isAbsolute ? ChildType.Absolute : ChildType.Relative;
            }
        }

        float GetRequiredParentWidth()
        {
            var effectiveParent = this.EffectiveParentGue;
            if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.TopToBottomStack && effectiveParent.WrapsChildren)
            {
                var asIpso = this as IPositionedSizedObject;
                return asIpso.X + asIpso.Width;
            }
            else
            {
                float positionValue = mX;

                // This GUE hasn't been set yet so it can't give
                // valid widths/heights
                if (this.mContainedObjectAsIpso == null)
                {
                    return 0;
                }
                float smallEdge = positionValue;
                if (mXOrigin == HorizontalAlignment.Center)
                {
                    smallEdge = positionValue - ((IPositionedSizedObject)this).Width / 2.0f;
                }
                else if (mXOrigin == HorizontalAlignment.Right)
                {
                    smallEdge = positionValue - ((IPositionedSizedObject)this).Width;
                }

                float bigEdge = positionValue;
                if (mXOrigin == HorizontalAlignment.Center)
                {
                    bigEdge = positionValue + ((IPositionedSizedObject)this).Width / 2.0f;
                }
                if (mXOrigin == HorizontalAlignment.Left)
                {
                    bigEdge = positionValue + ((IPositionedSizedObject)this).Width;
                }

                var units = mXUnits;

                float dimensionToReturn = GetDimensionFromEdges(smallEdge, bigEdge, units);

                return dimensionToReturn;
            }
        }

        float GetRequiredParentHeight()
        {
            var effectiveParent = this.EffectiveParentGue;
            if (effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack && effectiveParent.WrapsChildren)
            {
                var asIpso = this as IPositionedSizedObject;
                return asIpso.Y + asIpso.Height;
            }
            else
            {
                float positionValue = mY;

                // This GUE hasn't been set yet so it can't give
                // valid widths/heights
                if (this.mContainedObjectAsIpso == null)
                {
                    return 0;
                }
                float smallEdge = positionValue;

                var units = mYUnits;
                if (units == GeneralUnitType.PixelsFromMiddleInverted)
                {
                    smallEdge *= -1;
                }

                if (mYOrigin == VerticalAlignment.Center)
                {
                    smallEdge = positionValue - ((IPositionedSizedObject)this).Height / 2.0f;
                }
                else if (mYOrigin == VerticalAlignment.TextBaseline)
                {
                    if (mContainedObjectAsIpso is Text text)
                    {
                        smallEdge = positionValue - ((IPositionedSizedObject)this).Height + text.DescenderHeight * text.FontScale;
                    }
                    else
                    {
                        smallEdge = positionValue - ((IPositionedSizedObject)this).Height;
                    }
                }
                else if (mYOrigin == VerticalAlignment.Bottom)
                {
                    smallEdge = positionValue - ((IPositionedSizedObject)this).Height;
                }

                float bigEdge = positionValue;
                if (mYOrigin == VerticalAlignment.Center)
                {
                    bigEdge = positionValue + ((IPositionedSizedObject)this).Height / 2.0f;
                }
                if (mYOrigin == VerticalAlignment.Top)
                {
                    bigEdge = positionValue + ((IPositionedSizedObject)this).Height;
                }

                float dimensionToReturn = GetDimensionFromEdges(smallEdge, bigEdge, units);

                return dimensionToReturn;
            }

        }

        private static float GetDimensionFromEdges(float smallEdge, float bigEdge, GeneralUnitType units)
        {
            float dimensionToReturn = 0;
            if (units == GeneralUnitType.PixelsFromSmall)
            // The value already comes in properly inverted
            {
                smallEdge = 0;

                bigEdge = System.Math.Max(0, bigEdge);
                dimensionToReturn = bigEdge - smallEdge;
            }
            else if (units == GeneralUnitType.PixelsFromMiddle ||
                units == GeneralUnitType.PixelsFromMiddleInverted)
            {
                // use the full width
                float abs1 = System.Math.Abs(smallEdge);
                float abs2 = System.Math.Abs(bigEdge);

                dimensionToReturn = 2 * System.Math.Max(abs1, abs2);
            }
            else if (units == GeneralUnitType.PixelsFromLarge)
            {
                smallEdge = System.Math.Min(0, smallEdge);
                bigEdge = 0;
                dimensionToReturn = bigEdge - smallEdge;

            }
            return dimensionToReturn;
        }

        public bool GetIfDimensionsDependOnChildren()
        {
            // If this is a Screen, then it doesn't have a size. Screens cannot depend on children:
            bool isScreen = ElementSave != null && ElementSave is ScreenSave;
            return !isScreen &&
                (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
                this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren);
        }

        public virtual void PreRender()
        {
            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.PreRender();
            }
        }

        public virtual void CreateChildrenRecursively(ElementSave elementSave, SystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
#if MONOGAME || XNA4
                var childGue = instance.ToGraphicalUiElement(systemManagers);

                if (childGue != null)
                {
                    if (!isScreen)
                    {
                        childGue.Parent = this;
                    }
                    childGue.ElementGueContainingThis = this;
                }
#endif
            }
        }

        bool GetIfShouldCallUpdateOnParent()
        {
            var asGue = this.Parent as GraphicalUiElement;

            if (asGue != null)
            {
                return asGue.GetIfDimensionsDependOnChildren() || asGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular;
            }
            else
            {
                return false;
            }
        }

        private static bool GetIfOneDimensionCanChangeOtherDimension(GraphicalUiElement gue)
        {
            var canOneDimensionChangeTheOtherOnChild = gue.RenderableComponent is Text ||
                    gue.WidthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                    gue.HeightUnits == DimensionUnitType.PercentageOfOtherDimension ||
                    gue.WidthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                    gue.HeightUnits == DimensionUnitType.MaintainFileAspectRatio ||


                    ((gue.ChildrenLayout == ChildrenLayout.LeftToRightStack || gue.ChildrenLayout == ChildrenLayout.TopToBottomStack) && gue.WrapsChildren);

            // If the child cannot be directly changed by a dimension, it may be indirectly changed by a dimension recursively. This can happen
            // if the child either depends on its own children's widths and heights, and one of its children can have its dimension changed.

            if (!canOneDimensionChangeTheOtherOnChild && gue.GetIfDimensionsDependOnChildren())
            {
                for (int i = 0; i < gue.Children.Count; i++)
                {
                    var uncastedChild = gue.Children[i];

                    if (uncastedChild is GraphicalUiElement child)
                    {

                        if (GetIfOneDimensionCanChangeOtherDimension(child))
                        {
                            canOneDimensionChangeTheOtherOnChild = true;
                            break;
                        }
                    }
                }
            }

            return canOneDimensionChangeTheOtherOnChild;

        }

        // Records the type of update needed when layout resumes
        private void MakeDirty(ParentUpdateType parentUpdateType, int childrenUpdateDepth, XOrY? xOrY)
        {
            if (currentDirtyState == null)
            {
                currentDirtyState = new DirtyState();

                currentDirtyState.XOrY = xOrY;
            }

            currentDirtyState.ParentUpdateType = currentDirtyState.ParentUpdateType | parentUpdateType;
            currentDirtyState.ChildrenUpdateDepth = Math.Max(
                currentDirtyState.ChildrenUpdateDepth, childrenUpdateDepth);

            // If the update is supposed to update all associations, make it null...
            if (xOrY == null)
            {
                currentDirtyState.XOrY = null;
            }
            // If neither are null and they differ, then that means update both, so set it to null
            else if (currentDirtyState.XOrY != null && currentDirtyState.XOrY != xOrY)
            {
                currentDirtyState.XOrY = null;
            }
            // It's not possible to set either X or Y here. That can only happen on initialization
            // of the currentDirtyState
        }

        private void RefreshParentRowColumnDimensionForThis()
        {
            // If it stacks, then update this row/column's dimensions given the index of this
            var indexToUpdate = this.StackedRowOrColumnIndex;

            if (indexToUpdate == -1)
            {
                return;
            }

            var parentGue = EffectiveParentGue;

            if (this.Visible)
            {

                if (parentGue.StackedRowOrColumnDimensions == null)
                {
                    parentGue.StackedRowOrColumnDimensions = new List<float>();
                }

                if (parentGue.StackedRowOrColumnDimensions.Count <= indexToUpdate)
                {
                    parentGue.StackedRowOrColumnDimensions.Add(0);
                }
                else
                {
                    if (indexToUpdate >= 0 && indexToUpdate < parentGue.StackedRowOrColumnDimensions.Count)
                    {
                        parentGue.StackedRowOrColumnDimensions[indexToUpdate] = 0;
                    }
                }
                foreach (GraphicalUiElement child in parentGue.Children)
                {
                    if (child.Visible)
                    {
                        var asIpso = child as IPositionedSizedObject;


                        if (child.StackedRowOrColumnIndex == indexToUpdate)
                        {
                            if (parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                            {
                                parentGue.StackedRowOrColumnDimensions[indexToUpdate] =
                                    System.Math.Max(parentGue.StackedRowOrColumnDimensions[indexToUpdate],
                                    child.Y + child.GetAbsoluteHeight());
                            }
                            else
                            {
                                parentGue.StackedRowOrColumnDimensions[indexToUpdate] =
                                    System.Math.Max(parentGue.StackedRowOrColumnDimensions[indexToUpdate],
                                    child.X + child.GetAbsoluteWidth());
                            }

                            // We don't need to worry about the children after this, because the siblings will get updated in order:
                            // This can (on average) make this run 2x as fast
                            if (this == child)
                            {
                                break;
                            }
                        }
                    }
                }
            }

        }

        private void UpdateChildren(int childrenUpdateDepth, ChildType childrenUpdateType, bool skipIgnoreByParentSize)
        {
            bool CanDoFullUpdate(ChildType thisChildUpdateType, GraphicalUiElement childGue)
            {

                if(skipIgnoreByParentSize && childGue.IgnoredByParentSize)
                {
                    return false;
                }

                return
                    childrenUpdateType == ChildType.All ||
                    (childrenUpdateType == ChildType.Absolute && thisChildUpdateType == ChildType.Absolute) ||
                    (childrenUpdateType == ChildType.Relative && (thisChildUpdateType == ChildType.Relative || thisChildUpdateType == ChildType.BothAbsoluteAndRelative)) ||
                    (childrenUpdateType == ChildType.StackedWrapped && thisChildUpdateType == ChildType.StackedWrapped);
            }
            if (this.mContainedObjectAsIpso == null)
            {
                for(int i = 0; i < mWhatThisContains.Count; i++)
                {
                    var child = mWhatThisContains[i];
                    // Victor Chelaru
                    // January 10, 2017
                    // I think we may not want to update any children which
                    // have parents, because they'll get updated through their
                    // parents...
                    if (child.Parent == null || child.Parent == this)
                    {
                        if (CanDoFullUpdate(child.GetChildLayoutType(this), child))
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        }
                        else
                        {
                            // only update absolute layout, and the child has some relative values, but let's see if 
                            // we can do only one axis:
                            if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.X, this), child))
                            {
                                child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.X);
                            }
                            else if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.Y, this), child))
                            {
                                child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var ipsoChild = this.Children[i];

                    if (ipsoChild is GraphicalUiElement child)
                    {
                        if (CanDoFullUpdate(child.GetChildLayoutType(this), child))
                        {
                            child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1);
                        }
                        else
                        {
                            // only update absolute layout, and the child has some relative values, but let's see if 
                            // we can do only one axis:
                            if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.X, this), child))
                            {
                                child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.X);
                            }
                            else if (CanDoFullUpdate(child.GetChildLayoutType(XOrY.Y, this), child))
                            {
                                child.UpdateLayout(ParentUpdateType.None, childrenUpdateDepth - 1, XOrY.Y);
                            }
                        }
                    }
                }
            }
        }

        private void GetParentDimensions(out float parentWidth, out float parentHeight)
        {
            parentWidth = CanvasWidth;
            parentHeight = CanvasHeight;

            // I think we want to obey the non GUE parent first if it exists, then the GUE
            //if (this.ParentGue != null && this.ParentGue.mContainedObjectAsRenderable != null)
            //{
            //    parentWidth = this.ParentGue.mContainedObjectAsIpso.Width;
            //    parentHeight = this.ParentGue.mContainedObjectAsIpso.Height;
            //}
            //else if (this.Parent != null)
            //{
            //    parentWidth = Parent.Width;
            //    parentHeight = Parent.Height;
            //}

            if (this.Parent != null)
            {
                parentWidth = Parent.Width;
                parentHeight = Parent.Height;
            }
            else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
            {
                parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
                parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;
            }

#if DEBUG
            if (float.IsPositiveInfinity(parentHeight))
            {
                throw new Exception();
            }
#endif
        }

        private void UpdateTextureCoordinatesDimensionBased()
        {
            if (mContainedObjectAsIpso is Sprite)
            {
                var sprite = mContainedObjectAsIpso as Sprite;
                var textureAddress = mTextureAddress;
                switch (textureAddress)
                {
                    case TextureAddress.DimensionsBased:
#if !SKIA
                        int left = mTextureLeft;
                        int top = mTextureTop;
                        int width = (int)(sprite.EffectiveWidth / mTextureWidthScale);
                        int height = (int)(sprite.EffectiveHeight / mTextureHeightScale);

                        sprite.SourceRectangle = new Rectangle(
                            left,
                            top,
                            width,
                            height);
                        sprite.Wrap = mWrap;
#endif
                        break;
                }
            }
#if !SKIA
            else if (mContainedObjectAsIpso is NineSlice)
            {
                var nineSlice = mContainedObjectAsIpso as NineSlice;
                var textureAddress = mTextureAddress;
                switch (textureAddress)
                {
                    case TextureAddress.DimensionsBased:
                        int left = mTextureLeft;
                        int top = mTextureTop;
                        int width = (int)(nineSlice.EffectiveWidth / mTextureWidthScale);
                        int height = (int)(nineSlice.EffectiveHeight / mTextureHeightScale);

                        nineSlice.SourceRectangle = new Rectangle(
                            left,
                            top,
                            width,
                            height);

                        break;
                }
            }
#endif

        }

        private void UpdateTextureCoordinatesNotDimensionBased()
        {
            if (mContainedObjectAsIpso is Sprite)
            {
                var sprite = mContainedObjectAsIpso as Sprite;
                var textureAddress = mTextureAddress;
                switch (textureAddress)
                {
                    case TextureAddress.EntireTexture:
#if !SKIA
                        sprite.SourceRectangle = null;
                        sprite.Wrap = false;
#endif
                        break;
                    case TextureAddress.Custom:
#if !SKIA
                        sprite.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                            mTextureLeft,
                            mTextureTop,
                            mTextureWidth,
                            mTextureHeight);
                        sprite.Wrap = mWrap;
#endif
                        break;
                    case TextureAddress.DimensionsBased:
                        // This is done *after* setting dimensions

                        break;
                }
            }
            else if (mContainedObjectAsIpso is NineSlice)
            {
#if !SKIA
                var nineSlice = mContainedObjectAsIpso as NineSlice;
                var textureAddress = mTextureAddress;
                switch (textureAddress)
                {
                    case TextureAddress.EntireTexture:
                        nineSlice.SourceRectangle = null;
                        break;
                    case TextureAddress.Custom:
                        nineSlice.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                            mTextureLeft,
                            mTextureTop,
                            mTextureWidth,
                            mTextureHeight);

                        break;
                    case TextureAddress.DimensionsBased:
                        int left = mTextureLeft;
                        int top = mTextureTop;
                        int width = (int)(nineSlice.EffectiveWidth / mTextureWidthScale);
                        int height = (int)(nineSlice.EffectiveHeight / mTextureHeightScale);

                        nineSlice.SourceRectangle = new Rectangle(
                            left,
                            top,
                            width,
                            height);

                        break;
                }
#endif
            }
        }

        private void UpdatePosition(float parentWidth, float parentHeight, XOrY? xOrY, float parentAbsoluteRotation, bool isParentFlippedHorizontally)
        {
            // First get the position of the object without considering if this object should be wrapped.
            // This call may result in the object being placed outside of its parent's bounds. In which case
            // it will be wrapped....later
            UpdatePosition(parentWidth, parentHeight, isParentFlippedHorizontally, shouldWrap: false, xOrY: xOrY, parentRotation: parentAbsoluteRotation);

            var effectiveParent = EffectiveParentGue;

            // Wrap the object if:
            bool shouldWrap =
                effectiveParent != null &&
            // * The parent stacks
                effectiveParent.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular &&

                // * And the parent wraps
                effectiveParent.WrapsChildren &&

                // * And the object is outside of parent's bounds
                ((effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack && this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight()) ||
                (effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack && this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom()));

            if (shouldWrap)
            {
                UpdatePosition(parentWidth, parentHeight, isParentFlippedHorizontally, shouldWrap, xOrY: xOrY, parentRotation: parentAbsoluteRotation);
            }
        }

        private void UpdatePosition(float parentWidth, float parentHeight, bool isParentFlippedHorizontally, bool shouldWrap, XOrY? xOrY, float parentRotation)
        {
#if DEBUG
            if (float.IsPositiveInfinity(parentHeight) || float.IsNegativeInfinity(parentHeight))
            {
                throw new ArgumentException(nameof(parentHeight));
            }
            if (float.IsPositiveInfinity(parentHeight) || float.IsNegativeInfinity(parentHeight))
            {
                throw new ArgumentException(nameof(parentHeight));
            }

#endif

            float parentOriginOffsetX;
            float parentOriginOffsetY;
            bool wasHandledX;
            bool wasHandledY;

            bool canWrap = EffectiveParentGue != null && EffectiveParentGue.WrapsChildren;

            GetParentOffsets(canWrap, shouldWrap, parentWidth, parentHeight, isParentFlippedHorizontally,
                out parentOriginOffsetX, out parentOriginOffsetY,
                out wasHandledX, out wasHandledY);


            float unitOffsetX = 0;
            float unitOffsetY = 0;

            AdjustOffsetsByUnits(parentWidth, parentHeight, isParentFlippedHorizontally, xOrY, ref unitOffsetX, ref unitOffsetY);
#if DEBUG
            if (float.IsNaN(unitOffsetX))
            {
                throw new Exception("Invalid unitOffsetX: " + unitOffsetX);
            }

            if (float.IsNaN(unitOffsetY))
            {
                throw new Exception("Invalid unitOffsetY: " + unitOffsetY);
            }
#endif



            AdjustOffsetsByOrigin(isParentFlippedHorizontally, ref unitOffsetX, ref unitOffsetY);
#if DEBUG
            if (float.IsNaN(unitOffsetX) || float.IsNaN(unitOffsetY))
            {
                throw new Exception("Invalid unit offsets");
            }
#endif


            Matrix matrix = Matrix.Identity;


            unitOffsetX += parentOriginOffsetX;
            unitOffsetY += parentOriginOffsetY;

            if (parentRotation != 0)
            {
                matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(parentRotation));

                var rotatedOffset = unitOffsetX * matrix.Right + unitOffsetY * matrix.Up;


                unitOffsetX = rotatedOffset.X;
                unitOffsetY = rotatedOffset.Y;

            }


            // See if we're explicitly updating only Y. If so, skip setting X.
            if (xOrY != XOrY.Y)
            {
                this.mContainedObjectAsIpso.X = unitOffsetX;
            }

            // See if we're explicitly updating only X. If so, skip setting Y.
            if (xOrY != XOrY.X)
            {
                this.mContainedObjectAsIpso.Y = unitOffsetY;
            }
        }

        public void GetParentOffsets(out float parentOriginOffsetX, out float parentOriginOffsetY)
        {
            float parentWidth;
            float parentHeight;
            GetParentDimensions(out parentWidth, out parentHeight);

            bool throwaway1;
            bool throwaway2;

            bool canWrap = false;
            var effectiveParent = EffectiveParentGue;
            bool isParentFlippedHorizontally = false;
            if (effectiveParent != null)
            {
                canWrap = effectiveParent.WrapsChildren;
                isParentFlippedHorizontally = effectiveParent.GetAbsoluteFlipHorizontal();
            }


            // indicating false to wrap will reset the index on this. We don't want this method
            // to modify anything so store it off and resume:
            var oldIndex = StackedRowOrColumnIndex;


            GetParentOffsets(canWrap, false, parentWidth, parentHeight, isParentFlippedHorizontally, out parentOriginOffsetX, out parentOriginOffsetY,
                out throwaway1, out throwaway2);

            StackedRowOrColumnIndex = oldIndex;

        }

        private void GetParentOffsets(bool canWrap, bool shouldWrap, float parentWidth, float parentHeight, bool isParentFlippedHorizontally, out float parentOriginOffsetX, out float parentOriginOffsetY,
            out bool wasHandledX, out bool wasHandledY)
        {
            parentOriginOffsetX = 0;
            parentOriginOffsetY = 0;

            TryAdjustOffsetsByParentLayoutType(canWrap, shouldWrap, ref parentOriginOffsetX, ref parentOriginOffsetY, out wasHandledX, out wasHandledY);

            wasHandledX = false;
            wasHandledY = false;

            AdjustParentOriginOffsetsByUnits(parentWidth, parentHeight, isParentFlippedHorizontally, ref parentOriginOffsetX, ref parentOriginOffsetY,
                ref wasHandledX, ref wasHandledY);

        }

        private void TryAdjustOffsetsByParentLayoutType(bool canWrap, bool shouldWrap, ref float unitOffsetX, ref float unitOffsetY,
            out bool wasHandledX, out bool wasHandledY)
        {

            wasHandledX = false;
            wasHandledY = false;

            if (GetIfParentStacks())
            {
                float whatToStackAfterX;
                float whatToStackAfterY;

                var whatToStackAfter = GetWhatToStackAfter(canWrap, shouldWrap, out whatToStackAfterX, out whatToStackAfterY);



                float xRelativeTo = 0;
                float yRelativeTo = 0;

                if (whatToStackAfter != null)
                {
                    var effectiveParent = this.EffectiveParentGue;
                    switch (effectiveParent.ChildrenLayout)
                    {
                        case Gum.Managers.ChildrenLayout.TopToBottomStack:

                            if (canWrap)
                            {
                                xRelativeTo = whatToStackAfterX;
                                wasHandledX = true;
                            }

                            yRelativeTo = whatToStackAfterY;
                            wasHandledY = true;


                            break;
                        case Gum.Managers.ChildrenLayout.LeftToRightStack:

                            xRelativeTo = whatToStackAfterX;
                            wasHandledX = true;

                            if (canWrap)
                            {
                                yRelativeTo = whatToStackAfterY;
                                wasHandledY = true;
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                unitOffsetX += xRelativeTo;
                unitOffsetY += yRelativeTo;
            }
        }

        private bool GetIfParentStacks()
        {
            return this.EffectiveParentGue != null && this.EffectiveParentGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular;
        }

        private GraphicalUiElement GetWhatToStackAfter(bool canWrap, bool shouldWrap, out float whatToStackAfterX, out float whatToStackAfterY)
        {
            var parentGue = this.EffectiveParentGue;

            int thisIndex = 0;

            // We used to have a static list we were populating, but that allocates memory so we
            // now use the actual list.
            System.Collections.IList siblings = null;

            if (this.Parent == null)
            {
                siblings = this.ElementGueContainingThis.mWhatThisContains;
            }
            else if (this.Parent is GraphicalUiElement)
            {
                siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
            }
            thisIndex = siblings.IndexOf(this);

            IPositionedSizedObject whatToStackAfter = null;
            whatToStackAfterX = 0;
            whatToStackAfterY = 0;

            if (parentGue.StackedRowOrColumnDimensions == null)
            {
                parentGue.StackedRowOrColumnDimensions = new List<float>();
            }

            int thisRowOrColumnIndex = 0;



            if (thisIndex > 0)
            {
                var index = thisIndex - 1;
                while (index > -1)
                {
                    if ((siblings[index] as IVisible).Visible)
                    {
                        whatToStackAfter = siblings[index] as GraphicalUiElement;
                        break;
                    }
                    index--;
                }
            }

            if (whatToStackAfter != null)
            {
                if (shouldWrap)
                {
                    // This is going to be on a new row/column. That means the following are true:
                    // * It will have a previous sibling.
                    // * It will be positioned at the start/end of its row/column
                    this.StackedRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex + 1;


                    thisRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex + 1;
                    var previousRowOrColumnIndex = thisRowOrColumnIndex - 1;
                    if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                    {
                        whatToStackAfterX = 0;

                        whatToStackAfterY = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                        }
                    }
                    else // top to bottom stack
                    {
                        whatToStackAfterY = 0;
                        whatToStackAfterX = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                        }
                    }

                }
                else
                {

                    if (whatToStackAfter != null)
                    {
                        thisRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;

                        this.StackedRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;
                        if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                        {
                            whatToStackAfterX = whatToStackAfter.X + whatToStackAfter.Width + parentGue.StackSpacing;

                            whatToStackAfterY = 0;
                            for (int i = 0; i < thisRowOrColumnIndex; i++)
                            {
                                whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                            }
                        }
                        else
                        {
                            whatToStackAfterY = whatToStackAfter.Y + whatToStackAfter.Height + parentGue.StackSpacing;
                            whatToStackAfterX = 0;
                            for (int i = 0; i < thisRowOrColumnIndex; i++)
                            {
                                whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i] + parentGue.StackSpacing;
                            }
                        }

                        // This is on the same row/column as its previous sibling
                    }
                }
            }
            else
            {
                StackedRowOrColumnIndex = 0;
            }

            return whatToStackAfter as GraphicalUiElement;
        }

        private void AdjustOffsetsByOrigin(bool isParentFlippedHorizontally, ref float unitOffsetX, ref float unitOffsetY)
        {
#if DEBUG
            if (float.IsPositiveInfinity(mRotation) || float.IsNegativeInfinity(mRotation))
            {
                throw new Exception("Rotation cannot be negative/positive infinity");
            }
#endif
            float offsetX = 0;
            float offsetY = 0;

            HorizontalAlignment effectiveXorigin = isParentFlippedHorizontally ? mXOrigin.Flip() : mXOrigin;

            if (!float.IsNaN(mContainedObjectAsIpso.Width))
            {
                if (effectiveXorigin == HorizontalAlignment.Center)
                {
                    offsetX -= mContainedObjectAsIpso.Width / 2.0f;
                }
                else if (effectiveXorigin == HorizontalAlignment.Right)
                {
                    offsetX -= mContainedObjectAsIpso.Width;
                }
            }
            // no need to handle left


            if (mYOrigin == VerticalAlignment.Center)
            {
                offsetY -= mContainedObjectAsIpso.Height / 2.0f;
            }
            else if (mYOrigin == VerticalAlignment.TextBaseline)
            {
                if (mContainedObjectAsIpso is Text text)
                {
                    offsetY += -mContainedObjectAsIpso.Height + text.DescenderHeight * text.FontScale;
                }
                else
                {
                    offsetY -= mContainedObjectAsIpso.Height;
                }
            }
            else if (mYOrigin == VerticalAlignment.Bottom)
            {
                offsetY -= mContainedObjectAsIpso.Height;
            }
            // no need to handle top

            // Adjust offsets by rotation
            if (mRotation != 0)
            {
                var rotation = isParentFlippedHorizontally ? -mRotation : mRotation;

                var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotation));

                var unrotatedX = offsetX;
                var unrotatedY = offsetY;

                offsetX = matrix.Right.X * unrotatedX + matrix.Up.X * unrotatedY;
                offsetY = matrix.Right.Y * unrotatedX + matrix.Up.Y * unrotatedY;
            }

            unitOffsetX += offsetX;
            unitOffsetY += offsetY;
        }

        private void AdjustParentOriginOffsetsByUnits(float parentWidth, float parentHeight, bool isParentFlippedHorizontally,
            ref float unitOffsetX, ref float unitOffsetY, ref bool wasHandledX, ref bool wasHandledY)
        {
            if (!wasHandledX)
            {
                var units = isParentFlippedHorizontally ? mXUnits.Flip() : mXUnits;

                if (units == GeneralUnitType.PixelsFromLarge)
                {
                    unitOffsetX = parentWidth;
                    wasHandledX = true;
                }
                else if (units == GeneralUnitType.PixelsFromMiddle)
                {
                    unitOffsetX = parentWidth / 2.0f;
                    wasHandledX = true;
                }
                else if (units == GeneralUnitType.PixelsFromSmall)
                {
                    // no need to do anything
                }
            }

            if (!wasHandledY)
            {
                if (mYUnits == GeneralUnitType.PixelsFromLarge)
                {
                    unitOffsetY = parentHeight;
                    wasHandledY = true;
                }
                else if (mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
                {
                    unitOffsetY = parentHeight / 2.0f;
                    wasHandledY = true;
                }
                else if (mYUnits == GeneralUnitType.PixelsFromBaseline)
                {
                    if (Parent is GraphicalUiElement gue && gue.RenderableComponent is Text text)
                    {
                        unitOffsetY = parentHeight - text.DescenderHeight;
                    }
                    else
                    {
                        // use the bottom as baseline:
                        unitOffsetY = parentHeight;
                    }
                    wasHandledY = true;
                }
            }
        }

        private void AdjustOffsetsByUnits(float parentWidth, float parentHeight, bool isParentFlippedHorizontally, XOrY? xOrY, ref float unitOffsetX, ref float unitOffsetY)
        {
            bool doX = xOrY == null || xOrY == XOrY.X;
            bool doY = xOrY == null || xOrY == XOrY.Y;

            if (doX)
            {
                if (mXUnits == GeneralUnitType.Percentage)
                {
                    unitOffsetX = parentWidth * mX / 100.0f;
                }
                else if (mXUnits == GeneralUnitType.PercentageOfFile)
                {
                    bool wasSet = false;

                    if (mContainedObjectAsIpso is Sprite)
                    {
                        Sprite sprite = mContainedObjectAsIpso as Sprite;

                        if (sprite.Texture != null)
                        {
                            unitOffsetX = sprite.Texture.Width * mX / 100.0f;
                        }
                    }

                    if (!wasSet)
                    {
                        unitOffsetX = 64 * mX / 100.0f;
                    }
                }
                else
                {
                    if (isParentFlippedHorizontally)
                    {
                        unitOffsetX -= mX;
                    }
                    else
                    {
                        unitOffsetX += mX;
                    }
                }
            }

            if (doY)
            {
                if (mYUnits == GeneralUnitType.Percentage)
                {
                    unitOffsetY = parentHeight * mY / 100.0f;
                }
                else if (mYUnits == GeneralUnitType.PercentageOfFile)
                {

                    bool wasSet = false;


                    if (mContainedObjectAsIpso is Sprite)
                    {
                        Sprite sprite = mContainedObjectAsIpso as Sprite;

                        if (sprite.Texture != null)
                        {
                            unitOffsetY = sprite.Texture.Height * mY / 100.0f;
                        }
                    }

                    if (!wasSet)
                    {
                        unitOffsetY = 64 * mY / 100.0f;
                    }
                }
                else if (mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
                {
                    unitOffsetY += -mY;
                }
                else
                {
                    unitOffsetY += mY;
                }
            }
        }

        private void UpdateDimensions(float parentWidth, float parentHeight, XOrY? xOrY, bool considerWrappedStacked)
        {
            // special case - if the user has set both values to depend on the other value, we don't want to have an infinite recursion so we'll just apply the width and height values as pixel values.
            // This really doesn't make much sense but...the alternative would be an object that may grow or shrink infinitely, which may cause lots of other problems:
            if ((mWidthUnit == DimensionUnitType.PercentageOfOtherDimension && mHeightUnit == DimensionUnitType.PercentageOfOtherDimension) ||
                (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio && mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
                )
            {
                mContainedObjectAsIpso.Width = mWidth;
                mContainedObjectAsIpso.Height = mHeight;
            }
            else
            {
                var doHeightFirst = mWidthUnit == DimensionUnitType.PercentageOfOtherDimension ||
                    mWidthUnit == DimensionUnitType.MaintainFileAspectRatio;

                if (doHeightFirst)
                {
                    // if width depends on height, do height first:
                    if (xOrY == null || xOrY == XOrY.Y)
                    {
                        UpdateHeight(parentHeight, considerWrappedStacked);
                    }
                    if (xOrY == null || xOrY == XOrY.X)
                    {
                        UpdateWidth(parentWidth, considerWrappedStacked);
                    }
                }
                else // either width needs to be first, or it doesn't matter so we just do width first arbitrarily
                {
                    // If height depends on width, do width first
                    if (xOrY == null || xOrY == XOrY.X)
                    {
                        UpdateWidth(parentWidth, considerWrappedStacked);
                    }
                    if (xOrY == null || xOrY == XOrY.Y)
                    {
                        UpdateHeight(parentHeight, considerWrappedStacked);
                    }
                }
            }
        }

        private void UpdateHeight(float parentHeight, bool considerWrappedStacked)
        {
            float heightToSet = mHeight;


            #region AbsoluteMultipliedByFontScale

            if (mHeightUnit == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                heightToSet *= SystemManagers.GlobalFontScale;
            }

            #endregion

            #region ScreenPixel

            else if(mHeightUnit == DimensionUnitType.ScreenPixel)
            {
                var effectiveManagers = this.EffectiveManagers;
                if (effectiveManagers != null)
                {
                    heightToSet /= effectiveManagers.Renderer.Camera.Zoom;
                }
            }

            #endregion

            #region RelativeToChildren

            if (mHeightUnit == DimensionUnitType.RelativeToChildren)
            {
                float maxHeight = 0;


                if (this.mContainedObjectAsIpso != null)
                {
                    if (mContainedObjectAsIpso is Text asText)
                    {
#if MONOGAME || XNA4
                        maxHeight = asText.WrappedTextHeight;
#endif
#if SKIA
                        var oldWidth = asText.Width;
                        if (WidthUnits == DimensionUnitType.RelativeToChildren)
                        {
                            asText.Width = float.PositiveInfinity;
                        }
                        var textBlock = asText.GetTextBlock();
                        maxHeight = textBlock.MeasuredHeight;

                        asText.Width = oldWidth;
#endif
                    }

                    for(int i = 0; i < Children.Count; i++)
                    {
                        var element = Children[i] as GraphicalUiElement;
                        var childLayout = element.GetChildLayoutType(XOrY.Y, this);
                        var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;
                        if (considerChild && element.Visible)
                        {
                            var elementHeight = element.GetRequiredParentHeight();

                            if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                            {
                                // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                if(i != 0)
                                {
                                    maxHeight += StackSpacing;
                                }
                                maxHeight += elementHeight;
                            }
                            else
                            {
                                maxHeight = System.Math.Max(maxHeight, elementHeight);
                            }
                        }
                    }
                }
                else
                {
                    for(int i = 0; i < mWhatThisContains.Count; i++)
                    {
                        var element = mWhatThisContains[i];
                        var childLayout = element.GetChildLayoutType(XOrY.Y, this);
                        var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                        if (considerChild && element.Visible)
                        {
                            var elementHeight = element.GetRequiredParentHeight();
                            if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                            {
                                // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                if (i != 0)
                                {
                                    maxHeight += StackSpacing;
                                }
                                maxHeight += elementHeight;
                            }
                            else
                            {
                                maxHeight = System.Math.Max(maxHeight, elementHeight);
                            }
                        }
                    }
                }

                heightToSet = maxHeight + mHeight;
            }

            #endregion

            #region Percentage (of parent)

            else if (mHeightUnit == DimensionUnitType.Percentage)
            {
                heightToSet = parentHeight * mHeight / 100.0f;
            }

            #endregion

            #region PercentageOfSourceFile

            else if (mHeightUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

#if SKIA
                if (mContainedObjectAsIpso is VectorSprite vectorSprite)
                {
                    if (vectorSprite.Texture != null)
                    {
                        heightToSet = vectorSprite.Texture.ViewBox.Height * mHeight / 100.0f;
                        wasSet = true;
                    }

                    if (wasSet)
                    {
                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        //if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        //{
                        //    heightToSet = sprite.EffectiveRectangle.Value.Height * mHeight / 100.0f;
                        //}
                    }
                }


#endif

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;

#if MONOGAME || XNA4
                    if (sprite.AtlasedTexture != null)
                    {
                        var atlasedTexture = sprite.AtlasedTexture;
                        heightToSet = atlasedTexture.SourceRectangle.Height * mHeight / 100.0f;
                        wasSet = true;
                    }
                    else
#endif
                    if (sprite.Texture != null)
                    {
                        heightToSet = sprite.Texture.Height * mHeight / 100.0f;
                        wasSet = true;
                    }

                    if (wasSet)
                    {
                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            heightToSet = sprite.EffectiveRectangle.Value.Height * mHeight / 100.0f;
                        }
                    }
                }

                if (!wasSet)
                {
                    heightToSet = 64 * mHeight / 100.0f;
                }
            }

            #endregion

            #region MaintainFileAspectRatio

            else if (mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                bool wasSet = false;


#if SKIA
                if (mContainedObjectAsIpso is VectorSprite vectorSprite)
                {
                    //if (sprite.AtlasedTexture != null)
                    //{
                    //    throw new NotImplementedException();
                    //}
                    //else 
                    if (vectorSprite.Texture != null)
                    {
                        var scale = GetAbsoluteWidth() / vectorSprite.Texture.ViewBox.Width;
                        heightToSet = vectorSprite.Texture.ViewBox.Height * scale * mHeight / 100.0f;
                        wasSet = true;
                    }

                    //if (wasSet)
                    //{
                    //    // If the address is dimension based, then that means texture coords depend on dimension...but we
                    //    // can't make dimension based on texture coords as that would cause a circular reference
                    //    if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                    //    {
                    //        var scale = GetAbsoluteWidth() / sprite.EffectiveRectangle.Value.Width;
                    //        heightToSet = sprite.EffectiveRectangle.Value.Height * scale * mHeight / 100.0f;
                    //    }
                    //}
                }

#endif

                if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject)
                {
                    //if(sprite.AtlasedTexture != null)
                    //{
                    //    throw new NotImplementedException();
                    //}
                    //else 
                    heightToSet = GetAbsoluteWidth() * (mHeight / 100.0f) / aspectRatioObject.AspectRatio;
                    wasSet = true;

                    if (wasSet && mContainedObjectAsIpso is Sprite sprite)
                    {
                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            var scale = GetAbsoluteWidth() / sprite.EffectiveRectangle.Value.Width;
                            heightToSet = sprite.EffectiveRectangle.Value.Height * scale * mHeight / 100.0f;
                        }
                    }
                }
                if (!wasSet)
                {
                    heightToSet = 64 * mHeight / 100.0f;
                }
            }

            #endregion

            #region RelativeToContainer (in pixels)

            else if (mHeightUnit == DimensionUnitType.RelativeToContainer)
            {
                heightToSet = parentHeight + mHeight;
            }

            #endregion

            #region PercentageOfOtherDimension

            else if (mHeightUnit == DimensionUnitType.PercentageOfOtherDimension)
            {
                heightToSet = mContainedObjectAsIpso.Width * mHeight / 100.0f;
            }

            #endregion

            #region Ratio
            else if (mHeightUnit == DimensionUnitType.Ratio)
            {
                var heightToSplit = parentHeight;

                if (mParent != null)
                {
                    for(int i = 0; i < mParent.Children.Count; i++)
                    {
                        var child = mParent.Children[i];
                        if (child != this && child is GraphicalUiElement gue)
                        {
                            if (gue.HeightUnits == DimensionUnitType.Absolute || gue.HeightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                            {
                                heightToSplit -= gue.Height;
                            }
                            else if (gue.HeightUnits == DimensionUnitType.RelativeToContainer)
                            {
                                var childAbsoluteWidth = parentHeight - gue.Height;
                                heightToSplit -= childAbsoluteWidth;
                            }
                            else if (gue.HeightUnits == DimensionUnitType.Percentage)
                            {
                                var childAbsoluteWidth = parentHeight * gue.Height;
                                heightToSplit -= childAbsoluteWidth;
                            }
                        }
                    }
                }

                float totalRatio = 0;
                if (mParent != null)
                {
                    for(int i = 0; i < mParent.Children.Count; i++)
                    {
                        var child = mParent.Children[i];
                        if (child is GraphicalUiElement gue && gue.HeightUnits == DimensionUnitType.Ratio)
                        {
                            totalRatio += gue.Height;
                        }
                    }
                }
                if (totalRatio > 0)
                {
                    heightToSet = heightToSplit * (this.Height / totalRatio);
                }
                else
                {
                    heightToSet = heightToSplit;
                }
            }
            #endregion

            mContainedObjectAsIpso.Height = heightToSet;
        }

        private void UpdateWidth(float parentWidth, bool considerWrappedStacked)
        {
            float widthToSet = mWidth;

            #region AbsoluteMultipliedByFontScale

            if (mWidthUnit == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                widthToSet *= SystemManagers.GlobalFontScale;
            }

            #endregion

            #region ScreenPixel

            else if (mWidthUnit == DimensionUnitType.ScreenPixel)
            {
                var effectiveManagers = this.EffectiveManagers;
                if (effectiveManagers != null)
                {
                    widthToSet /= effectiveManagers.Renderer.Camera.Zoom;
                }
            }

            #endregion

            #region RelativeToChildren

            else if (mWidthUnit == DimensionUnitType.RelativeToChildren)
            {
                float maxWidth = 0;

                List<GraphicalUiElement> childrenToUse = mWhatThisContains;

                if (this.mContainedObjectAsIpso != null)
                {
                    if (mContainedObjectAsIpso is Text asText)
                    {
#if SKIA
                        // This is relative to children so no wrapping:
                        var textBlock = asText.GetTextBlock(float.PositiveInfinity);

                        // Sometimes this crashes, not sure why, but I think it is some kind of internal error. We can tolerate it instead of blow up:
                        try
                        {
                            maxWidth = textBlock.MeasuredWidth;
                        }
                        catch (BadImageFormatException)
                        {
                            // not sure why but let's tolerate:
                            // https://appcenter.ms/orgs/Mtn-Green-Engineering/apps/BioCheck-2/crashes/errors/738313670/overview
                            maxWidth = 64;

                            //        // It's possible that the text has itself wrapped, but the dimensions changed.
                            //        if (asText.WrappedText.Count > 0 &&
                            //            (asText.Width != 0 && float.IsPositiveInfinity(asText.Width) == false))
                            //        {
                            //            // this could be either because it wrapped, or because the raw text
                            //            // actually has newlines. Vic says - this difference could maybe be tested
                            //            // but I'm not sure it's worth the extra code for the minor savings here, so just
                            //            // set the wrap width to positive infinity and refresh the text
                            //            asText.Width = float.PositiveInfinity;
                            //        }

                            //        maxWidth = asText.WrappedTextWidth;
                        }
#endif
#if MONOGAME || XNA4
                        // It's possible that the text has itself wrapped, but the dimensions changed.
                        if (asText.WrappedText.Count > 0 &&
                            (asText.Width != 0 && float.IsPositiveInfinity(asText.Width) == false))
                        {
                            // this could be either because it wrapped, or because the raw text
                            // actually has newlines. Vic says - this difference could maybe be tested
                            // but I'm not sure it's worth the extra code for the minor savings here, so just
                            // set the wrap width to positive infinity and refresh the text
                            asText.Width = float.PositiveInfinity;
                        }

                        maxWidth = asText.WrappedTextWidth;
#endif
                    }

                    for(int i = 0; i < this.Children.Count; i++)
                    {
                        var element = this.Children[i] as GraphicalUiElement;
                        var childLayout = element.GetChildLayoutType(XOrY.X, this);
                        var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                        if (considerChild && element.Visible)
                        {
                            var elementWidth = element.GetRequiredParentWidth();

                            if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                            {
                                // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                if (i != 0)
                                {
                                    maxWidth += StackSpacing;
                                }
                                maxWidth += elementWidth;
                            }
                            else
                            {
                                maxWidth = System.Math.Max(maxWidth, elementWidth);
                            }
                        }
                    }
                }
                else
                {
                    for(int i = 0; i < mWhatThisContains.Count; i++)
                    {
                        var element = mWhatThisContains[i];
                        var childLayout = element.GetChildLayoutType(XOrY.X, this);
                        var considerChild = (childLayout == ChildType.Absolute || (considerWrappedStacked && childLayout == ChildType.StackedWrapped)) && element.IgnoredByParentSize == false;

                        if (considerChild && element.Visible)
                        {
                            var elementWidth = element.GetRequiredParentWidth();

                            if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                            {
                                // The first item in the stack doesn't consider the stack spacing, but all subsequent ones do:
                                if (i != 0)
                                {
                                    maxWidth += StackSpacing;
                                }
                                maxWidth += elementWidth;
                            }
                            else
                            {
                                maxWidth = System.Math.Max(maxWidth, elementWidth);
                            }
                        }
                    }
                }

                widthToSet = maxWidth + mWidth;
            }
            #endregion

            #region Percentage (of parent)

            else if (mWidthUnit == DimensionUnitType.Percentage)
            {
                widthToSet = parentWidth * mWidth / 100.0f;
            }

            #endregion

            #region PercentageOfSourceFile

            else if (mWidthUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

#if SKIA
                if (mContainedObjectAsIpso is VectorSprite vectorSprite)
                {
                    if (vectorSprite.Texture != null)
                    {
                        widthToSet = vectorSprite.Texture.ViewBox.Width * mWidth / 100.0f;
                    }
                    if (wasSet)
                    {
                        //        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        //        // can't make dimension based on texture coords as that would cause a circular reference
                        //        if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        //        {
                        //            widthToSet = sprite.EffectiveRectangle.Value.Width * mWidth / 100.0f;
                        //        }
                    }
                }
#endif

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;
#if MONOGAME || XNA4

                    if (sprite.AtlasedTexture != null)
                    {
                        var atlasedTexture = sprite.AtlasedTexture;
                        widthToSet = atlasedTexture.SourceRectangle.Width * mWidth / 100.0f;
                        wasSet = true;
                    }

                    else

#endif
                    if (sprite.Texture != null)
                    {
                        widthToSet = sprite.Texture.Width * mWidth / 100.0f;
                        wasSet = true;
                    }

                    if (wasSet)
                    {
                        // If the address is dimension based, then that means texture coords depend on dimension...but we
                        // can't make dimension based on texture coords as that would cause a circular reference
                        if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            widthToSet = sprite.EffectiveRectangle.Value.Width * mWidth / 100.0f;
                        }
                    }
                }

                if (!wasSet)
                {
                    widthToSet = 64 * mWidth / 100.0f;
                }
            }

            #endregion

            #region MaintainFileAspectRatio

            else if (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                bool wasSet = false;

#if SKIA
                if (mContainedObjectAsIpso is VectorSprite vectorSprite)
                {
                    //if (sprite.AtlasedTexture != null)
                    //{
                    //    throw new NotImplementedException();
                    //}
                    //else 
                    if (vectorSprite.Texture != null)
                    {
                        var scale = GetAbsoluteHeight() / vectorSprite.Texture.ViewBox.Height;
                        widthToSet = vectorSprite.Texture.ViewBox.Width * scale * mWidth / 100.0f;
                        wasSet = true;
                    }

                    //if (wasSet)
                    //{
                    //    if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                    //    {
                    //        var scale = GetAbsoluteHeight() / sprite.EffectiveRectangle.Value.Height;
                    //        widthToSet = sprite.EffectiveRectangle.Value.Width * scale * mWidth / 100.0f;
                    //    }
                    //}
                }
#endif

                if (mContainedObjectAsIpso is IAspectRatio aspectRatioObject)
                {
                    // mWidth is a percent where 100 means maintain aspect ratio
                    widthToSet = GetAbsoluteHeight() * aspectRatioObject.AspectRatio * (mWidth / 100.0f);
                    wasSet = true;

                    if (wasSet && mContainedObjectAsIpso is Sprite sprite)
                    {
                        if (sprite.EffectiveRectangle.HasValue && mTextureAddress != TextureAddress.DimensionsBased)
                        {
                            var scale = GetAbsoluteHeight() / sprite.EffectiveRectangle.Value.Height;
                            widthToSet = sprite.EffectiveRectangle.Value.Width * scale * mWidth / 100.0f;
                        }
                    }
                }
                if (!wasSet)
                {
                    widthToSet = 64 * mWidth / 100.0f;
                }
            }

            #endregion

            #region RelativeToContainer (in pixels)

            else if (mWidthUnit == DimensionUnitType.RelativeToContainer)
            {
                widthToSet = parentWidth + mWidth;
            }

            #endregion

            #region PercentageOfOtherDimension

            else if (mWidthUnit == DimensionUnitType.PercentageOfOtherDimension)
            {
                widthToSet = mContainedObjectAsIpso.Height * mWidth / 100.0f;
            }

            #endregion

            #region Ratio

            else if (mWidthUnit == DimensionUnitType.Ratio)
            {
                var widthToSplit = parentWidth;

                if (mParent != null)
                {
                    for(int i = 0; i < mParent.Children.Count; i++)
                    {
                        var child = mParent.Children[i];
                        if (child != this && child is GraphicalUiElement gue)
                        {
                            if (gue.WidthUnits == DimensionUnitType.Absolute || gue.WidthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                            {
                                widthToSplit -= gue.Width;
                            }
                            else if (gue.WidthUnits == DimensionUnitType.RelativeToContainer)
                            {
                                var childAbsoluteWidth = parentWidth - gue.Width;
                                widthToSplit -= childAbsoluteWidth;
                            }
                            else if (gue.WidthUnits == DimensionUnitType.Percentage)
                            {
                                var childAbsoluteWidth = parentWidth * gue.Width;
                                widthToSplit -= childAbsoluteWidth;
                            }
                        }
                    }
                }

                float totalRatio = 0;
                if (mParent != null)
                {
                    for(int i = 0; i < mParent.Children.Count; i++)
                    {
                        var child = mParent.Children[i];
                        if (child is GraphicalUiElement gue && gue.WidthUnits == DimensionUnitType.Ratio)
                        {
                            totalRatio += gue.Width;
                        }
                    }
                }
                if (totalRatio > 0)
                {
                    widthToSet = widthToSplit * (this.Width / totalRatio);

                }
                else
                {
                    widthToSet = widthToSplit;
                }
            }

            #endregion

            mContainedObjectAsIpso.Width = widthToSet;
        }

        public override string ToString()
        {
            return Name;
        }

        public void SetGueValues(IVariableFinder rvf)
        {

            this.SuspendLayout();

            this.Width = rvf.GetValue<float>("Width");
            this.Height = rvf.GetValue<float>("Height");

            this.HeightUnits = rvf.GetValue<DimensionUnitType>("Height Units");
            this.WidthUnits = rvf.GetValue<DimensionUnitType>("Width Units");

            this.XOrigin = rvf.GetValue<HorizontalAlignment>("X Origin");
            this.YOrigin = rvf.GetValue<VerticalAlignment>("Y Origin");

            this.X = rvf.GetValue<float>("X");
            this.Y = rvf.GetValue<float>("Y");

            this.XUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("X Units"));
            this.YUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("Y Units"));

            this.TextureWidth = rvf.GetValue<int>("Texture Width");
            this.TextureHeight = rvf.GetValue<int>("Texture Height");
            this.TextureLeft = rvf.GetValue<int>("Texture Left");
            this.TextureTop = rvf.GetValue<int>("Texture Top");

            this.TextureWidthScale = rvf.GetValue<float>("Texture Width Scale");
            this.TextureHeightScale = rvf.GetValue<float>("Texture Height Scale");

            this.Wrap = rvf.GetValue<bool>("Wrap");

            this.TextureAddress = rvf.GetValue<TextureAddress>("Texture Address");

            this.ChildrenLayout = rvf.GetValue<ChildrenLayout>("Children Layout");
            this.WrapsChildren = rvf.GetValue<bool>("Wraps Children");
            this.ClipsChildren = rvf.GetValue<bool>("Clips Children");

            if (this.ElementSave != null)
            {
                foreach (var category in ElementSave.Categories)
                {
                    string valueOnThisState = rvf.GetValue<string>(category.Name + "State");

                    if (!string.IsNullOrEmpty(valueOnThisState))
                    {
                        this.ApplyState(valueOnThisState);
                    }
                }
            }

            this.ResumeLayout();
        }

        partial void CustomAddToManagers();

        /// <summary>
        /// Adds this as a renderable to the SystemManagers if not already added. If already added
        /// this does not perform any operations - it can be safely called multiple times.
        /// </summary>
        public virtual void AddToManagers()
        {

            AddToManagers(SystemManagers.Default, null);

        }

        /// <summary>
        /// Adds this as a renderable to the SystemManagers on the argument layer if not already added
        /// to SystemManagers. If already added
        /// this does not perform any operations - it can be safely called multiple times, but
        /// calling it multiple times will not move this to a different layer.
        /// </summary>
        public virtual void AddToManagers(SystemManagers managers, Layer layer)
        {
#if DEBUG
            if (managers == null)
            {
                throw new ArgumentNullException("managers cannot be null");
            }
#endif
            // If mManagers isn't null, it's already been added
            if (mManagers == null)
            {
                mLayer = layer;
                mManagers = managers;

#if MONOGAME || XNA4
                AddContainedRenderableToManagers(managers, layer);

                RecursivelyAddIManagedChildren(this);

                // Custom should be called before children have their Custom called
                CustomAddToManagers();

                // that means this is a screen, so the children need to be added directly to managers
                if (this.mContainedObjectAsIpso == null)
                {
                    AddChildren(managers, layer);
                }
                else
                {
                    CustomAddChildren();
                }
#endif
            }
        }

        private static void RecursivelyAddIManagedChildren(GraphicalUiElement gue)
        {
            if (gue.ElementSave != null && gue.ElementSave is ScreenSave)
            {

                //Recursively add children to the managers
                foreach (var child in gue.mWhatThisContains)
                {
                    if(child is IManagedObject managedObject)
                    {
                        managedObject.AddToManagers();
                    }
                    RecursivelyAddIManagedChildren(child);
                }
            }
            else if (gue.Children != null)
            {
                foreach (var child in gue.Children)
                {
                    if (child is IManagedObject managedObject)
                    {
                        managedObject.AddToManagers();
                    }
                    if(child is GraphicalUiElement childGue)
                    {
                        RecursivelyAddIManagedChildren(childGue);
                    }
                }
            }
        }

        private void CustomAddChildren()
        {
            foreach (var child in this.mWhatThisContains)
            {
                child.mManagers = this.mManagers;
                child.CustomAddToManagers();

                child.CustomAddChildren();
            }
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (IRenderableIpso ipso in e.NewItems)
                {
                    if (ipso.Parent != this)
                    {
                        ipso.Parent = this;

                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (IRenderableIpso ipso in e.OldItems)
                {
                    if (ipso.Parent == this)
                    {
                        ipso.Parent = null;
                    }
                }
            }
        }

        private void AddChildren(SystemManagers managers, Layer layer)
        {
            // In a simple situation we'd just loop through the
            // ContainedElements and add them to the manager.  However,
            // this means that the container will dictate the Layer that
            // its children reside on.  This is not what we want if we have
            // two children, one of which is attached to the other, and the parent
            // instance clips its children.  Therefore, we should make sure that we're
            // only adding direct children and letting instances handle their own children

            if (this.ElementSave != null && this.ElementSave is ScreenSave)
            {

                //Recursively add children to the managers
                foreach (var child in this.mWhatThisContains)
                {
                    // July 27, 2014
                    // Is this an unnecessary check?
                    // if (child is GraphicalUiElement)
                    {
                        // December 1, 2014
                        // I think that when we
                        // add a screen we should
                        // add all of the children of
                        // the screen.  There's nothing
                        // "above" that.
                        if (child.Parent == null || child.Parent == this)
                        {
                            (child as GraphicalUiElement).AddToManagers(managers, layer);
                        }
                        else
                        {
                            child.mManagers = this.mManagers;

                            child.CustomAddToManagers();

                            child.CustomAddChildren();
                        }
                    }
                }
            }
            else if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    if (child is GraphicalUiElement)
                    {
                        var childGue = child as GraphicalUiElement;

                        if (child.Parent == null || child.Parent == this)
                        {
                            childGue.AddToManagers(managers, layer);
                        }
                        else
                        {
                            childGue.mManagers = this.mManagers;

                            childGue.CustomAddToManagers();

                            childGue.CustomAddChildren();
                        }
                    }
                }

                // If a Component contains a child and that child is parented to the screen bounds then we should still add it
                foreach (var child in this.mWhatThisContains)
                {
                    var childGue = child as GraphicalUiElement;

                    // We'll check if this child has a parent, and if that parent isn't part of this component. If not, then
                    // we'll add it
                    if (child.Parent != null && this.mWhatThisContains.Contains(child.Parent) == false)
                    {
                        childGue.AddToManagers(managers, layer);
                    }
                    else
                    {
                        childGue.mManagers = this.mManagers;

                        childGue.CustomAddToManagers();

                        childGue.CustomAddChildren();
                    }
                }
            }
        }

        private void AddContainedRenderableToManagers(SystemManagers managers, Layer layer)
        {
            // This may be a Screen
            if (mContainedObjectAsIpso != null)
            {
#if MONOGAME || XNA4
                if (mContainedObjectAsIpso is Sprite)
                {
                    managers.SpriteManager.Add(mContainedObjectAsIpso as Sprite, layer);
                }
                else if (mContainedObjectAsIpso is NineSlice)
                {
                    managers.SpriteManager.Add(mContainedObjectAsIpso as NineSlice, layer);
                }
                else if (mContainedObjectAsIpso is LineRectangle)
                {
                    managers.ShapeManager.Add(mContainedObjectAsIpso as LineRectangle, layer);
                }
                else if (mContainedObjectAsIpso is SolidRectangle)
                {
                    managers.ShapeManager.Add(mContainedObjectAsIpso as SolidRectangle, layer);
                }
                else if (mContainedObjectAsIpso is Text)
                {
                    managers.TextManager.Add(mContainedObjectAsIpso as Text, layer);
                }
                else if (mContainedObjectAsIpso is LineCircle)
                {
                    managers.ShapeManager.Add(mContainedObjectAsIpso as LineCircle, layer);
                }
                else if (mContainedObjectAsIpso is LinePolygon)
                {
                    managers.ShapeManager.Add(mContainedObjectAsIpso as LinePolygon, layer);
                }
                else if (mContainedObjectAsIpso is InvisibleRenderable)
                {
                    managers.SpriteManager.Add(mContainedObjectAsIpso as InvisibleRenderable, layer);
                }

                else
                {
                    if (layer == null)
                    {
                        managers.Renderer.Layers[0].Add(mContainedObjectAsIpso);
                    }
                    else
                    {
                        layer.Add(mContainedObjectAsIpso);
                    }
                }
#endif
            }
        }

        // todo:  This should be called on instances and not just on element saves.  This is messing up animation
        public void AddExposedVariable(string variableName, string underlyingVariable)
        {
            mExposedVariables[variableName] = underlyingVariable;
        }

        public bool IsExposedVariable(string variableName)
        {
            return this.mExposedVariables.ContainsKey(variableName);
        }

        partial void CustomRemoveFromManagers();

        public void MoveToLayer(Layer layer)
        {
            var layerToRemoveFrom = mLayer;
            if (mLayer == null && mManagers != null)
            {
#if MONOGAME || XNA4
                layerToRemoveFrom = mManagers.Renderer.Layers[0];
#endif
            }

            var layerToAddTo = layer;
            if (layerToAddTo == null)
            {
#if MONOGAME || XNA4
                layerToAddTo = mManagers.Renderer.Layers[0];
#endif
            }

            bool isScreen = mContainedObjectAsIpso == null;
            if (!isScreen)
            {
                if (layerToRemoveFrom != null)
                {
                    layerToRemoveFrom.Remove(mContainedObjectAsIpso);
                }
                layerToAddTo.Add(mContainedObjectAsIpso);
            }
            else
            {
                // move all contained objects:
                foreach (var containedInstance in this.ContainedElements)
                {
                    var containedAsGue = containedInstance as GraphicalUiElement;
                    // If it's got a parent, the parent will handle it
                    if (containedAsGue.Parent == null)
                    {
                        containedAsGue.MoveToLayer(layer);
                    }
                }

            }
        }

        public void RemoveFromManagers()
        {
            foreach (var child in this.mWhatThisContains)
            {
                if (child is GraphicalUiElement)
                {
                    (child as GraphicalUiElement).RemoveFromManagers();
                }
            }

            // if mManagers is null, then it was never added to the managers
            if (mManagers != null)
            {
#if MONOGAME || XNA4
                if (mContainedObjectAsIpso is Sprite)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsIpso as Sprite);
                }
                else if (mContainedObjectAsIpso is NineSlice)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsIpso as NineSlice);
                }
                else if (mContainedObjectAsIpso is global::RenderingLibrary.Math.Geometry.LineRectangle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as global::RenderingLibrary.Math.Geometry.LineRectangle);
                }
                else if (mContainedObjectAsIpso is global::RenderingLibrary.Math.Geometry.LinePolygon)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as global::RenderingLibrary.Math.Geometry.LinePolygon);
                }
                else if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.SolidRectangle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as global::RenderingLibrary.Graphics.SolidRectangle);
                }
                else if (mContainedObjectAsIpso is Text)
                {
                    mManagers.TextManager.Remove(mContainedObjectAsIpso as Text);
                }
                else if (mContainedObjectAsIpso is LineCircle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as LineCircle);
                }
                else if (mContainedObjectAsIpso is InvisibleRenderable)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsIpso as InvisibleRenderable);
                }
                else if (mContainedObjectAsIpso != null)
                {
                    // This could be a custom visual object, so don't do anything:
                    //throw new NotImplementedException();
                    mManagers.Renderer.RemoveRenderable(mContainedObjectAsIpso);
                }


                if (mContainedObjectAsIpso is IManagedObject asManagedObject)
                {
                    asManagedObject.RemoveFromManagers();
                }
#endif

                CustomRemoveFromManagers();

                mManagers = null;
            }
        }

        public void SuspendLayout(bool recursive = false)
        {
            mIsLayoutSuspended = true;

            if (recursive)
            {
                for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                {
                    mWhatThisContains[i].SuspendLayout(true);
                }
            }
        }

        public void ResumeLayout(bool recursive = false)
        {
            mIsLayoutSuspended = false;

            if (recursive)
            {
                ResumeLayoutUpdateIfDirtyRecursive();
            }
            else
            {
                if (isFontDirty)
                {
                    if (!IsAllLayoutSuspended)
                    {
                        this.UpdateToFontValues();
                        isFontDirty = false;
                    }
                }
                if (currentDirtyState != null)
                {
                    UpdateLayout(currentDirtyState.ParentUpdateType,
                        currentDirtyState.ChildrenUpdateDepth,
                        currentDirtyState.XOrY);
                }
            }
        }

        private void ResumeLayoutUpdateIfDirtyRecursive()
        {

            mIsLayoutSuspended = false;
            UpdateFontRecursive();

            if (currentDirtyState != null)
            {
                UpdateLayout(currentDirtyState.ParentUpdateType,
                    currentDirtyState.ChildrenUpdateDepth,
                    currentDirtyState.XOrY);
            }

            if(this?.Children.Count > 0)
            {
                var count = Children.Count;
                for (int i = 0; i < count; i++)
                {
                    var asGraphicalUiElement = Children[i] as GraphicalUiElement;
                    asGraphicalUiElement.ResumeLayoutUpdateIfDirtyRecursive();
                }
            }
            else
            {
                int count = mWhatThisContains.Count;
                for (int i = 0; i < count; i++)
                {
                    mWhatThisContains[i].ResumeLayoutUpdateIfDirtyRecursive();
                }
            }
        }

        /// <summary>
        /// Searches for and returns a GraphicalUiElement in this instance by name. Returns null
        /// if not found.
        /// </summary>
        /// <param name="name">The case-sensitive name to search for.</param>
        /// <returns>The found GraphicalUiElement, or null if no match is found.</returns>
        public GraphicalUiElement GetGraphicalUiElementByName(string name)
        {
            var containsDots = ToolsUtilities.StringFunctions.ContainsNoAlloc(name, '.');
            if (containsDots)
            {
                // rare, so we can do allocation calls here:
                var indexOfDot = name.IndexOf('.');

                var prefix = name.Substring(0, indexOfDot);

                GraphicalUiElement container = null;
                for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                {
                    var item = mWhatThisContains[i];
                    if (item.name == prefix)
                    {
                        container = item;
                        break;
                    }
                }

                var suffix = name.Substring(indexOfDot + 1);

                return container?.GetGraphicalUiElementByName(suffix);
            }
            else
            {
                if (this.Children?.Count > 0 && mWhatThisContains.Count == 0)
                {
                    // This is a regular item that hasn't had its mWhatThisContains populated
                    return this.GetChildByNameRecursively(name) as GraphicalUiElement;
                }
                else
                {
                    for (int i = mWhatThisContains.Count - 1; i > -1; i--)
                    {
                        var item = mWhatThisContains[i];
                        if (item.name == name)
                        {
                            return item;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Performs a recursive search for graphical UI elements, where eacn name in the parameters
        /// is the name of a GraphicalUiElement one level deeper than the last.
        /// </summary>
        /// <param name="names">The names to search for, allowing retrieval multiple levels deep.</param>
        /// <returns>The found element, or null if no match is found.</returns>
        public GraphicalUiElement GetGraphicalUiElementByName(params string[] names)
        {
            if (names.Length > 0)
            {
                var directChild = GetGraphicalUiElementByName(names[0]);

                if (names.Length == 1)
                {
                    return directChild;
                }
                else
                {
                    var subArray = names.Skip(1).ToArray();

                    return directChild?.GetGraphicalUiElementByName(subArray);
                }
            }
            return null;
        }

        public IPositionedSizedObject GetChildByName(string name)
        {
            for(int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.Name == name)
                {
                    return child;
                }
            }
            return null;
        }

        public IRenderableIpso GetChildByNameRecursively(string name)
        {
            return GetChildByName(Children, name);
        }

        private IRenderableIpso GetChildByName(ObservableCollection<IRenderableIpso> children, string name)
        {
            foreach (var child in children)
            {
                if (child.Name == name)
                {
                    return child;
                }

                var subChild = GetChildByName(child.Children, name);
                if (subChild != null)
                {
                    return subChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets a variable on this object (such as "X") to the argument value
        /// (such as 100.0f);
        /// </summary>
        /// <param name="propertyName">The name of the variable on this object.</param>
        /// <param name="value">The value, casted to the correct type.</param>
        public void SetProperty(string propertyName, object value)
        {

            if (mExposedVariables.ContainsKey(propertyName))
            {
                string underlyingProperty = mExposedVariables[propertyName];
                int indexOfDot = underlyingProperty.IndexOf('.');
                string instanceName = underlyingProperty.Substring(0, indexOfDot);
                GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = underlyingProperty.Substring(indexOfDot + 1);

                // Children may not have been created yet
                if (containedGue != null)
                {
                    containedGue.SetProperty(variable, value);
                }
            }
            else if (ToolsUtilities.StringFunctions.ContainsNoAlloc(propertyName, '.'))
            {
                int indexOfDot = propertyName.IndexOf('.');
                string instanceName = propertyName.Substring(0, indexOfDot);
                GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = propertyName.Substring(indexOfDot + 1);

                // instances may not have been set yet
                if (containedGue != null)
                {
                    containedGue.SetProperty(variable, value);
                }


            }
            else if (TrySetValueOnThis(propertyName, value))
            {
                // success, do nothing, but it's in an else if to prevent the following else if's from evaluating
            }
            else if (this.mContainedObjectAsIpso != null)
            {
                SetPropertyOnRenderable(propertyName, value);

            }
        }

        private bool TrySetValueOnThis(string propertyName, object value)
        {
            bool toReturn = false;
            try
            {
                switch (propertyName)
                {
#if MONOGAME || XNA4

                    case nameof(Animate):
                        this.Animate = (bool)value;
                        break;
#endif
                    case "Children Layout":
                        this.ChildrenLayout = (ChildrenLayout)value;
                        toReturn = true;
                        break;
                    case "Clips Children":
                        this.ClipsChildren = (bool)value;
                        toReturn = true;
                        break;
#if MONOGAME || XNA4
                    case "CurrentChainName":
                        this.CurrentChainName = (string)value;
                        toReturn = true;
                        break;
#endif
                    case "FlipHorizontal":
                        this.FlipHorizontal = (bool)value;
                        toReturn = true;
                        break;
                    case "Height":
                        this.Height = (float)value;
                        toReturn = true;
                        break;
                    case "Height Units":
                        this.HeightUnits = (DimensionUnitType)value;
                        toReturn = true;
                        break;
                    case nameof(IgnoredByParentSize):
                        this.IgnoredByParentSize = (bool)value;
                        toReturn = true;
                        break;
                    case "Parent":
                        {
                            string valueAsString = (string)value;

                            if (!string.IsNullOrEmpty(valueAsString) && mWhatContainsThis != null)
                            {
                                var newParent = this.mWhatContainsThis.GetGraphicalUiElementByName(valueAsString);
                                if (newParent != null)
                                {
                                    Parent = newParent;
                                }
                            }
                            toReturn = true;
                        }
                        break;
                    case "Rotation":
                        this.Rotation = (float)value;
                        toReturn = true;
                        break;
                    case "StackSpacing":
                        this.StackSpacing = (float)value;
                        toReturn = true;
                        break;
                    case "Texture Left":
                        this.TextureLeft = (int)value;
                        toReturn = true;
                        break;
                    case "Texture Top":
                        this.TextureTop = (int)value;
                        toReturn = true;
                        break;
                    case "Texture Width":
                        this.TextureWidth = (int)value;
                        toReturn = true;
                        break;
                    case "Texture Height":
                        this.TextureHeight = (int)value;
                        toReturn = true;

                        break;
                    case "Texture Width Scale":
                        this.TextureWidthScale = (float)value;
                        toReturn = true;
                        break;
                    case "Texture Height Scale":
                        this.TextureHeightScale = (float)value;
                        toReturn = true;
                        break;
                    case "Texture Address":

                        this.TextureAddress = (Gum.Managers.TextureAddress)value;
                        toReturn = true;
                        break;
                    case "Visible":
                        this.Visible = (bool)value;
                        toReturn = true;
                        break;
                    case "Width":
                        this.Width = (float)value;
                        toReturn = true;
                        break;
                    case "Width Units":
                        this.WidthUnits = (DimensionUnitType)value;
                        toReturn = true;
                        break;
                    case "X":
                        this.X = (float)value;
                        toReturn = true;
                        break;
                    case "X Origin":
                        this.XOrigin = (HorizontalAlignment)value;
                        toReturn = true;
                        break;
                    case "X Units":
                        this.XUnits = UnitConverter.ConvertToGeneralUnit(value);
                        toReturn = true;
                        break;
                    case "Y":
                        this.Y = (float)value;
                        toReturn = true;
                        break;
                    case "Y Origin":
                        this.YOrigin = (VerticalAlignment)value;
                        toReturn = true;
                        break;
                    case "Y Units":

                        this.YUnits = UnitConverter.ConvertToGeneralUnit(value);
                        toReturn = true;
                        break;
                    case "Wrap":
                        this.Wrap = (bool)value;
                        toReturn = true;
                        break;
                    case "Wraps Children":
                        this.WrapsChildren = (bool)value;
                        toReturn = true;
                        break;
                }

                if (!toReturn)
                {
                    var propertyNameLength = propertyName.Length;
                    if (propertyNameLength > 5
                        && propertyName[propertyNameLength - 1] == 'e'
                        && propertyName[propertyNameLength - 2] == 't'
                        && propertyName[propertyNameLength - 3] == 'a'
                        && propertyName[propertyNameLength - 4] == 't'
                        && propertyName[propertyNameLength - 5] == 'S'
                        && value is string)
                    {
                        var valueAsString = value as string;

                        string nameWithoutState = propertyName.Substring(0, propertyName.Length - "State".Length);

                        if (string.IsNullOrEmpty(nameWithoutState))
                        {
                            // This is an uncategorized state
                            if (mStates.ContainsKey(valueAsString))
                            {
                                ApplyState(mStates[valueAsString]);
                                toReturn = true;
                            }
                        }
                        else if (mCategories.ContainsKey(nameWithoutState))
                        {

                            var category = mCategories[nameWithoutState];

                            var state = category.States.FirstOrDefault(item => item.Name == valueAsString);
                            if (state != null)
                            {
                                ApplyState(state);
                                toReturn = true;
                            }
                        }
                    }
                }
            }
            catch (InvalidCastException)
            {
                // There could be some rogue value set to the incorrect type, or maybe
                // a new type or plugin initialized the default to the wrong type. We don't
                // want to blow up if this happens
            }
            return toReturn;
        }

        private void SetPropertyOnRenderable(string propertyName, object value)
        {
            bool handled = false;

            // First try special-casing.  

            if (mContainedObjectAsIpso is Text)
            {
                handled = TrySetPropertyOnText(propertyName, value);
            }
#if MONOGAME || XNA4
            else if (mContainedObjectAsIpso is LineCircle)
            {
                handled = TrySetPropertyOnLineCircle(propertyName, value);
            }
            else if (mContainedObjectAsIpso is LineRectangle)
            {
                handled = TrySetPropertyOnLineRectangle(propertyName, value);
            }
            else if (mContainedObjectAsIpso is LinePolygon)
            {
                handled = TrySetPropertyOnLinePolygon(propertyName, value);
            }
            else if (mContainedObjectAsIpso is SolidRectangle)
            {
                var solidRect = mContainedObjectAsIpso as SolidRectangle;

                if (propertyName == "Blend")
                {
                    var valueAsGumBlend = (RenderingLibrary.Blend)value;

                    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                    solidRect.BlendState = valueAsXnaBlend;

                    handled = true;
                }
                else if (propertyName == "Alpha")
                {
                    int valueAsInt = (int)value;
                    solidRect.Alpha = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Red")
                {
                    int valueAsInt = (int)value;
                    solidRect.Red = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Green")
                {
                    int valueAsInt = (int)value;
                    solidRect.Green = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Blue")
                {
                    int valueAsInt = (int)value;
                    solidRect.Blue = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Color")
                {
                    var valueAsColor = (Color)value;
                    solidRect.Color = valueAsColor;
                    handled = true;
                }

            }
            else if (mContainedObjectAsIpso is Sprite)
            {
                var sprite = mContainedObjectAsIpso as Sprite;

                if (propertyName == "SourceFile")
                {
                    var asString = value as String;
                    handled = AssignSourceFileOnSprite(asString, sprite);

                }
                else if (propertyName == "Alpha")
                {
                    int valueAsInt = (int)value;
                    sprite.Alpha = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Red")
                {
                    int valueAsInt = (int)value;
                    sprite.Red = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Green")
                {
                    int valueAsInt = (int)value;
                    sprite.Green = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Blue")
                {
                    int valueAsInt = (int)value;
                    sprite.Blue = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Color")
                {
                    var valueAsColor = (Color)value;
                    sprite.Color = valueAsColor;
                    handled = true;
                }

                else if (propertyName == "Blend")
                {
                    var valueAsGumBlend = (RenderingLibrary.Blend)value;

                    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                    sprite.BlendState = valueAsXnaBlend;

                    handled = true;
                }
                if (!handled)
                {
                    int m = 3;
                }
            }
            else if (mContainedObjectAsIpso is NineSlice)
            {
                var nineSlice = mContainedObjectAsIpso as NineSlice;

                if (propertyName == "SourceFile")
                {
                    string valueAsString = value as string;

                    if (string.IsNullOrEmpty(valueAsString))
                    {
                        nineSlice.SetSingleTexture(null);
                    }
                    else
                    {
                        if (ToolsUtilities.FileManager.IsRelative(valueAsString))
                        {
                            valueAsString = ToolsUtilities.FileManager.RelativeDirectory + valueAsString;
                            valueAsString = ToolsUtilities.FileManager.RemoveDotDotSlash(valueAsString);
                        }

                        //check if part of atlas
                        //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
                        var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(valueAsString);
                        if (atlasedTexture != null)
                        {
                            nineSlice.LoadAtlasedTexture(valueAsString, atlasedTexture);
                        }
                        else
                        {
                            if (NineSliceExtensions.GetIfShouldUsePattern(valueAsString))
                            {
                                nineSlice.SetTexturesUsingPattern(valueAsString, SystemManagers.Default, false);
                            }
                            else
                            {
                                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

                                Microsoft.Xna.Framework.Graphics.Texture2D texture =
                                    global::RenderingLibrary.Content.LoaderManager.Self.InvalidTexture;

                                try
                                {
                                    texture =
                                        loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(valueAsString);
                                }
                                catch (Exception e)
                                {
                                    if (MissingFileBehavior == MissingFileBehavior.ThrowException)
                                    {
                                        string message = $"Error setting SourceFile on NineSlice in {this.Tag}:\n{valueAsString}";
                                        throw new System.IO.FileNotFoundException(message);
                                    }
                                    // do nothing?
                                }
                                nineSlice.SetSingleTexture(texture);

                            }
                        }
                    }
                    handled = true;
                }
                else if (propertyName == "Blend")
                {
                    var valueAsGumBlend = (RenderingLibrary.Blend)value;

                    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                    nineSlice.BlendState = valueAsXnaBlend;

                    handled = true;
                }
            }
#endif

            // If special case didn't work, let's try reflection
            if (!handled)
            {
                if (propertyName == "Parent")
                {
                    // do something
                }
                else
                {
                    System.Reflection.PropertyInfo propertyInfo = mContainedObjectAsIpso.GetType().GetProperty(propertyName);

                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {

                        if (value.GetType() != propertyInfo.PropertyType)
                        {
                            value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                        }
                        propertyInfo.SetValue(mContainedObjectAsIpso, value, null);
                    }
                }
            }
        }

#if MONOGAME || XNA4

        private bool TrySetPropertyOnLinePolygon(string propertyName, object value)
        {
            bool handled = false;


            if (propertyName == "Alpha")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LinePolygon)mContainedObjectAsIpso).Color;
                color.A = (byte)valueAsInt;

                ((LinePolygon)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LinePolygon)mContainedObjectAsIpso).Color;
                color.R = (byte)valueAsInt;

                ((LinePolygon)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LinePolygon)mContainedObjectAsIpso).Color;
                color.G = (byte)valueAsInt;

                ((LinePolygon)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LinePolygon)mContainedObjectAsIpso).Color;
                color.B = (byte)valueAsInt;

                ((LinePolygon)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Color")
            {
                var valueAsColor = (Color)value;
                ((LinePolygon)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
            }


            else if (propertyName == "Points")
            {
                var points = (List<Vector2>)value;

                ((LinePolygon)mContainedObjectAsIpso).SetPoints(points);
                handled = true;
            }

            return handled;
        }

        private bool TrySetPropertyOnLineRectangle(string propertyName, object value)
        {
            bool handled = false;

            if (propertyName == "Alpha")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineRectangle)mContainedObjectAsIpso).Color;
                color.A = (byte)valueAsInt;

                ((LineRectangle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineRectangle)mContainedObjectAsIpso).Color;
                color.R = (byte)valueAsInt;

                ((LineRectangle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineRectangle)mContainedObjectAsIpso).Color;
                color.G = (byte)valueAsInt;

                ((LineRectangle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineRectangle)mContainedObjectAsIpso).Color;
                color.B = (byte)valueAsInt;

                ((LineRectangle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }
            else if (propertyName == "Color")
            {
                var valueAsColor = (Color)value;
                ((LineRectangle)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
            }

            return handled;
        }

        private bool TrySetPropertyOnLineCircle(string propertyName, object value)
        {
            bool handled = false;

            if (propertyName == "Alpha")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineCircle)mContainedObjectAsIpso).Color;
                color.A = (byte)valueAsInt;

                ((LineCircle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineCircle)mContainedObjectAsIpso).Color;
                color.R = (byte)valueAsInt;

                ((LineCircle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineCircle)mContainedObjectAsIpso).Color;
                color.G = (byte)valueAsInt;

                ((LineCircle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;

                var color =
                    ((LineCircle)mContainedObjectAsIpso).Color;
                color.B = (byte)valueAsInt;

                ((LineCircle)mContainedObjectAsIpso).Color = color;
                handled = true;
            }

            else if (propertyName == "Color")
            {
                var valueAsColor = (Color)value;
                ((LineCircle)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
            }

            else if (propertyName == "Radius")
            {
                var valueAsFloat = (float)value;
                ((LineCircle)mContainedObjectAsIpso).Width = 2 * valueAsFloat;
                ((LineCircle)mContainedObjectAsIpso).Height = 2 * valueAsFloat;
                ((LineCircle)mContainedObjectAsIpso).Radius = valueAsFloat;
                this.Width = 2 * valueAsFloat;
                this.Height = 2 * valueAsFloat;
            }

            return handled;
        }

        public bool AssignSourceFileOnSprite(string value, Sprite sprite)
        {
            bool handled;

            var loaderManager =
                global::RenderingLibrary.Content.LoaderManager.Self;

            if (string.IsNullOrEmpty(value))
            {
                sprite.Texture = null;
                sprite.AtlasedTexture = null;

                UpdateLayout();
            }
            else if (value.EndsWith(".achx"))
            {
                if (ToolsUtilities.FileManager.IsRelative(value))
                {
                    value = ToolsUtilities.FileManager.RelativeDirectory + value;

                    value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
                }



                AnimationChainList animationChainList = null;

                if (loaderManager.CacheTextures)
                {
                    animationChainList = loaderManager.GetDisposable(value) as AnimationChainList;
                }

                if (animationChainList == null)
                {
                    var animationChainListSave = Content.AnimationChain.AnimationChainListSave.FromFile(value);
                    animationChainList = animationChainListSave.ToAnimationChainList(null);
                    if (loaderManager.CacheTextures)
                    {
                        loaderManager.AddDisposable(value, animationChainList);
                    }
                }

                this.mAnimationChains = animationChainList;

                RefreshCurrentChainToDesiredName();

                UpdateToCurrentAnimationFrame();
                handled = true;
            }
            else
            {
                if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
                {
                    value = ToolsUtilities.FileManager.RelativeDirectory + value;

                    value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
                }

                // see if an atlas exists:
                var atlasedTexture = loaderManager.TryLoadContent<AtlasedTexture>(value);

                if (atlasedTexture != null)
                {
                    sprite.AtlasedTexture = atlasedTexture;
                    UpdateLayout();
                }
                else
                {
                    // We used to check if the file exists. But internally something may
                    // alias a file. Ultimately the content loader should make that decision,
                    // not the GUE
                    try
                    {
                        sprite.Texture = loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(value);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on Sprite in {this.Tag}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        sprite.Texture = null;
                    }
                    UpdateLayout();
                }
            }
            handled = true;
            return handled;
        }
#endif
        private bool TrySetPropertyOnText(string propertyName, object value)
        {
            bool handled = false;

            void ReactToFontValueChange()
            {
                UpdateToFontValues();
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    this.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    UpdateLayout();
                }
                handled = true;
            }

            if (propertyName == "Text")
            {
                var asText = ((Text)mContainedObjectAsIpso);
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    this.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    // make it have no line wrap width before assignign the text:
                    asText.Width = 0;
                }

                asText.RawText = value as string;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    this.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    UpdateLayout();
                }
                handled = true;
            }
            else if (propertyName == "Font Scale")
            {
                ((Text)mContainedObjectAsIpso).FontScale = (float)value;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    this.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    UpdateLayout();
                }
                handled = true;

            }
            else if (propertyName == "Font")
            {
                this.Font = value as string;

                ReactToFontValueChange();
            }
#if MONOGAME || XNA4
            else if (propertyName == nameof(UseCustomFont))
            {
                this.UseCustomFont = (bool)value;
                ReactToFontValueChange();
            }

            else if (propertyName == nameof(CustomFontFile))
            {
                CustomFontFile = (string)value;
                ReactToFontValueChange();

            }
#endif
            else if (propertyName == nameof(FontSize))
            {
                FontSize = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(OutlineThickness))
            {
                OutlineThickness = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(IsItalic))
            {
                IsItalic = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(IsBold))
            {
                IsBold = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(UseFontSmoothing))
            {
                useFontSmoothing = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(Blend))
            {
#if MONOGAME || XNA4
                var valueAsGumBlend = (RenderingLibrary.Blend)value;

                var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                var text = mContainedObjectAsIpso as Text;
                text.BlendState = valueAsXnaBlend;
                handled = true;
#endif
            }
            else if (propertyName == "Alpha")
            {
#if MONOGAME || XNA4
                int valueAsInt = (int)value;
                ((Text)mContainedObjectAsIpso).Alpha = valueAsInt;
                handled = true;
#endif
            }
            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;
                ((Text)mContainedObjectAsIpso).Red = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;
                ((Text)mContainedObjectAsIpso).Green = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;
                ((Text)mContainedObjectAsIpso).Blue = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Color")
            {
#if MONOGAME || XNA4
                var valueAsColor = (Color)value;
                ((Text)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
#endif
            }

            else if (propertyName == "HorizontalAlignment")
            {
                ((Text)mContainedObjectAsIpso).HorizontalAlignment = (HorizontalAlignment)value;
                handled = true;
            }
            else if (propertyName == "VerticalAlignment")
            {
                ((Text)mContainedObjectAsIpso).VerticalAlignment = (VerticalAlignment)value;
                handled = true;
            }
            else if (propertyName == "MaxLettersToShow")
            {
#if MONOGAME || XNA4
                ((Text)mContainedObjectAsIpso).MaxLettersToShow = (int?)value;
                handled = true;
#endif
            }

            return handled;
        }

#if MONOGAME || XNA4
        bool useCustomFont;
        public bool UseCustomFont
        {
            get { return useCustomFont; }
            set { useCustomFont = value; UpdateToFontValues(); }
        }

        string customFontFile;
        public string CustomFontFile
        {
            get { return customFontFile; }
            set { customFontFile = value; UpdateToFontValues(); }
        }
#endif

        string font;
        public string Font
        {
            get { return font; }
            set { font = value; UpdateToFontValues(); }
        }

        int fontSize;
        public int FontSize
        {
            get { return fontSize; }
            set { fontSize = value; UpdateToFontValues(); }
        }

        bool isItalic;
        public bool IsItalic
        {
            get => isItalic;
            set { isItalic = value; UpdateToFontValues(); }
        }

        bool isBold;
        public bool IsBold
        {
            get => isBold;
            set { isBold = value; UpdateToFontValues(); }
        }

        // Not sure if we need to make this a public value, but we do need to store it
        // Update - yes we do need this to be public so it can be assigned in codegen:
        bool useFontSmoothing = true;
        public bool UseFontSmoothing
        {
            get { return useFontSmoothing; }
            set { useFontSmoothing = value; UpdateToFontValues(); }
        }

        int outlineThickness;
        public int OutlineThickness
        {
            get { return outlineThickness; }
            set { outlineThickness = value; UpdateToFontValues(); }
        }

        public void UpdateFontRecursive()
        {
            if (this.mContainedObjectAsIpso is Text && isFontDirty)
            {
                UpdateToFontValues();
                isFontDirty = false;
            }

            if (this.Children != null)
            {
                for(int i = 0; i < this.Children.Count; i++)
                {
                    (this.Children[i] as GraphicalUiElement).UpdateFontRecursive();
                }
            }
            else
            {
                for(int i = 0; i < this.mWhatThisContains.Count; i++)
                {
                    mWhatThisContains[i].UpdateFontRecursive();
                }
            }
        }

#if SKIA
        void UpdateToFontValues()
        {
            // BitmapFont font = null;

            var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
            var contentLoader = loaderManager.ContentLoader;

            //if(UseCustomFont)
            //{

            //}
            //else
            {
                if (/*FontSize > 0 &&*/ !string.IsNullOrEmpty(Font))
                {
                    //SKTypeface font = contentLoader.LoadContent<SKTypeface>(Font);
                    if (font != null && mContainedObjectAsIpso is Text text)
                    {
                        text.FontName = font;
                        text.FontSize = fontSize;
                    }
                }
            }
        }
#endif

#if MONOGAME || XNA4
        public void UpdateToFontValues()
        {
            if (mIsLayoutSuspended || IsAllLayoutSuspended)
            {
                isFontDirty = true;
            }
            // todo: This could make things faster, but it will require
            // extra calls in generated code, or an "UpdateAll" method
            //if (!mIsLayoutSuspended && !IsAllLayoutSuspended)
            else
            {
                BitmapFont font = null;

                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;

                if (UseCustomFont)
                {

                    if (!string.IsNullOrEmpty(CustomFontFile))
                    {
                        font = contentLoader.TryGetCachedDisposable<BitmapFont>(CustomFontFile);
                        if (font == null)
                        {
                            // so normally we would just let the content loader check if the file exists but since we're not going to
                            // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                            if (ToolsUtilities.FileManager.FileExists(CustomFontFile))
                            {
                                font = new BitmapFont(CustomFontFile, SystemManagers.Default);
                                contentLoader.AddDisposable(CustomFontFile, font);
                            }
                        }
                    }


                }
                else
                {
                    if (FontSize > 0 && !string.IsNullOrEmpty(Font))
                    {

                        string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                            FontSize,
                            Font,
                            OutlineThickness,
                            useFontSmoothing,
                            IsItalic,
                            IsBold);

                        string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, false, true);

#if ANDROID || IOS
                        fullFileName = fullFileName.ToLowerInvariant();
#endif


                        font = contentLoader.TryGetCachedDisposable<BitmapFont>(fullFileName);
                        if (font == null)
                        {
                            // so normally we would just let the content loader check if the file exists but since we're not going to
                            // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                            if (ToolsUtilities.FileManager.FileExists(fullFileName))
                            {
                                font = new BitmapFont(fullFileName, SystemManagers.Default);

                                contentLoader.AddDisposable(fullFileName, font);
                            }
                        }

#if DEBUG
                        if (font?.Textures.Any(item => item?.IsDisposed == true) == true)
                        {
                            throw new InvalidOperationException("The returned font has a disposed texture");
                        }
#endif
                    }
                }

                var text = this.mContainedObjectAsIpso as Text;

                text.BitmapFont = font ?? global::RenderingLibrary.Content.LoaderManager.Self.DefaultBitmapFont;
            }
        }

#endif

        #region IVisible Implementation

        bool IVisible.AbsoluteVisible
        {
            get
            {
                bool explicitParentVisible = true;
                if (ExplicitIVisibleParent != null)
                {
                    explicitParentVisible = ExplicitIVisibleParent.AbsoluteVisible;
                }

                return explicitParentVisible && mContainedObjectAsIVisible?.AbsoluteVisible == true;
            }
        }

        IVisible IVisible.Parent
        {
            get { return this.Parent as IVisible; }
        }

        #endregion

        public void ApplyState(string name)
        {
            if (mStates.ContainsKey(name))
            {
                var state = mStates[name];

                ApplyState(state);

            }


            // This is a little dangerous because it's ambiguous.
            // Technically categories could have same-named states.
            foreach (var category in mCategories.Values)
            {
                var foundState = category.States.FirstOrDefault(item => item.Name == name);

                if (foundState != null)
                {
                    ApplyState(foundState);
                }
            }
        }

        public void ApplyState(string categoryName, string stateName)
        {
            if (mCategories.ContainsKey(categoryName))
            {
                var category = mCategories[categoryName];

                var state = category.States.FirstOrDefault(item => item.Name == stateName);

                if (state != null)
                {
                    ApplyState(state);
                }
            }
        }

        public virtual void ApplyState(DataTypes.Variables.StateSave state)
        {
#if DEBUG
            if (state.ParentContainer == null)
            {
                throw new InvalidOperationException("State.ParentContainer is null - did you remember to initialize the state?");
            }

#endif
            if (GraphicalUiElement.IsAllLayoutSuspended == false)
            {
                this.SuspendLayout(true);
            }

            var variablesWithoutStatesOnParent =
                state.Variables.Where(item =>
                    // We can set the variable if it's not setting a state (to prevent recursive setting).                   
                    (item.IsState(state.ParentContainer) == false ||
                    // If it is setting a state we'll allow it if it's on a child.
                    !string.IsNullOrEmpty(item.SourceObject)) &&
                    item.SetsValue

                    ).ToArray();


            var parentSettingVariables =
                variablesWithoutStatesOnParent
                    .Where(item => item.GetRootName() == "Parent")
                    .OrderBy(item => GetOrderedIndexForParentVariable(item))
                    .ToArray();

            var nonParentSettingVariables =
                variablesWithoutStatesOnParent
                    .Except(parentSettingVariables)
                    // Even though we removed state-setting variables on the parent, we still allow setting
                    // states on the contained objects
                    .OrderBy(item => !item.IsState(state.ParentContainer))
                    .ToArray();

            var variablesToConsider =
                parentSettingVariables.Concat(nonParentSettingVariables)
                .ToArray();

            int variableCount = variablesToConsider.Length;
            for (int i = 0; i < variableCount; i++)
            {
                var variable = variablesToConsider[i];
                if (variable.SetsValue && variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }

            foreach (var variableList in state.VariableLists)
            {
                this.SetProperty(variableList.Name, variableList.ValueAsIList);
            }

            if (GraphicalUiElement.IsAllLayoutSuspended == false)
            {
                this.ResumeLayout(true);

            }
        }

        private int GetOrderedIndexForParentVariable(VariableSave item)
        {
            var objectName = item.SourceObject;
            for (int i = 0; i < ElementSave.Instances.Count; i++)
            {
                if (objectName == ElementSave.Instances[i].Name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void ApplyState(List<DataTypes.Variables.VariableSaveValues> variableSaveValues)
        {
            this.SuspendLayout(true);

            foreach (var variable in variableSaveValues)
            {
                if (variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }
            this.ResumeLayout(true);
        }

        public void AddCategory(DataTypes.Variables.StateSaveCategory category)
        {
            //mCategories[category.Name] = category;
            // Why call "Add"? This makes Gum crash if there are duplicate catgories...
            //mCategories.Add(category.Name, category);
            mCategories[category.Name] = category;
        }

        public void AddStates(List<DataTypes.Variables.StateSave> list)
        {
            foreach (var state in list)
            {
                // Right now this doesn't support inheritance
                // Need to investigate this....at some point:
                mStates[state.Name] = state;
            }
        }

#if MONOGAME || XNA4
        public void GetUsedTextures(List<Microsoft.Xna.Framework.Graphics.Texture2D> listToFill)
        {
            var renderable = this.mContainedObjectAsIpso;

            if (renderable is Sprite)
            {
                var texture = (renderable as Sprite).Texture;

                if (texture != null && !listToFill.Contains(texture)) listToFill.Add(texture);
            }
            else if (renderable is NineSlice)
            {
                var nineSlice = renderable as NineSlice;

                if (nineSlice.TopLeftTexture != null && !listToFill.Contains(nineSlice.TopLeftTexture)) listToFill.Add(nineSlice.TopLeftTexture);
                if (nineSlice.TopTexture != null && !listToFill.Contains(nineSlice.TopTexture)) listToFill.Add(nineSlice.TopTexture);
                if (nineSlice.TopRightTexture != null && !listToFill.Contains(nineSlice.TopRightTexture)) listToFill.Add(nineSlice.TopRightTexture);

                if (nineSlice.LeftTexture != null && !listToFill.Contains(nineSlice.LeftTexture)) listToFill.Add(nineSlice.LeftTexture);
                if (nineSlice.CenterTexture != null && !listToFill.Contains(nineSlice.CenterTexture)) listToFill.Add(nineSlice.CenterTexture);
                if (nineSlice.RightTexture != null && !listToFill.Contains(nineSlice.RightTexture)) listToFill.Add(nineSlice.RightTexture);

                if (nineSlice.BottomLeftTexture != null && !listToFill.Contains(nineSlice.BottomLeftTexture)) listToFill.Add(nineSlice.BottomLeftTexture);
                if (nineSlice.BottomTexture != null && !listToFill.Contains(nineSlice.BottomTexture)) listToFill.Add(nineSlice.BottomTexture);
                if (nineSlice.BottomRightTexture != null && !listToFill.Contains(nineSlice.BottomRightTexture)) listToFill.Add(nineSlice.BottomRightTexture);
            }
            else if (renderable is Text)
            {
                // what do we do here?  Texts could change so do we want to return them if used in a atlas?
                // This is todo for later
            }

            foreach (var item in this.mWhatThisContains)
            {
                item.GetUsedTextures(listToFill);
            }
        }
#endif

        // When interpolating between two states,
        // the code is goign to merge the values from
        // the two states to create a 3rd set of (merged)
        // values. Interpolation can happen in complex animations
        // resulting in lots of merged lists being created. This allocates
        // tons of memory. Therefore we create a static set of variable lists
        // to store the merged values. We don't know how deep the stack will go
        // (animations within animations) so we need to support a dynamically growing
        // list. The numberOfUsedInterpolationLists stores how many times this is being
        // called so it knows if it needs to add more lists.
        static List<List<Gum.DataTypes.Variables.VariableSaveValues>> listOfListsForReducingAllocInInterpolation = new List<List<Gum.DataTypes.Variables.VariableSaveValues>>();
        int numberOfUsedInterpolationLists = 0;

        public void InterpolateBetween(Gum.DataTypes.Variables.StateSave first, Gum.DataTypes.Variables.StateSave second, float interpolationValue)
        {
            if (numberOfUsedInterpolationLists >= listOfListsForReducingAllocInInterpolation.Count)
            {
                const int capacity = 20;
                var newList = new List<DataTypes.Variables.VariableSaveValues>(capacity);
                listOfListsForReducingAllocInInterpolation.Add(newList);
            }

            List<Gum.DataTypes.Variables.VariableSaveValues> values = listOfListsForReducingAllocInInterpolation[numberOfUsedInterpolationLists];
            values.Clear();
            numberOfUsedInterpolationLists++;

            Gum.DataTypes.Variables.StateSaveExtensionMethods.Merge(first, second, interpolationValue, values);

            this.ApplyState(values);
            numberOfUsedInterpolationLists--;
        }

        #region AnimationChain 
#if MONOGAME || XNA4
        public bool Animate { get; set; } = true;
        int mCurrentChainIndex;
        int mCurrentFrameIndex;
        AnimationChainList mAnimationChains;
        float mAnimationSpeed = 1;
        double mTimeIntoAnimation;
        public AnimationChain CurrentChain
        {
            get
            {
                if (mCurrentChainIndex != -1 && mAnimationChains.Count > 0 && mCurrentChainIndex < mAnimationChains.Count)
                {
                    return mAnimationChains[mCurrentChainIndex];
                }
                else
                    return null;
            }
        }

        string desiredCurrentChainName;
        public string CurrentChainName
        {
            get => CurrentChain?.Name;
            set
            {
                desiredCurrentChainName = value;
                mCurrentChainIndex = -1;
                if (mAnimationChains?.Count > 0)
                {
                    RefreshCurrentChainToDesiredName();

                    UpdateToCurrentAnimationFrame();

                }
            }
        }

        private void RefreshCurrentChainToDesiredName()
        {
            for (int i = 0; i < mAnimationChains.Count; i++)
            {
                if (mAnimationChains[i].Name == desiredCurrentChainName)
                {
                    mCurrentChainIndex = i;
                    break;
                }
            }
        }

        bool mJustChangedFrame;
        bool mJustCycled;

        /// <summary>
        /// Performs AnimationChain (.achx) animation on this and all children recurisvely.
        /// This is typically called on the top-level object (usually Screen) when Gum is running
        /// in a game.
        /// </summary>
        public void AnimateSelf()
        {
            var shouldAnimateSelf = true;
            //mJustChangedFrame = false;
            //mJustCycled = false;
            if (Animate == false || mCurrentChainIndex == -1 || mAnimationChains == null || mAnimationChains.Count == 0 || mAnimationChains[mCurrentChainIndex].Count == 0)
            {
                shouldAnimateSelf = false;
            }

            if (shouldAnimateSelf)
            {
                int frameBefore = mCurrentFrameIndex;

                // June 10, 2011
                // A negative animation speed should cause the animation to play in reverse
                //Removed the System.Math.Abs on the mAnimationSpeed variable to restore the correct behaviour.
                //double modifiedTimePassed = TimeManager.SecondDifference * System.Math.Abs(mAnimationSpeed);
                double modifiedTimePassed = TimeManager.Self.SecondDifference * mAnimationSpeed;

                mTimeIntoAnimation += modifiedTimePassed;

                AnimationChain animationChain = mAnimationChains[mCurrentChainIndex];

                mTimeIntoAnimation = MathFunctions.Loop(mTimeIntoAnimation, animationChain.TotalLength, out mJustCycled);

                UpdateFrameBasedOffOfTimeIntoAnimation();

                if (mCurrentFrameIndex != frameBefore)
                {
                    UpdateToCurrentAnimationFrame();
                    mJustChangedFrame = true;
                }
            }
            if (Children != null)
            {
                for(int i = 0; i < this.Children.Count; i++)
                {
                    var child = this.Children[i];
                    if (child is GraphicalUiElement childGue)
                    {
                        childGue.AnimateSelf();
                    }
                }
            }
            else
            {
                for(int i = 0; i < this.mWhatThisContains.Count; i++)
                {
                    var child = mWhatThisContains[i];
                    if (child is GraphicalUiElement childGue)
                    {
                        childGue.AnimateSelf();
                    }
                }
            }
        }

        void UpdateFrameBasedOffOfTimeIntoAnimation()
        {
            double timeIntoAnimation = mTimeIntoAnimation;

            if (timeIntoAnimation < 0)
            {
                throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
            }
            else if (CurrentChain != null && CurrentChain.Count > 1)
            {
                int frameIndex = 0;
                while (timeIntoAnimation >= 0)
                {
                    double frameTime = CurrentChain[frameIndex].FrameLength;

                    if (timeIntoAnimation < frameTime)
                    {
                        mCurrentFrameIndex = frameIndex;

                        break;
                    }
                    else
                    {
                        timeIntoAnimation -= frameTime;

                        frameIndex = (frameIndex + 1) % CurrentChain.Count;
                    }
                }
            }
        }

        public void UpdateToCurrentAnimationFrame()
        {
            if (mAnimationChains != null && 
                mAnimationChains.Count > mCurrentChainIndex && 
                mCurrentChainIndex != -1 &&
                mCurrentFrameIndex > -1 &&
                mAnimationChains[mCurrentChainIndex].Count > 0
                // If we switch animations, we still want it to apply right away
                // so do a frame check:
                //mCurrentFrameIndex < mAnimationChains[mCurrentChainIndex].Count
                )
            {
                var index = mCurrentFrameIndex;
                if(index >= mAnimationChains[mCurrentChainIndex].Count)
                {
                    index = 0;
                }
                var frame = mAnimationChains[mCurrentChainIndex][index];
                // Set the property so that any necessary values change:
                //				mTexture = mAnimationChains[mCurrentChainIndex][mCurrentFrameIndex].Texture;
                //this.Vertices[0].TextureCoordinate.X = frame.LeftCoordinate;
                //this.Vertices[1].TextureCoordinate.X = frame.RightCoordinate;
                //this.Vertices[2].TextureCoordinate.X = frame.RightCoordinate;
                //this.Vertices[3].TextureCoordinate.X = frame.LeftCoordinate;

                //this.Vertices[0].TextureCoordinate.Y = frame.TopCoordinate;
                //this.Vertices[1].TextureCoordinate.Y = frame.TopCoordinate;
                //this.Vertices[2].TextureCoordinate.Y = frame.BottomCoordinate;
                //this.Vertices[3].TextureCoordinate.Y = frame.BottomCoordinate;
                if (mContainedObjectAsIpso is Sprite sprite)
                {
                    sprite.Texture = frame.Texture;
                }
                this.TextureLeft = MathFunctions.RoundToInt(frame.LeftCoordinate * frame.Texture.Width);
                this.TextureWidth = MathFunctions.RoundToInt(frame.RightCoordinate * frame.Texture.Width) - this.TextureLeft;

                this.TextureTop = MathFunctions.RoundToInt(frame.TopCoordinate * frame.Texture.Height);
                this.TextureHeight = MathFunctions.RoundToInt(frame.BottomCoordinate * frame.Texture.Height) - this.TextureTop;

                this.FlipHorizontal = frame.FlipHorizontal;

                if(this.TextureAddress == TextureAddress.EntireTexture)
                {
                    this.TextureAddress = TextureAddress.Custom; // If it's not custom, then the animation chain won't apply. I think we should force this.
                }
                //frame.FlipVertical

                //if (mIgnoreAnimationChainTextureFlip == false)
                //{
                //    mFlipHorizontal = frame.FlipHorizontal;
                //    mFlipVertical = frame.FlipVertical;
                //}

                //if (mUseAnimationRelativePosition)
                //{

                //    RelativePosition.X = frame.RelativeX;
                //    RelativePosition.Y = frame.RelativeY;
                //}

                //foreach (var instruction in frame.Instructions)
                //{
                //    instruction.Execute();
                //}

                //UpdateScale();

            }
        }
#endif
        #endregion


#if SKIA
        public bool IsPointInside(float x, float y)
        {
            var asIpso = this as IRenderableIpso;

            var absoluteX = asIpso.GetAbsoluteX();
            var absoluteY = asIpso.GetAbsoluteY();

            return
                x > absoluteX &&
                y > absoluteY &&
                x < absoluteX + this.GetAbsoluteWidth() &&
                y < absoluteY + this.GetAbsoluteHeight();
        }
#endif

        #endregion
    }

    // additional interfaces, added here to make it easier to manage multiple projects.
    public interface IManagedObject
    {
        void AddToManagers();
        void RemoveFromManagers();
    }
}
