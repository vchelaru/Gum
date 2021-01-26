using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class LayerCameraSettings
    {
        /// <summary>
        /// Whether the current layer is in Screen Space. If true, then the Camera position does
        /// not impact the position of objects on this layer.
        /// </summary>
        public bool IsInScreenSpace
        {
            get;
            set;
        }

        public float? Zoom
        {
            get;
            set;
        }
    }
}
