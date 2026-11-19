using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Gum.Wireframe;

/// <summary>The built-in BBCode text effects, named after Godot 4's RichTextLabel effects.</summary>
internal enum BbCodeTextEffectKind
{
    Wave,
    Shake,
    Tornado,
    Fade,
    Rainbow,
    Pulse,
}

/// <summary>
/// A built-in, data-only per-letter effect parsed from a tag such as <c>[Wave amp=50 freq=5]</c>. The parameter
/// names, defaults and math follow Godot 4's RichTextLabel source (scene/gui/rich_text_label.cpp), so a value
/// copied from a Godot project looks the same. Every runtime calls this one implementation, so a letter at the
/// same position and time gets the same offset and color everywhere.
/// </summary>
internal sealed class BbCodeTextEffect
{
    internal static HashSet<string> EffectTagSet => _effectTagNames;

    private static readonly HashSet<string> _effectTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(BbCodeTextEffectKind.Wave),
        nameof(BbCodeTextEffectKind.Shake),
        nameof(BbCodeTextEffectKind.Tornado),
        nameof(BbCodeTextEffectKind.Fade),
        nameof(BbCodeTextEffectKind.Rainbow),
        nameof(BbCodeTextEffectKind.Pulse),
    };

    /// <summary>The effect tag names in PascalCase. Tags match them ignoring case.</summary>
    public static IReadOnlyCollection<string> EffectTagNames => _effectTagNames;

    /// <summary>Whether <paramref name="tagName"/> names a built-in effect, ignoring case.</summary>
    public static bool IsEffectTagName(string tagName) => _effectTagNames.Contains(tagName);

    /// <summary>
    /// Whether effect tags are recognized: always when no project is loaded (text set from code), otherwise only
    /// when the loaded project is at <see cref="Gum.DataTypes.GumProjectSave.GumxVersions.BbCodeTextEffects"/> or
    /// later. Below it, effect tags stay literal text as they did before the effects existed.
    /// </summary>
    public static bool AreEnabled
    {
        get
        {
            Gum.DataTypes.GumProjectSave? project = Gum.Managers.ObjectFinder.Self.GumProjectSave;
            return project == null || project.SupportsBbCodeTextEffects;
        }
    }

    /// <summary>The clock effects read, in seconds. Tests replace it; otherwise <see cref="DefaultTimeSeconds"/>.</summary>
    internal static Func<double> TimeSeconds { get; set; } = DefaultTimeSeconds;

    private static readonly Stopwatch _wallClock = Stopwatch.StartNew();

    /// <summary>
    /// The game time of <c>IGumService.Default</c> once it has updated, so pausing the game freezes the effects;
    /// otherwise (the tool canvas, or FlatRedBall) wall-clock time.
    /// </summary>
    internal static double DefaultTimeSeconds()
    {
#if !FRB
        float? gameTime = global::RenderingLibrary.IGumService.Default?.GameTime;
        if (gameTime != null)
        {
            return gameTime.Value;
        }
#endif
        return _wallClock.Elapsed.TotalSeconds;
    }

    public BbCodeTextEffectKind Kind { get; private set; }

    /// <summary>The stripped-text index of the first letter inside the tag. Fade counts letters from here.</summary>
    public int SpanStart { get; set; }

    /// <summary>Wave <c>amp</c>. Godot default 20.</summary>
    public float Amplitude { get; private set; } = 20f;

    /// <summary><c>freq</c> of Wave (default 5), Tornado, Rainbow and Pulse (default 1).</summary>
    public float Frequency { get; private set; } = 5f;

    /// <summary>Tornado <c>radius</c>. Godot default 10.</summary>
    public float Radius { get; private set; } = 10f;

    /// <summary>Shake <c>rate</c>. Godot default 20.</summary>
    public float Rate { get; private set; } = 20f;

    /// <summary>Shake <c>level</c>. Godot default 5.</summary>
    public float Level { get; private set; } = 5f;

    /// <summary>Fade <c>start</c>, in letters from the tag's first letter. Godot default 0.</summary>
    public int Start { get; private set; } = 0;

    /// <summary>Fade <c>length</c>, in letters. Godot default 10.</summary>
    public int Length { get; private set; } = 10;

    /// <summary>Rainbow <c>sat</c>. Godot default 0.8.</summary>
    public float Saturation { get; private set; } = 0.8f;

    /// <summary>Rainbow <c>val</c>. Godot default 0.8.</summary>
    public float Value { get; private set; } = 0.8f;

    /// <summary>Rainbow <c>speed</c>. Godot default 1.</summary>
    public float Speed { get; private set; } = 1f;

    /// <summary>Pulse <c>color</c>. Godot default white at 25% alpha.</summary>
    public System.Drawing.Color PulseColor { get; private set; } = System.Drawing.Color.FromArgb(64, 255, 255, 255);

    /// <summary>Pulse <c>ease</c>. Godot default -2 (ease in and out).</summary>
    public float Ease { get; private set; } = -2f;

    /// <summary>
    /// Godot's <c>connected</c> option. Parsed and kept, but it has no effect: in Godot it only keeps the glyphs of
    /// one cluster (such as a ligature) moving together, and Gum moves one character at a time.
    /// </summary>
    public bool Connected { get; private set; } = true;

    /// <summary>True for effects that change a letter's color or alpha (Fade, Rainbow, Pulse).</summary>
    public bool ChangesColor => Kind == BbCodeTextEffectKind.Fade || Kind == BbCodeTextEffectKind.Rainbow
        || Kind == BbCodeTextEffectKind.Pulse;

    /// <summary>The farthest, in pixels, this effect moves a letter on either axis.</summary>
    public float MaxOffset => Kind switch
    {
        BbCodeTextEffectKind.Wave => Math.Abs(Amplitude) / 10f,
        BbCodeTextEffectKind.Tornado => Math.Abs(Radius),
        BbCodeTextEffectKind.Shake => Math.Abs(Level) / 10f,
        _ => 0,
    };

    /// <summary>
    /// The effect for a tag, or null when <paramref name="tagName"/> is not an effect. <paramref name="options"/>
    /// is the text after the name, such as <c>amp=50 freq=5</c>. A parameter that is unknown or does not parse
    /// keeps its default.
    /// </summary>
    public static BbCodeTextEffect? Create(string tagName, string? options)
    {
        if (!IsEffectTagName(tagName))
        {
            return null;
        }

        var effect = new BbCodeTextEffect
        {
            Kind = tagName.ToLowerInvariant() switch
            {
                "shake" => BbCodeTextEffectKind.Shake,
                "tornado" => BbCodeTextEffectKind.Tornado,
                "fade" => BbCodeTextEffectKind.Fade,
                "rainbow" => BbCodeTextEffectKind.Rainbow,
                "pulse" => BbCodeTextEffectKind.Pulse,
                _ => BbCodeTextEffectKind.Wave,
            },
        };
        // Godot's frequency defaults: Wave 5, Tornado, Rainbow and Pulse 1.
        if (effect.Kind != BbCodeTextEffectKind.Wave)
        {
            effect.Frequency = 1f;
        }

        if (string.IsNullOrEmpty(options))
        {
            return effect;
        }

        foreach (string pair in options!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = pair.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }
            effect.SetOption(pair.Substring(0, equals).ToLowerInvariant(), pair.Substring(equals + 1).Trim('"'));
        }

        return effect;
    }

    private void SetOption(string key, string value)
    {
        if (key == "connected")
        {
            if (TryParseFloat(value, out float connected))
            {
                Connected = connected != 0;
            }
            else if (bool.TryParse(value, out bool asBool))
            {
                Connected = asBool;
            }
            return;
        }

        if (Kind == BbCodeTextEffectKind.Pulse && key == "color")
        {
            System.Drawing.Color? color = BbCodeFontStacks.ParseColor(value);
            if (color != null)
            {
                PulseColor = color.Value;
            }
            return;
        }

        if (!TryParseFloat(value, out float number))
        {
            return;
        }

        switch (Kind, key)
        {
            case (BbCodeTextEffectKind.Wave, "amp"): Amplitude = number; break;
            case (BbCodeTextEffectKind.Wave, "freq"): Frequency = number; break;
            case (BbCodeTextEffectKind.Tornado, "radius"): Radius = number; break;
            case (BbCodeTextEffectKind.Tornado, "freq"): Frequency = number; break;
            case (BbCodeTextEffectKind.Shake, "rate"): Rate = number; break;
            case (BbCodeTextEffectKind.Shake, "level"): Level = number; break;
            case (BbCodeTextEffectKind.Fade, "start"): Start = (int)number; break;
            case (BbCodeTextEffectKind.Fade, "length"): Length = (int)number; break;
            case (BbCodeTextEffectKind.Rainbow, "freq"): Frequency = number; break;
            case (BbCodeTextEffectKind.Rainbow, "sat"): Saturation = number; break;
            case (BbCodeTextEffectKind.Rainbow, "val"): Value = number; break;
            case (BbCodeTextEffectKind.Rainbow, "speed"): Speed = number; break;
            case (BbCodeTextEffectKind.Pulse, "freq"): Frequency = number; break;
            case (BbCodeTextEffectKind.Pulse, "ease"): Ease = number; break;
        }
    }

    private static bool TryParseFloat(string value, out float number) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
        && !float.IsNaN(number) && !float.IsInfinity(number);

    /// <summary>Adds the effect's offset and applies its color for one letter (see <see cref="AddOffset"/> and
    /// <see cref="ApplyColor"/>).</summary>
    public void Apply(double timeSeconds, float letterX, int letterIndex, ref float xOffset, ref float yOffset,
        ref byte red, ref byte green, ref byte blue, ref byte alpha)
    {
        AddOffset(timeSeconds, letterX, letterIndex, ref xOffset, ref yOffset);
        ApplyColor(timeSeconds, letterX, letterIndex, ref red, ref green, ref blue, ref alpha);
    }

    /// <summary>
    /// Adds the effect's offset for one letter at <paramref name="timeSeconds"/>. <paramref name="letterX"/> is the
    /// letter's distance in pixels from the start of its line (Godot phases by x / 50);
    /// <paramref name="letterIndex"/> is the letter's stripped-text index, which seeds the shake.
    /// </summary>
    public void AddOffset(double timeSeconds, float letterX, int letterIndex, ref float xOffset, ref float yOffset)
    {
        switch (Kind)
        {
            case BbCodeTextEffectKind.Wave:
                yOffset += (float)(Math.Sin(Frequency * timeSeconds + letterX / 50.0) * (Amplitude / 10.0));
                break;
            case BbCodeTextEffectKind.Tornado:
                {
                    double phase = Frequency * timeSeconds + letterX / 50.0;
                    xOffset += (float)(Math.Sin(phase) * Radius);
                    yOffset += (float)(Math.Cos(phase) * Radius);
                }
                break;
            case BbCodeTextEffectKind.Shake:
                if (Rate > 0)
                {
                    // Godot re-rolls a random angle every 1 / rate seconds and moves from the previous angle to the
                    // new one over the first half of the period. The angle here is a hash of the period and the
                    // letter, so it needs no per-frame state and every runtime gets the same one.
                    double periods = timeSeconds * Rate;
                    long period = (long)Math.Floor(periods);
                    double amount = Math.Min((periods - period) * 2.0, 1.0);
                    double previous = HashToAngle(period - 1, letterIndex);
                    double current = HashToAngle(period, letterIndex);
                    double scale = Level / 10.0;
                    xOffset += (float)(Lerp(Math.Sin(previous), Math.Sin(current), amount) * scale);
                    yOffset += (float)(Lerp(Math.Cos(previous), Math.Cos(current), amount) * scale);
                }
                break;
        }
    }

    /// <summary>
    /// Applies the effect's color to one letter whose color is already resolved (base color, then <c>[Color]</c>,
    /// <c>[Alpha]</c> and so on). Rainbow replaces red, green and blue and keeps alpha; Pulse blends toward the
    /// color multiplied by its <c>color</c>; Fade multiplies alpha so it composes with <c>[Alpha]</c>.
    /// </summary>
    public void ApplyColor(double timeSeconds, float letterX, int letterIndex, ref byte red, ref byte green, ref byte blue, ref byte alpha)
    {
        switch (Kind)
        {
            case BbCodeTextEffectKind.Fade:
                if (Length > 0)
                {
                    double visibility = 1.0 - (double)(letterIndex - SpanStart - Start) / Length;
                    alpha = ToByte(alpha / 255.0 * Math.Max(0, Math.Min(1, visibility)));
                }
                break;
            case BbCodeTextEffectKind.Rainbow:
                {
                    // Godot: Color::from_ok_hsv(MAX(freq, 0) * ABS(t * speed + x / 50), sat, val), clamped.
                    double hue = Math.Max(Frequency, 0) * Math.Abs(timeSeconds * Speed + letterX / 50.0);
                    OkHsvToSrgb(hue, Saturation, Value, out double r, out double g, out double b);
                    red = ToByte(r);
                    green = ToByte(g);
                    blue = ToByte(b);
                }
                break;
            case BbCodeTextEffectKind.Pulse:
                if (Frequency > 0)
                {
                    double amount = GodotEase(PingPong(timeSeconds, 1.0 / Frequency) * Frequency, Ease);
                    red = ToByte(Lerp(red / 255.0, red / 255.0 * PulseColor.R / 255.0, amount));
                    green = ToByte(Lerp(green / 255.0, green / 255.0 * PulseColor.G / 255.0, amount));
                    blue = ToByte(Lerp(blue / 255.0, blue / 255.0 * PulseColor.B / 255.0, amount));
                    alpha = ToByte(Lerp(alpha / 255.0, alpha / 255.0 * PulseColor.A / 255.0, amount));
                }
                break;
        }
    }

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Max(0, Math.Min(1, unit)) * 255);

    private static double Lerp(double from, double to, double amount) => from + ((to - from) * amount);

    // Godot's Math::pingpong.
    private static double PingPong(double value, double length)
    {
        double scaled = (value - length) / (length * 2.0);
        double fraction = scaled - Math.Floor(scaled);
        return Math.Abs(fraction * length * 2.0 - length);
    }

    // Godot's Math::ease.
    private static double GodotEase(double x, double curve)
    {
        x = Math.Max(0, Math.Min(1, x));
        if (curve > 0)
        {
            return curve < 1 ? 1.0 - Math.Pow(1.0 - x, 1.0 / curve) : Math.Pow(x, curve);
        }
        if (curve < 0)
        {
            return x < 0.5
                ? Math.Pow(x * 2.0, -curve) * 0.5
                : (1.0 - Math.Pow(1.0 - (x - 0.5) * 2.0, -curve)) * 0.5 + 0.5;
        }
        return 0;
    }

    // Okhsv to sRGB, ported from Björn Ottosson's ok_color.h (MIT license), the file Godot vendors as
    // thirdparty/misc/ok_color.h and calls from Color::from_ok_hsv. Results are unclamped; ToByte clamps.
    private static void OkHsvToSrgb(double h, double s, double v, out double r, out double g, out double b)
    {
        double a_ = Math.Cos(2 * Math.PI * h);
        double b_ = Math.Sin(2 * Math.PI * h);

        FindCusp(a_, b_, out double cuspL, out double cuspC);
        double sMax = cuspC / cuspL;
        double tMax = cuspC / (1 - cuspL);
        const double s0 = 0.5;
        double k = 1 - s0 / sMax;

        double lV = 1 - s * s0 / (s0 + tMax - tMax * k * s);
        double cV = s * tMax * s0 / (s0 + tMax - tMax * k * s);
        double l = v * lV;
        double c = v * cV;
        if (l <= 0 || lV <= 0)
        {
            r = g = b = 0;
            return;
        }

        double lVt = ToeInverse(lV);
        double cVt = cV * lVt / lV;
        double lNew = ToeInverse(l);
        c = c * lNew / l;
        l = lNew;

        OklabToLinearSrgb(lVt, a_ * cVt, b_ * cVt, out double scaleR, out double scaleG, out double scaleB);
        double scaleL = Math.Pow(1.0 / Math.Max(Math.Max(scaleR, scaleG), Math.Max(scaleB, 0.0)), 1.0 / 3.0);
        l *= scaleL;
        c *= scaleL;

        OklabToLinearSrgb(l, c * a_, c * b_, out r, out g, out b);
        r = SrgbTransfer(r);
        g = SrgbTransfer(g);
        b = SrgbTransfer(b);
    }

    private static double ToeInverse(double x)
    {
        const double k1 = 0.206;
        const double k2 = 0.03;
        const double k3 = (1 + k1) / (1 + k2);
        return (x * x + k1 * x) / (k3 * (x + k2));
    }

    private static double SrgbTransfer(double a) =>
        a <= 0.0031308 ? 12.92 * a : 1.055 * Math.Pow(a, 1 / 2.4) - 0.055;

    private static void OklabToLinearSrgb(double lightness, double a, double b, out double r, out double g, out double bl)
    {
        double l = Cube(lightness + 0.3963377774 * a + 0.2158037573 * b);
        double m = Cube(lightness - 0.1055613458 * a - 0.0638541728 * b);
        double s = Cube(lightness - 0.0894841775 * a - 1.2914855480 * b);
        r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        bl = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;
    }

    private static double Cube(double x) => x * x * x;

    private static void FindCusp(double a, double b, out double lCusp, out double cCusp)
    {
        double sCusp = ComputeMaxSaturation(a, b);
        OklabToLinearSrgb(1, sCusp * a, sCusp * b, out double r, out double g, out double bl);
        lCusp = Math.Pow(1.0 / Math.Max(Math.Max(r, g), bl), 1.0 / 3.0);
        cCusp = lCusp * sCusp;
    }

    private static double ComputeMaxSaturation(double a, double b)
    {
        double k0, k1, k2, k3, k4, wl, wm, ws;
        if (-1.88170328 * a - 0.80936493 * b > 1)
        {
            k0 = 1.19086277; k1 = 1.76576728; k2 = 0.59662641; k3 = 0.75515197; k4 = 0.56771245;
            wl = 4.0767416621; wm = -3.3077115913; ws = 0.2309699292;
        }
        else if (1.81444104 * a - 1.19445276 * b > 1)
        {
            k0 = 0.73956515; k1 = -0.45954404; k2 = 0.08285427; k3 = 0.12541070; k4 = 0.14503204;
            wl = -1.2684380046; wm = 2.6097574011; ws = -0.3413193965;
        }
        else
        {
            k0 = 1.35733652; k1 = -0.00915799; k2 = -1.15130210; k3 = -0.50559606; k4 = 0.00692167;
            wl = -0.0041960863; wm = -0.7034186147; ws = 1.7076147010;
        }

        double sat = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;

        double kL = 0.3963377774 * a + 0.2158037573 * b;
        double kM = -0.1055613458 * a - 0.0638541728 * b;
        double kS = -0.0894841775 * a - 1.2914855480 * b;

        double l_ = 1 + sat * kL;
        double m_ = 1 + sat * kM;
        double s_ = 1 + sat * kS;
        double l = l_ * l_ * l_;
        double m = m_ * m_ * m_;
        double s = s_ * s_ * s_;
        double lDs = 3 * kL * l_ * l_;
        double mDs = 3 * kM * m_ * m_;
        double sDs = 3 * kS * s_ * s_;
        double lDs2 = 6 * kL * kL * l_;
        double mDs2 = 6 * kM * kM * m_;
        double sDs2 = 6 * kS * kS * s_;
        double f = wl * l + wm * m + ws * s;
        double f1 = wl * lDs + wm * mDs + ws * sDs;
        double f2 = wl * lDs2 + wm * mDs2 + ws * sDs2;
        return sat - f * f1 / (f1 * f1 - 0.5 * f * f2);
    }

    private static double HashToAngle(long period, int letterIndex)
    {
        unchecked
        {
            ulong hash = (ulong)period * 0x9E3779B97F4A7C15UL ^ (ulong)letterIndex * 0xBF58476D1CE4E5B9UL;
            hash ^= hash >> 31;
            hash *= 0x94D049BB133111EBUL;
            hash ^= hash >> 29;
            return (hash % 2147483647UL) / 2147483647.0 * 2.0 * Math.PI;
        }
    }
}
