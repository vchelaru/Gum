using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.ComponentModel;

namespace Gum.Logic;

/// <summary>
/// Narrow seam over <c>Gum.DataTypes.VariableSaveExtensionMethodsGumTool.GetTypeConverter</c>
/// (Locator-resolving, tool-only) so <see cref="VariableSaveLogic"/> (headless, Gum.Presentation)
/// can obtain a variable's <see cref="TypeConverter"/> without referencing that tool-only
/// extension method directly.
/// </summary>
public interface IVariableTypeConverterProvider
{
    /// <summary>
    /// Returns the <see cref="TypeConverter"/> that should be used to edit/display the given variable.
    /// </summary>
    TypeConverter GetTypeConverter(VariableSave defaultVariable, ElementSave? container);
}
