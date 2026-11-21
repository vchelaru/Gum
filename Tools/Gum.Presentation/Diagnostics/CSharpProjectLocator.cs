using System;
using System.IO;
using System.Linq;

namespace Gum.Diagnostics;

/// <summary>Finds the folder of the game's C# project: the nearest folder at or above the .gumx that holds a .csproj.</summary>
public static class CSharpProjectLocator
{
    /// <summary>The folder holding the .csproj, or null when there is no .gumx or no .csproj above it.</summary>
    public static string? FindFolder(string? gumProjectFile)
    {
        if (string.IsNullOrEmpty(gumProjectFile))
        {
            return null;
        }

        string? folder = Path.GetDirectoryName(Path.GetFullPath(gumProjectFile));
        while (!string.IsNullOrEmpty(folder))
        {
            try
            {
                if (Directory.GetFiles(folder, "*.csproj").Any())
                {
                    return folder;
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            folder = Path.GetDirectoryName(folder);
        }
        return null;
    }
}
