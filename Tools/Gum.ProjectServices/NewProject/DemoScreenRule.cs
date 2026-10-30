using System;

namespace Gum.ProjectServices;

/// <summary>
/// The one rule for which screen is a Forms theme's demo screen: <c>DemoScreenGum</c>, or <c>DemoScreenGum</c> in a
/// folder (a non-Standard theme writes <c>Bubblegum/DemoScreenGum</c>). The theme importer, the New Project flow and the
/// New Project Files plan all use it.
/// </summary>
public static class DemoScreenRule
{
    /// <summary>The demo screen's name without a folder.</summary>
    public const string DemoScreenName = "DemoScreenGum";

    /// <summary>Whether a screen name, with or without folders, is the demo screen.</summary>
    public static bool IsDemoScreenName(string name) =>
        name == DemoScreenName || name.EndsWith("/" + DemoScreenName, StringComparison.Ordinal);

    /// <summary>
    /// Whether a project-relative file path is a demo screen's file: under a <c>Screens</c> folder, named like
    /// <see cref="IsDemoScreenName"/> once the folder and the extension are taken apart. Either separator works.
    /// </summary>
    public static bool IsDemoScreenFile(string relativePath)
    {
        string normalized = relativePath.Replace((char)92, '/');
        int screens = normalized.LastIndexOf("Screens/", StringComparison.Ordinal);
        if (screens < 0 || (screens > 0 && normalized[screens - 1] != '/'))
        {
            return false;
        }

        string screenPath = normalized.Substring(screens + "Screens/".Length);
        int dot = screenPath.LastIndexOf('.');
        return dot > 0 && IsDemoScreenName(screenPath.Substring(0, dot));
    }
}
