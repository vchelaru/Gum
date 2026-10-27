using System;
using System.Collections.Generic;
using System.Globalization;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Converts shader parameter values to and from the text saved in a Gum project, for example
/// <c>Glow=true; OutlineColor=(1.0, 1.0, 1.0, 1.0); Thickness=3.0</c>. Floats always have a decimal
/// point and ints never do, so the kind survives a round trip. Numbers always use the invariant
/// culture.
/// </summary>
public static class ShaderParameterText
{
    /// <summary>
    /// Formats <paramref name="parameters"/> as <c>Name=value</c> pairs separated by <c>"; "</c>,
    /// sorted by name so the saved text does not depend on the order values were set.
    /// </summary>
    public static string Format(ShaderParameterCollection parameters)
    {
        List<KeyValuePair<string, ShaderParameterValue>> entries = new List<KeyValuePair<string, ShaderParameterValue>>();
        foreach (KeyValuePair<string, ShaderParameterValue> pair in parameters)
        {
            entries.Add(pair);
        }
        entries.Sort((first, second) => string.CompareOrdinal(first.Key, second.Key));

        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, ShaderParameterValue> entry in entries)
        {
            parts.Add(entry.Key + "=" + FormatValue(entry.Value));
        }
        return string.Join("; ", parts);
    }

    /// <summary>Formats a single value, such as <c>3.0</c>, <c>4</c>, <c>true</c> or <c>(1.0, 2.0)</c>.</summary>
    public static string FormatValue(ShaderParameterValue value)
    {
        if (value.Kind == ShaderParameterKind.Bool)
        {
            return value.X != 0f ? "true" : "false";
        }
        if (value.Kind == ShaderParameterKind.Int)
        {
            return ((int)value.X).ToString(CultureInfo.InvariantCulture);
        }

        if (value.Count == 1)
        {
            return FormatFloat(value.X);
        }

        string text = FormatFloat(value.X) + ", " + FormatFloat(value.Y);
        if (value.Count >= 3)
        {
            text += ", " + FormatFloat(value.Z);
        }
        if (value.Count >= 4)
        {
            text += ", " + FormatFloat(value.W);
        }
        return "(" + text + ")";
    }

    /// <summary>
    /// Reads <c>Name=value</c> pairs separated by semicolons. An entry that cannot be read is skipped
    /// and described in <paramref name="errors"/>, so one typo does not drop the other parameters.
    /// </summary>
    public static ShaderParameterCollection Parse(string? text, List<string>? errors)
    {
        ShaderParameterCollection parameters = new ShaderParameterCollection();
        if (string.IsNullOrWhiteSpace(text))
        {
            return parameters;
        }

        foreach (string rawEntry in text!.Split(';'))
        {
            string entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            int equalsIndex = entry.IndexOf('=');
            if (equalsIndex <= 0)
            {
                errors?.Add("Couldn't read '" + entry + "': expected Name=value.");
                continue;
            }

            string name = entry.Substring(0, equalsIndex).Trim();
            string valueText = entry.Substring(equalsIndex + 1).Trim();
            if (TryParseValue(valueText, out ShaderParameterValue value))
            {
                parameters.Set(name, value);
            }
            else
            {
                errors?.Add("Couldn't read the value of '" + name + "': '" + valueText + "'.");
            }
        }

        return parameters;
    }

    /// <summary>
    /// Replaces <paramref name="effectOwner"/>'s shader parameters with the values in
    /// <paramref name="text"/>. Empty text leaves the container with no parameters. Returns a message
    /// naming every entry that could not be read, or null when all of them could.
    /// </summary>
    public static string? Assign(IRenderTargetRenderable effectOwner, string? text)
    {
        List<string> errors = new List<string>();
        ShaderParameterCollection parsed = Parse(text, errors);
        effectOwner.ShaderParameters = parsed.Count == 0 ? null : parsed;
        return errors.Count == 0 ? null : string.Join("\n", errors);
    }

    private static string FormatFloat(float value)
    {
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        bool hasDecimalMarker = text.IndexOf('.') >= 0 || text.IndexOf('E') >= 0 || text.IndexOf('N') >= 0;
        return hasDecimalMarker ? text : text + ".0";
    }

    private static bool TryParseValue(string text, out ShaderParameterValue value)
    {
        value = default;

        if (text == "true" || text == "false")
        {
            value = ShaderParameterValue.FromBool(text == "true");
            return true;
        }

        if (text.Length >= 2 && text[0] == '(' && text[text.Length - 1] == ')')
        {
            string[] parts = text.Substring(1, text.Length - 2).Split(',');
            float[] components = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out components[i]))
                {
                    return false;
                }
            }

            switch (components.Length)
            {
                case 2:
                    value = ShaderParameterValue.FromVector2(components[0], components[1]);
                    return true;
                case 3:
                    value = ShaderParameterValue.FromVector3(components[0], components[1], components[2]);
                    return true;
                case 4:
                    value = ShaderParameterValue.FromVector4(components[0], components[1], components[2], components[3]);
                    return true;
                default:
                    return false;
            }
        }

        bool looksLikeFloat = text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0;
        if (looksLikeFloat)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                value = ShaderParameterValue.FromFloat(floatValue);
                return true;
            }
            return false;
        }

        if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int intValue))
        {
            value = ShaderParameterValue.FromInt(intValue);
            return true;
        }
        return false;
    }
}
