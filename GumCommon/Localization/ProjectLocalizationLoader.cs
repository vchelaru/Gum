using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Localization;

/// <summary>
/// Loads a project's <see cref="GumProjectSave.LocalizationFiles"/> into an
/// <see cref="ILocalizationService"/>: the auto-load <c>GumService</c> runs when it loads a project.
/// </summary>
/// <remarks>
/// Policy mirrors the tool's <c>FileCommands.LoadLocalizationFile</c>: one file dispatches by
/// extension, several files must all be <c>.resx</c> (the service has no merge API for CSV), and
/// anything else is skipped with a warning.
/// </remarks>
public static class ProjectLocalizationLoader
{
    /// <summary>
    /// Loads <paramref name="project"/>'s localization files into <paramref name="service"/>.
    /// </summary>
    /// <param name="project">A project loaded from disk or a bundle, so it has a file name.</param>
    /// <param name="service">The service to populate.</param>
    /// <param name="bundleFileProvider">
    /// The bundle's provider when the project came from a <c>.gumpkg</c>, else <c>null</c>. A bundle
    /// has no directory to enumerate, so RESX satellites are found through the provider instead.
    /// </param>
    /// <param name="warnings">Receives non-fatal problems: skipped files and string ID collisions.</param>
    public static void Load(GumProjectSave project, ILocalizationService service,
        IGumFileProvider? bundleFileProvider, ICollection<string> warnings)
    {
        List<string> relativePaths = project.LocalizationFiles
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => path.Replace('\\', '/'))
            .ToList();
        if (relativePaths.Count == 0)
        {
            return;
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName!);

        if (relativePaths.Count == 1)
        {
            string relativePath = relativePaths[0];
            if (IsResx(relativePath))
            {
                LoadResx(service, relativePaths, projectDirectory, bundleFileProvider, onWarning: null);
            }
            else
            {
                using Stream stream = bundleFileProvider != null
                    ? bundleFileProvider.OpenRead(relativePath)
                    : FileManager.GetStreamForFile(projectDirectory + relativePath);
                service.AddCsvDatabase(stream);
            }
            return;
        }

        if (!relativePaths.All(IsResx))
        {
            warnings.Add(
                "Localization: multiple files configured but not all are .resx. " +
                "Mixed CSV/RESX and multi-CSV loading are not supported. Loading was skipped.");
            return;
        }

        List<string> existingPaths = new List<string>();
        foreach (string relativePath in relativePaths)
        {
            bool exists = bundleFileProvider != null
                ? bundleFileProvider.Exists(relativePath)
                : File.Exists(projectDirectory + relativePath);
            if (exists)
            {
                existingPaths.Add(relativePath);
            }
            else
            {
                warnings.Add($"Localization: file not found, skipping: {projectDirectory + relativePath}");
            }
        }

        if (existingPaths.Count > 0)
        {
            LoadResx(service, existingPaths, projectDirectory, bundleFileProvider,
                onWarning: message => warnings.Add("Localization warning: " + message));
        }
    }

    private static bool IsResx(string path) =>
        string.Equals(Path.GetExtension(path), ".resx", StringComparison.OrdinalIgnoreCase);

    private static void LoadResx(ILocalizationService service, List<string> relativePaths,
        string projectDirectory, IGumFileProvider? bundleFileProvider, Action<string>? onWarning)
    {
        if (bundleFileProvider == null)
        {
            // Loose files: the path overload discovers satellites with Directory.GetFiles.
            service.AddResxDatabase(relativePaths.Select(path => projectDirectory + path), onWarning);
            return;
        }

        List<Stream> openedStreams = new List<Stream>();
        try
        {
            var fileGroups = new List<(string? groupName, IEnumerable<(string languageName, Stream stream)> streams)>();
            foreach (string relativePath in relativePaths)
            {
                var languages = new List<(string languageName, Stream stream)>();
                foreach ((string languageName, string path) in GetResxLanguageFiles(bundleFileProvider, relativePath))
                {
                    Stream stream = bundleFileProvider.OpenRead(path);
                    openedStreams.Add(stream);
                    languages.Add((languageName, stream));
                }
                fileGroups.Add((Path.GetFileName(relativePath), languages));
            }
            service.AddResxDatabase(fileGroups, onWarning);
        }
        finally
        {
            foreach (Stream stream in openedStreams)
            {
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// The base file labeled "Default", then its <c>{BaseName}.{culture}.resx</c> satellites in the
    /// same directory, ordered by path: the same labels and order the path-based loader produces.
    /// </summary>
    private static IEnumerable<(string languageName, string path)> GetResxLanguageFiles(
        IGumFileProvider provider, string baseRelativePath)
    {
        yield return ("Default", baseRelativePath);

        int slash = baseRelativePath.LastIndexOf('/');
        string directory = slash < 0 ? string.Empty : baseRelativePath.Substring(0, slash + 1);
        string baseName = Path.GetFileNameWithoutExtension(baseRelativePath);

        // A pattern without '/' matches file names in every folder, so keep only this folder's.
        // Bundle lookups are case-sensitive, as every bundle entry lookup is. The default comparer
        // matches the path-based loader's OrderBy, so language columns line up in both modes.
        IEnumerable<string> satellites = provider.EnumerateFiles(directory + baseName + ".*.resx")
            .Where(path => path.LastIndexOf('/') == slash
                && string.CompareOrdinal(path, 0, directory, 0, directory.Length) == 0)
            .OrderBy(path => path);

        foreach (string satellite in satellites)
        {
            string satelliteName = Path.GetFileNameWithoutExtension(satellite);
            yield return (satelliteName.Substring(baseName.Length + 1), satellite);
        }
    }
}
