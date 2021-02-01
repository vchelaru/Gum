#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

#if SKIA
using SkiaGum.Xna;
#endif
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
#if !NO_XNA && !SKIA
        public static Microsoft.Xna.Framework.Graphics.BlendState ToBlendState(this Blend blend)
        {
            switch (blend)
            {
                case Blend.Normal:
                    return global::RenderingLibrary.Graphics.Renderer.NormalBlendState;
                case Blend.Additive:
                    return BlendState.Additive;
                case Blend.Replace:
                    return BlendState.Opaque;
            }
            return BlendState.NonPremultiplied;
        }
#endif

#if MONOGAME || SKIA

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
#endif 
    }
}
