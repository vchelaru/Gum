using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using System.ComponentModel.Composition;
using TextureCoordinateSelectionPlugin.Views;

namespace TextureCoordinateSelectionPlugin;

/// <summary>
/// The WPF head's texture-coordinates tab: <see cref="TextureCoordinatePluginBase"/> with the
/// XAML <see cref="MainControl"/> as its view.
/// </summary>
[Export(typeof(PluginBase))]
public class MainTextureCoordinatePlugin : TextureCoordinatePluginBase
{
    [ImportingConstructor]
    public MainTextureCoordinatePlugin(
        ISelectedState selectedState,
        IWireframeCommands wireframeCommands,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ITabManager tabManager,
        IHotkeyManager hotkeyManager,
        IProjectManager projectManager,
        IFileWatchManager fileWatchManager,
        IMessenger messenger,
        IThemingService themingService)
        : base(selectedState, wireframeCommands, undoManager, guiCommands, fileCommands, setVariableLogic,
            tabManager, hotkeyManager, projectManager, fileWatchManager, messenger, themingService)
    {
    }

    /// <inheritdoc/>
    protected override ITextureCoordinateView CreateView() => new MainControl();
}
