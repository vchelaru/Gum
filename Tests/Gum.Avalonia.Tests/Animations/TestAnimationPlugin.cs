using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// A playback timer the test fires itself, so playback does not depend on the headless
/// dispatcher's timers, which do not tick reliably in a long test run.
/// </summary>
internal sealed class ManualUiTimer : IUiTimer
{
    /// <inheritdoc/>
    public event Action? Tick;

    /// <summary>True between Start and Stop.</summary>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public void Start(TimeSpan interval) => IsRunning = true;

    /// <inheritdoc/>
    public void Stop() => IsRunning = false;

    /// <summary>Raises a tick if the timer is running.</summary>
    public void Fire()
    {
        if (IsRunning)
        {
            Tick?.Invoke();
        }
    }
}

/// <summary>The head's Animations plugin with the harness's manual playback timers.</summary>
internal sealed class TestAnimationPlugin : AvaloniaStateAnimationPlugin
{
    public TestAnimationPlugin(
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

    /// <summary>Every timer handed to a view model, so the harness can fire them.</summary>
    public List<ManualUiTimer> Timers { get; } = new List<ManualUiTimer>();

    /// <inheritdoc/>
    protected override IUiTimer CreateUiTimer()
    {
        ManualUiTimer timer = new ManualUiTimer();
        Timers.Add(timer);
        return timer;
    }
}
