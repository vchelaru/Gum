using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

#if MONOGAME
using GumRuntime;
using RenderingLibrary.Math.Geometry;
#endif

using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;

#if SKIA
using SkiaGum;
using SkiaGum.Graphics;
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
    public class GraphicalUiElement : IRenderableIpso, IVisible
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
            public bool UpdateParent;
            public int ChildrenUpdateDepth;
            public XOrY? XOrY;
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

#if MONOGAME
        SystemManagers mManagers;
#endif

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

        bool mIsLayoutSuspended = false;

        // We need ThreadStatic in case screens are being loaded
        // in the background - we don't want to interrupt the foreground
        // layout behavior.
        [ThreadStatic]
        public static bool IsAllLayoutSuspended = false;

        Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
            new Dictionary<string, DataTypes.Variables.StateSave>();

        Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
            new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

        //Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
        //    new Dictionary<string, DataTypes.Variables.StateSave>();

        //Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
        //    new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

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

#if MONOGAME
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
                    return this.ElementGueContainingThis?.EffectiveManagers;
                }
            }
        }
#endif

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

                    // See if this has a parent that stacks children. If so, update its layout:
                    if (GetIfParentStacks())
                    {
                        this.UpdateLayout();
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


#if MONOGAME
        Microsoft.Xna.Framework.Graphics.BlendState IRenderable.BlendState
        {
            get
            {
#if DEBUG
                if(mContainedObjectAsIpso == null)
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

#if MONOGAME
        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            mContainedObjectAsIpso.Render(spriteRenderer, managers);
        }
#endif

#if SKIA
        public virtual void Render(SKCanvas canvas)
        {
            mContainedObjectAsIpso.Render(canvas);

            foreach (var child in this.Children)
            {
                child.Render(canvas);
            }
        }
#endif


#if MONOGAME
        Layer mLayer;
#endif

        #endregion

        public GeneralUnitType XUnits
        {
            get { return mXUnits; }
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
                    mYUnits = value; UpdateLayout();
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

        public ChildrenLayout ChildrenLayout
        {
            get;
            set;
        }

        public float Rotation
        {
            get
            {
                return mRotation;
            }
            set
            {
#if DEBUG
                if(float.IsNaN(value) || float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
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

                    // special case:
                    if (Parent as GraphicalUiElement == null && XUnits == GeneralUnitType.PixelsFromSmall && XOrigin == HorizontalAlignment.Left)
                    {
                        this.mContainedObjectAsIpso.X = mX;
                    }
                    else
                    {
                        UpdateLayout(true, 0);
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
                        UpdateLayout(true, 0);
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
                            originOffset.Y -= text.DescenderHeight;
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
                            originOffset.Y -= text.DescenderHeight;
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

        public event EventHandler SizeChanged;
        public event EventHandler PositionChanged;
        public event EventHandler ParentChanged;

        #endregion

        public GraphicalUiElement()
        {
            mIsLayoutSuspended = true;
            Width = 32;
            Height = 32;
            mIsLayoutSuspended = false;

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


        bool IsAllLayoutAbsolute()
        {
            return 
                //mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                //mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
                    mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted) &&
                (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
                    mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted ||
                    mYUnits == GeneralUnitType.PixelsFromBaseline);
        }

        bool IsAllLayoutAbsolute(XOrY xOrY)
        {
            if (xOrY == XOrY.X)
            {
                return //mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                    (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
                        mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted);
            }
            else // Y
            {
                return //mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent &&
                    (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
                        mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted &&
                        mYUnits == GeneralUnitType.PixelsFromBaseline);
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

        /// <summary>
        /// Sets the default state.
        /// </summary>
        /// <remarks>
        /// This function is virtual so that derived classes can override it
        /// and provide a quicker method for setting default states
        /// </remarks>
        //public virtual void SetInitialState()
        //{
        //    var elementSave = this.Tag as ElementSave;
        //    this.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        //}

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


        public void UpdateLayout()
        {
            UpdateLayout(true, true);
        }

        public bool GetIfDimensionsDependOnChildren()
        {
            // If this is a Screen, then it doesn't have a size. Screens cannot depend on children:
            //bool isScreen = ElementSave != null && ElementSave is ScreenSave;
            bool isScreen = false;
            return !isScreen &&
                (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
                this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren);
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

        bool GetIfShouldCallUpdateOnParent()
        {
            var asGue = this.Parent as GraphicalUiElement;

            if (asGue != null)
            {
                //return asGue.GetIfDimensionsDependOnChildren() || asGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular;
                return false;
            }
            else
            {
                return false;
            }
        }

        public void UpdateLayout(bool updateParent, int childrenUpdateDepth, XOrY? xOrY = null)
        {
            var isSuspended = mIsLayoutSuspended || IsAllLayoutSuspended;

            if (isSuspended)
            {
                MakeDirty(updateParent, childrenUpdateDepth, xOrY);
            }
            else
            {
                currentDirtyState = null;

                UpdateLayoutCallCount++;

                // May 15, 2014
                // This needs to be
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

                float widthBeforeLayout = 0;
                float heightBeforeLayout = 0;
                float xBeforeLayout = 0;
                float yBeforeLayout = 0;

                if (updateParent && GetIfShouldCallUpdateOnParent())
                {
                    var asGue = this.Parent as GraphicalUiElement;
                    // Just climb up one and update from there
                    asGue.UpdateLayout(true, childrenUpdateDepth + 1);
                    ChildrenUpdatingParentLayoutCalls++;
                }
                else
                {
                    float parentWidth;
                    float parentHeight;

                    GetParentDimensions(out parentWidth, out parentHeight);

                    float absoluteParentRotation = 0;

                    if (this.Parent != null)
                    {
                        absoluteParentRotation = this.Parent.GetAbsoluteRotation();
                    }
                    else if (this.ElementGueContainingThis != null && this.ElementGueContainingThis.mContainedObjectAsIpso != null)
                    {
                        parentWidth = this.ElementGueContainingThis.mContainedObjectAsIpso.Width;
                        parentHeight = this.ElementGueContainingThis.mContainedObjectAsIpso.Height;

                        absoluteParentRotation = this.ElementGueContainingThis.GetAbsoluteRotation();
                    }

                    if (mContainedObjectAsIpso != null)
                    {
                        if (false /*mContainedObjectAsIpso is LineRectangle*/)
                        {
                            //(mContainedObjectAsIpso as LineRectangle).ClipsChildren = ClipsChildren;
                        }
                        else if (mContainedObjectAsIpso is InvisibleRenderable)
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
                        if (mContainedObjectAsIpso is Sprite /*|| mContainedObjectAsIpso is NineSlice*/)
                        {
                            UpdateTextureCoordinatesNotDimensionBased();
                        }

                        if (this.WidthUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren ||
                            this.HeightUnits.GetDependencyType() == HierarchyDependencyType.DependsOnChildren)
                        {
                            UpdateChildren(childrenUpdateDepth, onlyAbsoluteLayoutChildren: true);
                        }

                        UpdateDimensions(parentWidth, parentHeight, xOrY);

                        if (mContainedObjectAsIpso is Sprite /*|| mContainedObjectAsIpso is NineSlice*/)
                        {
                            UpdateTextureCoordinatesDimensionBased();
                        }

                        // If the update is "deep" then we want to refresh the text texture.
                        // Otherwise it may have been something shallow like a reposition.
                        if (mContainedObjectAsIpso is Text && childrenUpdateDepth > 0)
                        {
                            // Only if the width or height have changed:
                            if (mContainedObjectAsIpso.Width != widthBeforeLayout ||
                                mContainedObjectAsIpso.Height != heightBeforeLayout)
                            {
                                // I think this should only happen when actually rendering:
                                //((Text)mContainedObjectAsIpso).UpdateTextureToRender();
                                var asText = mContainedObjectAsIpso as Text;

                                //asText.SetNeedsRefreshToTrue();
                                //asText.UpdatePreRenderDimensions();
                            }
                        }

                        // See the above call to UpdateTextureCoordiantes
                        // on why this is called both before and after UpdateDimensions
                        if (mContainedObjectAsIpso is Sprite)
                        {
                            UpdateTextureCoordinatesNotDimensionBased();
                        }


                        UpdatePosition(parentWidth, parentHeight, xOrY, absoluteParentRotation);

                        if (GetIfParentStacks())
                        {
                            RefreshParentRowColumnDimensionForThis();
                        }

                        mContainedObjectAsIpso.Rotation = this.GetAbsoluteRotation();
                    }

                    if (childrenUpdateDepth > 0)
                    {
                        UpdateChildren(childrenUpdateDepth);

                        var sizeDependsOnChildren = this.WidthUnits == DimensionUnitType.RelativeToChildren ||
                            this.HeightUnits == DimensionUnitType.RelativeToChildren;

                        var canOneDimensionChangeOtherDimension = false;

                        if (this.mContainedObjectAsIpso == null)
                        {
                            foreach (var child in this.mWhatThisContains)
                            {
                                canOneDimensionChangeOtherDimension = GetIfOneDimensionCanChangeOtherDimension(child);

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

                            UpdateDimensions(parentWidth, parentHeight, xOrY);

                            if (widthBeforeSecondLayout != mContainedObjectAsIpso.Width ||
                                heightBeforeSecondLayout != mContainedObjectAsIpso.Height)
                            {
                                UpdateChildren(childrenUpdateDepth);
                            }

                        }

                    }

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
                            SizeChanged?.Invoke(this, null);
                        }

                        if (xBeforeLayout != mContainedObjectAsIpso.X ||
                                yBeforeLayout != mContainedObjectAsIpso.Y)
                        {
                            PositionChanged?.Invoke(this, null);
                        }
                    }

                    //UpdateLayerScissor();

                }
            }
        }

        private void UpdateChildren(int childrenUpdateDepth, bool onlyAbsoluteLayoutChildren = false)
        {
            if (this.mContainedObjectAsIpso == null)
            {
                foreach (var child in this.mWhatThisContains)
                {
                    // Victor Chelaru
                    // January 10, 2017
                    // I think we may not want to update any children which
                    // have parents, because they'll get updated through their
                    // parents...
                    if (child.Parent == null || child.Parent == this)
                    {
                        if (child.IsAllLayoutAbsolute() || onlyAbsoluteLayoutChildren == false)
                        {
                            child.UpdateLayout(false, childrenUpdateDepth - 1);
                        }
                        else
                        {
                            // only update absolute layout, and the child has some relative values, but let's see if 
                            // we can do only one axis:
                            if (child.IsAllLayoutAbsolute(XOrY.X))
                            {
                                child.UpdateLayout(false, childrenUpdateDepth - 1, XOrY.X);
                            }
                            else if (child.IsAllLayoutAbsolute(XOrY.Y))
                            {
                                child.UpdateLayout(false, childrenUpdateDepth - 1, XOrY.Y);
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

                    if (ipsoChild is GraphicalUiElement)
                    {

                        var child = ipsoChild as GraphicalUiElement;
                        if (child.IsAllLayoutAbsolute() || onlyAbsoluteLayoutChildren == false)
                        {
                            child.UpdateLayout(false, childrenUpdateDepth - 1);
                        }
                        else
                        {
                            // only update absolute layout, and the child has some relative values, but let's see if 
                            // we can do only one axis:
                            if (child.IsAllLayoutAbsolute(XOrY.X))
                            {
                                child.UpdateLayout(false, childrenUpdateDepth - 1, XOrY.X);
                            }
                            else if (child.IsAllLayoutAbsolute(XOrY.Y))
                            {
                                child.UpdateLayout(false, childrenUpdateDepth - 1, XOrY.Y);
                            }
                        }
                    }
                }
            }
        }


        private static bool GetIfOneDimensionCanChangeOtherDimension(GraphicalUiElement gue)
        {
            var canOneDimensionChangeTheOtherOnChild = 
                gue.RenderableComponent is Text ||
                    gue.WidthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                    gue.HeightUnits == DimensionUnitType.PercentageOfOtherDimension ||
                    gue.WidthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                    gue.HeightUnits == DimensionUnitType.MaintainFileAspectRatio 
                    //||
                    //((gue.ChildrenLayout == ChildrenLayout.LeftToRightStack || gue.ChildrenLayout == ChildrenLayout.TopToBottomStack) && gue.WrapsChildren)
                    ;

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
        private void MakeDirty(bool updateParent, int childrenUpdateDepth, XOrY? xOrY)
        {
            if (currentDirtyState == null)
            {
                currentDirtyState = new DirtyState();

                currentDirtyState.XOrY = xOrY;
            }

            currentDirtyState.UpdateParent = currentDirtyState.UpdateParent || updateParent;
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
            //It's not possible to set either X or Y here. That can only happen on initialization
            // of the currentDirtyState
        }

        private void RefreshParentRowColumnDimensionForThis()
        {
            // If it stacks, then update this row/column's dimensions given the index of this
            var indexToUpdate = this.StackedRowOrColumnIndex;

            if(indexToUpdate == -1)
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
                    if(indexToUpdate >= 0  && indexToUpdate < parentGue.StackedRowOrColumnDimensions.Count)
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


        private void UpdatePosition(float parentWidth, float parentHeight, XOrY? xOrY, float parentAbsoluteRotation)
        {
            // First get the position of the object without considering if this object should be wrapped.
            // This call may result in the object being placed outside of its parent's bounds. In which case
            // it will be wrapped....later
            UpdatePosition(parentWidth, parentHeight, wrap: false, xOrY: xOrY, parentRotation: parentAbsoluteRotation);

            var effectiveParent = EffectiveParentGue;

            // Wrap the object if:
            bool shouldWrap =
                effectiveParent != null &&
            // * The parent stacks
                effectiveParent.ChildrenLayout != ChildrenLayout.Regular &&

            // * And the parent wraps
                effectiveParent.WrapsChildren &&

            // * And the object is outside of parent's bounds
                ((effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack && this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight()) ||
                (effectiveParent.ChildrenLayout == ChildrenLayout.TopToBottomStack && this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom()));

            if (shouldWrap)
            {
                UpdatePosition(parentWidth, parentHeight, wrap: true, xOrY: xOrY, parentRotation: parentAbsoluteRotation);
            }
        }

        private void UpdatePosition(float parentWidth, float parentHeight, bool wrap, XOrY? xOrY, float parentRotation)
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

            //bool canWrap = EffectiveParentGue != null && EffectiveParentGue.WrapsChildren;
            bool canWrap = false;

            GetParentOffsets(canWrap, wrap, parentWidth, parentHeight,
                out parentOriginOffsetX, out parentOriginOffsetY,
                out wasHandledX, out wasHandledY);


            float unitOffsetX = 0;
            float unitOffsetY = 0;

            AdjustOffsetsByUnits(parentWidth, parentHeight, xOrY, ref unitOffsetX, ref unitOffsetY);
#if DEBUG
            if (float.IsNaN(unitOffsetX))
            {
                throw new Exception("Invalid unitOffsetX: " + unitOffsetX);
            }

            if (float.IsNaN(unitOffsetY))
            {
                throw new Exception("Invalid unitOffsetY: " + unitOffsetY);
            }

            var unitXOffsetBeforeAdjustByOrigin = unitOffsetX;
            var unitYOffsetBeforeAdjustByOrigin = unitOffsetY;
#endif
            


            AdjustOffsetsByOrigin(ref unitOffsetX, ref unitOffsetY);
#if DEBUG
            if (float.IsNaN(unitOffsetX) || float.IsNaN(unitOffsetY))
            {
                throw new Exception("Invalid unit offsets");
            }
#endif




            unitOffsetX += parentOriginOffsetX;
            unitOffsetY += parentOriginOffsetY;

            Matrix matrix = Matrix.Identity;
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

        private void AdjustOffsetsByOrigin(ref float unitOffsetX, ref float unitOffsetY)
        {
#if DEBUG
            if(float.IsPositiveInfinity(mRotation) || float.IsNegativeInfinity(mRotation))
            {
                throw new Exception("Rotation cannot be negative/positive infinity");
            }
#endif
            float offsetX = 0;
            float offsetY = 0;

            if (mXOrigin == HorizontalAlignment.Center)
            {
                offsetX -= mContainedObjectAsIpso.Width / 2.0f;
            }
            else if (mXOrigin == HorizontalAlignment.Right)
            {
                offsetX -= mContainedObjectAsIpso.Width;
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
                var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(mRotation));

                var unrotatedX = offsetX;
                var unrotatedY = offsetY;

                offsetX = matrix.Right.X * unrotatedX + matrix.Up.X * unrotatedY;
                offsetY = matrix.Right.Y * unrotatedX + matrix.Up.Y * unrotatedY;
            }

            unitOffsetX += offsetX;
            unitOffsetY += offsetY;
        }


        private void AdjustOffsetsByUnits(float parentWidth, float parentHeight, XOrY? xOrY, ref float unitOffsetX, ref float unitOffsetY)
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
                    unitOffsetX += mX;
                }
            }

#if DEBUG
            if (float.IsNaN(unitOffsetX))
            {
                throw new Exception("Invalid unit offsets");
            }
#endif

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


        public void GetParentOffsets(out float parentOriginOffsetX, out float parentOriginOffsetY)
        {
            float parentWidth;
            float parentHeight;
            GetParentDimensions(out parentWidth, out parentHeight);

            bool throwaway1;
            bool throwaway2;

            bool wrap = false;
            bool shouldWrap = false;
            var effectiveParent = EffectiveParentGue;
            if (effectiveParent != null)
            {
                wrap = (effectiveParent as GraphicalUiElement).Wrap;

            }


            // indicating false to wrap will reset the index on this. We don't want this method
            // to modify anything so store it off and resume:
            var oldIndex = StackedRowOrColumnIndex;


            GetParentOffsets(wrap, false, parentWidth, parentHeight, out parentOriginOffsetX, out parentOriginOffsetY,
                out throwaway1, out throwaway2);

            StackedRowOrColumnIndex = oldIndex;

        }

        private void GetParentOffsets(bool canWrap, bool shouldWrap, float parentWidth, float parentHeight, out float parentOriginOffsetX, out float parentOriginOffsetY,
            out bool wasHandledX, out bool wasHandledY)
        {
            parentOriginOffsetX = 0;
            parentOriginOffsetY = 0;

            TryAdjustOffsetsByParentLayoutType(canWrap, shouldWrap, ref parentOriginOffsetX, ref parentOriginOffsetY, out wasHandledX, out wasHandledY);

            wasHandledX = false;
            wasHandledY = false;

            AdjustParentOriginOffsetsByUnits(parentWidth, parentHeight, ref parentOriginOffsetX, ref parentOriginOffsetY,
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

                IPositionedSizedObject whatToStackAfter = GetWhatToStackAfter(canWrap, shouldWrap, out whatToStackAfterX, out whatToStackAfterY);



                float xRelativeTo = 0;
                float yRelativeTo = 0;

                if (whatToStackAfter != null)
                {
                    switch (this.EffectiveParentGue.ChildrenLayout)
                    {
                        case ChildrenLayout.TopToBottomStack:

                            if (canWrap)
                            {
                                xRelativeTo = whatToStackAfterX;
                                wasHandledX = true;
                            }

                            yRelativeTo = whatToStackAfterY;
                            wasHandledY = true;


                            break;
                        case ChildrenLayout.LeftToRightStack:
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
            return this.EffectiveParentGue != null && this.EffectiveParentGue.ChildrenLayout != ChildrenLayout.Regular;
        }

        private IPositionedSizedObject GetWhatToStackAfter(bool canWrap, bool shouldWrap, out float whatToStackAfterX, out float whatToStackAfterY)
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
                        whatToStackAfter = siblings[index] as IPositionedSizedObject;
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
                    if (parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        whatToStackAfterX = 0;

                        whatToStackAfterY = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i];
                        }
                    }
                    else // top to bottom stack
                    {
                        whatToStackAfterY = 0;
                        whatToStackAfterX = 0;
                        for (int i = 0; i < thisRowOrColumnIndex; i++)
                        {
                            whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i];
                        }
                    }

                }
                else
                {

                    if (whatToStackAfter != null)
                    {
                        thisRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;
                        this.StackedRowOrColumnIndex = (whatToStackAfter as GraphicalUiElement).StackedRowOrColumnIndex;
                        if (parentGue.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                        {
                            whatToStackAfterX = whatToStackAfter.X + whatToStackAfter.Width;

                            whatToStackAfterY = 0;
                            for (int i = 0; i < thisRowOrColumnIndex; i++)
                            {
                                whatToStackAfterY += parentGue.StackedRowOrColumnDimensions[i];
                            }
                        }
                        else
                        {
                            whatToStackAfterY = whatToStackAfter.Y + whatToStackAfter.Height;
                            whatToStackAfterX = 0;
                            for (int i = 0; i < thisRowOrColumnIndex; i++)
                            {
                                whatToStackAfterX += parentGue.StackedRowOrColumnDimensions[i];
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

            return whatToStackAfter;
        }


        private void AdjustParentOriginOffsetsByUnits(float parentWidth, float parentHeight,
            ref float unitOffsetX, ref float unitOffsetY, ref bool wasHandledX, ref bool wasHandledY)
        {
            if (!wasHandledX)
            {

                if (mXUnits == GeneralUnitType.PixelsFromLarge)
                {
                    unitOffsetX = parentWidth;
                    wasHandledX = true;
                }
                else if (mXUnits == GeneralUnitType.PixelsFromMiddle)
                {
                    unitOffsetX = parentWidth / 2.0f;
                    wasHandledX = true;
                }
                else if (mXUnits == GeneralUnitType.PixelsFromSmall)
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



        private void UpdateDimensions(float parentWidth, float parentHeight, XOrY? xOrY)
        {
            // special case - if the user has set both values to depend on the other value, we don't want to have an infinite recursion so we'll just apply the width and height values as pixel values.
            // This really doesn't make much sense but...the alternative would be an object that may grow or shrink infinitely, which may cause lots of other problems:
            if (mWidthUnit == DimensionUnitType.PercentageOfOtherDimension && mHeightUnit == DimensionUnitType.PercentageOfOtherDimension)
            {
                mContainedObjectAsIpso.Width = mWidth;
                mContainedObjectAsIpso.Height = mHeight;
            }
            // see above
            if (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio && mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                mContainedObjectAsIpso.Width = mWidth;
                mContainedObjectAsIpso.Height = mHeight;
            }
            else if (mWidthUnit == DimensionUnitType.PercentageOfOtherDimension ||
                mWidthUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                // if width depends on height, do height first:
                if (xOrY == null || xOrY == XOrY.Y)
                {
                    UpdateHeight(parentHeight);
                }
                if (xOrY == null || xOrY == XOrY.X)
                {
                    UpdateWidth(parentWidth);
                }
            }
            else if (mHeightUnit == DimensionUnitType.PercentageOfOtherDimension ||
                mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                // If height depends on width, do width first
                if (xOrY == null || xOrY == XOrY.X)
                {
                    UpdateWidth(parentWidth);
                }
                if (xOrY == null || xOrY == XOrY.Y)
                {
                    UpdateHeight(parentHeight);
                }
            }
            else
            {
                // order doesn't matter, arbitrarily do width first
                if (xOrY == null || xOrY == XOrY.X)
                {
                    UpdateWidth(parentWidth);
                }
                if (xOrY == null || xOrY == XOrY.Y)
                {
                    UpdateHeight(parentHeight);
                }
            }
        }

        private void UpdateHeight(float parentHeight)
        {
            float heightToSet = mHeight;

            #region RelativeToChildren

            if (mHeightUnit == DimensionUnitType.RelativeToChildren)
            {
                float maxHeight = 0;


                if (this.mContainedObjectAsIpso != null)
                {
                    if (mContainedObjectAsIpso is Text asText)
                    {
                        var oldWidth = asText.Width;
                        if(WidthUnits == DimensionUnitType.RelativeToChildren)
                        {
                            asText.Width = float.PositiveInfinity;
                        }
                        var textBlock = asText.GetTextBlock();
                        maxHeight = textBlock.MeasuredHeight;

                        asText.Width = oldWidth;
                    }

                    foreach (GraphicalUiElement element in this.Children)
                    {
                        if (element.IsAllLayoutAbsolute(XOrY.Y) && element.Visible)
                        {
                            var elementHeight = element.GetRequiredParentHeight();

                            if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                            {
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

                    foreach (var element in this.mWhatThisContains)
                    {
                        if (element.IsAllLayoutAbsolute(XOrY.Y) && element.Visible)
                        {
                            var elementHeight = element.GetRequiredParentHeight();
                            if (this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
                            {
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

            #region Percentage

            else if (mHeightUnit == DimensionUnitType.Percentage)
            {
                heightToSet = parentHeight * mHeight / 100.0f;
            }

            #endregion

            else if (mHeightUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

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

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;

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
            else if (mHeightUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                bool wasSet = false;
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
                if (mContainedObjectAsIpso is Sprite aspectRatioObject)
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


            else if (mHeightUnit == DimensionUnitType.RelativeToContainer)
            {
                heightToSet = parentHeight + mHeight;
            }
            else if (mHeightUnit == DimensionUnitType.PercentageOfOtherDimension)
            {
                heightToSet = mContainedObjectAsIpso.Width * mHeight / 100.0f;
            }

            mContainedObjectAsIpso.Height = heightToSet;
        }

        private void UpdateWidth(float parentWidth)
        {
            float widthToSet = mWidth;

            if (mWidthUnit == DimensionUnitType.RelativeToChildren)
            {
                float maxWidth = 0;

                List<GraphicalUiElement> childrenToUse = mWhatThisContains;

                if (this.mContainedObjectAsIpso != null)
                {
                    if (mContainedObjectAsIpso is Text asText)
                    {
                        // This is relative to children so no wrapping:
                        var textBlock = asText.GetTextBlock(float.PositiveInfinity);

                        // Sometimes this crashes, not sure why, but I think it is some kind of internal error. We can tolerate it instead of blow up:
                        try
                        {
                            maxWidth = textBlock.MeasuredWidth;
                        }
                        catch(BadImageFormatException)
                        {
                            // not sure why but let's tolerate:
                            // https://appcenter.ms/orgs/Mtn-Green-Engineering/apps/BioCheck-2/crashes/errors/738313670/overview
                            maxWidth = 64;
                        }
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

                    foreach (GraphicalUiElement element in this.Children)
                    {
                        if (element.IsAllLayoutAbsolute(XOrY.X) && element.Visible)
                        {
                            var elementWidth = element.GetRequiredParentWidth();

                            if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                            {
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
                    foreach (var element in this.mWhatThisContains)
                    {
                        if (element.IsAllLayoutAbsolute(XOrY.X) && element.Visible)
                        {
                            var elementWidth = element.GetRequiredParentWidth();

                            if (this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
                            {
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
            else if (mWidthUnit == DimensionUnitType.Percentage)
            {
                widthToSet = parentWidth * mWidth / 100.0f;
            }
            else if (mWidthUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

                if (mContainedObjectAsIpso is VectorSprite vectorSprite)
                {
                    if (vectorSprite.Texture != null)
                    {
                        widthToSet = vectorSprite.Texture.ViewBox.Width * mWidth / 100.0f;
                        wasSet = true;
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

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;

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
            else if (mWidthUnit == DimensionUnitType.MaintainFileAspectRatio)
            {
                bool wasSet = false;
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

                if (mContainedObjectAsIpso is Sprite aspectRatioObject)
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

            else if (mWidthUnit == DimensionUnitType.RelativeToContainer)
            {
                widthToSet = parentWidth + mWidth;
            }
            else if (mWidthUnit == DimensionUnitType.PercentageOfOtherDimension)
            {
                widthToSet = mContainedObjectAsIpso.Height * mWidth / 100.0f;
            }

            mContainedObjectAsIpso.Width = widthToSet;
        }



        private void GetParentDimensions(out float parentWidth, out float parentHeight)
        {
            parentWidth = CanvasWidth;
            parentHeight = CanvasHeight;

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
                        //int left = mTextureLeft;
                        //int top = mTextureTop;
                        //int width = (int)(sprite.EffectiveWidth / mTextureWidthScale);
                        //int height = (int)(sprite.EffectiveHeight / mTextureHeightScale);

                        //sprite.SourceRectangle = new Rectangle(
                        //    left,
                        //    top,
                        //    width,
                        //    height);
                        //sprite.Wrap = mWrap;

                        break;
                }
            }
            //else if (mContainedObjectAsIpso is NineSlice)
            //{
            //    var nineSlice = mContainedObjectAsIpso as NineSlice;
            //    var textureAddress = mTextureAddress;
            //    switch (textureAddress)
            //    {
            //        case TextureAddress.DimensionsBased:
            //            int left = mTextureLeft;
            //            int top = mTextureTop;
            //            int width = (int)(nineSlice.EffectiveWidth / mTextureWidthScale);
            //            int height = (int)(nineSlice.EffectiveHeight / mTextureHeightScale);

            //            nineSlice.SourceRectangle = new Rectangle(
            //                left,
            //                top,
            //                width,
            //                height);

            //            break;
            //    }
            //}


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
                        //sprite.SourceRectangle = null;
                        //sprite.Wrap = false;
                        break;
                    case TextureAddress.Custom:
                        //sprite.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                        //    mTextureLeft,
                        //    mTextureTop,
                        //    mTextureWidth,
                        //    mTextureHeight);
                        //sprite.Wrap = mWrap;

                        break;
                    case TextureAddress.DimensionsBased:
                        // This is done *after* setting dimensions

                        break;
                }
            }
            //else if (mContainedObjectAsIpso is NineSlice)
            //{
            //    var nineSlice = mContainedObjectAsIpso as NineSlice;
            //    var textureAddress = mTextureAddress;
            //    switch (textureAddress)
            //    {
            //        case TextureAddress.EntireTexture:
            //            nineSlice.SourceRectangle = null;
            //            break;
            //        case TextureAddress.Custom:
            //            nineSlice.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(
            //                mTextureLeft,
            //                mTextureTop,
            //                mTextureWidth,
            //                mTextureHeight);

            //            break;
            //        case TextureAddress.DimensionsBased:
            //            int left = mTextureLeft;
            //            int top = mTextureTop;
            //            int width = (int)(nineSlice.EffectiveWidth / mTextureWidthScale);
            //            int height = (int)(nineSlice.EffectiveHeight / mTextureHeightScale);

            //            nineSlice.SourceRectangle = new Rectangle(
            //                left,
            //                top,
            //                width,
            //                height);

            //            break;
            //    }
            //}
        }

        public void SetProperty(string propertyName, object value)
        {

            //if (mExposedVariables.ContainsKey(propertyName))
            //{
            //    string underlyingProperty = mExposedVariables[propertyName];
            //    int indexOfDot = underlyingProperty.IndexOf('.');
            //    string instanceName = underlyingProperty.Substring(0, indexOfDot);
            //    GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
            //    string variable = underlyingProperty.Substring(indexOfDot + 1);

            //    // Children may not have been created yet
            //    if (containedGue != null)
            //    {
            //        containedGue.SetProperty(variable, value);
            //    }
            //}
            //else if (ToolsUtilities.StringFunctions.ContainsNoAlloc(propertyName, '.'))
            //{
            //    int indexOfDot = propertyName.IndexOf('.');
            //    string instanceName = propertyName.Substring(0, indexOfDot);
            //    GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
            //    string variable = propertyName.Substring(indexOfDot + 1);

            //    // instances may not have been set yet
            //    if (containedGue != null)
            //    {
            //        containedGue.SetProperty(variable, value);
            //    }


            //}
            //else 
            if (TrySetValueOnThis(propertyName, value))
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
            //else if (mContainedObjectAsIpso is LineCircle)
            //{
            //    handled = TrySetPropertyOnLineCircle(propertyName, value);
            //}
            //else if (mContainedObjectAsIpso is LineRectangle)
            //{
            //    handled = TrySetPropertyOnLineRectangle(propertyName, value);
            //}
            //else if (mContainedObjectAsIpso is LinePolygon)
            //{
            //    handled = TrySetPropertyOnLinePolygon(propertyName, value);
            //}
            //else if (mContainedObjectAsIpso is SolidRectangle)
            //{
            //    var solidRect = mContainedObjectAsIpso as SolidRectangle;

            //    if (propertyName == "Blend")
            //    {
            //        var valueAsGumBlend = (RenderingLibrary.Blend)value;

            //        var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            //        solidRect.BlendState = valueAsXnaBlend;

            //        handled = true;
            //    }
            //    else if (propertyName == "Alpha")
            //    {
            //        int valueAsInt = (int)value;
            //        solidRect.Alpha = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Red")
            //    {
            //        int valueAsInt = (int)value;
            //        solidRect.Red = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Green")
            //    {
            //        int valueAsInt = (int)value;
            //        solidRect.Green = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Blue")
            //    {
            //        int valueAsInt = (int)value;
            //        solidRect.Blue = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Color")
            //    {
            //        var valueAsColor = (Color)value;
            //        solidRect.Color = valueAsColor;
            //        handled = true;
            //    }

            //}
            //else if (mContainedObjectAsIpso is Sprite)
            //{
            //    var sprite = mContainedObjectAsIpso as Sprite;

            //    if (propertyName == "SourceFile")
            //    {
            //        handled = AssignSourceFileOnSprite(value, sprite);
            //    }
            //    else if (propertyName == "Alpha")
            //    {
            //        int valueAsInt = (int)value;
            //        sprite.Alpha = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Red")
            //    {
            //        int valueAsInt = (int)value;
            //        sprite.Red = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Green")
            //    {
            //        int valueAsInt = (int)value;
            //        sprite.Green = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Blue")
            //    {
            //        int valueAsInt = (int)value;
            //        sprite.Blue = valueAsInt;
            //        handled = true;
            //    }
            //    else if (propertyName == "Color")
            //    {
            //        var valueAsColor = (Color)value;
            //        sprite.Color = valueAsColor;
            //        handled = true;
            //    }

            //    else if (propertyName == "Blend")
            //    {
            //        var valueAsGumBlend = (RenderingLibrary.Blend)value;

            //        var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            //        sprite.BlendState = valueAsXnaBlend;

            //        handled = true;
            //    }
            //    if (!handled)
            //    {
            //        int m = 3;
            //    }
            //}
            //else if (mContainedObjectAsIpso is NineSlice)
            //{
            //    var nineSlice = mContainedObjectAsIpso as NineSlice;

            //    if (propertyName == "SourceFile")
            //    {
            //        string valueAsString = value as string;

            //        if (string.IsNullOrEmpty(valueAsString))
            //        {
            //            nineSlice.SetSingleTexture(null);
            //        }
            //        else
            //        {
            //            if (ToolsUtilities.FileManager.IsRelative(valueAsString))
            //            {
            //                valueAsString = ToolsUtilities.FileManager.RelativeDirectory + valueAsString;
            //                valueAsString = ToolsUtilities.FileManager.RemoveDotDotSlash(valueAsString);
            //            }

            //            //check if part of atlas
            //            //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
            //            var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(valueAsString);
            //            if (atlasedTexture != null)
            //            {
            //                nineSlice.LoadAtlasedTexture(valueAsString, atlasedTexture);
            //            }
            //            else
            //            {
            //                if (NineSlice.GetIfShouldUsePattern(valueAsString))
            //                {
            //                    nineSlice.SetTexturesUsingPattern(valueAsString, SystemManagers.Default, false);
            //                }
            //                else
            //                {
            //                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

            //                    Microsoft.Xna.Framework.Graphics.Texture2D texture =
            //                        global::RenderingLibrary.Content.LoaderManager.Self.InvalidTexture;

            //                    try
            //                    {
            //                        texture =
            //                            loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(valueAsString);
            //                    }
            //                    catch (Exception e)
            //                    {
            //                        if (MissingFileBehavior == MissingFileBehavior.ThrowException)
            //                        {
            //                            string message = $"Error setting SourceFile on NineSlice in {this.Tag}:\n{valueAsString}";
            //                            throw new System.IO.FileNotFoundException(message);
            //                        }
            //                        // do nothing?
            //                    }
            //                    nineSlice.SetSingleTexture(texture);

            //                }
            //            }
            //        }
            //        handled = true;
            //    }
            //    else if (propertyName == "Blend")
            //    {
            //        var valueAsGumBlend = (RenderingLibrary.Blend)value;

            //        var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            //        nineSlice.BlendState = valueAsXnaBlend;

            //        handled = true;
            //    }
            //}

            //// If special case didn't work, let's try reflection
            //if (!handled)
            //{
            //    if (propertyName == "Parent")
            //    {
            //        // do something
            //    }
            //    else
            //    {
            //        System.Reflection.PropertyInfo propertyInfo = mContainedObjectAsIpso.GetType().GetProperty(propertyName);

            //        if (propertyInfo != null && propertyInfo.CanWrite)
            //        {

            //            if (value.GetType() != propertyInfo.PropertyType)
            //            {
            //                value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
            //            }
            //            propertyInfo.SetValue(mContainedObjectAsIpso, value, null);
            //        }
            //    }
            //}
        }

        private bool TrySetPropertyOnText(string propertyName, object value)
        {
            bool handled = false;

            if (propertyName == "Text")
            {
                var asText = ((Text)mContainedObjectAsIpso);
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
                {
                    // make it have no line wrap width before assignign the text:
                    asText.Width = 0;
                }

                asText.RawText = value as string;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
                {
                    UpdateLayout();
                }
                handled = true;
            }
            //else if (propertyName == "Font Scale")
            //{
            //    ((Text)mContainedObjectAsIpso).FontScale = (float)value;
            //    // we want to update if the text's size is based on its "children" (the letters it contains)
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;

            //}
            else if (propertyName == "Font")
            {
                this.Font = value as string;

                UpdateToFontValues();
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
                {
                    UpdateLayout();
                }
                handled = true;
            }
            //else if (propertyName == "UseCustomFont")
            //{
            //    this.UseCustomFont = (bool)value;
            //    UpdateToFontValues();
            //    // we want to update if the text's size is based on its "children" (the letters it contains)
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;
            //}

            //else if (propertyName == "CustomFontFile")
            //{
            //    CustomFontFile = (string)value;
            //    UpdateToFontValues();
            //    // we want to update if the text's size is based on its "children" (the letters it contains)
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;
            //}
            //else if (propertyName == "FontSize")
            //{
            //    FontSize = (int)value;
            //    UpdateToFontValues();
            //    // we want to update if the text's size is based on its "children" (the letters it contains)
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;
            //}
            //else if (propertyName == "OutlineThickness")
            //{
            //    OutlineThickness = (int)value;
            //    UpdateToFontValues();
            //    // we want to update if the text's size is based on its "children" (the letters it contains)
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;
            //}
            //else if (propertyName == "UseFontSmoothing")
            //{
            //    useFontSmoothing = (bool)value;
            //    UpdateToFontValues();
            //    if (this.WidthUnits == DimensionUnitType.RelativeToChildren)
            //    {
            //        UpdateLayout();
            //    }
            //    handled = true;
            //}
            //else if (propertyName == "Blend")
            //{
            //    var valueAsGumBlend = (RenderingLibrary.Blend)value;

            //    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            //    var text = mContainedObjectAsIpso as Text;
            //    text.BlendState = valueAsXnaBlend;
            //    handled = true;
            //}
            //else if (propertyName == "Alpha")
            //{
            //    int valueAsInt = (int)value;
            //    ((Text)mContainedObjectAsIpso).Alpha = valueAsInt;
            //    handled = true;
            //}
            //else if (propertyName == "Red")
            //{
            //    int valueAsInt = (int)value;
            //    ((Text)mContainedObjectAsIpso).Red = valueAsInt;
            //    handled = true;
            //}
            //else if (propertyName == "Green")
            //{
            //    int valueAsInt = (int)value;
            //    ((Text)mContainedObjectAsIpso).Green = valueAsInt;
            //    handled = true;
            //}
            //else if (propertyName == "Blue")
            //{
            //    int valueAsInt = (int)value;
            //    ((Text)mContainedObjectAsIpso).Blue = valueAsInt;
            //    handled = true;
            //}
            //else if (propertyName == "Color")
            //{
            //    var valueAsColor = (Color)value;
            //    ((Text)mContainedObjectAsIpso).Color = valueAsColor;
            //    handled = true;
            //}

            //else if (propertyName == "HorizontalAlignment")
            //{
            //    ((Text)mContainedObjectAsIpso).HorizontalAlignment = (HorizontalAlignment)value;
            //    handled = true;
            //}
            //else if (propertyName == "VerticalAlignment")
            //{
            //    ((Text)mContainedObjectAsIpso).VerticalAlignment = (VerticalAlignment)value;
            //    handled = true;
            //}
            //else if (propertyName == "MaxLettersToShow")
            //{
            //    ((Text)mContainedObjectAsIpso).MaxLettersToShow = (int)value;
            //    handled = true;
            //}

            return handled;
        }

        string font;
        public string Font
        {
            get { return font; }
            set { font = value; UpdateToFontValues(); }
        }

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
                    if(font != null && mContainedObjectAsIpso is Text text)
                    {
                        text.FontName = font;
                    }
                }
            }
        }

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

                return explicitParentVisible && mContainedObjectAsIVisible.AbsoluteVisible;
            }
        }

        IVisible IVisible.Parent
        {
            get { return this.Parent as IVisible; }
        }

        #endregion
    }
}
