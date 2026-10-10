using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic.FileWatch;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class FormsTemplateCreator : IFormsTemplateCreator
{
    private const string ResourcePrefix = "Gum.ProjectServices.Templates.FormsTemplate.";
    private const string ManifestResourceName = ResourcePrefix + "manifest.txt";
    private const string ProjectTemplateRelativePath = "GumProject.gumx";

    // The embedded Forms template is authored (and shipped) entirely as XML. When the caller
    // wants a JSON project, extract the template as XML as usual, then convert it in place and
    // remove the XML - reusing ConvertProjectToJsonService rather than hand-authoring a second,
    // parallel JSON template that would need to be kept in sync with this one forever (#4705).
    private static readonly string[] ConvertibleXmlExtensions =
        { "gumx", "gusx", "gucx", "gutx", "behx" };

    /// <inheritdoc/>
    public void Create(string filePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var isJsonFormat = GumProjectSave.IsJsonFormat(filePath);
        var xmlProjectFileName = isJsonFormat
            ? Path.GetFileNameWithoutExtension(filePath) + "." + GumProjectSave.ProjectExtension
            : Path.GetFileName(filePath);

        var manifest = ReadManifest(assembly);
        var extractedPaths = new List<string>();

        foreach (var relativePath in manifest)
        {
            var resourceName = ResourcePrefix + relativePath.Replace('/', '.');
            var destinationPath = BuildDestinationPath(directory, relativePath, xmlProjectFileName);

            ExtractResource(assembly, resourceName, destinationPath);
            extractedPaths.Add(destinationPath);
        }

        var extractedXmlProjectPath = Path.Combine(directory, xmlProjectFileName);
        StripStaleBehaviorSourcePaths(extractedXmlProjectPath);

        if (isJsonFormat)
        {
            ConvertExtractedTemplateToJson(extractedXmlProjectPath, extractedPaths);
        }
    }

    // Files in a theme folder that describe the theme rather than make up a project: its own code
    // generation settings (the new project gets settings for its own game project) and gallery preview.
    private static readonly string[] ThemeOnlyFileNames = { "ProjectCodeSettings.codsj", "preview.png", "theme.txt" };

    /// <inheritdoc/>
    public void CreateFromTheme(string filePath, string themeDirectory)
    {
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        bool isJsonFormat = GumProjectSave.IsJsonFormat(filePath);
        string xmlProjectFileName = isJsonFormat
            ? Path.GetFileNameWithoutExtension(filePath) + "." + GumProjectSave.ProjectExtension
            : Path.GetFileName(filePath);
        string themeProjectPath = Path.Combine(themeDirectory, ProjectTemplateRelativePath);

        if (!File.Exists(themeProjectPath))
        {
            throw new InvalidOperationException($"Theme project not found: '{themeProjectPath}'.");
        }

        List<string> copiedPaths = new List<string>();
        foreach (string sourcePath in Directory.GetFiles(themeDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(themeDirectory, sourcePath);
            if (Array.IndexOf(ThemeOnlyFileNames, relativePath) >= 0)
            {
                continue;
            }

            string destinationPath = relativePath == ProjectTemplateRelativePath
                ? Path.Combine(directory, xmlProjectFileName)
                : Path.Combine(directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourcePath, destinationPath, overwrite: true);
            copiedPaths.Add(destinationPath);
        }

        // A theme in a checkout links its behaviors to shared files that the copy above can't carry
        // along. A theme already staged for the tool has them flattened into Behaviors/, and the
        // staging service skips links that don't resolve.
        string behaviorsDirectory = Path.Combine(directory, "Behaviors");
        new FormsThemeBehaviorStagingService().Stage(themeProjectPath, behaviorsDirectory);
        foreach (string stagedPath in Directory.GetFiles(behaviorsDirectory))
        {
            if (!copiedPaths.Contains(stagedPath))
            {
                copiedPaths.Add(stagedPath);
            }
        }

        string extractedXmlProjectPath = Path.Combine(directory, xmlProjectFileName);
        StripStaleBehaviorSourcePaths(extractedXmlProjectPath);

        if (isJsonFormat)
        {
            ConvertExtractedTemplateToJson(extractedXmlProjectPath, copiedPaths);
        }
    }

    // The checked-in template's GumProject.gumx carries BehaviorReference.SourcePath values
    // (e.g. "../FormsBehaviors/ButtonBehavior.behx") that only resolve at build time, when
    // Gum.FormsStaging flattens them into the shipped tool's Content/FormsThemes folder. This
    // extraction instead copies every manifest-listed .behx locally into the new project's own
    // Behaviors/ folder (see the manifest loop above), so a SourcePath surviving into the
    // extracted project is always stale - GetRelativeFilePath prefers it over the local copy,
    // and the referenced file never exists relative to a real project, so it silently loads as
    // an empty IsSourceFileMissing stub instead of the real behavior (no reported error).
    private static void StripStaleBehaviorSourcePaths(string xmlProjectPath)
    {
        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(xmlProjectPath);
        if (!loadResult.Success || loadResult.Project == null)
        {
            throw new InvalidOperationException(
                $"Failed to load the extracted Forms template to fix stale behavior source paths: {loadResult.ErrorMessage}");
        }

        bool anyChanged = false;
        foreach (var behaviorReference in loadResult.Project.BehaviorReferences)
        {
            if (!string.IsNullOrEmpty(behaviorReference.SourcePath))
            {
                behaviorReference.SourcePath = string.Empty;
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            loadResult.Project.Save(xmlProjectPath, saveElements: false);
        }
    }

    private static void ConvertExtractedTemplateToJson(string xmlProjectPath, List<string> extractedPaths)
    {
        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(xmlProjectPath);
        if (!loadResult.Success || loadResult.Project == null)
        {
            throw new InvalidOperationException(
                $"Failed to load the extracted Forms template for JSON conversion: {loadResult.ErrorMessage}");
        }

        IConvertProjectToJsonService convertService = new ConvertProjectToJsonService(new NullFileWatchIgnoreList());
        convertService.ConvertToJson(loadResult.Project);

        // The JSON siblings now hold everything the project references; the freshly-extracted XML
        // is dead weight for a brand-new project (nothing to preserve, unlike the opt-in
        // gumcli convert-to-json path this reuses, which is non-destructive by design).
        foreach (var path in extractedPaths)
        {
            var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            if (Array.IndexOf(ConvertibleXmlExtensions, extension) >= 0)
            {
                File.Delete(path);
            }
        }
    }

    private static string[] ReadManifest(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream(ManifestResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Forms template manifest not found as embedded resource '{ManifestResourceName}'.");
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines;
    }

    private static string BuildDestinationPath(string directory, string relativePath, string projectFileName)
    {
        if (relativePath == ProjectTemplateRelativePath)
        {
            return Path.Combine(directory, projectFileName);
        }

        return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ExtractResource(Assembly assembly, string resourceName, string destinationPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Forms template resource not found: '{resourceName}'. The manifest may be out of sync with the embedded resources.");
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var fileStream = File.Create(destinationPath);
        stream.CopyTo(fileStream);
    }
}
