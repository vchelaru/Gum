#if GUM_COLOR_OPT_IN
namespace Gum.XnaCompat;
#else
namespace Gum;
#endif

public partial struct Color
{
    /// <summary>Converts to KNI's <c>Color</c>, so a Gum color can be passed to KNI calls such as <c>GraphicsDevice.Clear</c>.</summary>
    public static implicit operator Microsoft.Xna.Framework.Color(Color color) =>
        new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);

    /// <summary>Converts from KNI's <c>Color</c>.</summary>
    public static implicit operator Color(Microsoft.Xna.Framework.Color color) =>
        new Color(color.R, color.G, color.B, color.A);
}
