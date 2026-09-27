using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.Presentation.Tests.Architecture;

/// <summary>
/// Tool code must resolve per-user folders through <c>FileManager.UserApplicationDataForThisApplication</c>,
/// which honors <c>--user-data</c> and the per-process test override. <c>BannedSymbols.ToolUserData.txt</c>
/// turns a direct <c>Environment.SpecialFolder.ApplicationData</c>/<c>LocalApplicationData</c> reference into
/// a build error (RS0030); this test keeps every tool project wired to that list, so a new project cannot
/// silently opt out (#5362).
/// </summary>
public class ToolUserDataFolderGuardTests
{
    [Fact]
    public void EveryToolProject_BansDirectApplicationDataFolders()
    {
        // Frozen WPF head projects (net*-windows TFMs, plus these net10.0 ones only it references),
        // the Roslyn analyzer project, and the SkiaInGum game runtimes, none of which run in the tool.
        string[] excluded =
        {
            "Gum/Gum.csproj",
            "Gum/CommonFormsAndControls/CommonFormsAndControls.csproj",
            "Gum/SvgPlugin/SkiaInGumShared/SkiaInGum.csproj",
            "Gum/SvgPlugin/SkiaInGumShared/SkiaInGum.FNA.csproj",
            "Tools/Gum.Analyzers/Gum.Analyzers.csproj",
        };

        string repoRoot = FindRepoRoot();
        string[] roots = { "Gum", "Tool", "Tools", "DataUi.Core", "AvaloniaDataUi" };
        Regex windowsTarget = new Regex(@"<TargetFrameworks?>[^<]*-windows");
        Regex banListInclude = new Regex(@"<AdditionalFiles\s+Include=""([^""]*BannedSymbols\.ToolUserData\.txt)""");

        List<string> unwired = roots
            .Select(root => Path.Combine(repoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(repoRoot, file).Replace('\\', '/'))
            .Where(relative => !relative.Contains("/Tests/")
                && !relative.Contains("/bin/")
                && !relative.Contains("/obj/")
                && !excluded.Contains(relative, StringComparer.OrdinalIgnoreCase))
            .Where(relative =>
            {
                string projectFile = Path.Combine(repoRoot, relative);
                string text = File.ReadAllText(projectFile);
                if (windowsTarget.IsMatch(text))
                {
                    return false;
                }
                // The include must resolve: a mistyped path is silently skipped by MSBuild and the
                // ban never applies.
                Match include = banListInclude.Match(text);
                bool resolves = include.Success && File.Exists(Path.Combine(
                    Path.GetDirectoryName(projectFile)!,
                    include.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar)));
                return !(resolves && text.Contains("Microsoft.CodeAnalysis.BannedApiAnalyzers"));
            })
            .ToList();

        unwired.ShouldBeEmpty(
            "These tool projects do not reference Microsoft.CodeAnalysis.BannedApiAnalyzers with a " +
            "resolvable BannedSymbols.ToolUserData.txt AdditionalFiles item:" + Environment.NewLine +
            string.Join(Environment.NewLine, unwired));
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}
