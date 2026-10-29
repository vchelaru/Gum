using Gum.DataTypes;
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
}

/// <inheritdoc cref="IProjectReferencedFiles"/>
/// <remarks>
/// The walk parses every referenced animation chain and font file from disk, so asking per file event costs
/// milliseconds each; a build writing hundreds of files would otherwise hold the UI thread. Whoever changes
/// what is referenced (an element, behavior or project reload, an edit of a variable) calls
/// <see cref="Invalidate"/>. Used on the UI thread only.
/// </remarks>
public sealed class ProjectReferencedFiles : IProjectReferencedFiles
{
    private readonly Func<IEnumerable<string>> _walkProject;
    private HashSet<FilePath>? _referenced;

    public ProjectReferencedFiles()
        : this(WalkProject)
    {
    }

    /// <param name="walkProject">The walk of the project, replaceable to count how often it runs.</param>
    public ProjectReferencedFiles(Func<IEnumerable<string>> walkProject)
    {
        _walkProject = walkProject;
    }

    private static IEnumerable<string> WalkProject() =>
        ObjectFinder.Self.GetAllFilesInProject(Gum.Bundle.GumBundleInclusion.Core | Gum.Bundle.GumBundleInclusion.ExternalFiles);

    public bool Contains(FilePath file)
    {
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
        }
        return _referenced.Contains(file);
    }

    public void Invalidate()
    {
        _referenced = null;
    }
}
