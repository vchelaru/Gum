using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Samples show the same init block on every runtime: <c>GumUI.Initialize(...)</c> followed by
/// <c>GumUI.UseKeyboardDefaults()</c> and <c>GumUI.UseGamepadDefaults()</c>, never a hand-wired
/// <c>FrameworkElement.GamePadsForUiControl</c> / <c>KeyboardsForUiControl</c> (#7780).
/// </summary>
public class SampleInitBlockTests
{
    // Unmaintained sample folders (first path segment under Samples/).
    private static readonly string[] UnmaintainedSampleFolders =
    {
        "SokolGum", "SokolGumFromFile", "SkiaGumWpfSample", "WpfRenderSurfaceHostHarness", "MauiSkiaGum",
    };

    // Relative path (forward slashes) -> reason it may skip the rule.
    private static readonly Dictionary<string, string> Allowlist = new();

    private static readonly string[] SkippedSegments = { "bin", "obj", "node_modules", ".git" };

    private static readonly Regex InitializeCall = new(@"\bGumUI\s*\.\s*Initialize\s*\(", RegexOptions.Compiled);
    private static readonly Regex KeyboardDefaults = new(@"\bGumUI\s*\.\s*UseKeyboardDefaults\s*\(", RegexOptions.Compiled);
    private static readonly Regex GamepadDefaults = new(@"\bGumUI\s*\.\s*UseGamepadDefaults\s*\(", RegexOptions.Compiled);
    private static readonly Regex HandWiring = new(@"\b(GamePads|Keyboards)ForUiControl\s*\.\s*(Clear|Add|AddRange)\s*\(", RegexOptions.Compiled);

    [Fact]
    public void SamplesThatInitializeGum_CallUseKeyboardDefaults()
    {
        List<string> offenders = Offenders(file =>
            Code(file.Text).Any(l => InitializeCall.IsMatch(l)) && !Code(file.Text).Any(l => KeyboardDefaults.IsMatch(l)));

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void SamplesThatInitializeGum_CallUseGamepadDefaults()
    {
        List<string> offenders = Offenders(file =>
            Code(file.Text).Any(l => InitializeCall.IsMatch(l)) && !Code(file.Text).Any(l => GamepadDefaults.IsMatch(l)));

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Samples_DoNotHandWireWhatTheDefaultCallsDo()
    {
        List<string> offenders = Offenders(file => Code(file.Text).Any(l => HandWiring.IsMatch(l)));

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Allowlist_OnlyNamesFilesThatExist()
    {
        Allowlist.Keys.Where(path => !File.Exists(Path.Combine(RepoPaths.RepoRoot, path))).ShouldBeEmpty();
    }

    private static List<string> Offenders(Func<(string RelativePath, string Text), bool> isOffender) =>
        EnumerateMaintainedSampleFiles()
            .Where(isOffender)
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<string> Code(string text) =>
        text.Split('\n').Where(line => !line.TrimStart().StartsWith("//"));

    private static IEnumerable<(string RelativePath, string Text)> EnumerateMaintainedSampleFiles()
    {
        string samples = Path.Combine(RepoPaths.RepoRoot, "Samples");
        foreach (string path in Directory.EnumerateFiles(samples, "*.cs", SearchOption.AllDirectories))
        {
            string[] segments = Path.GetRelativePath(samples, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => SkippedSegments.Contains(segment))
                || UnmaintainedSampleFolders.Contains(segments[0]))
            {
                continue;
            }

            string relative = "Samples/" + string.Join('/', segments);
            if (!Allowlist.ContainsKey(relative))
            {
                yield return (relative, File.ReadAllText(path));
            }
        }
    }
}
