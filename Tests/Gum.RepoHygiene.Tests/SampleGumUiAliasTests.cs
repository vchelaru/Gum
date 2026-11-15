using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Maintained samples are the main copy-paste source, so they follow the docs: declare
/// <c>GumService GumUI => GumService.Default;</c> (or a local / parameter named GumUI) and call
/// <c>GumUI.Initialize/Update/Draw</c>, never <c>GumService.Default.Initialize(...)</c> (#7773).
/// </summary>
public class SampleGumUiAliasTests
{
    // Unmaintained sample folders (first path segment under Samples/).
    private static readonly string[] UnmaintainedSampleFolders =
    {
        "SokolGum", "SokolGumFromFile", "SkiaGumWpfSample", "WpfRenderSurfaceHostHarness", "MauiSkiaGum",
    };

    // Relative path (forward slashes) -> reason it may call GumService.Default directly.
    private static readonly Dictionary<string, string> Allowlist = new();

    private static readonly string[] SkippedSegments = { "bin", "obj", "node_modules", ".git" };

    // GumService.Default.Something, optionally namespace-qualified (MonoGameGum.GumService.Default.).
    private static readonly Regex DefaultAsCallTarget = new(@"\bGumService\.Default\s*\.", RegexOptions.Compiled);

    // A member call through the alias.
    private static readonly Regex AliasCall = new(@"\bGumUI\s*\.", RegexOptions.Compiled);

    // A declaration of the alias: property, field, local, or parameter.
    private static readonly Regex AliasDeclaration = new(@"\bGumService\s+GumUI\b", RegexOptions.Compiled);

    [Fact]
    public void MaintainedSamples_DoNotCallGumServiceDefaultDirectly()
    {
        List<string> offenders = EnumerateMaintainedSampleFiles()
            .Where(file => CodeLines(file.Text).Any(line => DefaultAsCallTarget.IsMatch(line)))
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void MaintainedSamples_CallingGumUi_DeclareTheAlias()
    {
        List<string> offenders = EnumerateMaintainedSampleFiles()
            .Where(file => CodeLines(file.Text).Any(line => AliasCall.IsMatch(line))
                && !CodeLines(file.Text).Any(line => AliasDeclaration.IsMatch(line)))
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Allowlist_OnlyNamesFilesThatExist()
    {
        List<string> stale = Allowlist.Keys
            .Where(path => !File.Exists(Path.Combine(RepoPaths.RepoRoot, path)))
            .ToList();

        stale.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultAsCallTarget_MatchesQualifiedAndSpacedForms_NotTheAliasDeclaration()
    {
        DefaultAsCallTarget.IsMatch("GumService.Default.Initialize(this);").ShouldBeTrue();
        DefaultAsCallTarget.IsMatch("MonoGameGum.GumService.Default.Draw();").ShouldBeTrue();
        DefaultAsCallTarget.IsMatch("GumService GumUI => GumService.Default;").ShouldBeFalse();
        DefaultAsCallTarget.IsMatch("IGumService.Default = _gameUi;").ShouldBeFalse();
    }

    private static IEnumerable<string> CodeLines(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith("//"))
            {
                yield return line;
            }
        }
    }

    private static IEnumerable<(string RelativePath, string Text)> EnumerateMaintainedSampleFiles()
    {
        string samples = Path.Combine(RepoPaths.RepoRoot, "Samples");
        foreach (string path in Directory.EnumerateFiles(samples, "*.cs", SearchOption.AllDirectories))
        {
            string relativeToSamples = Path.GetRelativePath(samples, path);
            string[] segments = relativeToSamples.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => SkippedSegments.Contains(segment))
                || UnmaintainedSampleFolders.Contains(segments[0]))
            {
                continue;
            }

            string relative = "Samples/" + string.Join('/', segments);
            if (Allowlist.ContainsKey(relative))
            {
                continue;
            }

            yield return (relative, File.ReadAllText(path));
        }
    }
}
