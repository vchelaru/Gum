using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Gum.DataTypes;
using Gum.ProjectServices;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <inheritdoc/>
public class PreviewGumxProjectionService : IPreviewGumxProjectionService
{
    private readonly IConvertProjectToJsonService _convertService;

    public PreviewGumxProjectionService(IConvertProjectToJsonService convertService)
    {
        _convertService = convertService;
    }

    /// <inheritdoc/>
    public PreviewGumxProjection Project(GumProjectSave project)
    {
        string contentRootDirectory = FileManager.GetDirectory(project.FullFileName);
        string outputDirectory = GetTempDirectory(project.FullFileName);
        ConvertProjectToJsonResult result = _convertService.ConvertToJson(project, outputDirectory);
        return new PreviewGumxProjection(result.ProjectFilePath, contentRootDirectory);
    }

    // Deterministic per-project directory: repeated calls for the same .gumx path (the initial
    // launch, then a refresh after every save while the preview stays open) must land in the same
    // place, since GumPreview's own hot-reload watcher is watching that one directory for changes.
    private static string GetTempDirectory(string gumxFullFileName)
    {
        string standardizedPath = gumxFullFileName.Replace('\\', '/').ToLowerInvariant();
        string hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(standardizedPath)));
        return Path.Combine(Path.GetTempPath(), "GumPreviewJson", hash);
    }
}
