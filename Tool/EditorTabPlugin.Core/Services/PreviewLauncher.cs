using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly string _headBaseDirectory;
    private readonly Func<bool> _isSortByBatchKey;

    private Process? _process;
    private string? _selectionFilePath;
    private bool _isConvertedGumxPreview;

    /// <param name="headBaseDirectory">
    /// The running head's own base directory (<c>AppContext.BaseDirectory</c>), used to locate the
    /// preview executable via <see cref="PreviewExecutableLocator"/>.
    /// </param>
    /// <param name="isSortByBatchKey">
    /// Whether the tool's canvas currently renders with <c>BatchKeyGroupedOrderer</c> (the
    /// Performance panel's "Sort by batch" option), so the preview renders the same way.
    /// </param>
    public PreviewLauncher(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IOutputManager outputManager,
        IPreviewGumxProjectionService previewGumxProjectionService,
        string headBaseDirectory,
        Func<bool> isSortByBatchKey)
    {
        _selectedState = selectedState;
        _projectManager = projectManager;
        _outputManager = outputManager;
        _previewGumxProjectionService = previewGumxProjectionService;
        _headBaseDirectory = headBaseDirectory;
        _isSortByBatchKey = isSortByBatchKey;
    }

    private bool IsRunning => _process is { HasExited: false };

    /// <inheritdoc/>
    public void Launch()
    {
        if (_process is { HasExited: true })
        {
            DeleteSelectionFileQuietly();
            _process = null;
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
            PushSelection(element, activate: true);
            return;
        }

        bool isJsonFormat = GumProjectSave.IsJsonFormat(project.FullFileName);
        ResolvedPreviewExecutable? resolved = PreviewExecutableLocator.Resolve(_headBaseDirectory);
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
        PreviewSelectionFile.TryWrite(_selectionFilePath, BuildMessage(element, activate: false).Serialize());

        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            resolved.Value.ExecutablePath, projectPathForLaunch, element.Name, _selectionFilePath, contentRootDirectory);

        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException)
        {
            _outputManager.AddError($"Could not launch the preview: {exception.Message}");
            _process = null;
            return;
        }

        _outputManager.AddOutput($"Launched preview for {element.Name}.");
    }

    /// <inheritdoc/>
    public void PushSelection(ElementSave? element, bool activate = false)
    {
        if (element == null || _selectionFilePath == null || !IsRunning)
        {
            return;
        }
        if (!PreviewSelectionFile.TryWrite(_selectionFilePath, BuildMessage(element, activate).Serialize()))
        {
            _outputManager.AddError("Could not update the running preview: its selection file is locked.");
        }
    }

    /// <inheritdoc/>
    public void RefreshIfRunning()
    {
        if (!IsRunning || !_isConvertedGumxPreview)
        {
            return;
        }

        GumProjectSave? project = _projectManager.GumProjectSave;
        if (project == null)
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
        if (state != null && state != element.DefaultState)
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
