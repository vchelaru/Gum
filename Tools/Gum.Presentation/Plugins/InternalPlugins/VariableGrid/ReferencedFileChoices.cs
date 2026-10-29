using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The files a file variable's drop-down offers: the files the project already references that
/// the variable accepts. Paths are project-relative with forward slashes, as a variable stores them.
/// </summary>
public class ReferencedFileChoices
{
    private readonly IProjectState _projectState;

    public ReferencedFileChoices(IProjectState projectState)
    {
        _projectState = projectState;
    }

    /// <summary>
    /// Every file on disk that the project references and <paramref name="isAccepted"/> allows,
    /// alphabetically. <paramref name="currentValue"/> is always included, so a row can show a value
    /// the project no longer references or whose file is gone. The walk reads the project's
    /// elements, so call it when the list is shown, not on every grid refresh.
    /// </summary>
    public IReadOnlyList<string> GetChoices(Func<string, bool> isAccepted, string? currentValue)
    {
        // One entry per file, whatever way the project spells its path ("./a.png", "x/../a.png",
        // the absolute path, another case on a case-insensitive file system).
        Dictionary<FilePath, string> choices = new Dictionary<FilePath, string>();
        string? projectDirectory = null;

        if (_projectState.GumProjectSave is { FullFileName: { Length: > 0 } projectFile } project)
        {
            projectDirectory = Path.GetDirectoryName(projectFile)!;
            // A walk of each element on its own lists what that element's variables name. The
            // project-wide walk also adds the files those files load (an .achx's textures, a
            // .fnt's pages), which nobody picks directly.
            GumProjectDependencyWalker walker = new GumProjectDependencyWalker();
            IEnumerable<string> referenced = project.AllElements
                .SelectMany(element => walker.Walk(project, projectDirectory, GumBundleInclusion.ExternalFiles, element).ExternalFiles)
                .Distinct(StringComparer.Ordinal);

            foreach (string file in referenced)
            {
                // The walk lists a reference whether or not its file exists.
                string fullPath = Path.GetFullPath(Path.Combine(projectDirectory, file));
                if (isAccepted(file) && File.Exists(fullPath))
                {
                    choices.TryAdd(new FilePath(fullPath), Path.GetRelativePath(projectDirectory, fullPath).Replace('\\', '/'));
                }
            }
        }

        if (!string.IsNullOrEmpty(currentValue))
        {
            // The row shows the value as stored, so it keeps its own spelling in the list.
            string currentFullPath = projectDirectory == null || Path.IsPathRooted(currentValue)
                ? currentValue
                : Path.GetFullPath(Path.Combine(projectDirectory, currentValue));
            choices[new FilePath(currentFullPath)] = currentValue;
        }

        return choices.Values.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// A predicate for the extensions in a file picker filter such as <c>Effect File|*.fx;*.slang</c>,
    /// so the drop-down accepts what the picker shows. A <c>*.*</c> pattern accepts everything.
    /// </summary>
    public static Func<string, bool> AcceptingExtensions(string filter)
    {
        string patterns = filter.Contains('|') ? filter.Substring(filter.LastIndexOf('|') + 1) : filter;
        string[] extensions = patterns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pattern => pattern.TrimStart('*'))
            .ToArray();

        return path => extensions.Any(extension =>
            extension == ".*" || path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}
