using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Refuses a screenshot whose text shows the user's profile folder or user name as a path segment.
/// Screenshots are published in a pull request, and a real install or temp path names the person
/// running the test. A test that trips it shows fixture data (a made-up folder) instead.
/// </summary>
internal static class UserPathGuard
{
    public static void ThrowIfShown(Window window)
    {
        // The temp folder too: on Windows it can be the short 8.3 form (C:\Users\PATDOE~1\...),
        // which matches neither the profile path nor the user name.
        string? leak = FindLeak(ShownTexts(window), Environment.UserName,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Path.GetTempPath());
        if (leak != null)
        {
            throw new InvalidOperationException(
                $"The screenshot would show a path naming the current user: \"{leak}\". Give the view fixture data instead of a real path.");
        }
    }

    /// <summary>The first text that contains one of <paramref name="privatePaths"/> or <paramref name="userName"/> as a path segment; null when none does.</summary>
    internal static string? FindLeak(IEnumerable<string> texts, string userName, params string[] privatePaths)
    {
        List<string> needles = new List<string>();
        foreach (string path in privatePaths.Select(path => path.TrimEnd('\\', '/')).Where(path => path.Length > 0))
        {
            needles.Add(path);
            needles.Add(path.Replace('\\', '/'));
        }
        if (userName.Length > 0)
        {
            // As a path segment only: a short user name ("dev") is an ordinary word elsewhere.
            needles.Add("\\" + userName);
            needles.Add("/" + userName);
        }
        return texts.FirstOrDefault(text => needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> ShownTexts(Window window)
    {
        if (window.Title is { Length: > 0 } title)
        {
            yield return title;
        }
        foreach (Control control in window.GetVisualDescendants().OfType<Control>())
        {
            string? text = control switch
            {
                TextBlock block => block.Text,
                TextBox box => box.Text,
                _ => null,
            };
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
            // A tooltip draws in its own popup, outside the window's visual tree.
            if (ToolTip.GetIsOpen(control) && ToolTip.GetTip(control) is string tip)
            {
                yield return tip;
            }
        }
    }
}
