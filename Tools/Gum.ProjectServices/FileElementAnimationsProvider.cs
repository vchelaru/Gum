using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <summary>
/// Loads each element's animations from its <c>.ganx</c> on disk, caching the deserialized result
/// and invalidating it by the file's last-write time. Editing an element's <em>states</em> (the
/// common way an animation error is introduced) does not touch the <c>.ganx</c>, so those checks
/// hit the cache; only editing an animation itself re-reads. Suitable as an app-lifetime singleton
/// (tool) or a per-run instance (CLI).
/// </summary>
public class FileElementAnimationsProvider : IElementAnimationsProvider
{
    private readonly Dictionary<string, CachedAnimations> _cache = new Dictionary<string, CachedAnimations>();

    /// <inheritdoc/>
    public ElementAnimationsSave? GetAnimationsFor(ElementSave element, GumProjectSave project)
    {
        if (string.IsNullOrEmpty(project?.FullFileName))
        {
            return null;
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName);
        FilePath? elementXmlPath = ElementFilePathHelper.GetFullPathXmlFile(element, projectDirectory);
        if (elementXmlPath == null)
        {
            return null;
        }

        FilePath animationFilePath = elementXmlPath.RemoveExtension() + "Animations.ganx";
        if (!animationFilePath.Exists())
        {
            return null;
        }

        string fullPath = animationFilePath.FullPath;
        DateTime lastWriteUtc = File.GetLastWriteTimeUtc(fullPath);

        if (_cache.TryGetValue(fullPath, out CachedAnimations cached) && cached.LastWriteUtc == lastWriteUtc)
        {
            return cached.Animations;
        }

        ElementAnimationsSave animations = FileManager.XmlDeserialize<ElementAnimationsSave>(fullPath);
        _cache[fullPath] = new CachedAnimations(lastWriteUtc, animations);
        return animations;
    }

    private readonly struct CachedAnimations
    {
        public CachedAnimations(DateTime lastWriteUtc, ElementAnimationsSave animations)
        {
            LastWriteUtc = lastWriteUtc;
            Animations = animations;
        }

        public DateTime LastWriteUtc { get; }
        public ElementAnimationsSave Animations { get; }
    }
}
