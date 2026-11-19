using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Gum.Wireframe;

/// <summary>The built-in BBCode text effects, named after Godot 4's RichTextLabel effects.</summary>
public enum BbCodeTextEffectKind
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
public sealed class BbCodeTextEffect
{
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
                    FromHsv(Frequency * (timeSeconds * Speed + letterX / 50.0), Saturation, Value,
                        out double r, out double g, out double b);
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

    // Godot's Color::from_hsv. The hue wraps, so any value is valid.
    private static void FromHsv(double hue, double saturation, double value, out double r, out double g, out double b)
    {
        double h = (hue * 360.0) % 360.0;
        if (h < 0)
        {
            h += 360.0;
        }
        h /= 60.0;
        int sector = (int)Math.Floor(h);
        double f = h - sector;
        double p = value * (1 - saturation);
        double q = value * (1 - saturation * f);
        double t = value * (1 - saturation * (1 - f));
        switch (sector)
        {
            case 0: r = value; g = t; b = p; break;
            case 1: r = q; g = value; b = p; break;
            case 2: r = p; g = value; b = t; break;
            case 3: r = p; g = q; b = value; break;
            case 4: r = t; g = p; b = value; break;
            default: r = value; g = p; b = q; break;
        }
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
