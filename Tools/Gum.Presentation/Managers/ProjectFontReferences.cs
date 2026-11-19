using System.Text;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.ProjectServices;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Managers;

/// <summary>
/// One <see cref="FontReferenceCache"/> for the loaded project, shared by the tree's error icons, the
/// Errors tab, the file watch and font generation until the project changes. Project load runs all four
/// back to back, so each element's fonts are collected once instead of once per pass.
/// </summary>
/// <remarks>
/// A new cache starts when <see cref="IProjectChangeVersion"/> moves, when another project is passed,
/// or when a project-wide font setting differs (spacing, ranges, project version), which a settings
/// page can change without a plugin notification.
/// </remarks>
public sealed class ProjectFontReferences : IFontReferenceCacheSource
{
    private readonly IProjectChangeVersion _changeVersion;
    private readonly object _lock = new object();
    private FontReferenceCache? _cache;
    private GumProjectSave? _project;
    private long _version;
    private string? _settings;

    public ProjectFontReferences(IProjectChangeVersion changeVersion)
    {
        _changeVersion = changeVersion;
    }

    /// <summary>How many caches this has started; a test reads it to see when a cache was replaced.</summary>
    public int CachesStarted { get; private set; }

    /// <summary>Single-element collections run by every cache this has started.</summary>
    public int CollectionCount => _previousCollections + (_cache?.CollectionCount ?? 0);

    private int _previousCollections;

    /// <inheritdoc/>
    public FontReferenceCache For(GumProjectSave project)
    {
        lock (_lock)
        {
            long version = _changeVersion.Version;
            string settings = DescribeFontSettings(project);
            if (_cache == null || !ReferenceEquals(_project, project) || _version != version || _settings != settings)
            {
                _previousCollections += _cache?.CollectionCount ?? 0;
                _cache = new FontReferenceCache();
                _project = project;
                _version = version;
                _settings = settings;
                CachesStarted++;
            }
            return _cache;
        }
    }

    // Everything project-wide that FontReferenceCollector reads into a BmfcSave.
    private static string DescribeFontSettings(GumProjectSave project)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(project.Version).Append('|')
            .Append(project.FontSpacingHorizontal).Append('|')
            .Append(project.FontSpacingVertical).Append('|')
            .Append(BmfcSave.GetEffectiveDefaultRanges());
        foreach (FontRangeEntry entry in project.FontRangeEntries)
        {
            builder.Append('|').Append(entry.FontFamily).Append('=').Append(entry.Ranges);
        }
        return builder.ToString();
    }
}
