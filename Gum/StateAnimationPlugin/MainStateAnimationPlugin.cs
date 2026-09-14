using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin.Models;
using StateAnimationPlugin.ViewModels;
using System.ComponentModel.Composition;
using System.Windows;

namespace StateAnimationPlugin;

/// <summary>
/// The WPF head's Animations tab: <see cref="StateAnimationPluginBase"/> (all of the tab's logic) with
/// the WPF view (<see cref="Views.MainWindow"/>) and a WPF dispatcher timer. The Avalonia head's twin
/// is <c>AvaloniaStateAnimationPlugin</c>.
/// </summary>
[Export(typeof(PluginBase))]
public class MainStateAnimationPlugin : StateAnimationPluginBase
{
    private Views.MainWindow? _mainWindow;

    [ImportingConstructor]
    public MainStateAnimationPlugin(
        ISelectedState selectedState,
        INameVerifier nameVerifier,
        IMessenger messenger,
        IOutputManager outputManager,
        IFileWatchManager fileWatchManager,
        IFileCommands fileCommands,
        IProjectState projectState,
        IProjectManager projectManager,
        IWireframeObjectManager wireframeObjectManager,
        IUndoManager undoManager,
        IAnimationUndoProviderRegistrar animationUndoProviderRegistrar,
        IHotkeyManager hotkeyManager)
        : base(selectedState, nameVerifier, messenger, outputManager, fileWatchManager, fileCommands, projectState,
            projectManager, wireframeObjectManager, undoManager, animationUndoProviderRegistrar, hotkeyManager)
    {
    }

    /// <inheritdoc/>
    protected override object CreateAnimationTabContent(AnimationPluginSettings settings)
    {
        Views.MainWindow mainWindow = new Views.MainWindow(KeyHandler)
        {
            FirstRowWidth = new GridLength((double)settings.FirstToSecondColumnRatio, GridUnitType.Star),
            SecondRowWidth = new GridLength(1, GridUnitType.Star),
        };
        mainWindow.AddStateKeyframeClicked += (_, _) => AddStateKeyframe();
        mainWindow.AnimationKeyframeAdded += AddPastedKeyframe;
        mainWindow.AnimationColumnsResized += () => SaveColumnRatio(mainWindow.FirstRowWidth.Value, mainWindow.SecondRowWidth.Value);
        _mainWindow = mainWindow;
        return mainWindow;
    }

    /// <inheritdoc/>
    protected override void ShowViewModel(ElementAnimationsViewModel? viewModel)
    {
        if (_mainWindow != null)
        {
            _mainWindow.DataContext = viewModel;
        }
    }

    /// <inheritdoc/>
    protected override IUiTimer CreateUiTimer() => new DispatcherUiTimer();
}
