using System;
using Gum.Input;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>The Avalonia head's Variables tab view and editor controls.</summary>
public class AvaloniaVariableGridHead : IVariableGridHead
{
    /// <inheritdoc/>
    public IVariablesTabView CreateVariablesTabView(MainControlViewModel viewModel) =>
        throw new NotSupportedException("The Avalonia Variables tab is not built yet.");

    /// <inheritdoc/>
    public void AttachReferenceTextEditor(object displayer, Action<GumKeyEventArgs, string> handleKeyDown)
    {
    }
}
