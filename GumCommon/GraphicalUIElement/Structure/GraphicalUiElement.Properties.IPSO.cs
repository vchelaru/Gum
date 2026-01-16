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

        void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
        {
            mContainedObjectAsIpso.SetParentDirect(parent);
        }

        #endregion
    }
}
