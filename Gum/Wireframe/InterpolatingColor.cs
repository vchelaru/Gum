using System;
using Microsoft.Xna.Framework;

namespace Gum.Wireframe
{
    public class InterpolatingColor
    {
        public enum VelocityType
        {
            Add,
            Subtract
        }

        VelocityType mVelocityType;

        public Color Color = Color.White;


        public void Activity()
        {
            int incrementAmount = 25;
            switch (mVelocityType)
            {
                case VelocityType.Add:
                    if (Color.R < 255)
                    {
                        Color.R = (byte)Math.Min(255, Color.R + incrementAmount);

                    }
                    else
                    {
                        mVelocityType = VelocityType.Subtract;
                    }
                    break;
                case VelocityType.Subtract:
                    if (Color.R > 0)
                    {
                        Color.R = (byte)Math.Max(0, Color.R - incrementAmount);
                    }
                    else
                    {
                        mVelocityType = VelocityType.Add;
                    }
                    break;
            }
            Color.G = Color.R;
            Color.B = Color.R;

        }
    }
}
