

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
        // rgb = max(dest - src * srcAlpha, 0); dest alpha is kept.
        Subtract
    }
}
