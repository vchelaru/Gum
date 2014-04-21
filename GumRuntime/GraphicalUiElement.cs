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
                mContainedObjectAsIVisible.Visible = value;
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
            get { return WidthUnit; }
            set { mWidthUnit = value; UpdateLayout(); }
        }

        public DimensionUnitType HeightUnit
        {
            get { return mHeightUnit; }
            set { mHeightUnit = value; UpdateLayout(); }
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

        public void UpdateLayout()
        {
            if (!mIsLayoutSuspended && mContainedObjectAsIpso != null)
            {

                float parentWidth = CanvasWidth;
                float parentHeight = CanvasHeight;
                float unitOffsetX = this.X;
                float unitOffsetY = this.Y;

                float widthToSet = mWidth;
                float heightToSet = mHeight;

                if (this.Parent != null)
                {
                    parentWidth = Parent.Width;
                    parentHeight = Parent.Height;
                }


                if (mWidthUnit == DimensionUnitType.Percentage)
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

                if (mHeightUnit == DimensionUnitType.Percentage)
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


                mContainedObjectAsIpso.Width = widthToSet;
                mContainedObjectAsIpso.Height = heightToSet;

                if (mContainedObjectAsIpso is Text)
                {
                    ((Text)mContainedObjectAsIpso).UpdateTextureToRender();
                }


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
                else if (mXUnits == GeneralUnitType.PixelsFromLarge)
                {
                    unitOffsetX = mX + parentWidth;
                }
                else if (mXUnits == GeneralUnitType.PixelsFromMiddle)
                {
                    unitOffsetX = mX + parentWidth / 2.0f;
                }
                //else if (mXUnits == GeneralUnitType.PixelsFromSmall)
                //{
                //    // no need to do anything
                //}

                if (mYUnits == GeneralUnitType.Percentage)
                {
                    unitOffsetY = parentHeight * mY / 100.0f;
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
                else if (mYUnits == GeneralUnitType.PixelsFromLarge)
                {
                    unitOffsetY = mY + parentHeight;
                }
                else if (mYUnits == GeneralUnitType.PixelsFromMiddle)
                {
                    unitOffsetY = mY + parentHeight / 2.0f;
                }




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

                this.mContainedObjectAsIpso.X = unitOffsetX;
                this.mContainedObjectAsIpso.Y = unitOffsetY;

                mContainedObjectAsIpso.Parent = mParent;

                foreach (var child in this.Children)
                {
                    if (child is GraphicalUiElement)
                    {
                        (child as GraphicalUiElement).UpdateLayout();
                    }
                }
            }

        }

        public override string ToString()
        {
            return Name;
        }

        public void SetGueWidthAndPositionValues(IVariableFinder rvf)
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
                mManagers = managers;

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

        public void AddStates(List<DataTypes.Variables.StateSave> list)
        {
            foreach (var state in list)
            {
                mStates.Add(state.Name, state);
            }
        }
    }
}
