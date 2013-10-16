using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe
{
    public class GraphicalUiElement : IRenderable, IPositionedSizedObject, IVisible
    {
        #region Fields

        IRenderable mContainedObjectAsRenderable;
        // to save on casting:
        IPositionedSizedObject mContainedObjectAsIpso;
        IVisible mContainedObjectAsIVisible;

        GraphicalUiElement mWhatContainsThis;

        List<GraphicalUiElement> mWhatThisContains = new List<GraphicalUiElement>();

        #endregion

        #region Properties

        public IEnumerable<GraphicalUiElement> ContainedElements
        {
            get
            {
                return mWhatThisContains;
            }
        }

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


        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
        {
            mContainedObjectAsIpso.SetParentDirect(parent);
        }

        public IPositionedSizedObject Parent
        {
            get { return mContainedObjectAsIpso.Parent; }
            set
            {
                mContainedObjectAsIpso.Parent = value;

                if (value != null)
                {
                    value.Children.Remove(mContainedObjectAsIpso);
                    value.Children.Add(this);
                }
            }
        }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mContainedObjectAsIpso.Children; }
        }

        public object Tag
        {
            get
            {
                return mContainedObjectAsIpso.Tag;
            }
            set
            {
                mContainedObjectAsIpso.Tag = value;
            }
        }

        #endregion

        #region Constructor

        public GraphicalUiElement(IRenderable containedObject, GraphicalUiElement whatContainsThis)
        {
            if (containedObject == null)
            {
                throw new ArgumentException("The containedObject cannot be null");
            }
            // We actually can have null whatContainsThis
            // if we're dealing with a top-level component
            //if (whatContainsThis == null)
            //{
            //    throw new ArgumentException("The whatContainsThis cannot be null");
            //}

            mWhatContainsThis = whatContainsThis;
            if (mWhatContainsThis != null)
            {
                mWhatContainsThis.mWhatThisContains.Add(this);
            }
            
            mContainedObjectAsRenderable = containedObject;
            mContainedObjectAsIpso = mContainedObjectAsRenderable as IPositionedSizedObject;
            mContainedObjectAsIVisible = mContainedObjectAsRenderable as IVisible;
        }

        #endregion


        public IPositionedSizedObject Component { get { return mContainedObjectAsIpso; } }

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

        public override string ToString()
        {
            return Name;
        }

        bool IVisible.Visible
        {
            get
            {
                return mContainedObjectAsIVisible.Visible;
            }
            set
            {
                mContainedObjectAsIVisible.Visible = value;
            }
        }

        bool IVisible.AbsoluteVisible
        {
            get { return mContainedObjectAsIVisible.AbsoluteVisible; }
        }

        IVisible IVisible.Parent
        {
            get { return this.Parent as IVisible; }
        }
    }
}
