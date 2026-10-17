using System.Collections.Generic;

namespace Gum.SettingsWindow;

/// <summary>
/// The pages plugins contribute to the Settings window. Gum's own General, Appearance and Plugins
/// pages are built by <see cref="SettingsDialogViewModel"/> each time it opens, so they never go stale.
/// </summary>
public interface ISettingsPageRegistry
{
    /// <summary>The registered pages, in registration order.</summary>
    IReadOnlyList<SettingsPage> Pages { get; }

    /// <summary>Adds <paramref name="page"/>, replacing a page with the same id.</summary>
    void Register(SettingsPage page);
}

/// <inheritdoc/>
public class SettingsPageRegistry : ISettingsPageRegistry
{
    private readonly List<SettingsPage> _pages;

    /// <summary>Creates an empty registry.</summary>
    public SettingsPageRegistry()
    {
        _pages = new List<SettingsPage>();
    }

    /// <inheritdoc/>
    public IReadOnlyList<SettingsPage> Pages => _pages;

    /// <inheritdoc/>
    public void Register(SettingsPage page)
    {
        int existing = _pages.FindIndex(candidate => candidate.Id == page.Id);
        if (existing >= 0)
        {
            _pages[existing] = page;
        }
        else
        {
            _pages.Add(page);
        }
    }
}
