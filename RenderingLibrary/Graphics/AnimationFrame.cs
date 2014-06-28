using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class AnimationFrame
    {
        /// <summary>
        /// The source rectangle to display.  If this is null then the entire source file is used.
        /// </summary>
        public Rectangle? SourceRectangle;

        /// <summary>
        /// The amount of time to show the frame for in seconds.
        /// </summary>
        public double FrameTime;

        public bool FlipHorizontal;
        public bool FlipVertical;

        public Texture2D Texture;
    }
}
