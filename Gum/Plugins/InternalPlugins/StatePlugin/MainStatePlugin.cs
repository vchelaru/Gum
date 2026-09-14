using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.Plugins.InternalPlugins.StatePlugin.Views;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.StatePlugin;

/// <summary>
/// The WPF States tab: <see cref="StateTreePluginBase"/> completed with the WPF
/// <see cref="StateTreeView"/>.
/// </summary>
[Export(typeof(PluginBase))]
public class MainStatePlugin : StateTreePluginBase
{
    [ImportingConstructor]
    public MainStatePlugin(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands,
        IElementCommands elementCommands, IEditCommands editCommands, IDialogService dialogService,
        IHotkeyManager hotkeyManager, IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        ICopyPasteLogic copyPasteLogic)
        : base(selectedState, guiCommands, fileCommands, elementCommands, editCommands, dialogService,
            hotkeyManager, variableInCategoryPropagationLogic, copyPasteLogic)
    {
    }

    /// <inheritdoc/>
    protected override object CreateView(StateTreeViewModel viewModel) =>
        new StateTreeView(viewModel, RightClickService, KeyboardHandler);
}
