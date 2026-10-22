using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// A packed Gum library may reference an engine's platform package (MonoGame.Framework.DesktopGL,
/// nkast.Xna.Framework.*, ...) for compile-time types only. Without <c>PrivateAssets="All"</c> the
/// reference is written into the nuspec, so every app that uses the library restores the desktop
/// MonoGame next to its own platform package, mobile apps included (#6433).
/// </summary>
public class PackedPlatformPackageTests
{
    private static readonly string[] PlatformPackagePrefixes =
    {
        "MonoGame.Framework.",
        "nkast.Xna.Framework",
        "nkast.Kni.Platform.",
    };

    [Fact]
    public void IsPlatformPackage_MatchesEnginePlatformPackages()
    {
        IsPlatformPackage("MonoGame.Framework.DesktopGL").ShouldBeTrue();
        IsPlatformPackage("MonoGame.Framework.Android").ShouldBeTrue();
        IsPlatformPackage("nkast.Xna.Framework.Graphics").ShouldBeTrue();
        IsPlatformPackage("nkast.Kni.Platform.SDL2.GL").ShouldBeTrue();
        IsPlatformPackage("KernSmith").ShouldBeFalse();
        IsPlatformPackage("Raylib-cs").ShouldBeFalse();
    }

    [Fact]
    public void IsPackedLibrary_RequiresAPackageIdAndNotOptedOut()
    {
        IsPackedLibrary(XDocument.Parse("<Project><PropertyGroup><PackageId>A</PackageId></PropertyGroup></Project>"))
            .ShouldBeTrue();
        IsPackedLibrary(XDocument.Parse("<Project><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>"))
            .ShouldBeFalse();
        IsPackedLibrary(XDocument.Parse(
            "<Project><PropertyGroup><PackageId>A</PackageId><IsPackable>false</IsPackable></PropertyGroup></Project>"))
            .ShouldBeFalse();
    }

    [Fact]
    public void KeepsPlatformPackageOutOfNuspec_ReadsAttributeAndElementForms()
    {
        KeepsOutOfNuspec(XElement.Parse("<PackageReference Include=\"X\" PrivateAssets=\"All\" />")).ShouldBeTrue();
        KeepsOutOfNuspec(XElement.Parse("<PackageReference Include=\"X\"><PrivateAssets>all</PrivateAssets></PackageReference>"))
            .ShouldBeTrue();
        KeepsOutOfNuspec(XElement.Parse("<PackageReference Include=\"X\" PrivateAssets=\"compile\" />")).ShouldBeFalse();
        KeepsOutOfNuspec(XElement.Parse("<PackageReference Include=\"X\" />")).ShouldBeFalse();
    }

    [Fact]
    public void PackedLibraries_KeepPlatformPackagesOutOfTheirNuspec()
    {
        List<string> offenders = new();

        foreach (string file in EnumerateProjects(RepoPaths.RepoRoot))
        {
            XDocument project = XDocument.Load(file);
            if (!IsPackedLibrary(project))
            {
                continue;
            }

            foreach (XElement reference in project.Descendants("PackageReference"))
            {
                string id = (string?)reference.Attribute("Include") ?? string.Empty;
                if (IsPlatformPackage(id) && !KeepsOutOfNuspec(reference))
                {
                    string relative = Path.GetRelativePath(RepoPaths.RepoRoot, file).Replace('\\', '/');
                    offenders.Add($"{relative}: {id} needs PrivateAssets=\"All\"");
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    private static bool IsPlatformPackage(string id) =>
        PlatformPackagePrefixes.Any(prefix => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    // An explicit PackageId is how a project declares it ships as a package. IsPackable alone is not
    // a signal: it defaults to true for every SDK project, samples and tests included.
    private static bool IsPackedLibrary(XDocument project)
    {
        bool hasPackageId = project.Descendants("PackageId").Any();
        bool optedOut = project.Descendants("IsPackable")
            .Any(element => string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase));
        return hasPackageId && !optedOut;
    }

    private static bool KeepsOutOfNuspec(XElement reference)
    {
        string value = (string?)reference.Attribute("PrivateAssets")
            ?? reference.Element("PrivateAssets")?.Value
            ?? string.Empty;
        return value.Split(';').Any(part => string.Equals(part.Trim(), "all", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateProjects(string root)
    {
        // Submodules and the hand-cloned Sokol.NET tree are third-party, not ours to police.
        string[] excludedSegments = { "bin", "obj", "node_modules", ".git", "Sokol.NET", "fna" };

        return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)));
    }
}
