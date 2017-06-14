using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Converters;
using GumDataTypes.Variables;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math.Geometry;
using Gum.RenderingLibrary;
using System.Reflection;

namespace Gum.Wireframe
{


    public partial class GraphicalUiElement : IRenderableIpso, IVisible
    {


        #region Fields


        public static int UpdateLayoutCallCount;
        public static int ChildrenUpdatingParentLayoutCalls;


        public static bool ShowLineRectangles = true;

        // to save on casting:
        IRenderableIpso mContainedObjectAsIpso;
        IVisible mContainedObjectAsIVisible;

        GraphicalUiElement mWhatContainsThis;

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
        float mWidth;
        float mHeight;
        float mRotation;

        static float mCanvasWidth = 800;
        static float mCanvasHeight = 600;

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



        #endregion

        #region Properties

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
                    return this.ParentGue?.EffectiveManagers;
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
                if (mContainedObjectAsIVisible != null)
                {
                    mContainedObjectAsIVisible.Visible = value;
                }
            }
        }

        public static float CanvasWidth
        {
            get { return mCanvasWidth; }
            set { mCanvasWidth = value; }
        }

        public static float CanvasHeight
        {
            get { return mCanvasHeight; }
            set { mCanvasHeight = value; }
        }


        #region IPSO properties

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
                mContainedObjectAsIpso.X = value;
            }
        }

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
                mContainedObjectAsIpso.Y = value;
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
                ((IPositionedSizedObject)mContainedObjectAsIpso).Z = value;
            }
        }


        #region IRenderable properties


        Microsoft.Xna.Framework.Graphics.BlendState IRenderable.BlendState
        {
            get { return mContainedObjectAsIpso.BlendState; }
        }

        bool IRenderable.Wrap
        {
            get { return mContainedObjectAsIpso.Wrap; }
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            mContainedObjectAsIpso.Render(spriteRenderer, managers);
        }

        /// <summary>
        /// Used for clipping.
        /// </summary>
        SortableLayer mSortableLayer;

        Layer mLayer;

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
                if (mRotation != value)
                {
                    mRotation = value;

                    UpdateLayout(true, 0);
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
                if (mX != value)
                {
                    mX = value;

                    UpdateLayout(true, 0);
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
                if (mY != value)
                {
                    mY = value;

                    UpdateLayout(true, 0);
                }
            }
        }

        public float Width
        {
            get { return mWidth; }
            set
            {
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
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Add(this);
                    }
                    UpdateLayout();

                }
            }
        }

        /// <summary>
        /// The ScreenSave or Component which contains this instance.
        /// </summary>
        public GraphicalUiElement ParentGue
        {
            get
            {
                return mWhatContainsThis;
            }
            set
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
                    return ParentGue;
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


        public string Name
        {
            get
            {
                if (mContainedObjectAsIpso != null)
                {
                    return mContainedObjectAsIpso.Name;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                mContainedObjectAsIpso.Name = value;
            }
        }

        public List<IRenderableIpso> Children
        {
            get
            {
                if (mContainedObjectAsIpso != null)
                {
                    return mContainedObjectAsIpso.Children;
                }
                else
                {
                    return null;
                }
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
                    case VerticalAlignment.Center:
                        originOffset.Y = ((IPositionedSizedObject)this).Height / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        break;
                }

                var matrix = this.GetRotationMatrix();
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
                    case VerticalAlignment.Center:
                        originOffset.Y = ((IPositionedSizedObject)this).Height / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originOffset.Y = ((IPositionedSizedObject)this).Height;
                        break;
                }
                var matrix = this.GetRotationMatrix();
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
                    UpdateLayout();
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
                    UpdateLayout();
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

        #region Constructor

        public GraphicalUiElement()
            : this(null, null)
        {

        }

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

            mContainedObjectAsIpso = containedObject as IRenderableIpso;
            mContainedObjectAsIVisible = containedObject as IVisible;

            if (containedObject != null)
            {
                UpdateLayout();
            }
        }

        #endregion

        #region Methods

        bool IsAllLayoutAbsolute()
        {
            return (mWidthUnit == DimensionUnitType.Absolute || mWidthUnit == DimensionUnitType.PercentageOfSourceFile || mWidthUnit == DimensionUnitType.RelativeToChildren) &&
                (mHeightUnit == DimensionUnitType.Absolute || mHeightUnit == DimensionUnitType.PercentageOfSourceFile || mHeightUnit == DimensionUnitType.RelativeToChildren) &&
                (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle || mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted) &&
                (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted);

             
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
            if(effectiveParent != null && effectiveParent.ChildrenLayout == ChildrenLayout.LeftToRightStack && effectiveParent.WrapsChildren)
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
                if (mYOrigin == VerticalAlignment.Center)
                {
                    smallEdge = positionValue - ((IPositionedSizedObject)this).Height / 2.0f;
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

                var units = mYUnits;
                float dimensionToReturn = GetDimensionFromEdges(smallEdge, bigEdge, units);

                return dimensionToReturn;
            }

        }

        private static float GetDimensionFromEdges(float smallEdge, float bigEdge, GeneralUnitType units)
        {
            float dimensionToReturn = 0;
            if (units == GeneralUnitType.PixelsFromSmall)
            {
                smallEdge = 0;

                bigEdge = System.Math.Max(0, bigEdge);
                dimensionToReturn = bigEdge - smallEdge;
            }
            else if (units == GeneralUnitType.PixelsFromMiddle)
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
            bool isScreen = ElementSave != null && ElementSave is ScreenSave;
            return !isScreen &&
                ((this.WidthUnits == DimensionUnitType.RelativeToChildren) ||
                (this.HeightUnits == DimensionUnitType.RelativeToChildren));
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

        void IRenderable.PreRender()
        {
            if(mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.PreRender();
            }
        }


        bool GetIfShouldCallUpdateOnParent()
        {
            var asGue = this.Parent as GraphicalUiElement;

            if(asGue != null)
            {
                return asGue.GetIfDimensionsDependOnChildren() || asGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular;
            }
            else
            {
                return false;
            }
        }

        public void UpdateLayout(bool updateParent, int childrenUpdateDepth)
        {
            if (!mIsLayoutSuspended && !IsAllLayoutSuspended)
            {
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

                // Not sure why we use the ParentGue and not the Parent itself...
                // We want to do it on the actual Parent so that objects attached to components
                // should update the components
                if (updateParent && GetIfShouldCallUpdateOnParent())
                {
                    var asGue = this.Parent as GraphicalUiElement;
                    // Just climb up one and update from there
                    asGue.UpdateLayout(true, childrenUpdateDepth + 1);
                    ChildrenUpdatingParentLayoutCalls++;
                }
                else
                {
                    // Victor Chelaru
                    // March 1, 2015
                    // We tested not doing
                    // "deep" UpdateLayouts
                    // if the object doesn't
                    // actually need it. This
                    // is the case if the if-statement
                    // below evaluates to true. But in practice
                    // we got very minor reduction in calls, but
                    // we incurred a lot of if-checks, so I don't
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

                    if (mContainedObjectAsIpso != null)
                    {
                        if(mContainedObjectAsIpso is LineRectangle)
                        {
                            (mContainedObjectAsIpso as LineRectangle).ClipsChildren = ClipsChildren;
                        }

                        float widthBefore = 0;
                        float heightBefore = 0;
                        if (this.mContainedObjectAsIpso != null)
                        {
                            widthBefore = mContainedObjectAsIpso.Width;
                            heightBefore = mContainedObjectAsIpso.Height;
                        }

                        // The texture dimensions may need to be set before
                        // updating width if we are using % of texture width/height.
                        // However, if the texture coordinates depend on the dimensions
                        // (like for a tiling background) then this also needs to be set
                        // after UpdateDimensions. 
                        if (mContainedObjectAsIpso is Sprite || mContainedObjectAsIpso is NineSlice)
                        {
                            UpdateTextureCoordinatesNotDimensionBased();
                        }

                        if(this.WidthUnits == DimensionUnitType.RelativeToChildren || this.HeightUnits == DimensionUnitType.RelativeToChildren)
                        {
                            UpdateChildren(childrenUpdateDepth, onlyAbsoluteLayoutChildren: true);
                        }

                        UpdateDimensions(parentWidth, parentHeight);

                        if (mContainedObjectAsIpso is Sprite || mContainedObjectAsIpso is NineSlice)
                        {
                            UpdateTextureCoordinatesDimensionBased();
                        }
                        
                        // If the update is "deep" then we want to refresh the text texture.
                        // Otherwise it may have been something shallow like a reposition.
                        if (mContainedObjectAsIpso is Text && childrenUpdateDepth > 0)
                        {
                            // Only if the width or height have changed:
                            if (mContainedObjectAsIpso.Width != widthBefore || mContainedObjectAsIpso.Height != heightBefore)
                            {
                                // I think this should only happen when actually rendering:
                                //((Text)mContainedObjectAsIpso).UpdateTextureToRender();
                                var asText = mContainedObjectAsIpso as Text;

                                asText.SetNeedsRefreshToTrue();
                                asText.UpdatePreRenderDimensions();
                            }
                        }

                        // See the above call to UpdateTextureCoordiantes
                        // on why this is called both before and after UpdateDimensions
                        if (mContainedObjectAsIpso is Sprite)
                        {
                            UpdateTextureCoordinatesNotDimensionBased();
                        }


                        UpdatePosition(parentWidth, parentHeight);

                        mContainedObjectAsIpso.Rotation = this.GetAbsoluteRotation();
                    }


                    if (childrenUpdateDepth > 0)
                    {
                        UpdateChildren(childrenUpdateDepth);
                    }

                    // Eventually add more conditions here to make it fire less often
                    // like check the width/height of the parent to see if they're 0
                    if (updateParent && GetIfShouldCallUpdateOnParent())
                    {
                        this.ParentGue.UpdateLayout(false, false);
                        ChildrenUpdatingParentLayoutCalls++;
                    }

                    UpdateLayerScissor();
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
                    if ((child.Parent == null || child.Parent == this) && 
                        (onlyAbsoluteLayoutChildren == false || child.IsAllLayoutAbsolute()))
                    {
                        child.UpdateLayout(false, childrenUpdateDepth - 1);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var child = this.Children[i];

                    if (child is GraphicalUiElement)
                    {
                        var asGue = child as GraphicalUiElement;
                        if(onlyAbsoluteLayoutChildren == false || asGue.IsAllLayoutAbsolute())
                        {
                            asGue.UpdateLayout(false, childrenUpdateDepth - 1);
                        }
                    }
                }
            }
        }

        private void UpdateLayerScissor()
        {
            if (mSortableLayer != null)
            {
                mSortableLayer.ScissorIpso = this;
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
            else if (this.ParentGue != null && this.ParentGue.mContainedObjectAsIpso != null)
            {
                parentWidth = this.ParentGue.mContainedObjectAsIpso.Width;
                parentHeight = this.ParentGue.mContainedObjectAsIpso.Height;
            }
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

                        break;
                }
            }
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
                        sprite.SourceRectangle = null;
                        sprite.Wrap = false;
                        break;
                    case TextureAddress.Custom:
                        sprite.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                            mTextureLeft,
                            mTextureTop,
                            mTextureWidth,
                            mTextureHeight);
                        sprite.Wrap = mWrap;

                        break;
                    case TextureAddress.DimensionsBased:
                        // This is done *after* setting dimensions

                        break;
                }
            }
            else if(mContainedObjectAsIpso is NineSlice)
            {
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
            }
        }

        private void UpdatePosition(float parentWidth, float parentHeight)
        {
            UpdatePosition(parentWidth, parentHeight, wrap: false);

            var effectiveParent = EffectiveParentGue;

            bool shouldWrap = GetIfParentStacks() && this.EffectiveParentGue.WrapsChildren &&
                ((effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack && this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight()) ||
                (effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack && this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom()));

            if (shouldWrap)
            {
                UpdatePosition(parentWidth, parentHeight, wrap: true);
            }
        }

        private void UpdatePosition(float parentWidth, float parentHeight, bool wrap)
        {
            float parentOriginOffsetX;
            float parentOriginOffsetY;
            bool wasHandledX;
            bool wasHandledY;

            bool canWrap = EffectiveParentGue != null && EffectiveParentGue.WrapsChildren;

            GetParentOffsets(canWrap, wrap, parentWidth, parentHeight,
                out parentOriginOffsetX, out parentOriginOffsetY,
                out wasHandledX, out wasHandledY);


            float unitOffsetX = 0;
            float unitOffsetY = 0;

            AdjustOffsetsByUnits(parentWidth, parentHeight, ref unitOffsetX, ref unitOffsetY);
#if DEBUG
            if (float.IsNaN(unitOffsetX))
            {
                throw new Exception("Invalid unitOffsetX: " + unitOffsetX);
            }

            if ( float.IsNaN(unitOffsetY))
            {
                throw new Exception("Invalid unitOffsetY: " + unitOffsetY);
            }
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



            this.mContainedObjectAsIpso.X = unitOffsetX;
            this.mContainedObjectAsIpso.Y = unitOffsetY;
        }

        public void GetParentOffsets(out float parentOriginOffsetX, out float parentOriginOffsetY)
        {
            float parentWidth;
            float parentHeight;
            GetParentDimensions(out parentWidth, out parentHeight);

            bool throwaway1;
            bool throwaway2;

            bool wrap = false;
            if (EffectiveParentGue != null)
            {
                wrap = (EffectiveParentGue as GraphicalUiElement).Wrap;
            }

            GetParentOffsets(wrap, false, parentWidth, parentHeight, out parentOriginOffsetX, out parentOriginOffsetY,
                out throwaway1, out throwaway2);
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

        private IPositionedSizedObject GetWhatToStackAfter(bool canWrap, bool shouldWrap, out float whatToStackAfterX, out float whatToStackAfterY)
        {
            var parentGue = this.EffectiveParentGue;

            int thisIndex = 0;

            // We used to have a static list we were populating, but that allocates memory so we
            // now use the actual list.
            System.Collections.IList siblings = null;

            if (this.Parent == null)
            {
                siblings = this.ParentGue.mWhatThisContains;
            }
            else if (this.Parent is GraphicalUiElement)
            {
                siblings = ((GraphicalUiElement)Parent).Children as System.Collections.IList;
            }
            thisIndex = siblings.IndexOf(this);

            IPositionedSizedObject whatToStackAfter = null;
            whatToStackAfterX = 0;
            whatToStackAfterY = 0;
            if (thisIndex > 0)
            {
                if (shouldWrap)
                {
                    int currentIndex = thisIndex - 1;
                    IRenderableIpso minimumItem = siblings[currentIndex] as IRenderableIpso;

                    Func<IRenderableIpso, float> getAbsoluteValueFunc = null;

                    if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                    {
                        getAbsoluteValueFunc = item => item.GetAbsoluteX();
                    }
                    else if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack)
                    {
                        getAbsoluteValueFunc = item => item.GetAbsoluteY();
                    }

                    float minValue = getAbsoluteValueFunc(minimumItem);
                    currentIndex--;

                    while (currentIndex > -1)
                    {
                        var candidate = siblings[currentIndex] as IRenderableIpso;

                        if (getAbsoluteValueFunc(candidate) < minValue)
                        {
                            minValue = getAbsoluteValueFunc(candidate);
                            minimumItem = candidate;
                        }
                        else
                        {
                            break;
                        }

                    }
                    whatToStackAfter = minimumItem;

                    if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack)
                    {
                        whatToStackAfterX = 0;
                        whatToStackAfterY = whatToStackAfter.Y + whatToStackAfter.Height;

                    }
                    else if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack)
                    {
                        whatToStackAfterY = 0;
                        whatToStackAfterX = whatToStackAfter.X + whatToStackAfter.Width;
                    }
                }
                else
                {
                    whatToStackAfter = siblings[thisIndex - 1] as IPositionedSizedObject;
                    if (whatToStackAfter != null)
                    {
                        if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack || shouldWrap)
                        {
                            whatToStackAfterX = whatToStackAfter.X + whatToStackAfter.Width;
                        }
                        else
                        {
                            whatToStackAfterX = whatToStackAfter.X;
                        }

                        if (parentGue.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack || shouldWrap)
                        {
                            whatToStackAfterY = whatToStackAfter.Y + whatToStackAfter.Height;
                        }
                        else
                        {
                            whatToStackAfterY = whatToStackAfter.Y;
                        }
                    }
                }
            }

            return whatToStackAfter;
        }

        private void AdjustOffsetsByOrigin(ref float unitOffsetX, ref float unitOffsetY)
        {
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
                //else if (mXUnits == GeneralUnitType.PixelsFromSmall)
                //{
                //    // no need to do anything
                //}
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
            }
        }

        private void AdjustOffsetsByUnits(float parentWidth, float parentHeight, ref float unitOffsetX, ref float unitOffsetY)
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
            else if(mYUnits == GeneralUnitType.PixelsFromMiddleInverted)
            {
                unitOffsetY += -mY;
            }
            else
            {
                unitOffsetY += mY;
            }
        }

        private void UpdateDimensions(float parentWidth, float parentHeight)
        {
            UpdateWidth(parentWidth);

            UpdateHeight(parentHeight);
        }

        private void UpdateHeight(float parentHeight)
        {
            float heightToSet = mHeight;

            if (mHeightUnit == DimensionUnitType.RelativeToChildren)
            {
                float maxHeight = 0;


                if (this.mContainedObjectAsIpso != null)
                {
                    foreach (GraphicalUiElement element in this.Children)
                    {
                        if (element.IsAllLayoutAbsolute())
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
                        if (element.IsAllLayoutAbsolute())
                        {
                            var elementHeight = element.GetRequiredParentHeight();
                            if(this.ChildrenLayout == ChildrenLayout.TopToBottomStack)
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
            else if (mHeightUnit == DimensionUnitType.Percentage)
            {
                heightToSet = parentHeight * mHeight / 100.0f;
            }
            else if (mHeightUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;

                    if (sprite.AtlasedTexture != null)
                    {
                        var atlasedTexture = sprite.AtlasedTexture;
                        heightToSet = atlasedTexture.SourceRectangle.Height * mHeight / 100.0f;
                        wasSet = true;
                    }
                    else if (sprite.Texture != null)
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
            else if (mHeightUnit == DimensionUnitType.RelativeToContainer)
            {
                heightToSet = parentHeight + mHeight;
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
                    foreach (GraphicalUiElement element in this.Children)
                    {
                        if (element.IsAllLayoutAbsolute())
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
                        if (element.IsAllLayoutAbsolute())
                        {
                            var elementWidth = element.GetRequiredParentWidth();

                            if(this.ChildrenLayout == ChildrenLayout.LeftToRightStack)
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

                if (mContainedObjectAsIpso is Sprite)
                {
                    Sprite sprite = mContainedObjectAsIpso as Sprite;

                    if (sprite.AtlasedTexture != null)
                    {
                        var atlasedTexture = sprite.AtlasedTexture;
                        widthToSet = atlasedTexture.SourceRectangle.Width * mWidth / 100.0f;
                        wasSet = true;
                    }

                    else if (sprite.Texture != null)
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
            else if (mWidthUnit == DimensionUnitType.RelativeToContainer)
            {
                widthToSet = parentWidth + mWidth;
            }
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

        public virtual void AddToManagers()
        {

            AddToManagers(SystemManagers.Default, null);

        }

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

                AddContainedRenderableToManagers(managers, layer);

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
                else if(mContainedObjectAsIpso is InvisibleRenderable)
                {
                    managers.SpriteManager.Add(mContainedObjectAsIpso as InvisibleRenderable, layer);
                }
                else
                {
                    throw new NotImplementedException();
                }
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
                layerToRemoveFrom = mManagers.Renderer.Layers[0];
            }

            var layerToAddTo = layer;
            if (layerToAddTo == null)
            {
                layerToAddTo = mManagers.Renderer.Layers[0];
            }

            //if (mSortableLayer != null)
            //{
            //    // all renderables are part of the mSortableLayer, so we
            //    // just move the mSortableLayer and everything comes along with it:
            //    mManagers.Renderer.RemoveLayer(mSortableLayer);
            //    mManagers.Renderer.AddLayer(mSortableLayer, layer);
            //}
            //else
            {
                // This may be a Screen
                if (mContainedObjectAsIpso != null)
                {
                    if(layerToRemoveFrom != null)
                    {
                        layerToRemoveFrom.Remove(mContainedObjectAsIpso);
                    }
                    layerToAddTo.Add(mContainedObjectAsIpso);
                }

                // We don't want to move the children to a layer, because the children
                // are drawn hierarchically
                //foreach (var contained in this.mWhatThisContains)
                //{
                //    contained.MoveToLayer(layer);
                //}
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
                if (mSortableLayer != null)
                {
                    mManagers.Renderer.RemoveLayer(this.mSortableLayer);
                }

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
                else if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.SolidRectangle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as global::RenderingLibrary.Graphics.SolidRectangle);
                }
                else if (mContainedObjectAsIpso is Text)
                {
                    mManagers.TextManager.Remove(mContainedObjectAsIpso as Text);
                }
                else if(mContainedObjectAsIpso is LineCircle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsIpso as LineCircle);
                }
                else if(mContainedObjectAsIpso is InvisibleRenderable)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsIpso as InvisibleRenderable);
                }
                else if (mContainedObjectAsIpso != null)
                {
                    throw new NotImplementedException();
                }


                CustomRemoveFromManagers();
            }
        }

        public void SuspendLayout(bool recursive = false)
        {
            mIsLayoutSuspended = true;

            if (recursive)
            {
                foreach (var item in this.mWhatThisContains)
                {
                    item.SuspendLayout(true);
                }
            }
        }

        public void ResumeLayout(bool recursive = false)
        {
            mIsLayoutSuspended = false;

            if (recursive)
            {
                ResumeLayoutNoUpdateRecursive();
            }

            UpdateLayout();
        }

        private void ResumeLayoutNoUpdateRecursive()
        {

            mIsLayoutSuspended = false;

            foreach (var item in this.mWhatThisContains)
            {
                item.ResumeLayoutNoUpdateRecursive();
            }
        }

        public GraphicalUiElement GetGraphicalUiElementByName(string name)
        {
            foreach (var item in mWhatThisContains)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }

        public IPositionedSizedObject GetChildByName(string name)
        {
            foreach (IPositionedSizedObject child in Children)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return null;
        }

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
            switch (propertyName)
            {
                case "Children Layout":
                    this.ChildrenLayout = (ChildrenLayout)value;
                    toReturn = true;
                    break;
                case "Clips Children":
                    this.ClipsChildren = (bool)value;
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
                    break;
                case "Width":
                    this.Width = (float)value;
                    toReturn = true;
                    break;
                case "Width Units":
                    this.WidthUnits = (DimensionUnitType)value;
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

                if (propertyName.EndsWith("State") && value is string)
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
            else if(mContainedObjectAsIpso is LineCircle)
            {
                handled = TrySetPropertyOnLineCircle(propertyName, value);
            }
            else if(mContainedObjectAsIpso is LineRectangle)
            {
                handled = TrySetPropertyOnLineRectangle(propertyName, value);
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
            }
            else if (mContainedObjectAsIpso is Sprite)
            {
                var sprite = mContainedObjectAsIpso as Sprite;

                if (propertyName == "SourceFile")
                {
                    handled = AssignSourceFileOnSprite(value, sprite);
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
                    else if (ToolsUtilities.FileManager.FileExists(valueAsString))
                    {
                        if (NineSlice.GetIfShouldUsePattern(valueAsString))
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
                                // do nothing?
                            }
                            nineSlice.SetSingleTexture(texture);

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

            return handled;
        }

        private bool AssignSourceFileOnSprite(object value, Sprite sprite)
        {
            bool handled;
            string valueAsString = value as string;

            if (string.IsNullOrEmpty(valueAsString))
            {
                sprite.Texture = null;
                sprite.AtlasedTexture = null;

                UpdateLayout();
            }
            else
            {
                if (ToolsUtilities.FileManager.IsRelative(valueAsString))
                {
                    valueAsString = ToolsUtilities.FileManager.RelativeDirectory + valueAsString;

                    valueAsString = ToolsUtilities.FileManager.RemoveDotDotSlash(valueAsString);
                }

                // see if an atlas exists:
                var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(valueAsString);

                if (atlasedTexture != null)
                {
                    sprite.AtlasedTexture = atlasedTexture;
                    UpdateLayout();
                }
                else
                {
                    if (ToolsUtilities.FileManager.FileExists(valueAsString))
                    {
                        sprite.Texture = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(valueAsString);
                        UpdateLayout();
                    }
                }
            }
            handled = true;
            return handled;
        }

        private bool TrySetPropertyOnText(string propertyName, object value)
        {
            bool handled = false;

            if (propertyName == "Text")
            {
                ((Text)mContainedObjectAsIpso).RawText = value as string;
                handled = true;
            }
            else if (propertyName == "Font Scale")
            {
                ((Text)mContainedObjectAsIpso).FontScale = (float)value;
                handled = true;

            }
            else if (propertyName == "Font")
            {
                this.Font = value as string;

                UpdateToFontValues();
                handled = true;
            }
            else if (propertyName == "UseCustomFont")
            {
                this.UseCustomFont = (bool)value;
                UpdateToFontValues();
                handled = true;
            }

            else if (propertyName == "CustomFontFile")
            {
                CustomFontFile = (string)value;
                UpdateToFontValues();
                handled = true;
            }
            else if (propertyName == "FontSize")
            {
                FontSize = (int)value;
                UpdateToFontValues();
                handled = true;
            }
            else if (propertyName == "OutlineThickness")
            {
                OutlineThickness = (int)value;
                UpdateToFontValues();
                handled = true;
            }
            else if (propertyName == "Blend")
            {
                var valueAsGumBlend = (RenderingLibrary.Blend)value;

                var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                var text = mContainedObjectAsIpso as Text;
                text.BlendState = valueAsXnaBlend;
                handled = true;
            }
            return handled;
        }

        public bool UseCustomFont { get; set; }
        public string CustomFontFile { get; set; }
        public string Font { get; set; }
        public int FontSize { get; set; }
        public int OutlineThickness { get; set; }

        void UpdateToFontValues()
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
                        font = new BitmapFont(CustomFontFile, SystemManagers.Default);
                        contentLoader.AddDisposable(CustomFontFile, font);
                    }
                }


            }
            else
            {
                if (FontSize > 0 && !string.IsNullOrEmpty(Font))
                {
                    string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(FontSize, Font, OutlineThickness);

                    string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, false, true);

#if ANDROID || IOS
                    fullFileName = fullFileName.ToLowerInvariant();
#endif

                    if (ToolsUtilities.FileManager.FileExists(fullFileName))
                    {

                        font = contentLoader.TryGetCachedDisposable<BitmapFont>(fullFileName);
                        if (font == null)
                        {
                            font = new BitmapFont(fullFileName, SystemManagers.Default);

                            contentLoader.AddDisposable(fullFileName, font);
                        }
                        if (font.Textures.Any(item => item?.IsDisposed == true))
                        {
                            throw new InvalidOperationException("The returned font has a disposed texture");
                        }
                    }
                }
            }

            var text = this.mContainedObjectAsIpso as Text;

            text.BitmapFont = font;

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
            if(mCategories.ContainsKey(categoryName))
            {
                var category = mCategories[categoryName];

                var state = category.States.FirstOrDefault(item => item.Name == stateName);

                if(state != null)
                {
                    ApplyState(state);
                }
            }
        }

        public virtual void ApplyState(DataTypes.Variables.StateSave state)
        {
            this.SuspendLayout(true);

            var variablesToConsider =
                state.Variables.Where(item =>
                    // We can set the variable if it's not setting a state (to prevent recursive setting).                   
                    item.IsState(state.ParentContainer) == false ||
                        // If it is setting a state we'll allow it if it's on a child.
                    !string.IsNullOrEmpty(item.SourceObject));

            foreach (var variable in variablesToConsider)
            {
                if (variable.SetsValue && variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }
            this.ResumeLayout(true);
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
            mCategories.Add(category.Name, category);
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

        static List<List<Gum.DataTypes.Variables.VariableSaveValues>> listOfLists = new List<List<Gum.DataTypes.Variables.VariableSaveValues>>();
        int index = 0;

        public void InterpolateBetween(Gum.DataTypes.Variables.StateSave first, Gum.DataTypes.Variables.StateSave second, float interpolationValue)
        {
            if (index >= listOfLists.Count)
            {
                const int capacity = 20;
                var newList = new List<DataTypes.Variables.VariableSaveValues>(capacity);
                listOfLists.Add(newList);
            }

            List<Gum.DataTypes.Variables.VariableSaveValues> values = listOfLists[index];
            values.Clear();
            index++;

            Gum.DataTypes.Variables.StateSaveExtensionMethods.Merge(first, second, interpolationValue, values);

            this.ApplyState(values);
            index--;
        }
        #endregion
    }
}
