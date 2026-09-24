using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// No checked-in MSBuild file may shell out to a build of another project. A nested build inherits
/// none of the outer build's global properties (so the release workflow's <c>-p:Version=</c> is
/// missing from it, #4974) and writes the same output folders the outer build is writing, which
/// races it for file locks (MSB3248, #4968). A <c>ProjectReference</c> is how a build is ordered.
/// </summary>
public class NestedBuildExecTests
{
    // Command="..." - the Exec task's process command line.
    private static readonly Regex CommandAttribute = new(
        @"Command\s*=\s*""(?<command>[^""]*)""", RegexOptions.Singleline);

    // A build verb as the host's first argument (`dotnet build`, `$(DotNetHost) publish`), or msbuild
    // invoked at all. `dotnet tool restore` does not match - the verb has to be the first argument.
    private static readonly Regex BuildInvocation = new(
        @"(?:dotnet(?:\.exe)?|\$\([^)]*(?:dotnet|msbuild)[^)]*\))(?:&quot;|[""'])?\s+(?:msbuild|build|publish|pack|restore)\b" +
        @"|(?<!\w)msbuild(?:\.exe)?(?:&quot;|[""'])?\s",
        RegexOptions.IgnoreCase);

    // GumPreview publishes itself, Debug-only, to its own RID-specific output folder, so it shares
    // no output with the outer build. The csproj explains the rest.
    private static readonly string[] AllowedFiles = { "Tool/GumPreview/GumPreview.csproj" };

    [Fact]
    public void BuildInvocation_IgnoresMsbuildPropertyReferences()
    {
        BuildInvocation.IsMatch("xcopy /s $(MSBuildThisFileDirectory)Content $(OutDir)").ShouldBeFalse();
        BuildInvocation.IsMatch("&quot;$(MSBuildProjectFullPath)&quot; --help").ShouldBeFalse();
    }

    [Fact]
    public void BuildInvocation_IgnoresRunningABuiltTool()
    {
        string command = "$(DotNetHost) &quot;$(SolutionDir)Tools/Gum.FormsStaging/bin/Release/net10.0/Gum.FormsStaging.dll&quot; &quot;in.gumx&quot; &quot;out&quot;";

        BuildInvocation.IsMatch(command).ShouldBeFalse();
    }

    [Fact]
    public void BuildInvocation_IgnoresToolRestore()
    {
        BuildInvocation.IsMatch("dotnet tool restore").ShouldBeFalse();
    }

    [Fact]
    public void BuildInvocation_MatchesABuildThroughAHostProperty()
    {
        string command = "$(DotNetHost) build &quot;$(SolutionDir)Tools/Gum.FormsStaging/Gum.FormsStaging.csproj&quot; -c $(ConfigurationName)";

        BuildInvocation.IsMatch(command).ShouldBeTrue();
    }

    [Fact]
    public void BuildInvocation_MatchesADirectDotnetOrMsbuildInvocation()
    {
        BuildInvocation.IsMatch("dotnet build Foo.csproj").ShouldBeTrue();
        BuildInvocation.IsMatch("dotnet publish Foo.csproj -o out").ShouldBeTrue();
        BuildInvocation.IsMatch("msbuild.exe /t:Restore Foo.csproj").ShouldBeTrue();
        BuildInvocation.IsMatch("dotnet msbuild Foo.csproj /t:Build").ShouldBeTrue();
    }

    [Fact]
    public void NoBuildFile_ShellsOutToANestedBuild()
    {
        List<string> offenders = new();

        foreach (string file in EnumerateBuildFiles(RepoPaths.RepoRoot))
        {
            string relative = Path.GetRelativePath(RepoPaths.RepoRoot, file).Replace('\\', '/');
            if (AllowedFiles.Contains(relative, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (Match match in CommandAttribute.Matches(File.ReadAllText(file)))
            {
                string command = match.Groups["command"].Value;
                if (BuildInvocation.IsMatch(command))
                {
                    offenders.Add($"{relative}: {command}");
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    private static IEnumerable<string> EnumerateBuildFiles(string root)
    {
        string[] patterns = { "*.csproj", "*.targets", "*.props", "*.projitems" };
        // Submodules and the hand-cloned Sokol.NET tree are third-party, not ours to police.
        string[] excludedSegments = { "bin", "obj", "node_modules", ".git", "Sokol.NET", "fna" };

        return patterns
            .SelectMany(pattern => Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)));
    }
}
