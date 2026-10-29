using Gum.DataTypes.Behaviors;
using Gum.Managers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Gum.ProjectServices;

/// <summary>
/// The name-validation rules that need nothing but the name (and, for duplicates, the names already in use),
/// shared by the tool's <c>NameVerifier</c> and the CLI so a name validates the same everywhere.
/// </summary>
public class NameRules
{
    private static readonly ImmutableArray<UnicodeCategory> ValidCharacterCategories =
    [
        UnicodeCategory.UppercaseLetter,
        UnicodeCategory.LowercaseLetter,
        UnicodeCategory.TitlecaseLetter,
        UnicodeCategory.ModifierLetter,
        UnicodeCategory.LetterNumber,
        UnicodeCategory.DecimalDigitNumber,
        UnicodeCategory.ConnectorPunctuation,
        UnicodeCategory.NonSpacingMark,
        UnicodeCategory.SpacingCombiningMark,
        UnicodeCategory.Format
    ];

    private static readonly ImmutableHashSet<string> InvalidWindowsFileNames =
    [
        "con", "prn", "aux", "nul",
        "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
    ];

    private static readonly ImmutableHashSet<string> CSharpReservedKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    ];

    /// <summary>
    /// Whether <paramref name="name"/> is usable as any kind of Gum name: not empty, no leading or trailing
    /// space, and only characters from <see cref="ValidCharacterCategories"/> (spaces inside are allowed).
    /// </summary>
    public bool IsNameValidCommon([NotNullWhen(true)] string? name, out string whyNotValid, out CommonValidationError commonValidationError)
    {
        whyNotValid = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            whyNotValid = "Empty names are not valid";
            commonValidationError = CommonValidationError.IsEmpty;
            return false;
        }
        if (name[0] == ' ')
        {
            whyNotValid = "The name can't begin with a space";
            commonValidationError = CommonValidationError.StartsWithSpace;
            return false;
        }
        if (name[^1] == ' ')
        {
            whyNotValid = "The name can't end with a space";
            commonValidationError = CommonValidationError.EndsWithSpace;
            return false;
        }
        foreach (Rune rune in name.EnumerateRunes())
        {
            if (rune.Value == ' ') continue;

            if (!ValidCharacterCategories.Contains(Rune.GetUnicodeCategory(rune)))
            {
                whyNotValid = $"The name can't contain invalid character {rune}";
                commonValidationError = CommonValidationError.InvalidCharacter;
                return false;
            }
        }
        commonValidationError = CommonValidationError.None;
        return true;
    }

    /// <summary>
    /// Whether <paramref name="name"/> can be used as a C# identifier as is. A name that fails with
    /// <see cref="CommonValidationError.InvalidStartingCharacterForCSharp"/> or
    /// <see cref="CommonValidationError.ReservedCSharpKeyword"/> can be repaired by prefixing it.
    /// </summary>
    public bool IsValidCSharpName(string name, [NotNullWhen(false)] out string? whyNotValid, out CommonValidationError commonValidationError)
    {
        if (string.IsNullOrEmpty(name))
        {
            whyNotValid = "Empty names are not valid";
            commonValidationError = CommonValidationError.IsEmpty;
            return false;
        }

        if (!StartsLikeCSharpIdentifier(name))
        {
            whyNotValid = $"Name may not begin with character {name[0]}";
            commonValidationError = CommonValidationError.InvalidStartingCharacterForCSharp;
            return false;
        }

        if (CSharpReservedKeywords.Contains(name))
        {
            whyNotValid = "Name is a C# reserved keyword";
            commonValidationError = CommonValidationError.ReservedCSharpKeyword;
            return false;
        }

        whyNotValid = null;
        commonValidationError = CommonValidationError.None;
        return true;
    }

    /// <summary>
    /// Turns any text (a project file name, a folder name) into one C# identifier: every character that
    /// can't be part of an identifier, including spaces, dots and hyphens, becomes an underscore; a leading
    /// character that can't start one gets an underscore in front (the rule MSBuild applies to a default
    /// RootNamespace); a reserved keyword gets an <c>@</c>. A text that is already an identifier is returned unchanged.
    /// </summary>
    public static string ToCSharpIdentifier(string? name)
    {
        name ??= string.Empty;
        // An identifier already escaped as @name stays as it is.
        if (name.Length > 1 && name[0] == '@' && ToCSharpIdentifier(name.Substring(1)) is string inner && (inner == name || inner == name.Substring(1)))
        {
            return name;
        }

        StringBuilder builder = new StringBuilder(name.Length + 1);
        foreach (Rune rune in name.EnumerateRunes())
        {
            builder.Append(IsCSharpIdentifierPart(rune) ? rune.ToString() : "_");
        }

        if (builder.Length == 0)
        {
            return "_";
        }

        string identifier = builder.ToString();
        if (!StartsLikeCSharpIdentifier(identifier))
        {
            return "_" + identifier;
        }

        return CSharpReservedKeywords.Contains(identifier) ? "@" + identifier : identifier;
    }

    private static bool StartsLikeCSharpIdentifier(string name)
    {
        // A letter outside the basic plane is a surrogate pair, so the first character alone is not a letter.
        Rune first = name.EnumerateRunes().First();
        return first.Value == '_' || Rune.IsLetter(first);
    }

    private static bool IsCSharpIdentifierPart(Rune rune)
    {
        switch (Rune.GetUnicodeCategory(rune))
        {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
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

    /// <summary>
    /// Whether <paramref name="name"/> is a file name Windows reserves (CON, LPT1, ...), compared ignoring case.
    /// </summary>
    public bool IsFileNameWindowsReserved(string? name, [NotNullWhen(true)] out string? whyReserved)
    {
        whyReserved = null;
        if (name != null && InvalidWindowsFileNames.Contains(name.ToLowerInvariant()))
        {
            whyReserved = $"The name {name} is a reserved file name in Windows";
            return true;
        }
        return false;
    }

    public bool IsNameValidAndroidFile(string name, [NotNullWhen(false)] out string? whyNotValid)
    {
        whyNotValid = null;
        foreach (char c in name)
        {
            if (!IsValidAndroidFileCharacter(c))
            {
                whyNotValid = $"The character {c} is not supported on Android.";
                break;
            }
        }
        return whyNotValid == null;
    }

    /// <summary>
    /// Validates a behavior name: the common rules, then the Windows reserved file names (behaviors are files
    /// named after them), then that no behavior in <paramref name="allBehaviors"/> other than
    /// <paramref name="behaviorSave"/> already has the name, compared with <see cref="Standardize"/> as elements are.
    /// </summary>
    /// <param name="behaviorName">The name to validate.</param>
    /// <param name="behaviorSave">The behavior being renamed, which may keep its own name; null when adding.</param>
    /// <param name="allBehaviors">The behaviors already in the project.</param>
    /// <param name="whyNotValid">The reason the name was rejected; null when it is valid.</param>
    public bool IsBehaviorNameValid(string? behaviorName, BehaviorSave? behaviorSave, IEnumerable<BehaviorSave> allBehaviors, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!IsNameValidCommon(behaviorName, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }
        if (IsFileNameWindowsReserved(behaviorName, out whyNotValid))
        {
            return false;
        }

        string standardizedName = Standardize(behaviorName);
        BehaviorSave? existing = allBehaviors.FirstOrDefault(item =>
            item != behaviorSave && Standardize(item.Name) == standardizedName);
        if (existing != null)
        {
            whyNotValid = $"There is already a behavior named {existing.Name} so this name can't be used.";
            return false;
        }

        whyNotValid = null;
        return true;
    }

    /// <summary>
    /// The form two names are compared in: spaces become underscores and case is ignored, which is how names
    /// end up in file names and code.
    /// </summary>
    [return: NotNullIfNotNull(nameof(name))]
    public string? Standardize(string? name)
    {
        if (name == null) return null;

        return name.Replace(" ", "_").ToLowerInvariant();
    }

    /// <summary>
    /// Replaces every character a name can't contain (including spaces) with an underscore.
    /// </summary>
    public string ToValidName(string candidate)
    {
        StringBuilder builder = new StringBuilder(candidate.Length);
        foreach (Rune rune in candidate.EnumerateRunes())
        {
            if (ValidCharacterCategories.Contains(Rune.GetUnicodeCategory(rune)))
            {
                builder.Append(rune.ToString());
            }
            else
            {
                builder.Append('_');
            }
        }
        return builder.ToString();
    }

    private static bool IsValidAndroidFileCharacter(char c)
    {
        return (c >= 'a' && c <= 'z') ||
            c == '_' ||
            (c >= '0' && c <= '9');
    }
}
