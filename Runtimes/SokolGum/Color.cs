namespace SokolGum;

/// <summary>
/// Simple RGBA8 color. We define our own instead of taking a dep on System.Drawing
/// (which is restricted on some .NET targets) or Raylib.
///
/// Lives in the root <c>SokolGum</c> namespace. Top-level infrastructure
/// types (SystemManagers, ContentLoader, FontAtlas) live there too; the
/// renderables in <c>Gum.Renderables</c> import it explicitly.
/// </summary>
public struct Color
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Red = new(255, 0, 0);
    public static readonly Color Green = new(0, 255, 0);
    public static readonly Color Blue = new(0, 0, 255);

    /// <summary>
    /// Fully transparent black (0, 0, 0, 0) — the additive identity. For
    /// sprite tinting you usually want (255, 255, 255, 0) or straight
    /// <see cref="White"/> with <c>Alpha = 0</c> instead, so the RGB
    /// channels don't zero out the sampled texture.
    /// </summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);
}
