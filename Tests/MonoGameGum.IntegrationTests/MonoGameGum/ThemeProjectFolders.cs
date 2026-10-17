using System;
using System.Collections.Generic;
using System.IO;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// The shipped Forms themes (Tools/Gum.ProjectServices/Templates/FormsThemes/&lt;theme&gt;) and the shared
/// FormsBehaviors they reference, copied once into a temp folder per test class (#6108). The tests only
/// read these files, so every test in the class loads the same copy instead of writing it again; copying
/// 40 files for each of two themes cost about 0.3 s per test, and the freshly written files were slower to
/// read for the first load. The folder is laid out the way the repo has it, so a theme's relative
/// ../../FormsBehaviors paths still resolve.
/// </summary>
public sealed class ThemeProjectFolders : IDisposable
{
    private readonly string _rootDirectory =
        Path.Combine(Path.GetTempPath(), "GumThemeFolders_" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _gumxPathsByTheme = new Dictionary<string, string>();

    /// <summary>Returns the .gumx path of the copied theme, copying it on first use.</summary>
    public string GetGumxPath(string theme)
    {
        if (_gumxPathsByTheme.TryGetValue(theme, out string? existing))
        {
            return existing;
        }

        string templates = FindTemplatesDirectory();
        string sharedBehaviors = Path.Combine(_rootDirectory, "FormsBehaviors");
        if (!Directory.Exists(sharedBehaviors))
        {
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), sharedBehaviors);
        }

        string destination = Path.Combine(_rootDirectory, "FormsThemes", theme);
        CopyDirectory(Path.Combine(templates, "FormsThemes", theme), destination);
        string gumxPath = Path.Combine(destination, "GumProject.gumx");
        _gumxPathsByTheme[theme] = gumxPath;
        return gumxPath;
    }

    public void Dispose()
    {
        try { Directory.Delete(_rootDirectory, recursive: true); } catch { /* best effort */ }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate Tools/Gum.ProjectServices/Templates from " + AppContext.BaseDirectory);
    }
}
