using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Services;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin;
using StateAnimationPlugin.Models;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.StateAnimation;

/// <summary>
/// The Avalonia head's Animations tab: <see cref="StateAnimationPluginBase"/> (all of the tab's logic)
/// with <see cref="AnimationsView"/> and a dispatcher timer. Twin of the WPF head's
/// <c>MainStateAnimationPlugin</c>.
/// </summary>
[Export(typeof(PluginBase))]
public class AvaloniaStateAnimationPlugin : StateAnimationPluginBase
{
    private readonly ICanvasRedrawScheduler _canvasRedrawScheduler;
    private AnimationsView? _view;

    /// <summary>Creates the plugin over the services the host bridges.</summary>
    [ImportingConstructor]
    public AvaloniaStateAnimationPlugin(
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
        IHotkeyManager hotkeyManager,
        ICanvasRedrawScheduler canvasRedrawScheduler)
        : base(selectedState, nameVerifier, messenger, outputManager, fileWatchManager, fileCommands, projectState,
            projectManager, wireframeObjectManager, undoManager, animationUndoProviderRegistrar, hotkeyManager)
    {
        _canvasRedrawScheduler = canvasRedrawScheduler;
    }

    /// <inheritdoc/>
    protected override object CreateAnimationTabContent(AnimationPluginSettings settings)
    {
        AnimationsView view = new AnimationsView(KeyHandler, (double)settings.FirstToSecondColumnRatio);
        view.AddStateKeyframeRequested += AddStateKeyframe;
        view.KeyframePasted += AddPastedKeyframe;
        view.ColumnsResized += SaveColumnRatio;
        _view = view;
        return view;
    }

    /// <inheritdoc/>
    protected override void ShowViewModel(ElementAnimationsViewModel? viewModel)
    {
        if (_view != null)
        {
            _view.DataContext = viewModel;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Each playback tick applies a state to the canvas with no input behind it, so it
    /// requests a redraw.</remarks>
    protected sealed override IUiTimer CreateUiTimer()
    {
        IUiTimer timer = CreatePlaybackTimer();
        timer.Tick += _canvasRedrawScheduler.RequestRedraw;
        return timer;
    }

    /// <summary>Creates the timer that drives playback; tests substitute a manual one.</summary>
    protected virtual IUiTimer CreatePlaybackTimer() => new AvaloniaUiTimer();
}
