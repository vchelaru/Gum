using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Gum.DataTypes;
using Gum.ProjectServices;
using Gum.Services;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <inheritdoc/>
public class PreviewGumxProjectionService : IPreviewGumxProjectionService
{
    private readonly IConvertProjectToJsonService _convertService;
    private readonly IPathCaseSensitivity _pathCaseSensitivity;

    public PreviewGumxProjectionService(IConvertProjectToJsonService convertService, IPathCaseSensitivity pathCaseSensitivity)
    {
        _convertService = convertService;
        _pathCaseSensitivity = pathCaseSensitivity;
    }

    /// <inheritdoc/>
    public PreviewGumxProjection Project(GumProjectSave project)
    {
        string? fullFileName = project.FullFileName;
        if (string.IsNullOrEmpty(fullFileName))
        {
            throw new ArgumentException("The project must be saved before it can be previewed.", nameof(project));
        }

        string contentRootDirectory = FileManager.GetDirectory(fullFileName);
        string outputDirectory = GetTempDirectory(fullFileName);
        ConvertProjectToJsonResult result = _convertService.ConvertToJson(project, outputDirectory);
        return new PreviewGumxProjection(result.ProjectFilePath, contentRootDirectory);
    }

    // Deterministic per-project directory: repeated calls for the same .gumx path (the initial
    // launch, then a refresh after every save while the preview stays open) must land in the same
    // place, since GumPreview's own hot-reload watcher is watching that one directory for changes.
    // Lowercased only where the file system ignores case, so two projects whose names differ only
    // by case on Linux get their own directories.
    private string GetTempDirectory(string gumxFullFileName)
    {
        string standardizedPath = gumxFullFileName.Replace('\\', '/');
        if (_pathCaseSensitivity.GetComparison(gumxFullFileName) == StringComparison.OrdinalIgnoreCase)
        {
            standardizedPath = standardizedPath.ToLowerInvariant();
        }
        string hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(standardizedPath)));
        return Path.Combine(Path.GetTempPath(), "GumPreviewJson", hash);
    }
}
