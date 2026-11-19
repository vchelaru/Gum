using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Gum.Wireframe;

/// <summary>
/// SPIKE: a built-in, data-only per-letter effect parsed from a Godot-style tag such as
/// <c>[wave amp=50 freq=5]</c>. The math is platform-neutral so every runtime's Text can call it.
/// Defaults and math follow Godot 4's RichTextLabel (scene/gui/rich_text_label.cpp).
/// </summary>
public sealed class BbCodeTextEffect
{
    /// <summary>The tag names that take Godot-style space-separated options.</summary>
    public static readonly HashSet<string> EffectTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "wave",
        "shake",
    };

    /// <summary>
    /// The clock effects read, in seconds. Wall time by default so the tool and a game animate with
    /// no wiring; a game can point it at its own game time.
    /// </summary>
    public static Func<double> TimeSeconds { get; set; } = DefaultTimeSeconds;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    // SPIKE 2: the game clock once a GumService has updated (pausing the game freezes the effect),
    // otherwise wall time (the tool canvas, or anything with no IGumService.Update).
    public static double DefaultTimeSeconds()
    {
#if !FRB
        float? gameTime = global::RenderingLibrary.IGumService.Default?.GameTime;
        if (gameTime != null)
        {
            return gameTime.Value;
        }
#endif
        return Clock.Elapsed.TotalSeconds;
    }

    public string Name { get; private set; } = string.Empty;

    // wave
    public float Amplitude = 20f;
    public float Frequency = 5f;

    // shake
    public float Rate = 20f;
    public int Level = 5;

    /// <summary>Godot's <c>connected</c> option; parsed but not applied (Gum has no ligature data).</summary>
    public bool Connected = true;

    /// <summary>
    /// Creates the effect for a tag, or null when <paramref name="tagName"/> is not an effect tag.
    /// <paramref name="options"/> is the text after the tag name, such as <c>amp=50 freq=5</c>.
    /// Unknown or malformed options keep the default, like Godot.
    /// </summary>
    public static BbCodeTextEffect? Create(string tagName, string? options)
    {
        if (!EffectTagNames.Contains(tagName))
        {
            return null;
        }

        var effect = new BbCodeTextEffect { Name = tagName.ToLowerInvariant() };
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

            string key = pair.Substring(0, equals);
            string value = pair.Substring(equals + 1).Trim('"');
            bool isFloat = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number);
            if (!isFloat)
            {
                continue;
            }

            switch (effect.Name)
            {
                case "wave":
                    if (key == "amp") effect.Amplitude = number;
                    else if (key == "freq") effect.Frequency = number;
                    else if (key == "connected") effect.Connected = number != 0;
                    break;
                case "shake":
                    if (key == "rate") effect.Rate = number;
                    else if (key == "level") effect.Level = (int)number;
                    else if (key == "connected") effect.Connected = number != 0;
                    break;
            }
        }

        return effect;
    }

    /// <summary>
    /// The letter's offset at <paramref name="timeSeconds"/>. <paramref name="letterX"/> is the letter's x in
    /// the text's own space (Godot phases the wave by x / 50); <paramref name="letterIndex"/> seeds the shake.
    /// </summary>
    public void GetOffset(double timeSeconds, float letterX, int letterIndex, out float xOffset, out float yOffset)
    {
        xOffset = 0;
        yOffset = 0;
        switch (Name)
        {
            case "wave":
                // Godot: sin(freq * t + x / 50) * (amp / 10), vertical only.
                yOffset = (float)(Math.Sin(Frequency * timeSeconds + letterX / 50.0) * (Amplitude / 10.0));
                break;
            case "shake":
                {
                    if (Rate <= 0)
                    {
                        break;
                    }
                    // Godot re-rolls a random phase every 1 / rate seconds and lerps from the previous
                    // phase over the first 0.5 / rate. Here the "random" phase is a hash of the cycle and
                    // the letter, so it is deterministic and needs no per-frame state.
                    double cycles = timeSeconds * Rate;
                    long cycle = (long)Math.Floor(cycles);
                    double nTime = Math.Min((cycles - cycle) * 2.0, 1.0);
                    double previous = HashToAngle(cycle - 1, letterIndex);
                    double current = HashToAngle(cycle, letterIndex);
                    double scale = Level / 10.0;
                    xOffset = (float)(Lerp(Math.Sin(previous), Math.Sin(current), nTime) * scale);
                    yOffset = (float)(Lerp(Math.Cos(previous), Math.Cos(current), nTime) * scale);
                }
                break;
        }
    }

    private static double Lerp(double from, double to, double amount) => from + ((to - from) * amount);

    private static double HashToAngle(long cycle, int letterIndex)
    {
        unchecked
        {
            ulong hash = (ulong)cycle * 0x9E3779B97F4A7C15UL ^ (ulong)letterIndex * 0xBF58476D1CE4E5B9UL;
            hash ^= hash >> 31;
            hash *= 0x94D049BB133111EBUL;
            hash ^= hash >> 29;
            return (hash % 2147483647UL) / 2147483647.0 * 2.0 * Math.PI;
        }
    }
}
