using System.IO;

namespace Gum.ProjectServices;

/// <summary>
/// Copies the Images/Fonts/FontCache trees a Screen import stages alongside its element file into
/// a project directory. Shared by HtmlToGumPlugin's Content → Import → HTML… merge step and
/// gumcli import-screen so both copy staged assets identically.
/// </summary>
public static class AssetTreeCopier
{
    private static readonly string[] AssetFolders = ["Images", "Fonts", "FontCache"];

    /// <summary>
    /// Copies each of Images/Fonts/FontCache found under <paramref name="stagedAssetsDirectory"/>
    /// into the matching subfolder of <paramref name="projectDirectory"/>, overwriting existing
    /// files. A missing source subfolder is skipped, not an error.
    /// </summary>
    public static void CopyStagedAssets(string stagedAssetsDirectory, string projectDirectory)
    {
        foreach (string folder in AssetFolders)
        {
            CopyTree(Path.Combine(stagedAssetsDirectory, folder), Path.Combine(projectDirectory, folder));
        }
    }

    private static void CopyTree(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(sourceDir, file);
            string dest = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
