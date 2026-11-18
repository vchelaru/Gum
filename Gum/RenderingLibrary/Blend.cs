

namespace Gum.RenderingLibrary
{
    public enum Blend
    {
        Normal,
        Additive,
        Replace,
        SubtractAlpha,
        ReplaceAlpha,
        MinAlpha,
        // #8263 spike: rgb = max(dst - src, 0), dst alpha kept. Wired on Skia only.
        Subtract
    }
}
