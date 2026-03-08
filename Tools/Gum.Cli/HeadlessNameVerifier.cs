using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Immutable;

namespace Gum.Cli;

/// <summary>
/// Minimal name verifier for headless/CLI use. Only implements C# name validation,
/// which is the only method needed by code generation.
/// </summary>
internal class HeadlessNameVerifier : INameVerifier
{
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

    /// <inheritdoc/>
    public bool IsValidCSharpName(string name, out string whyNotValid, out CommonValidationError commonValidationError)
    {
        if (name[0] != '_' && !char.IsLetter(name[0]))
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

        whyNotValid = null!;
        commonValidationError = CommonValidationError.None;
        return true;
    }

    /// <inheritdoc/>
    public bool IsFolderNameValid(string? folderName, out string whyNotValid)
    {
        whyNotValid = null!;
        return true;
    }

    /// <inheritdoc/>
    public bool IsElementNameValid(string? componentNameWithoutFolder, string folderName, ElementSave elementSave, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsCategoryNameValid(string? name, IStateContainer categoryContainer, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsStateNameValid(string name, StateSaveCategory category, StateSave stateSave, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsInstanceNameValid(string instanceName, InstanceSave instanceSave, IInstanceContainer instanceContainer, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsVariableNameValid(string variableName, ElementSave elementSave, VariableSave variableSave, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsBehaviorNameValid(string behaviorName, BehaviorSave behaviorSave, out string? whyNotValid)
    {
        whyNotValid = null;
        return true;
    }

    /// <inheritdoc/>
    public bool IsComponentNameAlreadyUsed(string name) => false;

    /// <inheritdoc/>
    public bool IsNameValidAndroidFile(string name, out string whyNotValid)
    {
        whyNotValid = null!;
        return true;
    }

    /// <inheritdoc/>
    public bool IsNameValidCommon(string name, out string whyNotValid, out CommonValidationError commonValidationError)
    {
        whyNotValid = null!;
        commonValidationError = CommonValidationError.None;
        return true;
    }
}
