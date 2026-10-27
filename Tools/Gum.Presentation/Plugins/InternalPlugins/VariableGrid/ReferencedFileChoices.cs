using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.ToolStates;

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
        HashSet<string> choices = new HashSet<string>(StringComparer.Ordinal);

        if (_projectState.GumProjectSave is { FullFileName: { Length: > 0 } projectFile } project)
        {
            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            WalkResult walk = new GumProjectDependencyWalker().Walk(project, projectDirectory, GumBundleInclusion.ExternalFiles);

            foreach (string file in walk.ExternalFiles)
            {
                // The walk lists a reference whether or not its file exists.
                if (isAccepted(file) && File.Exists(Path.Combine(projectDirectory, file)))
                {
                    choices.Add(file);
                }
            }
        }

        if (!string.IsNullOrEmpty(currentValue))
        {
            choices.Add(currentValue);
        }

        return choices.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal).ToList();
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
