using System;
using System.Globalization;
using System.Text;

namespace RenderingLibrary.Graphics;

/// <summary>
/// The canonical names of the variables that hold shader parameters (#7010): <c>Outline_Thickness</c>
/// for a scalar parameter <c>Thickness</c> of <c>Outline.fx</c>, and <c>Outline_Tint__X</c> to
/// <c>Outline_Tint__W</c> for the components of a vector parameter <c>Tint</c>. The tool, codegen, the
/// runtimes and gumcli all go through this class and never assemble these names themselves.
/// </summary>
/// <remarks>
/// <para>
/// A name has to be a valid Gum variable name, exposed name, Roslyn identifier (variable references are
/// parsed as C#) and C# identifier at once. A dot already separates an instance from its property, so
/// the underscore is the joiner: <c>stem</c> + <c>_</c> + <c>parameter</c>, plus <c>__X</c>, <c>__Y</c>,
/// <c>__Z</c> or <c>__W</c> for a vector component. The runtime recovers the parameter from the name
/// alone (it never sees the variable), so the component suffix is a double underscore: a single one would
/// misread a real scalar parameter called <c>Offset_X</c>.
/// </para>
/// <para>
/// The stem is the shader file name without directory or last extension, with every character that is
/// the tool's name rules do not accept (see <see cref="GetSanitizedStem"/>) replaced by an underscore.
/// <c>Outline.fx</c>, <c>Outline.slang</c> and <c>Outline.fs</c> share the stem <c>Outline</c> on purpose,
/// so one set of saved values follows a shader across its per-runtime file types. Kana, CJK ideographs and the like are not accepted, so a
/// shader named in them has a stem of underscores and shares it with every other such name.
/// </para>
/// <para>
/// Collisions. For one shader file, two different (parameter, component) inputs never produce the same
/// name: a parameter name may not contain a double underscore (it is rejected, see
/// <see cref="IsSupportedParameterName"/>), so a trailing <c>__X</c> can only be a component suffix and
/// the name decodes back to exactly one input. The stem is not injective: <c>My Outline.fx</c> and
/// <c>My_Outline.fx</c> share the stem <c>My_Outline</c>, and <c>S.fx</c> with parameter <c>_X</c> gives
/// the same name as <c>S_.fx</c> with parameter <c>X</c>. Names are therefore only unique within one
/// shader file name, which is why parsing a name takes the shader file.
/// </para>
/// </remarks>
public static class ShaderVariableNaming
{
    private const string EmptyStem = "Shader";
    private const string ComponentSeparator = "__";
    private const string ComponentLetters = "XYZW";

    /// <summary>
    /// The shader file name without directory or last extension, made safe to put in a variable name:
    /// every character the tool's name rules do not accept becomes an underscore (this includes letters
    /// that are neither cased nor modifiers, such as kana and CJK ideographs, so a shader named in those
    /// scripts gets a stem of underscores), a name that does not start with a letter or underscore gets
    /// an underscore in front, and a file name with nothing usable becomes <c>Shader</c>.
    /// </summary>
    /// <param name="shaderFile">A path or a name, with either kind of slash. May be null or empty.</param>
    public static string GetSanitizedStem(string? shaderFile)
    {
        if (shaderFile == null)
        {
            return EmptyStem;
        }

        int separatorIndex = shaderFile.LastIndexOfAny(new[] { '/', '\\' });
        string name = shaderFile.Substring(separatorIndex + 1);
        int dotIndex = name.LastIndexOf('.');
        if (dotIndex >= 0)
        {
            name = name.Substring(0, dotIndex);
        }

        if (name.Trim().Length == 0)
        {
            return EmptyStem;
        }

        StringBuilder builder = new StringBuilder(name.Length + 1);
        if (!StartsWithLetterOrUnderscore(name))
        {
            builder.Append('_');
        }
        foreach (char character in name)
        {
            builder.Append(IsStemCharacter(char.GetUnicodeCategory(character)) ? character : '_');
        }
        return builder.ToString();
    }

    /// <summary>
    /// Whether a character of this category can stay in a stem. These are the categories the tool's name
    /// rules (<c>NameRules</c> in Gum.ProjectServices, which this assembly cannot reference) accept in a
    /// name, minus the space; <c>ShaderVariableNamingToolRulesTests</c> checks the two agree. Roslyn
    /// accepts all of them in an identifier. Kana, CJK and other letters of category OtherLetter are not
    /// in the list, so they are replaced. Characters are looked at one UTF-16 unit at a time, so every
    /// half of a surrogate pair is replaced too: Roslyn rejects characters outside the BMP in an
    /// identifier, and variable references are parsed by Roslyn.
    /// </summary>
    private static bool IsStemCharacter(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.LetterNumber:
            case UnicodeCategory.DecimalDigitNumber:
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.Format:
                return true;
            default:
                return false;
        }
    }

    // A name has to start with a letter or underscore for C#. A first character that is replaced
    // becomes an underscore, which also qualifies.
    private static bool StartsWithLetterOrUnderscore(string name)
    {
        UnicodeCategory category = char.GetUnicodeCategory(name[0]);
        return name[0] == '_' || char.IsLetter(name[0]) || !IsStemCharacter(category);
    }

    /// <summary>
    /// Whether a parameter can be saved as a variable: not empty, only ASCII letters, digits and
    /// underscores, and no double underscore (which would be read as a component suffix, and which GLSL
    /// reserves anyway). A parameter that fails stays on the legacy <c>ShaderParameters</c> text.
    /// </summary>
    public static bool IsSupportedParameterName(string? parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return false;
        }

        string name = parameter!;
        foreach (char character in name)
        {
            bool isAsciiLetter = (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
            bool isDigit = character >= '0' && character <= '9';
            if (!isAsciiLetter && !isDigit && character != '_')
            {
                return false;
            }
        }
        return name.IndexOf(ComponentSeparator, StringComparison.Ordinal) < 0;
    }

    /// <summary>The variable name of a scalar parameter, such as <c>Outline_Thickness</c>.</summary>
    /// <exception cref="ArgumentException"><paramref name="parameter"/> is not supported.</exception>
    public static string GetVariableName(string? shaderFile, string parameter) =>
        GetVariableName(shaderFile, new ShaderParameterMarker(parameter));

    /// <summary>The variable name of one component of a vector parameter, such as <c>Outline_Tint__Y</c>.</summary>
    /// <param name="shaderFile">The shader file path or name.</param>
    /// <param name="parameter">The parameter name as declared in the shader.</param>
    /// <param name="componentIndex">The zero based component, 0 for X up to 3 for W.</param>
    /// <param name="componentCount">How many components the vector has, 2 to 4.</param>
    /// <exception cref="ArgumentException"><paramref name="parameter"/> is not supported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is not 2 to 4, or the index is outside the count.</exception>
    public static string GetVariableName(string? shaderFile, string parameter, int componentIndex, int componentCount) =>
        GetVariableName(shaderFile, new ShaderParameterMarker(parameter, componentIndex, componentCount));

    /// <summary>
    /// The variable name for <paramref name="marker"/>. A caller that needs the name and the marker text
    /// builds the marker once and uses this and <see cref="ShaderParameterMarker.Format"/>.
    /// </summary>
    public static string GetVariableName(string? shaderFile, ShaderParameterMarker marker)
    {
        if (marker.Parameter.Length == 0)
        {
            throw new ArgumentException("The marker does not name a shader parameter.", nameof(marker));
        }

        string name = GetSanitizedStem(shaderFile) + "_" + marker.Parameter;
        if (marker.IsComponent)
        {
            name += ComponentSeparator + ComponentLetters[marker.ComponentIndex];
        }
        return name;
    }

    /// <summary>
    /// The inverse of <see cref="GetVariableName(string?, string, int, int)"/>: recovers the parameter
    /// and component from a variable name of the given shader (the stem is matched ignoring case; the
    /// parameter is returned as written). The name does not carry the component
    /// count; that is in the <see cref="ShaderParameterMarker"/> saved with the variable.
    /// </summary>
    /// <param name="shaderFile">The shader the variable belongs to; names are only unique per shader.</param>
    /// <param name="variableName">The variable name without any instance prefix.</param>
    /// <param name="parameter">The parameter name, or empty when this returns false.</param>
    /// <param name="componentIndex">The zero based component, or null for a scalar or when this returns false.</param>
    /// <returns>False when <paramref name="variableName"/> is not a canonical name for this shader.</returns>
    public static bool TryParseVariableName(string? shaderFile, string? variableName, out string parameter, out int? componentIndex)
    {
        parameter = "";
        componentIndex = null;
        if (variableName == null)
        {
            return false;
        }

        string prefix = GetSanitizedStem(shaderFile) + "_";
        // The stem is compared ignoring case: Windows file names are case-insensitive and the tool's
        // duplicate-name check is too, so "outline.FX" and "Outline_Thickness" name the same shader's
        // parameter. The parameter keeps the case it has in the variable name.
        if (!variableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string remainder = variableName.Substring(prefix.Length);
        if (TrySplitComponentSuffix(remainder, out string baseName, out int index))
        {
            if (!IsSupportedParameterName(baseName))
            {
                return false;
            }
            parameter = baseName;
            componentIndex = index;
            return true;
        }

        if (!IsSupportedParameterName(remainder))
        {
            return false;
        }
        parameter = remainder;
        return true;
    }

    /// <summary>
    /// Whether <paramref name="variableName"/> could be a canonical name of some shader: a non-empty stem
    /// (which may start with an underscore), a separating underscore, and a supported parameter name,
    /// optionally followed by a component suffix. It needs no shader, so it is the cheap test for a name
    /// that arrives before the container's shader file is known; for a given shader
    /// <see cref="TryParseVariableName"/> decides. Never false for a name <see cref="GetVariableName(string?, string)"/>
    /// can produce. Does not allocate.
    /// </summary>
    public static bool IsPossibleVariableName(string? variableName)
    {
        if (variableName == null)
        {
            return false;
        }

        int length = variableName.Length;

        // A trailing __X to __W is a component suffix, but it is also a legal ending of a scalar
        // parameter (stem "B_" and parameter "Z" give "B__Z"), so both readings are tried.
        if (length >= ComponentSeparator.Length + 2
            && ComponentLetters.IndexOf(variableName[length - 1]) >= 0
            && string.CompareOrdinal(variableName, length - 3, ComponentSeparator, 0, ComponentSeparator.Length) == 0
            && HasSeparatorBefore(variableName, length - 3))
        {
            return true;
        }
        return HasSeparatorBefore(variableName, length);
    }

    // The separator is an underscore that leaves a non-empty stem before it and a supported parameter
    // name between it and end. The stem may hold underscores and the parameter may end in one, so every
    // underscore from the second character on is a candidate.
    private static bool HasSeparatorBefore(string name, int end)
    {
        for (int separator = 1; separator < end - 1; separator++)
        {
            if (name[separator] == '_' && IsSupportedParameterRange(name, separator + 1, end))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSupportedParameterRange(string name, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            char character = name[i];
            bool isAsciiLetter = (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
            bool isDigit = character >= '0' && character <= '9';
            if (!isAsciiLetter && !isDigit && character != '_')
            {
                return false;
            }
            if (character == '_' && i + 1 < end && name[i + 1] == '_')
            {
                return false;
            }
        }
        return end > start;
    }

    /// <summary>
    /// Whether <paramref name="variableName"/> ends in a component suffix (<c>__X</c> to <c>__W</c>)
    /// after at least one other character. Needs no shader; a name that passes is a component name of
    /// some shader.
    /// </summary>
    public static bool IsComponentName(string? variableName) =>
        variableName != null && TrySplitComponentSuffix(variableName, out _, out _);

    private static bool TrySplitComponentSuffix(string name, out string baseName, out int componentIndex)
    {
        baseName = "";
        componentIndex = 0;

        int length = name.Length;
        if (length < ComponentSeparator.Length + 2)
        {
            return false;
        }

        int letterIndex = ComponentLetters.IndexOf(name[length - 1]);
        if (letterIndex < 0
            || string.CompareOrdinal(name, length - 3, ComponentSeparator, 0, ComponentSeparator.Length) != 0)
        {
            return false;
        }

        baseName = name.Substring(0, length - 3);
        componentIndex = letterIndex;
        return true;
    }
}
