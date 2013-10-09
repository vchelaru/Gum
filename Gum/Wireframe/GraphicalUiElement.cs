using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe
{
    public class GraphicalUiElement : IRenderable, IPositionedSizedObject
    {
        #region Fields

        IRenderable mContainedObject;
        // to save on casting:
        IPositionedSizedObject mContainedObjectAsIpso;
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
            get { return mContainedObject.BlendState; }
        }

        bool IRenderable.Wrap
        {
            get { return mContainedObject.Wrap; }
        }

        void IRenderable.Render(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, SystemManagers managers)
        {
            mContainedObject.Render(spriteBatch, managers);
        }

        float IRenderable.Z
        {
            get
            {
                return mContainedObject.Z;
            }
            set
            {
                mContainedObject.Z = value;
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
            
            mContainedObject = containedObject;
            mContainedObjectAsIpso = mContainedObject as IPositionedSizedObject;
        }

        #endregion


        public IPositionedSizedObject Component { get { return mContainedObjectAsIpso; } }

        public IRenderable RenderableComponent
        {
            get
            {
                if (mContainedObject is GraphicalUiElement)
                {
                    return ((GraphicalUiElement)mContainedObject).RenderableComponent;
                }
                else
                {
                    return mContainedObject;
                }

            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
