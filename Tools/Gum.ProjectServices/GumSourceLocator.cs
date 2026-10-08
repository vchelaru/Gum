using System.IO;

namespace Gum.ProjectServices;

/// <summary>
/// Finds a local Gum checkout, for scaffolding projects that reference Gum source instead of NuGet.
/// </summary>
public static class GumSourceLocator
{
    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> and returns the first folder containing
    /// <c>GumCommon/GumCommon.csproj</c>, or <see langword="null"/> when there is none.
    /// </summary>
    public static string? Find(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumCommon", "GumCommon.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
