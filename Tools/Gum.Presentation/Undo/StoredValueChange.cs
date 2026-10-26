using Gum.DataTypes;

namespace Gum.Undo;

/// <summary>
/// A variable value written into <paramref name="Element"/> by something other than an edit the user made
/// there, such as a variable reference re-evaluated after the element it reads was reloaded from disk.
/// </summary>
/// <param name="Element">The element that holds the value.</param>
/// <param name="CategoryName">The category of the state, or null for an uncategorized state.</param>
/// <param name="StateName">The state the value is stored in.</param>
/// <param name="VariableName">The variable's name, such as <c>Background.Red</c>.</param>
/// <param name="VariableType">The variable's type, used when an undo snapshot has no such variable yet.</param>
/// <param name="Value">The value now stored.</param>
public record StoredValueChange(
    ElementSave Element,
    string? CategoryName,
    string StateName,
    string VariableName,
    string? VariableType,
    object? Value);
