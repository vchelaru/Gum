using System.ComponentModel.Composition;
using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;

namespace Gum.Avalonia.Plugins.States;

/// <summary>
/// The Avalonia States tab: <see cref="StateTreePluginBase"/> completed with
/// <see cref="AvaloniaStateTreeView"/>.
/// </summary>
[Export(typeof(PluginBase))]
public class AvaloniaStatePlugin : StateTreePluginBase
{
    [ImportingConstructor]
    public AvaloniaStatePlugin(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands,
        IElementCommands elementCommands, IEditCommands editCommands, IDialogService dialogService,
        IHotkeyManager hotkeyManager, IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        ICopyPasteLogic copyPasteLogic)
        : base(selectedState, guiCommands, fileCommands, elementCommands, editCommands, dialogService,
            hotkeyManager, variableInCategoryPropagationLogic, copyPasteLogic)
    {
    }

    /// <inheritdoc/>
    protected override object CreateView(StateTreeViewModel viewModel) =>
        new AvaloniaStateTreeView(viewModel, RightClickService, KeyboardHandler);
}
