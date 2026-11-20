using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using ToolsUtilities;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The animation sidecars an edit run has changed and not yet written. An element's animations live beside its file
/// rather than in the project, so they are loaded on first change, held here, and written together by
/// <see cref="Save"/> before the project saves, so a failed write leaves the project unsaved. Reads see the held copy, so the error check and the name
/// checks judge the animations as edited.
/// </summary>
public class PendingAnimationEdits : IElementAnimationsProvider, ITopLevelNameSource
{
    private readonly IElementAnimationsProvider _onDisk;
    private readonly List<(string Path, byte[]? Content)> _backups = new List<(string, byte[]?)>();
    private readonly Dictionary<ElementSave, ElementAnimationsSave> _edited = new Dictionary<ElementSave, ElementAnimationsSave>();

    /// <param name="onDisk">Reads the sidecars the run has not changed.</param>
    public PendingAnimationEdits(IElementAnimationsProvider onDisk)
    {
        _onDisk = onDisk;
    }

    /// <inheritdoc/>
    public ElementAnimationsSave? GetAnimationsFor(ElementSave element, GumProjectSave project) =>
        _edited.TryGetValue(element, out ElementAnimationsSave? edited) ? edited : _onDisk.GetAnimationsFor(element, project);

    /// <summary>
    /// The animations of <paramref name="element"/> to change: the content of its sidecar, or an empty set when it has
    /// none. Whatever is changed in it is written by <see cref="Save"/>.
    /// </summary>
    public ElementAnimationsSave GetForEdit(ElementSave element, GumProjectSave project)
    {
        if (!_edited.TryGetValue(element, out ElementAnimationsSave? edited))
        {
            FilePath? path = ElementAnimationFiles.GetPath(element, project);
            try
            {
                edited = path != null && path.Exists()
                    ? ElementAnimationsSave.Load(path.FullPath)
                    : new ElementAnimationsSave();
            }
            catch (Exception ex)
            {
                throw new EditException($"The animation file {path} could not be read, so it was not changed: {ex.Message}");
            }
            _edited[element] = edited;
        }
        return edited;
    }

    /// <summary>
    /// Writes every changed sidecar, deleting one left with no animations. Skips an element the project no longer has.
    /// If a write fails, the sidecars already written are put back and an IOException naming the file is thrown. Keeps what it
    /// replaced so <see cref="Rollback"/> can undo a successful save when a later step fails.
    /// </summary>
    public void Save(GumProjectSave project)
    {
        _backups.Clear();
        string? failingPath = null;
        try
        {
            foreach ((ElementSave element, ElementAnimationsSave animations) in _edited)
            {
                if (!project.AllElements.Contains(element))
                {
                    continue;
                }
                if (ElementAnimationFiles.GetPath(element, project) is { } path)
                {
                    failingPath = path.FullPath;
                    byte[]? original = File.Exists(failingPath) ? File.ReadAllBytes(failingPath) : null;
                    ElementAnimationFiles.WriteOrDelete(failingPath, animations);
                    // Recorded only once written: a file that failed to write has nothing to put back.
                    _backups.Add((failingPath, original));
                }
            }
        }
        catch (Exception ex)
        {
            string restoreFailures = Rollback();
            throw new IOException($"cannot write {failingPath}: {ex.Message}{restoreFailures}", ex);
        }
    }

    /// <summary>
    /// Puts back every sidecar the last <see cref="Save"/> replaced, newest first. Never throws: each file is
    /// restored on its own, and the result names any that could not be (empty when all were).
    /// </summary>
    public string Rollback()
    {
        string failures = "";
        for (int i = _backups.Count - 1; i >= 0; i--)
        {
            (string path, byte[]? content) = _backups[i];
            try
            {
                if (content != null)
                {
                    File.WriteAllBytes(path, content);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                failures += $" Could not put back {path}: {ex.Message}";
            }
        }
        _backups.Clear();
        return failures;
    }

    /// <inheritdoc/>
    public void FillTopLevelNames(ElementSave element, List<TopLevelName> names)
    {
        if (ObjectFinder.Self.GumProjectSave is { } project)
        {
            AnimationTopLevelNames.Add(GetAnimationsFor(element, project), names);
        }
    }
}
