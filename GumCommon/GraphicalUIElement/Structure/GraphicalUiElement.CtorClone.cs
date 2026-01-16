using Gum.Collections;
using Gum.DataTypes;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Constructor / Clone

        public GraphicalUiElement()
            : this(null, null)
        {
            mIsLayoutSuspended = true;
            Width = 32;
            Height = 32;
            mIsLayoutSuspended = false;
        }

        public GraphicalUiElement(IRenderable containedObject, GraphicalUiElement whatContainsThis = null)
        {
            Width = 32;
            Height = 32;
#if FULL_DIAGNOSTICS
            if (containedObject is GraphicalUiElement)
            {
                throw new InvalidOperationException("GraphicalUiElements cannot contain other GraphicalUiElements as their renderable. " +
                    $"The contained object should be a renderable, such as a (platform specific) Sprite or Text. " +
                    $"It cannot be {containedObject.GetType()}");
            }
#endif
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

            // This is a bit of a hack to support GraphicalUiElement.IWindow.
            // This isn't needed in MonoGame:
            OnConstructor();
        }

        partial void OnConstructor();

        public void SetContainedObject(IRenderable containedObject)
        {
            if (containedObject == this)
            {
                throw new ArgumentException("The argument containedObject cannot be 'this'");
            }

            mContainedObjectAsIpso = containedObject as IRenderableIpso;

            if (mContainedObjectAsIpso == null)
            {
                _childrenWrapper = GraphicalUiElementCollection.Empty;
            }
            else
            {
                _childrenWrapper = new GraphicalUiElementCollection(mContainedObjectAsIpso.Children);
                _childrenWrapper.CollectionChanged += HandleCollectionChanged;
            }

            mContainedObjectAsIVisible = containedObject as IVisible;

            if (mContainedObjectAsIpso != null)
            {
                mContainedObjectAsIpso.Name ??= name;
                name = mContainedObjectAsIpso.Name;
            }

            // in case this had been changed before the Text was assigned, or in case the text
            // default differs.
            if (containedObject is IText asText)
            {
                asText.TextOverflowVerticalMode = this.TextOverflowVerticalMode;
            }

            if (containedObject != null)
            {
                UpdateLayout();
            }
        }

        public virtual void CreateChildrenRecursively(ElementSave elementSave, ISystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);

                if (childGue != null)
                {
                    // As of November 22, 2024 we now add children
                    // to Screen GraphicalUiElements to make Entities
                    // and Screens consistent.
                    if (!isScreen || this.Children != null)
                    {
                        childGue.Parent = this;
                    }
                    childGue.ElementGueContainingThis = this;
                }
            }
        }

        public virtual GraphicalUiElement Clone()
        {

            IRenderable? clonedRenderable = (this.mContainedObjectAsIpso as ICloneable)?.Clone() as IRenderable;

            if (clonedRenderable == null)
            {
                if (CloneRenderableFunction == null)
                {
                    throw new InvalidOperationException($"{this.mContainedObjectAsIpso?.GetType()} needs to implement ICloneable or " +
                        $"GraphicalUiElement.CloneRenderableFunction must be set before calling clone");
                }
                clonedRenderable = GraphicalUiElement.CloneRenderableFunction(this.mContainedObjectAsIpso);
            }

            GraphicalUiElement? newClone = (GraphicalUiElement)this.MemberwiseClone();

            newClone.SetContainedObject(clonedRenderable);
            newClone.mWhatContainsThis = null;
            return newClone;
        }

        #endregion
    }
}