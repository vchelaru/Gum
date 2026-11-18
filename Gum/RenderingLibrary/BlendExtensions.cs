using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.RenderingLibrary;

public static class BlendExtensions
{
    // BlendState resolves to Gum.BlendState via the enclosing-namespace rule (Gum.RenderingLibrary
    // is nested under Gum) on every platform, XNA-likes included, so no per-platform using/alias is
    // needed here.
    #if !NO_XNA
    public static BlendState ToBlendState(this Blend blend, bool isUsingPremultipliedAlpha = false)
    {
#if FRB
        isUsingPremultipliedAlpha = true;
#endif

        switch (blend)
        {
            case Blend.Normal:
                return global::RenderingLibrary.Graphics.Renderer.NormalBlendState;
            case Blend.Additive:
                return isUsingPremultipliedAlpha ? BlendState.AdditivePremultiplied : BlendState.Additive;
            case Blend.Replace:
                return BlendState.Opaque;
            case Blend.SubtractAlpha:
                return isUsingPremultipliedAlpha ? BlendState.SubtractAlphaPremultiplied : BlendState.SubtractAlpha;
            case Blend.ReplaceAlpha:
                return isUsingPremultipliedAlpha ? BlendState.ReplaceAlphaPremultiplied : BlendState.ReplaceAlpha;
            case Blend.MinAlpha:
                return isUsingPremultipliedAlpha ? BlendState.MinAlphaPremultiplied : BlendState.MinAlpha;
        }
        return BlendState.NonPremultiplied;
    }
#endif

#if MONOGAME || KNI || SKIA || FNA || RAYLIB

#if FRB
    // FRB uses generated code which is harder to upgrade, so we will keep this signature the same here
    public static Blend ToBlend(this BlendState? blendState)
#else
    public static Blend? ToBlend(this BlendState? blendState)
#endif
    {
        // Normal resolves to Renderer.NormalBlendState, which may be custom or AlphaBlend (a
        // premultiplied pipeline), and an element can hold a field-equal copy of it, so compare by
        // value before the presets.
        if (blendState != null && IsSameBlend(blendState, global::RenderingLibrary.Graphics.Renderer.NormalBlendState))
        {
            return Blend.Normal;
        }
        else if (blendState == BlendState.NonPremultiplied)
        {
            return Blend.Normal;
        }
        else if (blendState == BlendState.Additive || blendState == BlendState.AdditivePremultiplied)
        {
            return Blend.Additive;
        }
        else if (blendState == BlendState.Opaque)
        {
            return Blend.Replace;
        }
        else if (blendState == BlendState.SubtractAlpha || blendState == BlendState.SubtractAlphaPremultiplied)
        {
            return Blend.SubtractAlpha;
        }
        else if(blendState == BlendState.ReplaceAlpha || blendState == BlendState.ReplaceAlphaPremultiplied)
        {
            return Blend.ReplaceAlpha;
        }
        else if(blendState == BlendState.MinAlpha || blendState == BlendState.MinAlphaPremultiplied)
        {
            return Blend.MinAlpha;
        }
        else
        {
#if FRB
            return Blend.Normal;
#else
            return null;
#endif
        }

    }
#endif

    // Whether two blend states blend the same way. Blend states reach the renderer as distinct
    // instances with identical factors (an XNA round trip, a hand-built copy, a custom
    // Renderer.NormalBlendState), so decisions about which blend is in use compare by value.
    internal static bool IsSameBlend(BlendState blendState, BlendState other) =>
        blendState.ColorSourceBlend == other.ColorSourceBlend
        && blendState.ColorBlendFunction == other.ColorBlendFunction
        && blendState.ColorDestinationBlend == other.ColorDestinationBlend
        && blendState.AlphaSourceBlend == other.AlphaSourceBlend
        && blendState.AlphaBlendFunction == other.AlphaBlendFunction
        && blendState.AlphaDestinationBlend == other.AlphaDestinationBlend;
}
