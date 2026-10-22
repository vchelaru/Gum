using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// No checked-in MSBuild file may turn on <c>GeneratePackageOnBuild</c>. A project that packs itself
/// on every build writes a .nupkg into bin/ for every local and CI build, and a consumer that wants
/// to avoid that has to reference it with <c>AdditionalProperties="GeneratePackageOnBuild=false"</c>,
/// which makes MSBuild build the shared libraries twice, in parallel, into the same obj folder
/// (CS2012 / file-lock failures, #6436). Packages come only from the release workflow's explicit
/// <c>dotnet pack</c> step. The one exception is <see cref="Frb1OnlyPackOnBuildProjects"/>.
/// </summary>
public class NoPackOnBuildTests
{
    // <GeneratePackageOnBuild>true</GeneratePackageOnBuild>, with or without a Condition attribute.
    private static readonly Regex PackOnBuildEnabled = new(
        @"<GeneratePackageOnBuild\b[^>]*>\s*true\s*</GeneratePackageOnBuild>",
        RegexOptions.IgnoreCase);

    private static readonly string[] SkippedFolders =
        { "bin", "obj", "node_modules", ".git", "fna", "Sokol.NET" };

    // FRB1 packages. They are published by hand from a local build, are in no solution the release
    // workflow packs, and are not reachable from any consumer-from-source path (MonoGameGum,
    // KernSmith.MonoGameGum, MonoGameGumShapes, GumCommon), so the double-build in #6436 cannot
    // happen for them. FRB1 is frozen: these keep packing on build.
    private static readonly string[] Frb1OnlyPackOnBuildProjects =
    {
        "GumCore/GumCoreXnaPc/GumCore.DesktopGlNet6/GumCore.DesktopGlNet6.csproj",
        "GumCore/GumCoreXnaPc/GumCore.FNA/GumCore.FNA.csproj",
        "GumCore/GumCoreXnaPc/GumCore.Kni.Web/GumCore.Kni.Web.csproj",
        "GumDataTypes/GumDataTypesNet6.csproj",
        "ToolsUtilities/ToolsUtilitiesStandard.csproj",
    };

    [Fact]
    public void NoCheckedInMsbuildFile_EnablesGeneratePackageOnBuild_ExceptFrb1OnlyProjects()
    {
        List<string> offenders = EnumerateMsbuildFiles(RepoPaths.RepoRoot)
            .Where(path => PackOnBuildEnabled.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepoPaths.RepoRoot, path).Replace('\\', '/'))
            .Where(path => !Frb1OnlyPackOnBuildProjects.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Frb1OnlyProjects_StillEnableGeneratePackageOnBuild()
    {
        List<string> dropped = Frb1OnlyPackOnBuildProjects
            .Where(path => !PackOnBuildEnabled.IsMatch(File.ReadAllText(Path.Combine(RepoPaths.RepoRoot, path))))
            .ToList();

        dropped.ShouldBeEmpty();
    }

    [Fact]
    public void PackOnBuildEnabled_MatchesTheValueAnyCasingOrCondition()
    {
        PackOnBuildEnabled.IsMatch("<GeneratePackageOnBuild>True</GeneratePackageOnBuild>").ShouldBeTrue();
        PackOnBuildEnabled.IsMatch("<GeneratePackageOnBuild>true</GeneratePackageOnBuild>").ShouldBeTrue();
        PackOnBuildEnabled.IsMatch("<GeneratePackageOnBuild Condition=\"'$(Configuration)' == 'Release'\"> true </GeneratePackageOnBuild>").ShouldBeTrue();
    }

    [Fact]
    public void PackOnBuildEnabled_IgnoresFalseAndMentionsInComments()
    {
        PackOnBuildEnabled.IsMatch("<GeneratePackageOnBuild>false</GeneratePackageOnBuild>").ShouldBeFalse();
        PackOnBuildEnabled.IsMatch("<!-- GeneratePackageOnBuild=true packs on every build -->").ShouldBeFalse();
    }

    private static IEnumerable<string> EnumerateMsbuildFiles(string root)
    {
        return new[] { "*.csproj", "*.props", "*.targets", "*.projitems" }
            .SelectMany(pattern => Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => SkippedFolders.Contains(segment)));
    }
}
