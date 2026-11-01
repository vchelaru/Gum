using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <inheritdoc cref="IPreviewLauncher"/>
public class PreviewLauncher : IPreviewLauncher
{
    private readonly ISelectedState _selectedState;
    private readonly IProjectManager _projectManager;
    private readonly IOutputManager _outputManager;
    private readonly IPreviewGumxProjectionService _previewGumxProjectionService;
    private readonly Func<bool> _isSortByBatchKey;

    private IPreviewProcess? _process;
    private ExitWatch? _exitWatch;
    private string? _selectionFilePath;
    private bool _isConvertedGumxPreview;
    private ElementSave? _shownElement;
    private ElementSave? _pinnedElement;

    /// <param name="processStarter">Finds and starts the preview executable.</param>
    /// <param name="isSortByBatchKey">
    /// Whether the tool's canvas currently renders with <c>BatchKeyGroupedOrderer</c> (the
    /// Performance panel's "Sort by batch" option), so the preview renders the same way.
    /// </param>
    public PreviewLauncher(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IOutputManager outputManager,
        IPreviewGumxProjectionService previewGumxProjectionService,
        IPreviewProcessStarter processStarter,
        Func<bool> isSortByBatchKey)
    {
        _selectedState = selectedState;
        _projectManager = projectManager;
        _outputManager = outputManager;
        _previewGumxProjectionService = previewGumxProjectionService;
        ProcessStarter = processStarter;
        _isSortByBatchKey = isSortByBatchKey;
    }

    /// <summary>Finds and starts the preview executable; a test swaps it for a stand-in.</summary>
    internal IPreviewProcessStarter ProcessStarter { get; set; }

    /// <summary>
    /// Runs an action on the UI thread; null uses the <see cref="SynchronizationContext"/> current
    /// at launch. A test sets it to run inline instead of depending on the ambient context.
    /// </summary>
    internal Action<Action>? PostToUiThread { get; set; }

    /// <inheritdoc/>
    public bool IsRunning => _process is { HasExited: false };

    /// <inheritdoc/>
    public event Action? PinnedChanged;

    /// <inheritdoc/>
    public ElementSave? PinnedElement => IsRunning ? _pinnedElement : null;

    /// <inheritdoc/>
    public void Launch()
    {
        if (_process is { HasExited: true })
        {
            DeleteSelectionFileQuietly();
            _process = null;
            SetPinned(null);
        }

        GumProjectSave? project = _projectManager.GumProjectSave;
        if (project == null || string.IsNullOrEmpty(project.FullFileName))
        {
            _outputManager.AddError("Preview requires a saved Gum project.");
            return;
        }

        ElementSave? element = _selectedState.SelectedElement;
        if (element == null)
        {
            _outputManager.AddError("Select a screen or component to preview.");
            return;
        }

        if (IsRunning)
        {
            // Re-clicking Preview is an explicit ask to bring the window forward, unlike a passive
            // tree-selection change (issue #4717 follow-up).
            PushSelection(PinnedElement ?? element, activate: true);
            return;
        }

        bool isJsonFormat = GumProjectSave.IsJsonFormat(project.FullFileName);
        ResolvedPreviewExecutable? resolved = ProcessStarter.Resolve();
        if (resolved == null)
        {
            _outputManager.AddError(
                $"Preview executable not found. Publish {PreviewExecutableLocator.DevBuildProjectPath} " +
                $"into the head's {PreviewExecutableLocator.PreviewFolderName}/ folder, or build it locally for development.");
            return;
        }

        string contentRootDirectory = FileManager.GetDirectory(project.FullFileName);
        string projectPathForLaunch = project.FullFileName;

        // The Native AOT build can't load .gumx directly (XmlSerializer isn't Native-AOT-safe) -
        // convert to a temporary JSON copy first and launch against that instead (issue #4748).
        // Content (fonts, textures) still resolves from the original directory: --content-root below.
        _isConvertedGumxPreview = !isJsonFormat && resolved.Value.IsNativeAot;
        if (_isConvertedGumxPreview)
        {
            projectPathForLaunch = _previewGumxProjectionService.Project(project).ProjectFilePath;
        }

        _selectionFilePath = Path.Combine(Path.GetTempPath(), $"GumPreviewSelection_{Guid.NewGuid():N}.txt");

        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            resolved.Value.ExecutablePath, projectPathForLaunch, element.Name, _selectionFilePath, contentRootDirectory, Environment.ProcessId,
            logPath: Environment.GetEnvironmentVariable(PreviewProcessStartInfoBuilder.LogPathEnvironmentVariable));

        try
        {
            PreviewSelectionFile.TryWrite(_selectionFilePath, BuildMessage(element, activate: false).Serialize());
            _process = ProcessStarter.Start(startInfo);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _outputManager.AddError($"Could not launch the preview: {exception.Message}");
            _process = null;
            DeleteSelectionFileQuietly();
            return;
        }

        if (_process == null)
        {
            _outputManager.AddError($"Could not launch the preview: the process did not start ({resolved.Value.ExecutablePath}).");
            DeleteSelectionFileQuietly();
            return;
        }

        _shownElement = element;
        _outputManager.AddOutput($"Launched preview for {element.Name}.");
        WatchForExit(_process);
    }

    /// <summary>
    /// Reports the process's exit when it ends by itself with a nonzero code. The exit is raised on
    /// a thread-pool thread, so it is posted to the launching (UI) thread's context before touching
    /// the output panel; with no context it runs inline.
    /// </summary>
    private void WatchForExit(IPreviewProcess process)
    {
        Action<Action>? post = PostToUiThread;
        SynchronizationContext? context = SynchronizationContext.Current;
        ExitWatch watch = new ExitWatch(process);
        watch.Handler = exit =>
        {
            if (post != null)
            {
                post(() => ReportExit(watch, exit));
            }
            else if (context == null)
            {
                ReportExit(watch, exit);
            }
            else
            {
                context.Post(_ => ReportExit(watch, exit), null);
            }
        };
        _exitWatch = watch;
        process.Exited += watch.Handler;
    }

    private void ReportExit(ExitWatch watch, PreviewProcessExit exit)
    {
        if (watch.IsDone)
        {
            return;
        }
        watch.Stop();
        if (exit.ExitCode == 0)
        {
            return;
        }
        string detail = string.IsNullOrWhiteSpace(exit.ErrorText) ? "no error output" : exit.ErrorText.Trim();
        _outputManager.AddError($"The preview exited with code {exit.ExitCode}: {detail}");
    }

    private sealed class ExitWatch
    {
        private readonly IPreviewProcess _process;

        public ExitWatch(IPreviewProcess process)
        {
            _process = process;
        }

        public Action<PreviewProcessExit>? Handler { get; set; }

        public bool IsDone { get; private set; }

        public void Stop()
        {
            IsDone = true;
            if (Handler != null)
            {
                _process.Exited -= Handler;
            }
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        if (_process == null)
        {
            return;
        }

        // A preview the tool closes itself is not an error.
        _exitWatch?.Stop();
        _exitWatch = null;
        if (!_process.HasExited)
        {
            _process.Close();
        }
        DeleteSelectionFileQuietly();
        _process = null;
        SetPinned(null);
    }

    /// <inheritdoc/>
    public bool Pin()
    {
        if (!IsRunning || _shownElement == null)
        {
            _outputManager.AddError("Open the preview first, then pin it.");
            return false;
        }
        SetPinned(_shownElement);
        return true;
    }

    /// <inheritdoc/>
    public void Unpin()
    {
        if (_pinnedElement == null)
        {
            return;
        }
        SetPinned(null);
        PushSelection(_selectedState.SelectedElement);
    }

    private void SetPinned(ElementSave? element)
    {
        if (_pinnedElement != element)
        {
            _pinnedElement = element;
            PinnedChanged?.Invoke();
        }
    }

    /// <inheritdoc/>
    public void PushSelection(ElementSave? element, bool activate = false)
    {
        if (element == null || _selectionFilePath == null || !IsRunning)
        {
            return;
        }
        // A pinned preview ignores every other element. This also keeps a state picked on another
        // element (a button's Pressed) from reaching the pinned screen.
        if (_pinnedElement != null && element != _pinnedElement)
        {
            return;
        }
        if (!PreviewSelectionFile.TryWrite(_selectionFilePath, BuildMessage(element, activate).Serialize()))
        {
            _outputManager.AddError("Could not update the running preview: its selection file is locked.");
            return;
        }
        _shownElement = element;
    }

    /// <inheritdoc/>
    public void ReactToElementRenamed(ElementSave element)
    {
        // A pinned preview shows its pinned element, so this covers the pin too: the renamed
        // element is the same object, and pushing it keeps the pin and updates the name.
        if (element == _shownElement)
        {
            PushSelection(element);
        }
    }

    /// <inheritdoc/>
    public void ReactToProjectLoaded(GumProjectSave project)
    {
        _shownElement = FindSameNamed(project, _shownElement);
        SetPinned(FindSameNamed(project, _pinnedElement));
    }

    private static ElementSave? FindSameNamed(GumProjectSave project, ElementSave? element) =>
        element == null
            ? null
            : project.AllElements.FirstOrDefault(candidate => candidate.GetType() == element.GetType() && candidate.Name == element.Name);

    /// <inheritdoc/>
    public void RefreshIfRunning()
    {
        if (!IsRunning || !_isConvertedGumxPreview)
        {
            return;
        }

        GumProjectSave? project = _projectManager.GumProjectSave;
        if (project == null || string.IsNullOrEmpty(project.FullFileName))
        {
            return;
        }

        _previewGumxProjectionService.Project(project);
    }

    /// <summary>
    /// The message for <paramref name="element"/> in the tool's currently selected state (none when
    /// the default state is selected), with the tool's current sibling orderer.
    /// </summary>
    internal PreviewSelectionMessage BuildMessage(ElementSave element, bool activate)
    {
        PreviewSelectionMessage message = new PreviewSelectionMessage(element.Name)
        {
            SortByBatchKey = _isSortByBatchKey(),
            Activate = activate,
        };

        StateSave? state = _selectedState.SelectedStateSave;
        if (state != null && state != element.DefaultState && element.AllStates.Contains(state))
        {
            message.StateName = state.Name;
            message.CategoryName = element.Categories.FirstOrDefault(category => category.States.Contains(state))?.Name;
        }
        return message;
    }

    private void DeleteSelectionFileQuietly()
    {
        if (_selectionFilePath == null)
        {
            return;
        }
        try
        {
            File.Delete(_selectionFilePath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
