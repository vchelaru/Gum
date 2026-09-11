using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using TextureCoordinateSelectionPlugin;
using TextureCoordinateSelectionPlugin.Views;

namespace Gum.Avalonia.Plugins.TextureCoordinates;

/// <summary>
/// The Avalonia head's texture-coordinates tab: <see cref="TextureCoordinatePluginBase"/> with
/// <see cref="TextureCoordinateView"/> as its view.
/// </summary>
[Export(typeof(PluginBase))]
public class AvaloniaTextureCoordinatePlugin : TextureCoordinatePluginBase
{
    [ImportingConstructor]
    public AvaloniaTextureCoordinatePlugin(
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
    protected override ITextureCoordinateView CreateView() => new TextureCoordinateView();
}
