using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Reads the defaults a shader declares in its own source text, such as <c>float Thickness = 1.0;</c>.
/// ShadowDusk drops these when it compiles the shader, so the compiled effect cannot tell them.
/// Understands bool, int, float and floatN (2 to 4) parameters whose initializer is a literal of their
/// own type, with or without a leading <c>uniform</c>.
/// </summary>
public static class ShaderSourceDefaults
{
    private static readonly Regex DeclarationRegex = new Regex(
        @"^\s*(?:(?:uniform|const|extern|shared|volatile)\s+)*(?<type>float[234]?|int|bool)\s+(?<rest>.+)$",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    // A name, an optional ": semantic" or ": register(...)", and an optional "= initializer". An array
    // ("A[2]") does not match, and has no row either.
    private static readonly Regex DeclaratorRegex = new Regex(
        @"^\s*(?<name>[A-Za-z_]\w*)\s*(?::[^=]*)?(?:=\s*(?<init>.*))?$",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex NumberRegex = new Regex(
        @"^[+-]?\s*(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?[fFhH]?$", RegexOptions.CultureInvariant);

    private static readonly Regex IntegerRegex = new Regex(@"^[+-]?\s*\d+$", RegexOptions.CultureInvariant);

    private static readonly Regex ConstructorRegex = new Regex(
        @"^(?<type>float[234])\s*\((?<args>.*)\)$", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>
    /// The declared default of every global parameter in <paramref name="source"/> whose initializer
    /// is a literal of its own type. Parameters with no initializer, or one that is not a plain
    /// literal, are left out. Never throws, whatever the text.
    /// </summary>
    public static IReadOnlyDictionary<string, ShaderParameterValue> Parse(string? source)
    {
        Dictionary<string, ShaderParameterValue> defaults = new Dictionary<string, ShaderParameterValue>();
        if (string.IsNullOrWhiteSpace(source))
        {
            return defaults;
        }

        foreach (string statement in GetGlobalStatements(StripDirectives(StripComments(source))))
        {
            Match declaration = DeclarationRegex.Match(statement);
            if (!declaration.Success)
            {
                continue;
            }

            string type = declaration.Groups["type"].Value;
            foreach (string declarator in SplitTopLevel(declaration.Groups["rest"].Value, ','))
            {
                Match match = DeclaratorRegex.Match(declarator);
                if (match.Success && match.Groups["init"].Success &&
                    TryParseInitializer(type, match.Groups["init"].Value.Trim(), out ShaderParameterValue value))
                {
                    defaults[match.Groups["name"].Value] = value;
                }
            }
        }
        return defaults;
    }

    private static bool TryParseInitializer(string type, string initializer, out ShaderParameterValue value)
    {
        value = default;
        switch (type)
        {
            case "bool":
                if (initializer == "true" || initializer == "1")
                {
                    value = ShaderParameterValue.FromBool(true);
                    return true;
                }
                if (initializer == "false" || initializer == "0")
                {
                    value = ShaderParameterValue.FromBool(false);
                    return true;
                }
                return false;
            case "int":
                if (IntegerRegex.IsMatch(initializer) &&
                    int.TryParse(RemoveWhitespace(initializer), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int integer))
                {
                    value = ShaderParameterValue.FromInt(integer);
                    return true;
                }
                return false;
            case "float":
                if (TryParseNumber(initializer, out float number))
                {
                    value = ShaderParameterValue.FromFloat(number);
                    return true;
                }
                return false;
            default:
                return TryParseVector(type, initializer, out value);
        }
    }

    private static bool TryParseVector(string type, string initializer, out ShaderParameterValue value)
    {
        value = default;
        Match constructor = ConstructorRegex.Match(initializer);
        if (!constructor.Success || constructor.Groups["type"].Value != type)
        {
            return false;
        }

        int count = type[type.Length - 1] - '0';
        List<string> arguments = SplitTopLevel(constructor.Groups["args"].Value, ',');
        if (arguments.Count != count && arguments.Count != 1)
        {
            return false;
        }

        float[] components = new float[4];
        for (int i = 0; i < count; i++)
        {
            // A single argument fills every component.
            if (!TryParseNumber(arguments[arguments.Count == 1 ? 0 : i], out components[i]))
            {
                return false;
            }
        }

        value = count switch
        {
            2 => ShaderParameterValue.FromVector2(components[0], components[1]),
            3 => ShaderParameterValue.FromVector3(components[0], components[1], components[2]),
            _ => ShaderParameterValue.FromVector4(components[0], components[1], components[2], components[3]),
        };
        return true;
    }

    private static bool TryParseNumber(string text, out float number)
    {
        number = 0f;
        text = text.Trim();
        if (!NumberRegex.IsMatch(text))
        {
            return false;
        }

        text = RemoveWhitespace(text);
        char last = text[text.Length - 1];
        if (last == 'f' || last == 'F' || last == 'h' || last == 'H')
        {
            text = text.Substring(0, text.Length - 1);
        }
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && float.IsFinite(number);
    }

    private static string RemoveWhitespace(string text)
    {
        StringBuilder builder = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }
        return builder.ToString();
    }

    /// <summary>Replaces every <c>//</c> and <c>/* */</c> comment with a space. An unclosed block comment runs to the end.</summary>
    private static string StripComments(string source)
    {
        StringBuilder builder = new StringBuilder(source.Length);
        int i = 0;
        while (i < source.Length)
        {
            if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n')
                {
                    i++;
                }
                builder.Append(' ');
            }
            else if (source[i] == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                int end = source.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                i = end < 0 ? source.Length : end + 2;
                builder.Append(' ');
            }
            else
            {
                builder.Append(source[i]);
                i++;
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Replaces every preprocessor line (first non-space character <c>#</c>, plus its backslash
    /// continuations) with a statement boundary. Conditions are not evaluated, so the declarations in
    /// every branch of an <c>#if</c> are read.
    /// </summary>
    private static string StripDirectives(string source)
    {
        StringBuilder builder = new StringBuilder(source.Length);
        string[] lines = source.Split('\n');
        bool continuing = false;
        foreach (string line in lines)
        {
            string trimmed = line.TrimEnd('\r');
            bool isDirective = continuing || trimmed.TrimStart().StartsWith("#", System.StringComparison.Ordinal);
            if (isDirective)
            {
                continuing = trimmed.EndsWith("\\", System.StringComparison.Ordinal);
                builder.Append(";\n");
            }
            else
            {
                builder.Append(line).Append('\n');
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// The text of each statement outside any <c>{ }</c> block, so a local variable in a function or a
    /// member of a struct is not mistaken for a parameter. What precedes a block (a function or struct
    /// header) is not a statement.
    /// </summary>
    private static IEnumerable<string> GetGlobalStatements(string source)
    {
        StringBuilder current = new StringBuilder();
        int depth = 0;
        foreach (char character in source)
        {
            if (character == '{')
            {
                if (depth == 0)
                {
                    current.Clear();
                }
                depth++;
            }
            else if (character == '}')
            {
                if (depth > 0)
                {
                    depth--;
                }
            }
            else if (depth == 0)
            {
                if (character == ';')
                {
                    yield return current.ToString();
                    current.Clear();
                }
                else
                {
                    current.Append(character);
                }
            }
        }
    }

    /// <summary>Splits on <paramref name="separator"/> where no parenthesis or bracket is open.</summary>
    private static List<string> SplitTopLevel(string text, char separator)
    {
        List<string> parts = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '(' || character == '[')
            {
                depth++;
            }
            else if ((character == ')' || character == ']') && depth > 0)
            {
                depth--;
            }
            else if (character == separator && depth == 0)
            {
                parts.Add(text.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(text.Substring(start));
        return parts;
    }
}
