﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if SKIA
using SkiaGum.Xna;
#endif

namespace Gum.RenderingLibrary
{
    public static class BlendExtensions
    {
        #if !NO_XNA && !SKIA
        public static BlendState ToBlendState(this Blend blend)
        {
            switch (blend)
            {
                case Blend.Normal:
                    return global::RenderingLibrary.Graphics.Renderer.NormalBlendState;
                case Blend.Additive:
                    return BlendState.Additive;
                case Blend.Replace:
                    return BlendState.Opaque;
                case Blend.SubtractAlpha:
                    return BlendState.SubtractAlpha;
                case Blend.ReplaceAlpha:
                    return BlendState.ReplaceAlpha;
                case Blend.MinAlpha:
                    return BlendState.MinAlpha;
            }
            return BlendState.NonPremultiplied;
        }
#endif

#if MONOGAME || KNI || SKIA || FNA || RAYLIB

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
            else if (blendState == BlendState.SubtractAlpha)
            {
                return Blend.SubtractAlpha;
            }
            else if(blendState == BlendState.ReplaceAlpha)
            {
                return Blend.ReplaceAlpha;
            }
            else if(blendState == BlendState.MinAlpha)
            {
                return Blend.MinAlpha;
            }
            else
            {
                return Blend.Normal;
            }

        }
#endif 
    }
}
