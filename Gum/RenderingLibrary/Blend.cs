using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.RenderingLibrary
{
    public enum Blend
    {
        Normal,
        Additive,
        Replace
    }



    public static class BlendExtensions
    {

        public static Microsoft.Xna.Framework.Graphics.BlendState ToBlendState(this Blend blend)
        {
            switch (blend)
            {
                case Blend.Normal:
                    return BlendState.NonPremultiplied;
                case Blend.Additive:
                    return BlendState.Additive;
                case Blend.Replace:
                    return BlendState.Opaque;
            }
            return BlendState.NonPremultiplied;
        }

        public static Blend ToBlend(this BlendState blendState)
        {
            if (blendState == BlendState.NonPremultiplied)
            {
                return Blend.Normal;
            }
            else if (blendState == BlendState.Additive)
            {
                return Blend.Additive;
            }
            else if (blendState == BlendState.Opaque)
            {
                return Blend.Replace;
            }
            else
            {
                return Blend.Normal;
            }

        }
    }
}
