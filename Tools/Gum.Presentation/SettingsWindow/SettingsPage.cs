using System;
using System.Collections.Generic;

namespace Gum.SettingsWindow;

/// <summary>Which group of the Settings window a page belongs to.</summary>
public enum SettingsSection
{
    /// <summary>Applies to this machine, whatever project is open.</summary>
    Application,

    /// <summary>Stored in the open project; its pages are only listed while a project is loaded.</summary>
    Project,
}

/// <summary>The ids of the pages Gum itself provides, for opening the Settings window on one.</summary>
public static class SettingsPageIds
{
    /// <summary>Application > General.</summary>
    public const string General = "general";

    /// <summary>Application > Appearance.</summary>
    public const string Appearance = "appearance";

    /// <summary>Application > Keyboard shortcuts.</summary>
    public const string KeyboardShortcuts = "keyboard-shortcuts";

    /// <summary>Application > Plugins.</summary>
    public const string Plugins = "plugins";

    /// <summary>Project > General.</summary>
    public const string ProjectGeneral = "project-general";

    /// <summary>Project > Canvas.</summary>
    public const string Canvas = "canvas";

    /// <summary>Project > Fonts.</summary>
    public const string Fonts = "fonts";

    /// <summary>Project > Localization.</summary>
    public const string Localization = "localization";

    /// <summary>Project > Code generation.</summary>
    public const string CodeGeneration = "code-generation";
}

/// <summary>
/// One page of the Settings window. <see cref="Content"/> is a view model, which each head resolves
/// to the view that presents it, the same way a tab's content is resolved.
/// </summary>
public class SettingsPage
{
    /// <summary>Creates a page.</summary>
    public SettingsPage(string id, string title, SettingsSection section, object content, int order = 0)
    {
        Id = id;
        Title = title;
        Section = section;
        Content = content;
        Order = order;
    }

    /// <summary>Identifies the page; registering a page with the same id replaces the earlier one.</summary>
    public string Id { get; }

    /// <summary>The name shown in the window's navigation list.</summary>
    public string Title { get; }

    /// <summary>The group the page is listed under.</summary>
    public SettingsSection Section { get; }

    /// <summary>The view model the page shows.</summary>
    public object Content { get; }

    /// <summary>Position within the section; lower comes first.</summary>
    public int Order { get; }

    /// <summary>Extra words the search box matches besides the title.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The labels of the settings on the page, read each time the user searches, so a search finds a
    /// setting by its own name and not only the page it is on. Null for a page with no list to give.
    /// </summary>
    public Func<IEnumerable<string>>? SearchTerms { get; init; }

    /// <summary>Called each time the page is shown, to refill <see cref="Content"/> from the project.</summary>
    public Action? Activated { get; init; }
}
