using System;
using System.IO;
using System.Text.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace GumToolUnitTests.Coupling;

/// <summary>
/// Locates the Gum tool source tree, runs <see cref="CouplingScanner"/> over it once, and
/// loads the checked-in baseline. Shared across all ratchet tests via <see cref="IClassFixture{T}"/>
/// so the (single) full-tree scan happens only once per test run.
/// </summary>
public sealed class CouplingScanFixture
{
    public CouplingMetrics Measured { get; }
    public CouplingBaseline Baseline { get; }
    public string ToolSourceRoot { get; }

    public CouplingScanFixture()
    {
        string repoRoot = LocateRepoRoot();
        ToolSourceRoot = Path.Combine(repoRoot, "Gum");

        CouplingScanner scanner = new CouplingScanner();
        Measured = scanner.ScanToolSource(ToolSourceRoot);

        // Sanity floor: a found-but-degenerate tool root (empty, wrong directory, or a future
        // source reorg) would make the scan return near-zero counts, and EVERY ratchet guard
        // (measured <= baseline) would then pass VACUOUSLY -- 0 <= baseline is always true.
        // Throwing here fails all ratchet tests loudly instead of going green for free. The
        // floors sit well below today's real counts (~470 files / ~55 ViewModels) but far above
        // zero, so they only trip on a genuinely broken scan, never on legitimate burn-down.
        const int minimumFilesScanned = 100;
        const int minimumViewModelFilesScanned = 10;
        if (Measured.FilesScanned < minimumFilesScanned ||
            Measured.ViewModelFilesScanned < minimumViewModelFilesScanned)
        {
            throw new InvalidOperationException(
                $"Coupling scan looks degenerate: scanned {Measured.FilesScanned} files and " +
                $"{Measured.ViewModelFilesScanned} ViewModel files under '{ToolSourceRoot}' " +
                $"(floors: {minimumFilesScanned} files, {minimumViewModelFilesScanned} ViewModels). " +
                "The scanner almost certainly pointed at the wrong root -- the ratchet guards would " +
                "otherwise pass vacuously against an empty or incorrect tree.");
        }

        string baselinePath = Path.Combine(
            repoRoot, "Tool", "Tests", "GumToolUnitTests", "Coupling", "coupling-baseline.json");
        Baseline = LoadBaseline(baselinePath);
    }

    /// <summary>
    /// Walks up from the test assembly's location until it finds the repo root, identified by the
    /// presence of the Gum tool's main project file (<c>Gum/Gum.csproj</c>). This is robust for both
    /// the primary checkout and a git worktree, because each worktree carries its own <c>Gum/</c>.
    /// </summary>
    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gum", "Gum.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Gum repo root (no ancestor of " +
            $"'{AppContext.BaseDirectory}' contains Gum/Gum.csproj). The coupling ratchet needs " +
            "the tool source tree to scan.");
    }

    private static CouplingBaseline LoadBaseline(string baselinePath)
    {
        if (!File.Exists(baselinePath))
        {
            throw new FileNotFoundException(
                $"Coupling baseline file not found: '{baselinePath}'.", baselinePath);
        }

        string json = File.ReadAllText(baselinePath);
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        CouplingBaseline? baseline = JsonSerializer.Deserialize<CouplingBaseline>(json, options);
        if (baseline == null)
        {
            throw new InvalidOperationException(
                $"Coupling baseline file '{baselinePath}' deserialized to null.");
        }
        return baseline;
    }
}

/// <summary>
/// The Phase 0 ratchet guard. Runs <see cref="CouplingScanner"/> over the real Gum tool source tree
/// and asserts each coupling count is &lt;= its checked-in baseline. Green today; red the moment any
/// metric regresses above baseline. A Phase 1 PR lowers a baseline (a one-line edit to
/// coupling-baseline.json) in the same diff that removes the coupling.
/// </summary>
public sealed class CouplingRatchetTests : IClassFixture<CouplingScanFixture>
{
    private readonly CouplingScanFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CouplingRatchetTests(CouplingScanFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void InlineDialogSites_DoNotExceedBaseline()
    {
        int measured = _fixture.Measured.InlineDialogSites;
        int baseline = _fixture.Baseline.Metrics.InlineDialogSites.RatchetedBaseline;

        measured.ShouldBeLessThanOrEqualTo(
            baseline,
            customMessage: BuildRatchetMessage("inlineDialogSites", measured, baseline));
    }

    [Fact]
    public void InterfaceTypeLeaks_DoNotExceedBaseline()
    {
        int measured = _fixture.Measured.InterfaceTypeLeaks;
        int baseline = _fixture.Baseline.Metrics.InterfaceTypeLeaks.RatchetedBaseline;

        measured.ShouldBeLessThanOrEqualTo(
            baseline,
            customMessage: BuildRatchetMessage("interfaceTypeLeaks", measured, baseline));
    }

    /// <summary>
    /// Not a guard -- a convenience that prints the scanner's current measurements so a Phase 1 PR
    /// can update coupling-baseline.json without re-deriving the numbers by hand. Always passes.
    /// </summary>
    [Fact]
    public void Scanner_DumpMeasuredMetrics_ForBaselineUpdates()
    {
        CouplingMetrics m = _fixture.Measured;

        _output.WriteLine($"Tool source root      : {_fixture.ToolSourceRoot}");
        _output.WriteLine($"Files scanned         : {m.FilesScanned}");
        _output.WriteLine($"ViewModel files       : {m.ViewModelFilesScanned}");
        _output.WriteLine("--- ratcheted metrics (measured vs baseline) ---");
        _output.WriteLine(
            $"selfStaticReferences        : {m.SelfExcludingObjectFinder} " +
            $"(baseline {_fixture.Baseline.Metrics.SelfStaticReferences.RatchetedBaseline}) " +
            $"[total .Self {m.SelfTotal}, ObjectFinder.Self {m.ObjectFinderSelf}]");
        _output.WriteLine(
            $"windowsCouplingInViewModels : {m.WindowsCouplingInViewModels} " +
            $"(baseline {_fixture.Baseline.Metrics.WindowsCouplingInViewModels.RatchetedBaseline})");
        _output.WriteLine(
            $"inlineDialogSites           : {m.InlineDialogSites} " +
            $"(baseline {_fixture.Baseline.Metrics.InlineDialogSites.RatchetedBaseline})");
        _output.WriteLine(
            $"interfaceTypeLeaks          : {m.InterfaceTypeLeaks} " +
            $"(baseline {_fixture.Baseline.Metrics.InterfaceTypeLeaks.RatchetedBaseline})");

        // Always-pass sentinel so the output is captured even on a green run.
        m.FilesScanned.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SelfReferencesExcludingObjectFinder_DoNotExceedBaseline()
    {
        int measured = _fixture.Measured.SelfExcludingObjectFinder;
        int baseline = _fixture.Baseline.Metrics.SelfStaticReferences.RatchetedBaseline;

        measured.ShouldBeLessThanOrEqualTo(
            baseline,
            customMessage: BuildRatchetMessage("selfStaticReferences", measured, baseline));
    }

    [Fact]
    public void WindowsCouplingInViewModels_DoNotExceedBaseline()
    {
        int measured = _fixture.Measured.WindowsCouplingInViewModels;
        int baseline = _fixture.Baseline.Metrics.WindowsCouplingInViewModels.RatchetedBaseline;

        measured.ShouldBeLessThanOrEqualTo(
            baseline,
            customMessage: BuildRatchetMessage("windowsCouplingInViewModels", measured, baseline));
    }

    // Shouldly only surfaces this customMessage when the assertion FAILS (measured > baseline),
    // so the message is unconditionally written for that case -- there is no "slack" branch to
    // build, because a passing assertion never shows it.
    private string BuildRatchetMessage(string metric, int measured, int baseline)
    {
        return $"Coupling metric '{metric}' regressed: measured {measured} > baseline {baseline}. " +
            "New UI/logic coupling was introduced. Remove it, or (if intentional) raise the baseline " +
            "in coupling-baseline.json with justification.";
    }
}

/// <summary>Deserialization shape for coupling-baseline.json. Only the ratcheted numbers are read.</summary>
public sealed class CouplingBaseline
{
    public CouplingBaselineMetrics Metrics { get; set; } = new CouplingBaselineMetrics();
}

/// <summary>The four ratcheted metric entries.</summary>
public sealed class CouplingBaselineMetrics
{
    public CouplingBaselineEntry SelfStaticReferences { get; set; } = new CouplingBaselineEntry();
    public CouplingBaselineEntry WindowsCouplingInViewModels { get; set; } = new CouplingBaselineEntry();
    public CouplingBaselineEntry InlineDialogSites { get; set; } = new CouplingBaselineEntry();
    public CouplingBaselineEntry InterfaceTypeLeaks { get; set; } = new CouplingBaselineEntry();
}

/// <summary>A single metric's ratcheted baseline (the maximum the ratchet allows).</summary>
public sealed class CouplingBaselineEntry
{
    public int RatchetedBaseline { get; set; }
}
