#nullable enable

using SkiaSharp;
using System;

namespace Gum;

/// <summary>
/// An XNA/MonoGame-compatible color that converts implicitly to and from <see cref="SKColor"/>, so code
/// written against MonoGame's <c>Color</c> (<c>Color.Red</c>, <c>new Color(255, 0, 0)</c>) compiles on
/// this host. Compiled into the BRUTAL, Silk.NET, KNI Skia and Stride hosts only (not SkiaGum). KNI and
/// Stride have their own <c>Color</c>, so those hosts add the conversion to it in a partial and their
/// users alias this type (<c>using Color = Gum.Color;</c>) instead of importing it by namespace alone.
/// </summary>
public partial struct Color : IEquatable<Color>
{
    private byte _r;
    private byte _g;
    private byte _b;
    private byte _a;

    /// <summary>Red channel, 0 to 255.</summary>
    public byte R { get => _r; set => _r = value; }

    /// <summary>Green channel, 0 to 255.</summary>
    public byte G { get => _g; set => _g = value; }

    /// <summary>Blue channel, 0 to 255.</summary>
    public byte B { get => _b; set => _b = value; }

    /// <summary>Alpha channel, 0 to 255.</summary>
    public byte A { get => _a; set => _a = value; }

    /// <summary>Creates an opaque color from 0-255 channels, clamping each to that range.</summary>
    public Color(int r, int g, int b) : this(r, g, b, 255)
    {
    }

    /// <summary>Creates a color from 0-255 channels, clamping each to that range.</summary>
    public Color(int r, int g, int b, int alpha)
    {
        _r = ClampToByte(r);
        _g = ClampToByte(g);
        _b = ClampToByte(b);
        _a = ClampToByte(alpha);
    }

    /// <summary>Creates a color from 0-255 channels.</summary>
    public Color(byte r, byte g, byte b, byte alpha)
    {
        _r = r;
        _g = g;
        _b = b;
        _a = alpha;
    }

    /// <summary>Creates an opaque color from 0-1 channels, truncating each to 0-255 and clamping.</summary>
    public Color(float r, float g, float b) : this((int)(r * 255), (int)(g * 255), (int)(b * 255))
    {
    }

    /// <summary>Creates a color from 0-1 channels, truncating each to 0-255 and clamping.</summary>
    public Color(float r, float g, float b, float alpha)
        : this((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(alpha * 255))
    {
    }

    /// <summary>Premultiplies 0-255 channels by alpha and returns the result.</summary>
    public static Color FromNonPremultiplied(int r, int g, int b, int a) =>
        new Color(r * a / 255, g * a / 255, b * a / 255, a);

    /// <summary>Scales every channel, alpha included, by <paramref name="scale"/>.</summary>
    public static Color operator *(Color value, float scale) =>
        new Color((int)(value.R * scale), (int)(value.G * scale), (int)(value.B * scale), (int)(value.A * scale));

    /// <summary>True when every channel matches.</summary>
    public static bool operator ==(Color a, Color b) => a.Equals(b);

    /// <summary>True when any channel differs.</summary>
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    /// <summary>Compares with an <see cref="SKColor"/> channel by channel.</summary>
    public static bool operator ==(Color a, SKColor b) => a.Equals((Color)b);

    /// <summary>Compares with an <see cref="SKColor"/> channel by channel.</summary>
    public static bool operator !=(Color a, SKColor b) => !a.Equals((Color)b);

    /// <summary>Compares with an <see cref="SKColor"/> channel by channel.</summary>
    public static bool operator ==(SKColor a, Color b) => b.Equals((Color)a);

    /// <summary>Compares with an <see cref="SKColor"/> channel by channel.</summary>
    public static bool operator !=(SKColor a, Color b) => !b.Equals((Color)a);

    /// <summary>Converts to the <see cref="SKColor"/> that Skia visuals use.</summary>
    public static implicit operator SKColor(Color color) => new SKColor(color.R, color.G, color.B, color.A);

    /// <summary>Converts from an <see cref="SKColor"/>, such as a Skia visual's color.</summary>
    public static implicit operator Color(SKColor color) => new Color(color.Red, color.Green, color.Blue, color.Alpha);

    /// <inheritdoc/>
    public bool Equals(Color other) => _r == other._r && _g == other._g && _b == other._b && _a == other._a;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (_a << 24) | (_b << 16) | (_g << 8) | _r;

    /// <summary>Formats as <c>{R:r G:g B:b A:a}</c>, matching MonoGame.</summary>
    public override string ToString() => $"{{R:{R} G:{G} B:{B} A:{A}}}";

    private static byte ClampToByte(int value) => (byte)(value < 0 ? 0 : value > 255 ? 255 : value);

    // Named colors: same names and values as MonoGame's Microsoft.Xna.Framework.Color.

    /// <summary>Transparent (R:0 G:0 B:0 A:0).</summary>
    public static Color Transparent { get; } = new Color(0, 0, 0, 0);
    /// <summary>AliceBlue (R:240 G:248 B:255 A:255).</summary>
    public static Color AliceBlue { get; } = new Color(240, 248, 255);
    /// <summary>AntiqueWhite (R:250 G:235 B:215 A:255).</summary>
    public static Color AntiqueWhite { get; } = new Color(250, 235, 215);
    /// <summary>Aqua (R:0 G:255 B:255 A:255).</summary>
    public static Color Aqua { get; } = new Color(0, 255, 255);
    /// <summary>Aquamarine (R:127 G:255 B:212 A:255).</summary>
    public static Color Aquamarine { get; } = new Color(127, 255, 212);
    /// <summary>Azure (R:240 G:255 B:255 A:255).</summary>
    public static Color Azure { get; } = new Color(240, 255, 255);
    /// <summary>Beige (R:245 G:245 B:220 A:255).</summary>
    public static Color Beige { get; } = new Color(245, 245, 220);
    /// <summary>Bisque (R:255 G:228 B:196 A:255).</summary>
    public static Color Bisque { get; } = new Color(255, 228, 196);
    /// <summary>Black (R:0 G:0 B:0 A:255).</summary>
    public static Color Black { get; } = new Color(0, 0, 0);
    /// <summary>BlanchedAlmond (R:255 G:235 B:205 A:255).</summary>
    public static Color BlanchedAlmond { get; } = new Color(255, 235, 205);
    /// <summary>Blue (R:0 G:0 B:255 A:255).</summary>
    public static Color Blue { get; } = new Color(0, 0, 255);
    /// <summary>BlueViolet (R:138 G:43 B:226 A:255).</summary>
    public static Color BlueViolet { get; } = new Color(138, 43, 226);
    /// <summary>Brown (R:165 G:42 B:42 A:255).</summary>
    public static Color Brown { get; } = new Color(165, 42, 42);
    /// <summary>BurlyWood (R:222 G:184 B:135 A:255).</summary>
    public static Color BurlyWood { get; } = new Color(222, 184, 135);
    /// <summary>CadetBlue (R:95 G:158 B:160 A:255).</summary>
    public static Color CadetBlue { get; } = new Color(95, 158, 160);
    /// <summary>Chartreuse (R:127 G:255 B:0 A:255).</summary>
    public static Color Chartreuse { get; } = new Color(127, 255, 0);
    /// <summary>Chocolate (R:210 G:105 B:30 A:255).</summary>
    public static Color Chocolate { get; } = new Color(210, 105, 30);
    /// <summary>Coral (R:255 G:127 B:80 A:255).</summary>
    public static Color Coral { get; } = new Color(255, 127, 80);
    /// <summary>CornflowerBlue (R:100 G:149 B:237 A:255).</summary>
    public static Color CornflowerBlue { get; } = new Color(100, 149, 237);
    /// <summary>Cornsilk (R:255 G:248 B:220 A:255).</summary>
    public static Color Cornsilk { get; } = new Color(255, 248, 220);
    /// <summary>Crimson (R:220 G:20 B:60 A:255).</summary>
    public static Color Crimson { get; } = new Color(220, 20, 60);
    /// <summary>Cyan (R:0 G:255 B:255 A:255).</summary>
    public static Color Cyan { get; } = new Color(0, 255, 255);
    /// <summary>DarkBlue (R:0 G:0 B:139 A:255).</summary>
    public static Color DarkBlue { get; } = new Color(0, 0, 139);
    /// <summary>DarkCyan (R:0 G:139 B:139 A:255).</summary>
    public static Color DarkCyan { get; } = new Color(0, 139, 139);
    /// <summary>DarkGoldenrod (R:184 G:134 B:11 A:255).</summary>
    public static Color DarkGoldenrod { get; } = new Color(184, 134, 11);
    /// <summary>DarkGray (R:169 G:169 B:169 A:255).</summary>
    public static Color DarkGray { get; } = new Color(169, 169, 169);
    /// <summary>DarkGreen (R:0 G:100 B:0 A:255).</summary>
    public static Color DarkGreen { get; } = new Color(0, 100, 0);
    /// <summary>DarkKhaki (R:189 G:183 B:107 A:255).</summary>
    public static Color DarkKhaki { get; } = new Color(189, 183, 107);
    /// <summary>DarkMagenta (R:139 G:0 B:139 A:255).</summary>
    public static Color DarkMagenta { get; } = new Color(139, 0, 139);
    /// <summary>DarkOliveGreen (R:85 G:107 B:47 A:255).</summary>
    public static Color DarkOliveGreen { get; } = new Color(85, 107, 47);
    /// <summary>DarkOrange (R:255 G:140 B:0 A:255).</summary>
    public static Color DarkOrange { get; } = new Color(255, 140, 0);
    /// <summary>DarkOrchid (R:153 G:50 B:204 A:255).</summary>
    public static Color DarkOrchid { get; } = new Color(153, 50, 204);
    /// <summary>DarkRed (R:139 G:0 B:0 A:255).</summary>
    public static Color DarkRed { get; } = new Color(139, 0, 0);
    /// <summary>DarkSalmon (R:233 G:150 B:122 A:255).</summary>
    public static Color DarkSalmon { get; } = new Color(233, 150, 122);
    /// <summary>DarkSeaGreen (R:143 G:188 B:139 A:255).</summary>
    public static Color DarkSeaGreen { get; } = new Color(143, 188, 139);
    /// <summary>DarkSlateBlue (R:72 G:61 B:139 A:255).</summary>
    public static Color DarkSlateBlue { get; } = new Color(72, 61, 139);
    /// <summary>DarkSlateGray (R:47 G:79 B:79 A:255).</summary>
    public static Color DarkSlateGray { get; } = new Color(47, 79, 79);
    /// <summary>DarkTurquoise (R:0 G:206 B:209 A:255).</summary>
    public static Color DarkTurquoise { get; } = new Color(0, 206, 209);
    /// <summary>DarkViolet (R:148 G:0 B:211 A:255).</summary>
    public static Color DarkViolet { get; } = new Color(148, 0, 211);
    /// <summary>DeepPink (R:255 G:20 B:147 A:255).</summary>
    public static Color DeepPink { get; } = new Color(255, 20, 147);
    /// <summary>DeepSkyBlue (R:0 G:191 B:255 A:255).</summary>
    public static Color DeepSkyBlue { get; } = new Color(0, 191, 255);
    /// <summary>DimGray (R:105 G:105 B:105 A:255).</summary>
    public static Color DimGray { get; } = new Color(105, 105, 105);
    /// <summary>DodgerBlue (R:30 G:144 B:255 A:255).</summary>
    public static Color DodgerBlue { get; } = new Color(30, 144, 255);
    /// <summary>Firebrick (R:178 G:34 B:34 A:255).</summary>
    public static Color Firebrick { get; } = new Color(178, 34, 34);
    /// <summary>FloralWhite (R:255 G:250 B:240 A:255).</summary>
    public static Color FloralWhite { get; } = new Color(255, 250, 240);
    /// <summary>ForestGreen (R:34 G:139 B:34 A:255).</summary>
    public static Color ForestGreen { get; } = new Color(34, 139, 34);
    /// <summary>Fuchsia (R:255 G:0 B:255 A:255).</summary>
    public static Color Fuchsia { get; } = new Color(255, 0, 255);
    /// <summary>Gainsboro (R:220 G:220 B:220 A:255).</summary>
    public static Color Gainsboro { get; } = new Color(220, 220, 220);
    /// <summary>GhostWhite (R:248 G:248 B:255 A:255).</summary>
    public static Color GhostWhite { get; } = new Color(248, 248, 255);
    /// <summary>Gold (R:255 G:215 B:0 A:255).</summary>
    public static Color Gold { get; } = new Color(255, 215, 0);
    /// <summary>Goldenrod (R:218 G:165 B:32 A:255).</summary>
    public static Color Goldenrod { get; } = new Color(218, 165, 32);
    /// <summary>Gray (R:128 G:128 B:128 A:255).</summary>
    public static Color Gray { get; } = new Color(128, 128, 128);
    /// <summary>Green (R:0 G:128 B:0 A:255).</summary>
    public static Color Green { get; } = new Color(0, 128, 0);
    /// <summary>GreenYellow (R:173 G:255 B:47 A:255).</summary>
    public static Color GreenYellow { get; } = new Color(173, 255, 47);
    /// <summary>Honeydew (R:240 G:255 B:240 A:255).</summary>
    public static Color Honeydew { get; } = new Color(240, 255, 240);
    /// <summary>HotPink (R:255 G:105 B:180 A:255).</summary>
    public static Color HotPink { get; } = new Color(255, 105, 180);
    /// <summary>IndianRed (R:205 G:92 B:92 A:255).</summary>
    public static Color IndianRed { get; } = new Color(205, 92, 92);
    /// <summary>Indigo (R:75 G:0 B:130 A:255).</summary>
    public static Color Indigo { get; } = new Color(75, 0, 130);
    /// <summary>Ivory (R:255 G:255 B:240 A:255).</summary>
    public static Color Ivory { get; } = new Color(255, 255, 240);
    /// <summary>Khaki (R:240 G:230 B:140 A:255).</summary>
    public static Color Khaki { get; } = new Color(240, 230, 140);
    /// <summary>Lavender (R:230 G:230 B:250 A:255).</summary>
    public static Color Lavender { get; } = new Color(230, 230, 250);
    /// <summary>LavenderBlush (R:255 G:240 B:245 A:255).</summary>
    public static Color LavenderBlush { get; } = new Color(255, 240, 245);
    /// <summary>LawnGreen (R:124 G:252 B:0 A:255).</summary>
    public static Color LawnGreen { get; } = new Color(124, 252, 0);
    /// <summary>LemonChiffon (R:255 G:250 B:205 A:255).</summary>
    public static Color LemonChiffon { get; } = new Color(255, 250, 205);
    /// <summary>LightBlue (R:173 G:216 B:230 A:255).</summary>
    public static Color LightBlue { get; } = new Color(173, 216, 230);
    /// <summary>LightCoral (R:240 G:128 B:128 A:255).</summary>
    public static Color LightCoral { get; } = new Color(240, 128, 128);
    /// <summary>LightCyan (R:224 G:255 B:255 A:255).</summary>
    public static Color LightCyan { get; } = new Color(224, 255, 255);
    /// <summary>LightGoldenrodYellow (R:250 G:250 B:210 A:255).</summary>
    public static Color LightGoldenrodYellow { get; } = new Color(250, 250, 210);
    /// <summary>LightGray (R:211 G:211 B:211 A:255).</summary>
    public static Color LightGray { get; } = new Color(211, 211, 211);
    /// <summary>LightGreen (R:144 G:238 B:144 A:255).</summary>
    public static Color LightGreen { get; } = new Color(144, 238, 144);
    /// <summary>LightPink (R:255 G:182 B:193 A:255).</summary>
    public static Color LightPink { get; } = new Color(255, 182, 193);
    /// <summary>LightSalmon (R:255 G:160 B:122 A:255).</summary>
    public static Color LightSalmon { get; } = new Color(255, 160, 122);
    /// <summary>LightSeaGreen (R:32 G:178 B:170 A:255).</summary>
    public static Color LightSeaGreen { get; } = new Color(32, 178, 170);
    /// <summary>LightSkyBlue (R:135 G:206 B:250 A:255).</summary>
    public static Color LightSkyBlue { get; } = new Color(135, 206, 250);
    /// <summary>LightSlateGray (R:119 G:136 B:153 A:255).</summary>
    public static Color LightSlateGray { get; } = new Color(119, 136, 153);
    /// <summary>LightSteelBlue (R:176 G:196 B:222 A:255).</summary>
    public static Color LightSteelBlue { get; } = new Color(176, 196, 222);
    /// <summary>LightYellow (R:255 G:255 B:224 A:255).</summary>
    public static Color LightYellow { get; } = new Color(255, 255, 224);
    /// <summary>Lime (R:0 G:255 B:0 A:255).</summary>
    public static Color Lime { get; } = new Color(0, 255, 0);
    /// <summary>LimeGreen (R:50 G:205 B:50 A:255).</summary>
    public static Color LimeGreen { get; } = new Color(50, 205, 50);
    /// <summary>Linen (R:250 G:240 B:230 A:255).</summary>
    public static Color Linen { get; } = new Color(250, 240, 230);
    /// <summary>Magenta (R:255 G:0 B:255 A:255).</summary>
    public static Color Magenta { get; } = new Color(255, 0, 255);
    /// <summary>Maroon (R:128 G:0 B:0 A:255).</summary>
    public static Color Maroon { get; } = new Color(128, 0, 0);
    /// <summary>MediumAquamarine (R:102 G:205 B:170 A:255).</summary>
    public static Color MediumAquamarine { get; } = new Color(102, 205, 170);
    /// <summary>MediumBlue (R:0 G:0 B:205 A:255).</summary>
    public static Color MediumBlue { get; } = new Color(0, 0, 205);
    /// <summary>MediumOrchid (R:186 G:85 B:211 A:255).</summary>
    public static Color MediumOrchid { get; } = new Color(186, 85, 211);
    /// <summary>MediumPurple (R:147 G:112 B:219 A:255).</summary>
    public static Color MediumPurple { get; } = new Color(147, 112, 219);
    /// <summary>MediumSeaGreen (R:60 G:179 B:113 A:255).</summary>
    public static Color MediumSeaGreen { get; } = new Color(60, 179, 113);
    /// <summary>MediumSlateBlue (R:123 G:104 B:238 A:255).</summary>
    public static Color MediumSlateBlue { get; } = new Color(123, 104, 238);
    /// <summary>MediumSpringGreen (R:0 G:250 B:154 A:255).</summary>
    public static Color MediumSpringGreen { get; } = new Color(0, 250, 154);
    /// <summary>MediumTurquoise (R:72 G:209 B:204 A:255).</summary>
    public static Color MediumTurquoise { get; } = new Color(72, 209, 204);
    /// <summary>MediumVioletRed (R:199 G:21 B:133 A:255).</summary>
    public static Color MediumVioletRed { get; } = new Color(199, 21, 133);
    /// <summary>MidnightBlue (R:25 G:25 B:112 A:255).</summary>
    public static Color MidnightBlue { get; } = new Color(25, 25, 112);
    /// <summary>MintCream (R:245 G:255 B:250 A:255).</summary>
    public static Color MintCream { get; } = new Color(245, 255, 250);
    /// <summary>MistyRose (R:255 G:228 B:225 A:255).</summary>
    public static Color MistyRose { get; } = new Color(255, 228, 225);
    /// <summary>Moccasin (R:255 G:228 B:181 A:255).</summary>
    public static Color Moccasin { get; } = new Color(255, 228, 181);
    /// <summary>MonoGameOrange (R:231 G:60 B:0 A:255).</summary>
    public static Color MonoGameOrange { get; } = new Color(231, 60, 0);
    /// <summary>NavajoWhite (R:255 G:222 B:173 A:255).</summary>
    public static Color NavajoWhite { get; } = new Color(255, 222, 173);
    /// <summary>Navy (R:0 G:0 B:128 A:255).</summary>
    public static Color Navy { get; } = new Color(0, 0, 128);
    /// <summary>OldLace (R:253 G:245 B:230 A:255).</summary>
    public static Color OldLace { get; } = new Color(253, 245, 230);
    /// <summary>Olive (R:128 G:128 B:0 A:255).</summary>
    public static Color Olive { get; } = new Color(128, 128, 0);
    /// <summary>OliveDrab (R:107 G:142 B:35 A:255).</summary>
    public static Color OliveDrab { get; } = new Color(107, 142, 35);
    /// <summary>Orange (R:255 G:165 B:0 A:255).</summary>
    public static Color Orange { get; } = new Color(255, 165, 0);
    /// <summary>OrangeRed (R:255 G:69 B:0 A:255).</summary>
    public static Color OrangeRed { get; } = new Color(255, 69, 0);
    /// <summary>Orchid (R:218 G:112 B:214 A:255).</summary>
    public static Color Orchid { get; } = new Color(218, 112, 214);
    /// <summary>PaleGoldenrod (R:238 G:232 B:170 A:255).</summary>
    public static Color PaleGoldenrod { get; } = new Color(238, 232, 170);
    /// <summary>PaleGreen (R:152 G:251 B:152 A:255).</summary>
    public static Color PaleGreen { get; } = new Color(152, 251, 152);
    /// <summary>PaleTurquoise (R:175 G:238 B:238 A:255).</summary>
    public static Color PaleTurquoise { get; } = new Color(175, 238, 238);
    /// <summary>PaleVioletRed (R:219 G:112 B:147 A:255).</summary>
    public static Color PaleVioletRed { get; } = new Color(219, 112, 147);
    /// <summary>PapayaWhip (R:255 G:239 B:213 A:255).</summary>
    public static Color PapayaWhip { get; } = new Color(255, 239, 213);
    /// <summary>PeachPuff (R:255 G:218 B:185 A:255).</summary>
    public static Color PeachPuff { get; } = new Color(255, 218, 185);
    /// <summary>Peru (R:205 G:133 B:63 A:255).</summary>
    public static Color Peru { get; } = new Color(205, 133, 63);
    /// <summary>Pink (R:255 G:192 B:203 A:255).</summary>
    public static Color Pink { get; } = new Color(255, 192, 203);
    /// <summary>Plum (R:221 G:160 B:221 A:255).</summary>
    public static Color Plum { get; } = new Color(221, 160, 221);
    /// <summary>PowderBlue (R:176 G:224 B:230 A:255).</summary>
    public static Color PowderBlue { get; } = new Color(176, 224, 230);
    /// <summary>Purple (R:128 G:0 B:128 A:255).</summary>
    public static Color Purple { get; } = new Color(128, 0, 128);
    /// <summary>Red (R:255 G:0 B:0 A:255).</summary>
    public static Color Red { get; } = new Color(255, 0, 0);
    /// <summary>RosyBrown (R:188 G:143 B:143 A:255).</summary>
    public static Color RosyBrown { get; } = new Color(188, 143, 143);
    /// <summary>RoyalBlue (R:65 G:105 B:225 A:255).</summary>
    public static Color RoyalBlue { get; } = new Color(65, 105, 225);
    /// <summary>SaddleBrown (R:139 G:69 B:19 A:255).</summary>
    public static Color SaddleBrown { get; } = new Color(139, 69, 19);
    /// <summary>Salmon (R:250 G:128 B:114 A:255).</summary>
    public static Color Salmon { get; } = new Color(250, 128, 114);
    /// <summary>SandyBrown (R:244 G:164 B:96 A:255).</summary>
    public static Color SandyBrown { get; } = new Color(244, 164, 96);
    /// <summary>SeaGreen (R:46 G:139 B:87 A:255).</summary>
    public static Color SeaGreen { get; } = new Color(46, 139, 87);
    /// <summary>SeaShell (R:255 G:245 B:238 A:255).</summary>
    public static Color SeaShell { get; } = new Color(255, 245, 238);
    /// <summary>Sienna (R:160 G:82 B:45 A:255).</summary>
    public static Color Sienna { get; } = new Color(160, 82, 45);
    /// <summary>Silver (R:192 G:192 B:192 A:255).</summary>
    public static Color Silver { get; } = new Color(192, 192, 192);
    /// <summary>SkyBlue (R:135 G:206 B:235 A:255).</summary>
    public static Color SkyBlue { get; } = new Color(135, 206, 235);
    /// <summary>SlateBlue (R:106 G:90 B:205 A:255).</summary>
    public static Color SlateBlue { get; } = new Color(106, 90, 205);
    /// <summary>SlateGray (R:112 G:128 B:144 A:255).</summary>
    public static Color SlateGray { get; } = new Color(112, 128, 144);
    /// <summary>Snow (R:255 G:250 B:250 A:255).</summary>
    public static Color Snow { get; } = new Color(255, 250, 250);
    /// <summary>SpringGreen (R:0 G:255 B:127 A:255).</summary>
    public static Color SpringGreen { get; } = new Color(0, 255, 127);
    /// <summary>SteelBlue (R:70 G:130 B:180 A:255).</summary>
    public static Color SteelBlue { get; } = new Color(70, 130, 180);
    /// <summary>Tan (R:210 G:180 B:140 A:255).</summary>
    public static Color Tan { get; } = new Color(210, 180, 140);
    /// <summary>Teal (R:0 G:128 B:128 A:255).</summary>
    public static Color Teal { get; } = new Color(0, 128, 128);
    /// <summary>Thistle (R:216 G:191 B:216 A:255).</summary>
    public static Color Thistle { get; } = new Color(216, 191, 216);
    /// <summary>Tomato (R:255 G:99 B:71 A:255).</summary>
    public static Color Tomato { get; } = new Color(255, 99, 71);
    /// <summary>Turquoise (R:64 G:224 B:208 A:255).</summary>
    public static Color Turquoise { get; } = new Color(64, 224, 208);
    /// <summary>Violet (R:238 G:130 B:238 A:255).</summary>
    public static Color Violet { get; } = new Color(238, 130, 238);
    /// <summary>Wheat (R:245 G:222 B:179 A:255).</summary>
    public static Color Wheat { get; } = new Color(245, 222, 179);
    /// <summary>White (R:255 G:255 B:255 A:255).</summary>
    public static Color White { get; } = new Color(255, 255, 255);
    /// <summary>WhiteSmoke (R:245 G:245 B:245 A:255).</summary>
    public static Color WhiteSmoke { get; } = new Color(245, 245, 245);
    /// <summary>Yellow (R:255 G:255 B:0 A:255).</summary>
    public static Color Yellow { get; } = new Color(255, 255, 0);
    /// <summary>YellowGreen (R:154 G:205 B:50 A:255).</summary>
    public static Color YellowGreen { get; } = new Color(154, 205, 50);
}
