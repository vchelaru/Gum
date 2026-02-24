using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace GumFormsPlugin.Services;

public class FormsFileService
{
    private const string FormsProjectSubfolder = "Content/FormsGumProject";
    private const string FormsGumxName = "GumProject.gumx";

    /// <summary>
    /// Returns the path to the GumProject.gumx file shipped with the tool.
    /// </summary>
    public string GetFormsGumxPath() =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FormsProjectSubfolder, FormsGumxName)
            .Replace('\\', '/');

    /// <summary>
    /// Returns the base directory of the FormsGumProject files.
    /// </summary>
    public string GetFormsProjectDirectory() =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FormsProjectSubfolder)
            .Replace('\\', '/') + "/";

    /// <summary>
    /// Returns a mapping of source file paths (in the FormsGumProject) to destination
    /// file paths (in the user's Gum project directory).
    /// Extensions skipped: .gumx, .gumfcs, .ganx (animation files, deferred), .codsj
    /// </summary>
    public Dictionary<string, FilePath> GetSourceDestinations(bool isIncludeDemoScreenGum)
    {
        var projectState = Locator.GetRequiredService<IProjectState>();
        var destinationFolder = projectState.ProjectDirectory;

        var sourceDestinations = new Dictionary<string, FilePath>();

        if (string.IsNullOrEmpty(destinationFolder)) return sourceDestinations;

        string formsDir = GetFormsProjectDirectory();

        if (!Directory.Exists(formsDir)) return sourceDestinations;

        var allFiles = Directory.GetFiles(formsDir, "*.*", SearchOption.AllDirectories);

        // Compute the canonical path of the root-level FontCache folder once, outside the loop
        string fontCachePath = new DirectoryInfo(Path.Combine(formsDir, "FontCache")).FullName
            + Path.DirectorySeparatorChar;

        foreach (var sourceFile in allFiles)
        {
            var extension = FileManager.GetExtension(sourceFile);

            // Skip font cache — fonts are regenerated locally and should not be copied
            if (new FileInfo(sourceFile).FullName.StartsWith(fontCachePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip files that are not content or not relevant to import
            if (extension is "gumx" or "gumfcs" or 
                "ganx" or "codsj" or "bmfc" or "fnt" or "exe")
            {
                continue;
            }

            // Only include the demo screen if requested
            if (extension == "gusx")
            {
                bool isDemoScreen = sourceFile.Contains("DemoScreenGum.gusx");
                if (!isDemoScreen || !isIncludeDemoScreenGum)
                {
                    continue;
                }
            }

            // Compute the relative path from the forms project directory
            string relativePath = sourceFile
                .Replace('\\', '/')
                .Substring(formsDir.Length)
                .TrimStart('/');

            string absoluteDestination = destinationFolder + relativePath;
            sourceDestinations.Add(sourceFile, absoluteDestination);
        }

        return sourceDestinations;
    }
}
