using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.ComponentModel;

namespace Gum.Logic;

/// <inheritdoc/>
public class VariableTypeConverterProvider : IVariableTypeConverterProvider
{
    /// <inheritdoc/>
    public TypeConverter GetTypeConverter(VariableSave defaultVariable, ElementSave? container) =>
        defaultVariable.GetTypeConverter(container);
}
