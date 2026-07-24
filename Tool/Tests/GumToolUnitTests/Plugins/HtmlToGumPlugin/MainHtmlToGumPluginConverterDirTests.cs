using HtmlToGumPlugin;
using Shouldly;
using System.IO;
using Xunit;

namespace GumToolUnitTests.Plugins.HtmlToGumPlugin;

public class MainHtmlToGumPluginConverterDirTests
{
    [Fact]
    public void GetConverterDirCandidates_FirstCandidate_ResolvesToRepoConverterFolder()
    {
        // Gum.csproj sets AppendTargetFrameworkToOutputPath=false, so the plugin DLL's base
        // directory is <root>/Gum/bin/<Config>/ with no TFM subfolder — three levels up is <root>.
        string baseDir = Path.Combine(@"C:\", "repo", "Gum", "bin", "Debug") + Path.DirectorySeparatorChar;

        string[] candidates = MainHtmlToGumPlugin.GetConverterDirCandidates(baseDir);

        string expected = Path.GetFullPath(Path.Combine(@"C:\", "repo", "Tool", "HtmlToGum", "converter"));
        candidates[0].ShouldBe(expected);
    }

    [Fact]
    public void GetConverterDirCandidates_FirstCandidate_ResolvesUnderGitWorktree()
    {
        // A git worktree nests the checkout under .claude/worktrees/<branch>/ — the plugin DLL's
        // base directory still sits three levels under that worktree's own root.
        string baseDir = Path.Combine(
            @"C:\", "repo", ".claude", "worktrees", "some-branch", "Gum", "bin", "Debug") + Path.DirectorySeparatorChar;

        string[] candidates = MainHtmlToGumPlugin.GetConverterDirCandidates(baseDir);

        string expected = Path.GetFullPath(
            Path.Combine(@"C:\", "repo", ".claude", "worktrees", "some-branch", "Tool", "HtmlToGum", "converter"));
        candidates[0].ShouldBe(expected);
    }
}
