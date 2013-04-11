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
        public Rectangle? SourceRectangle;

        public double FrameTime;

        public bool FlipHorizontal;
        public bool FlipVertical;

        public Texture2D Texture;
    }
}
