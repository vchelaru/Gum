using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using ToolsUtilities;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The files a rename changes beyond what <see cref="GumProjectSave.Save(string, bool)"/> writes: the element's
/// animation file (<c>.ganx</c>/<c>.ganj</c>), which follows the element and names its instances and states, the
/// element's <c>.codsj</c> code settings, and a behavior's file. Edits are made in memory while the operations run,
/// so the error check sees them through <see cref="IElementAnimationsProvider"/>, and reach disk only in
/// <see cref="Commit"/>, after the project has saved.
/// </summary>
public class PendingFileChanges : IElementAnimationsProvider
{
    private readonly string? _projectDirectory;
    private readonly bool _isJsonFormat;
    private readonly Dictionary<ElementSave, string> _originalNames = new Dictionary<ElementSave, string>();
    private readonly Dictionary<(string Subfolder, string Name), ElementAnimationsSave?> _animations =
        new Dictionary<(string, string), ElementAnimationsSave?>();
    private readonly HashSet<(string Subfolder, string Name)> _changedAnimations = new HashSet<(string, string)>();
    private readonly List<(BehaviorSave Behavior, string OldName)> _renamedBehaviors = new List<(BehaviorSave, string)>();

    /// <summary>
    /// Creates the changes for <paramref name="project"/>. A project that was never saved has no folder, so
    /// there are no files to follow.
    /// </summary>
    public PendingFileChanges(GumProjectSave project)
    {
        if (!string.IsNullOrEmpty(project.FullFileName))
        {
            _projectDirectory = FileManager.GetDirectory(project.FullFileName);
            _isJsonFormat = GumProjectSave.IsJsonFormat(project.FullFileName);
        }
    }

    /// <summary>Whether anything is waiting to be written.</summary>
    public bool HasChanges => _originalNames.Count > 0 || _changedAnimations.Count > 0 || _renamedBehaviors.Count > 0;

    /// <inheritdoc/>
    public ElementAnimationsSave? GetAnimationsFor(ElementSave element, GumProjectSave project) => Load(element);

    /// <summary>Records that <paramref name="element"/>, now named <c>element.Name</c>, was called <paramref name="oldName"/>.</summary>
    public void ElementRenamed(ElementSave element, string oldName)
    {
        if (!_originalNames.ContainsKey(element))
        {
            _originalNames[element] = oldName;
        }
    }

    /// <summary>Records that <paramref name="behavior"/> was called <paramref name="oldName"/>.</summary>
    public void BehaviorRenamed(BehaviorSave behavior, string oldName)
    {
        _renamedBehaviors.Add((behavior, oldName));
    }

    /// <summary>Points the keyframes of <paramref name="element"/> that play an animation of the instance at its new name.</summary>
    public void InstanceRenamed(ElementSave element, string oldName, string newName)
    {
        if (Load(element) is not { } animations)
        {
            return;
        }

        bool changed = false;
        foreach (AnimationReferenceSave keyframe in animations.Animations.SelectMany(item => item.Animations))
        {
            if (keyframe.SourceObject == oldName)
            {
                keyframe.Name = newName + "." + keyframe.RootName;
                changed = true;
            }
        }
        MarkChanged(element, changed);
    }

    /// <summary>Points the keyframes of <paramref name="element"/> that show a state at its new name.</summary>
    public void StateRenamed(ElementSave element, string? categoryName, string oldName, string newName)
    {
        string prefix = categoryName == null ? "" : categoryName + "/";
        RenameStateKeyframes(element, prefix + oldName, prefix + newName, exact: true);
    }

    /// <summary>Points the keyframes of <paramref name="element"/> that show a state of the category at its new name.</summary>
    public void CategoryRenamed(ElementSave element, string oldName, string newName)
    {
        RenameStateKeyframes(element, oldName + "/", newName + "/", exact: false);
    }

    private void RenameStateKeyframes(ElementSave element, string oldName, string newName, bool exact)
    {
        if (Load(element) is not { } animations)
        {
            return;
        }

        bool changed = false;
        foreach (AnimatedStateSave keyframe in animations.Animations.SelectMany(item => item.States))
        {
            if (exact ? keyframe.StateName == oldName : keyframe.StateName.StartsWith(oldName))
            {
                keyframe.StateName = newName + keyframe.StateName.Substring(oldName.Length);
                changed = true;
            }
        }
        MarkChanged(element, changed);
    }

    private void MarkChanged(ElementSave element, bool changed)
    {
        if (changed)
        {
            _changedAnimations.Add(Key(element));
        }
    }

    private (string Subfolder, string Name) Key(ElementSave element) =>
        (element.Subfolder, _originalNames.TryGetValue(element, out string? original) ? original : element.Name);

    private string? AnimationPath(string subfolder, string name) =>
        _projectDirectory == null
            ? null
            : _projectDirectory + subfolder + "/" + name + ElementAnimationsSave.GetFileNameSuffix(_isJsonFormat);

    private ElementAnimationsSave? Load(ElementSave element)
    {
        (string Subfolder, string Name) key = Key(element);
        if (_animations.TryGetValue(key, out ElementAnimationsSave? cached))
        {
            return cached;
        }

        string? path = AnimationPath(key.Subfolder, key.Name);
        ElementAnimationsSave? loaded = path != null && File.Exists(path) ? ElementAnimationsSave.Load(path) : null;
        _animations[key] = loaded;
        return loaded;
    }

    /// <summary>
    /// Writes the changed animation files and moves the files of renamed elements and behaviors. Call after the
    /// project has saved. An element's files are not overwritten at the new name when something is already there.
    /// </summary>
    public void Commit(GumProjectSave project)
    {
        if (_projectDirectory == null)
        {
            return;
        }

        foreach (KeyValuePair<ElementSave, string> renamed in _originalNames.Where(pair => pair.Key.Name != pair.Value))
        {
            ElementSave element = renamed.Key;
            string oldName = renamed.Value;
            (string Subfolder, string Name) key = (element.Subfolder, oldName);

            string? oldAnimation = AnimationPath(key.Subfolder, oldName);
            string? newAnimation = AnimationPath(element.Subfolder, element.Name);
            if (_changedAnimations.Remove(key) && _animations[key] is { } animations && newAnimation != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newAnimation)!);
                animations.Save(newAnimation);
                if (oldAnimation != null && !IsSamePath(oldAnimation, newAnimation))
                {
                    File.Delete(oldAnimation);
                }
            }
            else if (oldAnimation != null && newAnimation != null)
            {
                Move(oldAnimation, newAnimation);
            }

            string oldSettings = _projectDirectory + element.Subfolder + "/" + oldName + ".codsj";
            Move(oldSettings, _projectDirectory + element.Subfolder + "/" + element.Name + ".codsj");
        }

        foreach ((string subfolder, string name) in _changedAnimations)
        {
            if (_animations[(subfolder, name)] is { } animations && AnimationPath(subfolder, name) is { } path)
            {
                animations.Save(path);
            }
        }

        bool useCompact = project.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
        foreach ((BehaviorSave behavior, string oldName) in _renamedBehaviors)
        {
            string extension = _isJsonFormat ? BehaviorReference.JsonExtension : BehaviorReference.Extension;
            string folder = _projectDirectory + BehaviorReference.Subfolder + "/";
            behavior.Save(folder + behavior.Name + "." + extension, useCompact);
            string oldFile = folder + oldName + "." + extension;
            if (File.Exists(oldFile))
            {
                File.Delete(oldFile);
            }
        }
    }

    private static bool IsSamePath(string first, string second) =>
        string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), System.StringComparison.OrdinalIgnoreCase);

    // A file already at the destination belongs to some other element, so both stay.
    private static void Move(string oldPath, string newPath)
    {
        FilePath source = new FilePath(oldPath);
        FilePath destination = new FilePath(newPath);
        if (!source.Exists() || source.FullPath == destination.FullPath)
        {
            return;
        }
        if (destination.Exists() && !SidecarFileMover.IsSameFileWithDifferentCase(source, destination))
        {
            return;
        }
        SidecarFileMover.Move(source, destination);
    }
}
