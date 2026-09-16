using System;
using System.Diagnostics;
using System.IO;
using Gum.DataTypes;
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

    private Process? _process;
    private string? _selectionFilePath;
    private bool _isConvertedGumxPreview;

    /// <param name="headBaseDirectory">
    /// The running head's own base directory (<c>AppContext.BaseDirectory</c>), used to locate the
    /// preview executable via <see cref="PreviewExecutableLocator"/>.
    /// </param>
    public PreviewLauncher(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IOutputManager outputManager,
        IPreviewGumxProjectionService previewGumxProjectionService,
        string headBaseDirectory)
    {
        _selectedState = selectedState;
        _projectManager = projectManager;
        _outputManager = outputManager;
        _previewGumxProjectionService = previewGumxProjectionService;
        _headBaseDirectory = headBaseDirectory;
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
        File.WriteAllText(_selectionFilePath, element.Name);

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
        File.WriteAllText(_selectionFilePath, BuildSelectionFileContent(element.Name, activate));
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
    /// The selection file's content: the element name, plus a trailing <see cref="ActivateMarker"/>
    /// line when GumPreview should also raise its window (see <c>Game1.PollSelectionFile</c>).
    /// </summary>
    internal static string BuildSelectionFileContent(string elementName, bool activate) =>
        activate ? $"{elementName}\n{ActivateMarker}" : elementName;

    internal const string ActivateMarker = "activate";

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
