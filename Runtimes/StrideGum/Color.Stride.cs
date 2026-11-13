#if GUM_COLOR_OPT_IN
namespace Gum.XnaCompat;
#else
namespace Gum;
#endif

public partial struct Color
{
    /// <summary>Converts to Stride's <c>Color</c>, so a Gum color can be passed to Stride calls.</summary>
    public static implicit operator Stride.Core.Mathematics.Color(Color color) =>
        new Stride.Core.Mathematics.Color(color.R, color.G, color.B, color.A);

    /// <summary>Converts from Stride's <c>Color</c>.</summary>
    public static implicit operator Color(Stride.Core.Mathematics.Color color) =>
        new Color(color.R, color.G, color.B, color.A);
}
