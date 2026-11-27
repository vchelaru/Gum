using System;
using System.IO;
using System.Reflection;

namespace Gum.ProjectServices;

/// <summary>
/// Extracts a template's pre-baked fonts (the <c>FontCache</c> folder embedded under the template's resource prefix)
/// into a new project's <c>FontCache</c>. A new project has no runtime font creator unless KernSmith is added, so
/// every font the template's content uses ships baked; the folder is flat, so no manifest lists the files.
/// </summary>
internal static class EmbeddedFontCache
{
    /// <summary>
    /// Writes every resource named <paramref name="templateResourcePrefix"/><c>FontCache.</c><i>file</i> to
    /// <paramref name="projectDirectory"/>/FontCache/<i>file</i>.
    /// </summary>
    public static void ExtractTo(Assembly assembly, string templateResourcePrefix, string projectDirectory)
    {
        string prefix = templateResourcePrefix + NewProjectLayout.FontCacheFolderName + ".";
        string fontCacheDirectory = Path.Combine(projectDirectory, NewProjectLayout.FontCacheFolderName);

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            Directory.CreateDirectory(fontCacheDirectory);
            using FileStream fileStream = File.Create(Path.Combine(fontCacheDirectory, resourceName.Substring(prefix.Length)));
            stream.CopyTo(fileStream);
        }
    }
}
