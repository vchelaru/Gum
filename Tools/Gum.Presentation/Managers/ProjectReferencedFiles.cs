using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// The files the project's elements reference (a variable's value, an animation chain's frame pictures, a
/// font's pages), answered from a set that is built by one walk of the project and kept until
/// <see cref="Invalidate"/>.
/// </summary>
public interface IProjectReferencedFiles
{
    /// <summary>True when some element in the project references <paramref name="file"/>.</summary>
    bool Contains(FilePath file);

    /// <summary>Forgets the set, so the next <see cref="Contains"/> walks the project again.</summary>
    void Invalidate();

    /// <summary>
    /// An edit that can add a reference only through a variable's value (an instance or variable edit, a canvas
    /// refresh), never through what the project lists (elements, behaviors, localization). The set is kept;
    /// <see cref="Contains"/> walks again only for a file some variable now names, or when a variable names an
    /// animation chain or font the set does not hold, whose pictures the set would then miss.
    /// </summary>
    void InvalidateFilesNamedByVariables();
}

/// <inheritdoc cref="IProjectReferencedFiles"/>
/// <remarks>
/// The walk parses every referenced animation chain and font file from disk, so asking per file event costs
/// milliseconds each; a build writing hundreds of files would otherwise hold the UI thread. Whoever changes
/// what is referenced (an element, behavior or project reload, an edit of a variable) calls
/// <see cref="Invalidate"/> or <see cref="InvalidateFilesNamedByVariables"/>. After the latter the kept set may
/// still hold a file nothing references any more, which costs at most a needless canvas reload. Used on the UI
/// thread only.
/// </remarks>
public sealed class ProjectReferencedFiles : IProjectReferencedFiles
{
    private readonly Func<IEnumerable<string>> _walkProject;
    private readonly Func<IEnumerable<string>> _variableValues;
    private HashSet<FilePath>? _referenced;
    private bool _variablesChanged;

    // The file names (no folder) of every variable value, built once after a variable edit.
    private HashSet<string>? _namedFileNames;

    public ProjectReferencedFiles()
        : this(WalkProject)
    {
    }

    /// <param name="walkProject">The walk of the project, replaceable to count how often it runs.</param>
    public ProjectReferencedFiles(Func<IEnumerable<string>> walkProject)
    {
        _walkProject = walkProject;
        _variableValues = VariableValuesInProject;
    }

    private static IEnumerable<string> WalkProject() =>
        // Resolving every Text instance's effective font is nearly all of a walk's cost and only finds font
        // files named through a base type; this walk runs after most edits, so it leaves that out.
        ObjectFinder.Self.GetAllFilesInProject(
            Gum.Bundle.GumBundleInclusion.Core | Gum.Bundle.GumBundleInclusion.ExternalFiles, resolveFontFiles: false);

    /// <summary>
    /// Every non-empty string a variable holds, resolved against the project folder the way the walk resolves a
    /// file variable, whatever the variable is. A superset of what the walk reads from the elements' states.
    /// </summary>
    private static IEnumerable<string> VariableValuesInProject()
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null || string.IsNullOrEmpty(project.FullFileName))
        {
            yield break;
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName);
        if (!string.IsNullOrEmpty(project.SinglePixelTextureFile))
        {
            yield return projectDirectory + project.SinglePixelTextureFile;
        }

        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Value is string value && value.Length > 0)
                    {
                        yield return System.IO.Path.IsPathRooted(value) ? value : projectDirectory + value;
                    }
                }
            }
        }
    }

    public bool Contains(FilePath file)
    {
        if (_referenced != null && _variablesChanged && !_referenced.Contains(file) && MayBeNewlyNamed(file))
        {
            _referenced = null;
        }

        if (_referenced == null)
        {
            HashSet<FilePath> referenced = new HashSet<FilePath>();
            foreach (string path in _walkProject())
            {
                try
                {
                    referenced.Add(new FilePath(path));
                }
                catch
                {
                    // An invalid path in a variable cannot be the file that changed.
                }
            }
            _referenced = referenced;
            _variablesChanged = false;
            _namedFileNames = null;
        }
        return _referenced.Contains(file);
    }

    /// <summary>
    /// True when a variable names a file called like <paramref name="file"/>, or names an animation chain or font
    /// the kept set does not hold (its pictures could be <paramref name="file"/>). Only then can a variable edit
    /// have made <paramref name="file"/> referenced.
    /// </summary>
    private bool MayBeNewlyNamed(FilePath file)
    {
        if (_namedFileNames == null)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string value in _variableValues())
            {
                string name = FileNameOf(value);
                if (IsListerOfOtherFiles(name) && !IsInKeptSet(value))
                {
                    // Its pictures are not in the set, so only a walk knows them.
                    return true;
                }
                names.Add(name);
            }
            _namedFileNames = names;
        }
        return _namedFileNames.Contains(FileNameOf(file.FullPath));
    }

    private bool IsInKeptSet(string path)
    {
        try
        {
            return _referenced!.Contains(new FilePath(path));
        }
        catch
        {
            // An invalid path is not a file the walk would parse.
            return true;
        }
    }

    private static bool IsListerOfOtherFiles(string fileName) =>
        fileName.EndsWith(".achx", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".achj", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase);

    private static string FileNameOf(string path) => path.Substring(path.LastIndexOfAny(new[] { '/', '\\' }) + 1);

    public void Invalidate()
    {
        _referenced = null;
    }

    public void InvalidateFilesNamedByVariables()
    {
        _variablesChanged = true;
        _namedFileNames = null;
    }
}
