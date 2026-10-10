using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class FormsThemeLocator : IFormsThemeLocator
{
    private const string ProjectFileName = "GumProject.gumx";

    private readonly IReadOnlyList<string> _themeRoots;

    /// <param name="themeRoots">Folders whose subfolders are themes. Earlier roots win when a name appears in more than one.</param>
    public FormsThemeLocator(IEnumerable<string> themeRoots)
    {
        _themeRoots = themeRoots.ToList();
    }

    /// <summary>
    /// Builds a locator that looks where a gumcli install or a Gum checkout keeps its themes: the
    /// <c>Content/FormsThemes</c> folder of the release layout (beside or above the gumcli folder), then
    /// <c>Tools/Gum.ProjectServices/Templates/FormsThemes</c> of a checkout found from the gumcli binary or the working folder.
    /// </summary>
    public static FormsThemeLocator CreateDefault(string baseDirectory, string workingDirectory)
    {
        List<string> roots = new List<string>
        {
            Path.Combine(baseDirectory, "..", "Content", "FormsThemes"),
            Path.Combine(baseDirectory, "Content", "FormsThemes")
        };

        foreach (string startDirectory in new[] { baseDirectory, workingDirectory })
        {
            string? checkout = GumSourceLocator.Find(startDirectory);
            if (checkout != null)
            {
                roots.Add(Path.Combine(checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes"));
            }
        }

        return new FormsThemeLocator(roots);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableThemes()
    {
        SortedSet<string> names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in _themeRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string directory in Directory.GetDirectories(root))
            {
                if (File.Exists(Path.Combine(directory, ProjectFileName)))
                {
                    names.Add(Path.GetFileName(directory));
                }
            }
        }

        return names.ToList();
    }

    /// <inheritdoc/>
    public string? FindThemeDirectory(string themeName)
    {
        foreach (string root in _themeRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string directory in Directory.GetDirectories(root))
            {
                bool nameMatches = string.Equals(Path.GetFileName(directory), themeName, StringComparison.OrdinalIgnoreCase);
                if (nameMatches && File.Exists(Path.Combine(directory, ProjectFileName)))
                {
                    return Path.GetFullPath(directory);
                }
            }
        }

        return null;
    }
}
