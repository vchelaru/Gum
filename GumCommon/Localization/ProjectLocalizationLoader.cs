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
/// <see cref="ILocalizationService"/>. The runtime's <c>GumService</c>, the tool and gumcli all load
/// through here, each reporting problems its own way through <see cref="ProjectLocalizationLoadOptions"/>.
/// </summary>
/// <remarks>
/// Policy: one file dispatches by extension, several files must all be <c>.resx</c> (the service has
/// no merge API for CSV) or loading is skipped, and a missing file is skipped while the rest load.
/// Paths may use either separator: bundle lookups use forward slashes, loose-file paths use native
/// separators. A file that exists but can't be parsed throws; the caller decides how to report it.
/// </remarks>
public static class ProjectLocalizationLoader
{
    /// <summary>
    /// Loads <paramref name="project"/>'s localization files into <paramref name="service"/>,
    /// resolving them against the project file's directory.
    /// </summary>
    /// <param name="project">A project loaded from disk or a bundle, so it has a file name.</param>
    /// <param name="service">The service to populate.</param>
    /// <param name="bundleFileProvider">
    /// The bundle's provider when the project came from a <c>.gumpkg</c>, else <c>null</c>.
    /// </param>
    /// <param name="warnings">Receives non-fatal problems: skipped files and string ID collisions.</param>
    public static void Load(GumProjectSave project, ILocalizationService service,
        IGumFileProvider? bundleFileProvider, ICollection<string> warnings)
    {
        Load(project, FileManager.GetDirectory(project.FullFileName!), service, new ProjectLocalizationLoadOptions
        {
            BundleFileProvider = bundleFileProvider,
            OnSkipped = warnings.Add,
            OnWarning = warnings.Add,
        });
    }

    /// <summary>
    /// Loads <paramref name="project"/>'s localization files into <paramref name="service"/>,
    /// resolving them against <paramref name="projectDirectory"/>.
    /// </summary>
    /// <param name="project">The project whose <see cref="GumProjectSave.LocalizationFiles"/> load.</param>
    /// <param name="projectDirectory">The folder the project-relative paths resolve against.</param>
    /// <param name="service">The service to populate.</param>
    /// <param name="options">Where files come from and where problems are reported.</param>
    public static void Load(GumProjectSave project, string projectDirectory, ILocalizationService service,
        ProjectLocalizationLoadOptions options)
    {
        List<string> relativePaths = ToRelativePaths(project.LocalizationFiles);
        if (relativePaths.Count == 0)
        {
            return;
        }

        if (!projectDirectory.EndsWith("/") && !projectDirectory.EndsWith("\\"))
        {
            projectDirectory += "/";
        }

        if (relativePaths.Count > 1 && !relativePaths.All(IsResx))
        {
            options.OnSkipped?.Invoke(
                "Localization: multiple files configured but not all are .resx. " +
                "Mixed CSV/RESX and multi-CSV loading are not supported. Loading was skipped.");
            return;
        }

        IGumFileProvider? bundleFileProvider = options.BundleFileProvider;
        List<string> existingPaths = new List<string>();
        foreach (string relativePath in relativePaths)
        {
            bool exists = bundleFileProvider != null
                ? bundleFileProvider.Exists(relativePath)
                : File.Exists(ToLooseFilePath(projectDirectory, relativePath));
            if (exists)
            {
                existingPaths.Add(relativePath);
            }
            else
            {
                options.OnSkipped?.Invoke(
                    $"Localization: file not found, skipping: {ToLooseFilePath(projectDirectory, relativePath)}");
            }
        }

        if (existingPaths.Count == 0)
        {
            return;
        }

        if (IsResx(existingPaths[0]))
        {
            Action<string>? onWarning = options.OnWarning;
            LoadResx(service, existingPaths, projectDirectory, bundleFileProvider,
                onWarning == null ? null : message => onWarning("Localization warning: " + message));
        }
        else
        {
            // Only a single file gets here: several files are all RESX by now.
            LoadCsv(service, existingPaths[0], projectDirectory, bundleFileProvider);
        }
    }

    /// <summary>
    /// The non-empty entries of <paramref name="localizationFiles"/> with forward slashes, the form
    /// bundle entries are keyed by. A project saved on Windows stores backslashes.
    /// </summary>
    internal static List<string> ToRelativePaths(IEnumerable<string?> localizationFiles) =>
        localizationFiles
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => path!.Replace('\\', '/'))
            .ToList();

    /// <summary>
    /// Joins a project-relative path onto <paramref name="projectDirectory"/> with native separators.
    /// Loose paths go to File.Exists and Directory.GetFiles, and macOS/Linux read a backslash as part
    /// of the file name.
    /// </summary>
    internal static string ToLooseFilePath(string projectDirectory, string relativePath) =>
        FileManager.Standardize(projectDirectory + relativePath, preserveCase: true);

    private static bool IsResx(string path) =>
        string.Equals(Path.GetExtension(path), ".resx", StringComparison.OrdinalIgnoreCase);

    private static void LoadCsv(ILocalizationService service, string relativePath, string projectDirectory,
        IGumFileProvider? bundleFileProvider)
    {
        if (bundleFileProvider != null)
        {
            using Stream bundleStream = bundleFileProvider.OpenRead(relativePath);
            service.AddCsvDatabase(bundleStream);
            return;
        }

        using Stream stream = FileManager.GetStreamForFile(ToLooseFilePath(projectDirectory, relativePath));
        service.AddCsvDatabase(stream);
    }

    private static void LoadResx(ILocalizationService service, List<string> relativePaths,
        string projectDirectory, IGumFileProvider? bundleFileProvider, Action<string>? onWarning)
    {
        if (bundleFileProvider == null)
        {
            // Loose files: the path overload discovers satellites with Directory.GetFiles.
            service.AddResxDatabase(relativePaths.Select(path => ToLooseFilePath(projectDirectory, path)), onWarning);
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
