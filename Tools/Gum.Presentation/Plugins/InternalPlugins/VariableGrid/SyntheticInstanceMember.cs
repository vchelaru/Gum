using System;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The grid member for a <see cref="SyntheticVariableRow"/>. It reports the row's own default state, so
/// the row shows the "set on this instance" marker only when a value is stored, and "Make Default"
/// clears the stored value.
/// </summary>
internal class SyntheticInstanceMember : InstanceMember
{
    private readonly Func<bool>? _isDefault;
    private readonly Action? _resetToDefault;

    public SyntheticInstanceMember(Func<bool>? isDefault, Action? resetToDefault)
    {
        _isDefault = isDefault;
        _resetToDefault = resetToDefault;
    }

    public override bool IsDefault
    {
        get => _isDefault?.Invoke() ?? false;
        set
        {
            if (value && _resetToDefault != null)
            {
                _resetToDefault();
                SimulateValueChanged();
            }
        }
    }
}
