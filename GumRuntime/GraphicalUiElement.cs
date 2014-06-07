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


namespace Gum.Wireframe
{


    public partial class GraphicalUiElement : IRenderable, IPositionedSizedObject, IVisible
    {
        #region Fields

        public static bool ShowLineRectangles = true;

        IRenderable mContainedObjectAsRenderable;
        // to save on casting:
        IPositionedSizedObject mContainedObjectAsIpso;
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

        static float mCanvasWidth = 800;
        static float mCanvasHeight = 600;

        IPositionedSizedObject mParent;


        bool mIsLayoutSuspended = false;

        Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
            new Dictionary<string, DataTypes.Variables.StateSave>();

        Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
            new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

        #endregion

        #region Properties

        public SystemManagers Managers
        {
            get
            {
                return mManagers;
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
                return mContainedObjectAsIpso.X;
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
                return mContainedObjectAsIpso.Y;

            }
            set
            {
                mContainedObjectAsIpso.Y = value;
            }
        }

        float IPositionedSizedObject.Z
        {
            get
            {
                return mContainedObjectAsIpso.Z;
            }
            set
            {
                mContainedObjectAsIpso.Z = value;
            }
        }


        float IPositionedSizedObject.Rotation 
        {
            get
            {
                return mContainedObjectAsIpso.Rotation;
            }
            set
            {
                mContainedObjectAsIpso.Rotation = value;
            }
        }

        float IPositionedSizedObject.Width
        {
            get
            {
                return mContainedObjectAsIpso.Width;
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
                return mContainedObjectAsIpso.Height;
            }
            set
            {
                mContainedObjectAsIpso.Height = value;
            }
        }

        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
        {
            mContainedObjectAsIpso.SetParentDirect(parent);
        }

        #endregion

        #region IRenderable properties


        Microsoft.Xna.Framework.Graphics.BlendState IRenderable.BlendState
        {
            get { return mContainedObjectAsRenderable.BlendState; }
        }

        bool IRenderable.Wrap
        {
            get { return mContainedObjectAsRenderable.Wrap; }
        }

        void IRenderable.Render(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, SystemManagers managers)
        {
            mContainedObjectAsRenderable.Render(spriteBatch, managers);
        }

        float IRenderable.Z
        {
            get
            {
                return mContainedObjectAsRenderable.Z;
            }
            set
            {
                mContainedObjectAsRenderable.Z = value;
            }
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
            set { mXUnits = value; UpdateLayout(); }
        }

        public GeneralUnitType YUnits
        {
            get { return mYUnits; }
            set { mYUnits = value; UpdateLayout(); }
        }

        public HorizontalAlignment XOrigin
        {
            get { return mXOrigin; }
            set { mXOrigin = value; UpdateLayout(); }
        }

        public VerticalAlignment YOrigin
        {
            get { return mYOrigin; }
            set { mYOrigin = value; UpdateLayout(); }
        }

        public DimensionUnitType WidthUnit
        {
            get { return mWidthUnit; }
            set { mWidthUnit = value; UpdateLayout(); }
        }

        public DimensionUnitType HeightUnit
        {
            get { return mHeightUnit; }
            set { mHeightUnit = value; UpdateLayout(); }
        }

        public ChildrenLayout ChildrenLayout
        {
            get;
            set;
        }

        public float X
        {
            get
            {
                return mX;
            }
            set
            {
                mX = value;
                UpdateLayout();
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
                mY = value;
                UpdateLayout();
            }
        }

        public float Width
        {
            get { return mWidth; }
            set { mWidth = value; UpdateLayout(); }
        }

        public float Height
        {
            get { return mHeight; }
            set { mHeight = value; UpdateLayout(); }
        }

        public IPositionedSizedObject Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                    UpdateLayout();

                }
            }
        }

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
                if (mContainedObjectAsRenderable is GraphicalUiElement)
                {
                    return ((GraphicalUiElement)mContainedObjectAsRenderable).RenderableComponent;
                }
                else
                {
                    return mContainedObjectAsRenderable;
                }

            }
        }

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
                return mContainedObjectAsIpso.Name;
            }
            set
            {
                mContainedObjectAsIpso.Name = value;
            }
        }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mContainedObjectAsIpso.Children; }
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

                switch (XOrigin)
                {
                    case HorizontalAlignment.Center:
                        toReturn += ((IPositionedSizedObject)this).Width / 2;
                        break;
                    case HorizontalAlignment.Right:
                        toReturn += ((IPositionedSizedObject)this).Width;
                        break;
                }
                return toReturn;
            }
        }


        public float AbsoluteY
        {
            get
            {
                float toReturn = this.GetAbsoluteY();

                switch (YOrigin)
                {
                    case VerticalAlignment.Center:
                        toReturn += ((IPositionedSizedObject)this).Height / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        toReturn += ((IPositionedSizedObject)this).Height;
                        break;
                }
                return toReturn;
            }
        }


        public IVisible ExplicitIVisibleParent
        {
            get;
            set;
        }


        public int TextureTop
        {
            get
            {
                return mTextureTop;
            }
            set
            {

                mTextureTop = value;
                UpdateLayout();
            }
        }

        public int TextureLeft
        {
            get
            {
                return mTextureLeft;
            }
            set
            {

                mTextureLeft = value;
                UpdateLayout();
            }
        }
        public int TextureWidth
        {
            get
            {
                return mTextureWidth;
            }
            set
            {

                mTextureWidth = value;
                UpdateLayout();
            }
        }
        public int TextureHeight
        {
            get
            {
                return mTextureHeight;
            }
            set
            {

                mTextureHeight = value;
                UpdateLayout();
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

                mTextureWidthScale = value;
                UpdateLayout();
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

                mTextureHeightScale = value;
                UpdateLayout();
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
                mTextureAddress = value;
                UpdateLayout();
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
                mWrap = value;
                UpdateLayout();
            }
        }

        public bool WrapsChildren
        {
            get { return mWrapsChildren; }
            set { mWrapsChildren = value; UpdateLayout(); }
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

                this.Parent = whatContainsThis;
            }
        }

        public void SetContainedObject(IRenderable containedObject)
        {
            if (containedObject == this)
            {
                throw new ArgumentException("The argument containedObject cannot be 'this'");
            }

            mContainedObjectAsRenderable = containedObject;
            mContainedObjectAsIpso = mContainedObjectAsRenderable as IPositionedSizedObject;
            mContainedObjectAsIVisible = mContainedObjectAsRenderable as IVisible;

            if (containedObject is global::RenderingLibrary.Math.Geometry.LineRectangle)
            {
                (containedObject as global::RenderingLibrary.Math.Geometry.LineRectangle).LocalVisible = ShowLineRectangles;
            }

            UpdateLayout();
        }

        #endregion

        #region Methods

        bool IsAllLayoutAbsolute()
        {
            return (mWidthUnit == DimensionUnitType.Absolute || mWidthUnit == DimensionUnitType.PercentageOfSourceFile) &&
                (mHeightUnit == DimensionUnitType.Absolute || mHeightUnit == DimensionUnitType.PercentageOfSourceFile) &&
                (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle || mXUnits == GeneralUnitType.PixelsFromSmall) &&
                (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle || mYUnits == GeneralUnitType.PixelsFromSmall);
        }

        float GetRequiredParentWidth()
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

        float GetRequiredParentHeight()
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
            return (this.WidthUnit == DimensionUnitType.Absolute && this.mWidth == 0) ||
                (this.HeightUnit == DimensionUnitType.Absolute && this.mHeight == 0);
        }

        public void UpdateLayout(bool updateParent, bool updateChildren)
        {
            if (!mIsLayoutSuspended && mContainedObjectAsIpso != null)
            {
                // May 15, 2014
                // This needs to be
                // set before we start
                // doing the updates because
                // we use foreaches internally
                // in the updates.
                mContainedObjectAsIpso.Parent = mParent;

                if (updateParent && this.ParentGue != null && 
                    (ParentGue.GetIfDimensionsDependOnChildren() || ParentGue.ChildrenLayout != Gum.Managers.ChildrenLayout.Regular ))
                {
                    // Just climb up one and update from there
                    this.ParentGue.UpdateLayout(true, true);
                }
                else
                {


                    float parentWidth;
                    float parentHeight;
                    GetParentDimensions(out parentWidth, out parentHeight);

                    UpdateDimensions(parentWidth, parentHeight);

                    if (mContainedObjectAsIpso is Text)
                    {
                        ((Text)mContainedObjectAsIpso).UpdateTextureToRender();
                    }
                    if (mContainedObjectAsRenderable is Sprite)
                    {
                        UpdateTextureCoordinates();
                    }

                    UpdatePosition(parentWidth, parentHeight);

                    if (updateChildren)
                    {
                        foreach (var child in this.Children)
                        {
                            if (child is GraphicalUiElement)
                            {
                                (child as GraphicalUiElement).UpdateLayout(false, true);
                            }
                        }
                    }

                    // Eventually add more conditions here to make it fire less often
                    // like check the width/height of the parent to see if they're 0
                    if (updateParent && this.ParentGue != null)
                    {
                        this.ParentGue.UpdateLayout(false, false);
                    }

                    UpdateLayerScissor();
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
            else if (this.ParentGue != null && this.ParentGue.mContainedObjectAsRenderable != null)
            {
                parentWidth = this.ParentGue.mContainedObjectAsIpso.Width;
                parentHeight = this.ParentGue.mContainedObjectAsIpso.Height;

            }

        }

        private void UpdateTextureCoordinates()
        {
            var sprite = mContainedObjectAsRenderable as Sprite;
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

        private void UpdatePosition(float parentWidth, float parentHeight)
        {
            UpdatePosition(parentWidth, parentHeight, wrap:false);

            var effectiveParent = EffectiveParentGue;
            
            bool shouldWrap = GetIfParentStacks() && ParentGue.WrapsChildren &&
                ((effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.LeftToRightStack && this.GetAbsoluteRight() > effectiveParent.GetAbsoluteRight()) ||
                (effectiveParent.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack && this.GetAbsoluteBottom() > effectiveParent.GetAbsoluteBottom()));

            if (shouldWrap)
            {
                UpdatePosition(parentWidth, parentHeight, wrap:true);
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


            AdjustOffsetsByOrigin(ref unitOffsetX, ref unitOffsetY);

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

            GetParentOffsets(true, false, parentWidth, parentHeight, out parentOriginOffsetX, out parentOriginOffsetY,
                out throwaway1, out throwaway2);
        }

        private void GetParentOffsets(bool canWrap, bool shouldWrap, float parentWidth, float parentHeight, out float parentOriginOffsetX, out float parentOriginOffsetY, out bool wasHandledX, out bool wasHandledY)
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

                if(whatToStackAfter != null)
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

                            break;
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

        static List<IPositionedSizedObject> mWhatToStackAfterList = new List<IPositionedSizedObject>();

        private IPositionedSizedObject GetWhatToStackAfter(bool canWrap, bool shouldWrap, out float whatToStackAfterX, out float whatToStackAfterY)
        {
            var parentGue = this.EffectiveParentGue;

            int thisIndex = 0;
            mWhatToStackAfterList.Clear();
            
            if (this.Parent == null)
            {
                mWhatToStackAfterList.AddRange(this.ParentGue.mWhatThisContains);
            }
            else if(this.Parent is GraphicalUiElement)
            {
                mWhatToStackAfterList.AddRange(this.Parent.Children);
            }
            thisIndex = mWhatToStackAfterList.IndexOf(this);

            IPositionedSizedObject whatToStackAfter = null;
            whatToStackAfterX = 0;
            whatToStackAfterY = 0;
            if (thisIndex > 0)
            {
                if (shouldWrap)
                {
                    int currentIndex = thisIndex - 1;
                    IPositionedSizedObject minimumItem = mWhatToStackAfterList[currentIndex];

                    Func<IPositionedSizedObject, float> getAbsoluteValueFunc = null;

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
                        var candidate = mWhatToStackAfterList[currentIndex];

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
                    whatToStackAfter = mWhatToStackAfterList[thisIndex - 1] as IPositionedSizedObject;
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

            if (mXOrigin == HorizontalAlignment.Center)
            {
                unitOffsetX -= mContainedObjectAsIpso.Width / 2.0f;
            }
            else if (mXOrigin == HorizontalAlignment.Right)
            {
                unitOffsetX -= mContainedObjectAsIpso.Width;
            }
            // no need to handle left


            if (mYOrigin == VerticalAlignment.Center)
            {
                unitOffsetY -= mContainedObjectAsIpso.Height / 2.0f;
            }
            else if (mYOrigin == VerticalAlignment.Bottom)
            {
                unitOffsetY -= mContainedObjectAsIpso.Height;
            }
            // no need to handle top
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
                else if (mYUnits == GeneralUnitType.PixelsFromMiddle)
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

                if (mContainedObjectAsRenderable is Sprite)
                {
                    Sprite sprite = mContainedObjectAsRenderable as Sprite;

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
                unitOffsetX = parentWidth * mX / 100.0f;
            }
            else if (mYUnits == GeneralUnitType.PercentageOfFile)
            {

                bool wasSet = false;


                if (mContainedObjectAsRenderable is Sprite)
                {
                    Sprite sprite = mContainedObjectAsRenderable as Sprite;

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

            if (mHeightUnit == DimensionUnitType.Absolute && heightToSet == 0)
            {
                float maxHeight = 0;
                foreach (var element in this.ContainedElements)
                {
                    if (element.IsAllLayoutAbsolute())
                    {
                        var elementWidth = element.GetRequiredParentHeight();
                        maxHeight = System.Math.Max(maxHeight, elementWidth);
                    }
                }

                heightToSet = maxHeight;
            }
            else if (mHeightUnit == DimensionUnitType.Percentage)
            {
                heightToSet = parentHeight * mHeight / 100.0f;
            }
            else if (mHeightUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

                if (mContainedObjectAsRenderable is Sprite)
                {
                    Sprite sprite = mContainedObjectAsRenderable as Sprite;

                    if (sprite.Texture != null)
                    {
                        heightToSet = sprite.Texture.Height * mHeight / 100.0f;
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

            if (mWidthUnit == DimensionUnitType.Absolute && widthToSet == 0)
            {
                float maxWidth = 0;
                foreach (var element in this.ContainedElements)
                {
                    if (element.IsAllLayoutAbsolute())
                    {
                        var elementWidth = element.GetRequiredParentWidth();
                        maxWidth = System.Math.Max(maxWidth, elementWidth);
                    }
                }

                widthToSet = maxWidth;
            }
            else if (mWidthUnit == DimensionUnitType.Percentage)
            {
                widthToSet = parentWidth * mWidth / 100.0f;
            }
            else if (mWidthUnit == DimensionUnitType.PercentageOfSourceFile)
            {
                bool wasSet = false;

                if (mContainedObjectAsRenderable is Sprite)
                {
                    Sprite sprite = mContainedObjectAsRenderable as Sprite;

                    if (sprite.Texture != null)
                    {
                        widthToSet = sprite.Texture.Width * mWidth / 100.0f;
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

            this.HeightUnit = rvf.GetValue<DimensionUnitType>("Height Units");
            this.WidthUnit = rvf.GetValue<DimensionUnitType>("Width Units");

            this.XOrigin = rvf.GetValue<HorizontalAlignment>("X Origin");
            this.YOrigin = rvf.GetValue<VerticalAlignment>("Y Origin");

            this.X = rvf.GetValue<float>("X");
            this.Y = rvf.GetValue<float>("Y");

            this.XUnits = UnitConverter.Self.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("X Units"));
            this.YUnits = UnitConverter.Self.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("Y Units"));

            this.TextureWidth = rvf.GetValue<int>("Texture Width");
            this.TextureHeight = rvf.GetValue<int>("Texture Height");
            this.TextureLeft = rvf.GetValue<int>("Texture Left");
            this.TextureTop = rvf.GetValue<int>("Texture Top");

            this.TextureWidthScale = rvf.GetValue<float>("Texture Width Scale");
            this.TextureHeightScale = rvf.GetValue<float>("Texture Height Scale");

            this.Wrap = rvf.GetValue<bool>("Wrap");

            this.TextureAddress = rvf.GetValue<TextureAddress>("Texture Address");

            this.ResumeLayout();
        }


        partial void CustomAddToManagers();

        public void AddToManagers()
        {

                AddToManagers(SystemManagers.Default, null);
            
        }

        public void AddToManagers(SystemManagers managers, Layer layer)
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

                // Set the managers first because it's used by the clip region
                mManagers = managers;

                // If this clips children...
                if (ClipsChildren)
                {
                    // Then let's use a new Layer...
                    if (mSortableLayer == null)
                    {
                        mSortableLayer = new SortableLayer();

                    }

                    mManagers.Renderer.AddLayer(mSortableLayer, layer);

                    // Now we'll just set layer to mSortableLayer so everything goes on as normal
                    layer = mSortableLayer;

                    UpdateLayerScissor();
                }





                // This may be a Screen
                if (mContainedObjectAsRenderable != null)
                {

                    if (mContainedObjectAsRenderable is Sprite)
                    {
                        managers.SpriteManager.Add(mContainedObjectAsRenderable as Sprite, layer);
                    }
                    else if (mContainedObjectAsRenderable is NineSlice)
                    {
                        managers.SpriteManager.Add(mContainedObjectAsRenderable as NineSlice, layer);
                    }
                    else if (mContainedObjectAsRenderable is global::RenderingLibrary.Math.Geometry.LineRectangle)
                    {
                        managers.ShapeManager.Add(mContainedObjectAsRenderable as global::RenderingLibrary.Math.Geometry.LineRectangle, layer);
                    }
                    else if (mContainedObjectAsRenderable is global::RenderingLibrary.Graphics.SolidRectangle)
                    {
                        managers.ShapeManager.Add(mContainedObjectAsRenderable as global::RenderingLibrary.Graphics.SolidRectangle, layer);
                    }
                    else if (mContainedObjectAsRenderable is Text)
                    {
                        managers.TextManager.Add(mContainedObjectAsRenderable as Text, layer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                // Custom should be called before children have their Custom called
                CustomAddToManagers();

                //Recursively add children to the managers
                foreach (var child in this.ContainedElements)
                {
                    if (child is GraphicalUiElement)
                    {
                        (child as GraphicalUiElement).AddToManagers(managers, layer);
                    }
                }
            }
        }

        public void AddExposedVariable(string variableName, string underlyingVariable)
        {
            mExposedVariables.Add(variableName, underlyingVariable);
        }

        public bool IsExposedVariable(string variableName)
        {
            return this.mExposedVariables.ContainsKey(variableName);
        }

        partial void CustomRemoveFromManagers();

        public void MoveToLayer(Layer layer)
        {
            var layerToRemoveFrom = mLayer;
            if (mLayer == null)
            {
                layerToRemoveFrom = mManagers.Renderer.Layers[0];
            }

            var layerToAddTo = layer;
            if (layerToAddTo == null)
            {
                layerToAddTo = mManagers.Renderer.Layers[0];
            }

            if (mSortableLayer != null)
            {
                throw new NotImplementedException();
            }



            // This may be a Screen
            if (mContainedObjectAsRenderable != null)
            {
                layerToRemoveFrom.Remove(mContainedObjectAsRenderable);
                layerToAddTo.Add(mContainedObjectAsRenderable);
            }

            foreach (var contained in this.ContainedElements)
            {
                contained.MoveToLayer(layer);
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

                if (mContainedObjectAsRenderable is Sprite)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsRenderable as Sprite);
                }
                else if (mContainedObjectAsRenderable is NineSlice)
                {
                    mManagers.SpriteManager.Remove(mContainedObjectAsRenderable as NineSlice);
                }
                else if (mContainedObjectAsRenderable is global::RenderingLibrary.Math.Geometry.LineRectangle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsRenderable as global::RenderingLibrary.Math.Geometry.LineRectangle);
                }
                else if (mContainedObjectAsRenderable is global::RenderingLibrary.Graphics.SolidRectangle)
                {
                    mManagers.ShapeManager.Remove(mContainedObjectAsRenderable as global::RenderingLibrary.Graphics.SolidRectangle);
                }
                else if (mContainedObjectAsRenderable is Text)
                {
                    mManagers.TextManager.Remove(mContainedObjectAsRenderable as Text);
                }
                else if (mContainedObjectAsRenderable != null)
                {
                    throw new NotImplementedException();
                }


                CustomRemoveFromManagers();
            }
        }

        public void SuspendLayout()
        {
            mIsLayoutSuspended = true;
        }

        public void ResumeLayout()
        {
            mIsLayoutSuspended = false;
            UpdateLayout();
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
                containedGue.SetProperty(variable, value);
            }
            else if (propertyName.Contains('.'))
            {
                int indexOfDot = propertyName.IndexOf('.');
                string instanceName = propertyName.Substring(0, indexOfDot);
                GraphicalUiElement containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = propertyName.Substring(indexOfDot + 1);
                containedGue.SetProperty(variable, value);
            }
            else if (this.mContainedObjectAsRenderable != null)
            {
                SetPropertyOnRenderable(propertyName, value);

            }

        }

        private void SetPropertyOnRenderable(string propertyName, object value)
        {
            bool handled = false;

            // First try special-casing.  
            if (mContainedObjectAsRenderable is Text)
            {
                if (propertyName == "Text")
                {
                    ((Text)mContainedObjectAsRenderable).RawText = value as string;
                    handled = true;
                }

            }

            // If special case didn't work, let's try reflection
            if (!handled)
            {
                System.Reflection.PropertyInfo propertyInfo = mContainedObjectAsRenderable.GetType().GetProperty(propertyName);

                if (propertyInfo != null)
                {

                    if (value.GetType() != propertyInfo.PropertyType)
                    {
                        value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                    }
                    propertyInfo.SetValue(mContainedObjectAsRenderable, value, null);
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

        public void ApplyState(string name)
        {
            if (mStates.ContainsKey(name))
            {
                var state = mStates[name];

                foreach (var variable in state.Variables)
                {
                    if (variable.SetsValue)
                    {
                        this.SetProperty(variable.Name, variable.Value);
                    }
                }

            }
        }

        public void AddCategory(DataTypes.Variables.StateSaveCategory category)
        {
            mCategories.Add(category.Name, category);
        }

        public void AddStates(List<DataTypes.Variables.StateSave> list)
        {
            foreach (var state in list)
            {
                mStates.Add(state.Name, state);
            }
        }

        #endregion
    }
}
