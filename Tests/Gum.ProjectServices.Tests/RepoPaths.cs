namespace Gum.ProjectServices.Tests;

/// <summary>Files in the repository checkout that tests read directly.</summary>
internal static class RepoPaths
{
    /// <summary>The repository root, found by walking up from the test binaries to the solution file.</summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>
    /// A font file shipped in the repository, for font generation tests that must not depend on
    /// which fonts the machine has installed (Linux has no Arial).
    /// </summary>
    internal static string TestFontFile => Path.Combine(Root, "Themes", "Gum.Themes.Bubblegum.MonoGame", "Content", "Fonts", "DejaVuSansMono.ttf");

    private static string FindRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("The tests are not running inside the Gum repository.");
    }
}
