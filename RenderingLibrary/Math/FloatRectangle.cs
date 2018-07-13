using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Math
{
    public struct FloatRectangle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public FloatRectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
