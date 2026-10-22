using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Gum.TieringRepro;

/// <summary>
/// Runs every case in child processes with the runtime settings under test, alternating the current and new
/// RoundToInt bodies inside the same run, and prints one markdown block.
/// </summary>
internal static class Runner
{
    private const double NoisySpread = 0.20;
    private const double PenaltyRatioThreshold = 1.25;
    private const double PenaltyRemovedRatio = 1.15;
    private const double ChangeThresholdPercent = 5;
    private const string ResultFileName = "tiering-repro-result.md";

    private sealed record Setting(string Name, string Description, Dictionary<string, string> Env);

    private sealed record ChildResult(int ExitCode, string Output, string Error, bool TimedOut);

    private sealed class Series
    {
        public List<double> Values { get; } = new List<double>();

        public int Count => Values.Count;

        public double Min => Values.Count == 0 ? double.NaN : Values.Min();

        public double Median
        {
            get
            {
                if (Values.Count == 0)
                {
                    return double.NaN;
                }
                List<double> sorted = Values.OrderBy(v => v).ToList();
                int mid = sorted.Count / 2;
                return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
            }
        }

        /// <summary>Slowest run over fastest run, minus one.</summary>
        public double Spread => Values.Count < 2 ? 0 : Values.Max() / Values.Min() - 1;

        public bool IsNoisy => Spread > NoisySpread;
    }

    private sealed class GlyphCell
    {
        public Series PerGlyphMicroseconds { get; } = new Series();
        public HashSet<int> IlBytes { get; } = new HashSet<int>();
        public string? Failure { get; set; }
    }

    private static readonly Setting[] Settings =
    {
        new("tiering on", "defaults (TieredCompilation=1)",
            new Dictionary<string, string> { ["DOTNET_TieredCompilation"] = "1" }),
        new("tiering off", "DOTNET_TieredCompilation=0",
            new Dictionary<string, string> { ["DOTNET_TieredCompilation"] = "0" }),
        new("tiering off, EnableAVX=0", "DOTNET_TieredCompilation=0, DOTNET_EnableAVX=0",
            new Dictionary<string, string> { ["DOTNET_TieredCompilation"] = "0", ["DOTNET_EnableAVX"] = "0" }),
        new("tiering off, vector width 128", "DOTNET_TieredCompilation=0, DOTNET_PreferredVectorBitWidth=128",
            new Dictionary<string, string> { ["DOTNET_TieredCompilation"] = "0", ["DOTNET_PreferredVectorBitWidth"] = "128" }),
    };

    // Runtime knobs the reporter may have set are removed for the cases; these are not knobs.
    private static readonly string[] KeptRuntimeVariablePrefixes =
    {
        "DOTNET_ROOT", "DOTNET_HOST_PATH", "DOTNET_CLI_", "DOTNET_NOLOGO", "DOTNET_MULTILEVEL_LOOKUP",
        "DOTNET_SKIP_", "DOTNET_INSTALL_", "DOTNET_MSBUILD_", "DOTNET_GENERATE_",
    };

    private static string s_dotnetHost = "dotnet";
    private static readonly Stopwatch s_clock = Stopwatch.StartNew();

    public static int Run(string[] args)
    {
        int glyphRepeats = ReadOption(args, "--repeats", 5);
        int microRuns = ReadOption(args, "--micro-runs", 3);
        int glyphWarmupMs = ReadOption(args, "--warmup-ms", 1000);
        bool skipGlyph = args.Contains("--skip-glyph");

        string projectDirectory = Metadata("ProjectDirectory");
        string configuration = Metadata("Configuration");
        string repoRoot = Path.GetFullPath(Path.Combine(projectDirectory, "..", ".."));
        string selfPath = Environment.ProcessPath ?? "dotnet";
        bool hostedByDotnet = Path.GetFileNameWithoutExtension(selfPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        s_dotnetHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? (hostedByDotnet ? selfPath : "dotnet");
        string[] microLaunch = hostedByDotnet
            ? new[] { typeof(Runner).Assembly.Location, "--child-micro" }
            : new[] { "--child-micro" };

        string workDirectory = Path.Combine(Path.GetTempPath(), "gum-tiering-repro");
        TryDelete(workDirectory);
        Directory.CreateDirectory(workDirectory);

        Log("Gum #6388 tiering repro. Please leave the machine idle until it finishes (about 4 minutes).");

        // ---- Micro cases ----
        Dictionary<Setting, Dictionary<string, Series>> micro = new Dictionary<Setting, Dictionary<string, Series>>();
        Dictionary<Setting, bool> sameResults = new Dictionary<Setting, bool>();
        foreach (Setting setting in Settings)
        {
            Dictionary<string, Series> byName = new Dictionary<string, Series>();
            micro[setting] = byName;
            sameResults[setting] = true;
            for (int run = 0; run < microRuns; run++)
            {
                Log($"micro: {setting.Name}, run {run + 1}/{microRuns}");
                ChildResult result = RunChild(selfPath, microLaunch, setting.Env, 120_000, null, scrub: true);
                if (result.ExitCode != 0)
                {
                    Log($"  micro run failed: {Tail(result.Error + result.Output)}");
                    continue;
                }
                foreach (string line in result.Output.Split('\n'))
                {
                    string[] parts = line.Trim().Split('\t');
                    if (parts[0] == "MICRO" && parts.Length == 3)
                    {
                        if (!byName.TryGetValue(parts[1], out Series? series))
                        {
                            series = new Series();
                            byName[parts[1]] = series;
                        }
                        series.Values.Add(double.Parse(parts[2], CultureInfo.InvariantCulture));
                    }
                    else if (parts[0] == "SAME" && parts.Length >= 2 && parts[1] != "1")
                    {
                        sameResults[setting] = false;
                    }
                }
            }
        }

        // ---- Real glyph path ----
        Dictionary<Setting, (GlyphCell Old, GlyphCell New)> glyph = new Dictionary<Setting, (GlyphCell, GlyphCell)>();
        string? glyphNote = null;
        bool newVariantAvailable = false;
        string oldDirectory = Path.Combine(workDirectory, "old");
        string newDirectory = Path.Combine(workDirectory, "new");
        if (skipGlyph)
        {
            glyphNote = "skipped (--skip-glyph)";
        }
        else
        {
            string benchOutput = Path.Combine(projectDirectory, "GlyphBench", "bin", configuration, "net10.0");
            if (!File.Exists(Path.Combine(benchOutput, "GlyphBench.dll")))
            {
                glyphNote = $"GlyphBench output not found at {benchOutput}";
            }
            else
            {
                CopyDirectory(benchOutput, oldDirectory);
                string? buildFailure = BuildNewVariant(repoRoot, projectDirectory, configuration, workDirectory, oldDirectory, newDirectory);
                newVariantAvailable = buildFailure == null;
                if (buildFailure != null)
                {
                    glyphNote = "new-body variant could not be built (" + buildFailure + "); only the current body was measured";
                    Log("  " + glyphNote);
                }

                foreach (Setting setting in Settings)
                {
                    GlyphCell oldCell = new GlyphCell();
                    GlyphCell newCell = new GlyphCell();
                    glyph[setting] = (oldCell, newCell);
                    for (int repeat = 0; repeat < glyphRepeats; repeat++)
                    {
                        // Alternate which body runs first so a slow start does not always land on one of them.
                        bool oldFirst = repeat % 2 == 0;
                        for (int step = 0; step < 2; step++)
                        {
                            bool isOld = (step == 0) == oldFirst;
                            if (!isOld && !newVariantAvailable)
                            {
                                continue;
                            }
                            Log($"glyphs: {setting.Name}, repeat {repeat + 1}/{glyphRepeats}, {(isOld ? "current" : "new")} body");
                            RunGlyph(isOld ? oldDirectory : newDirectory, setting, glyphWarmupMs, isOld ? oldCell : newCell);
                        }
                    }
                }
            }
        }

        string block = BuildReport(micro, sameResults, glyph, glyphNote, newVariantAvailable, glyphRepeats, microRuns);
        string resultPath = Path.GetFullPath(ResultFileName);
        File.WriteAllText(resultPath, block);
        Console.WriteLine();
        Console.WriteLine($"Paste everything between the lines into the issue (also saved to {resultPath}):");
        Console.WriteLine("-----8<-----");
        Console.WriteLine(block);
        Console.WriteLine("-----8<-----");
        TryDelete(workDirectory);
        return 0;
    }

    private static void RunGlyph(string directory, Setting setting, int warmupMs, GlyphCell cell)
    {
        ChildResult result = RunChild(s_dotnetHost,
            new[] { Path.Combine(directory, "GlyphBench.dll"), warmupMs.ToString(CultureInfo.InvariantCulture), "200", "9" },
            setting.Env, 180_000, directory, scrub: true);
        string? resultLine = result.Output.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("RESULT\t", StringComparison.Ordinal));
        if (result.ExitCode != 0 || resultLine == null)
        {
            cell.Failure = result.TimedOut ? "timed out" : Tail(result.Error + result.Output);
            Log($"  glyph run failed: {cell.Failure}");
            return;
        }
        string[] parts = resultLine.Split('\t');
        cell.PerGlyphMicroseconds.Values.Add(double.Parse(parts[1], CultureInfo.InvariantCulture));
        cell.IlBytes.Add(int.Parse(parts[4], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Builds GumCommon.dll again with RoundToInt(float) replaced, using an MSBuild hook passed on the command
    /// line (no product file is edited), and drops it into a copy of the benchmark output. Returns a failure
    /// description, or null on success.
    /// </summary>
    private static string? BuildNewVariant(string repoRoot, string projectDirectory, string configuration,
        string workDirectory, string oldDirectory, string newDirectory)
    {
        const string oldBody = "(int)(System.Math.Round(floatToRound, MidpointRounding.AwayFromZero) + (System.Math.Sign(floatToRound) * .5f))";
        const string newBody = "(int)MathF.Round(floatToRound, MidpointRounding.AwayFromZero)";

        string sourcePath = Path.Combine(repoRoot, "RenderingLibrary", "Math", "MathFunctions.cs");
        string source = File.ReadAllText(sourcePath);
        int first = source.IndexOf(oldBody, StringComparison.Ordinal);
        if (first < 0 || source.IndexOf(oldBody, first + 1, StringComparison.Ordinal) >= 0)
        {
            return "MathFunctions.cs no longer has exactly one RoundToInt(float) body of the expected shape";
        }

        string swapFile = Path.Combine(workDirectory, "MathFunctions.NewRound.cs");
        File.WriteAllText(swapFile, source.Replace(oldBody, newBody, StringComparison.Ordinal));

        string binDirectory = Path.Combine(workDirectory, "newbin");
        string objDirectory = Path.Combine(workDirectory, "obj-new");
        Log("building GumCommon.dll with the new RoundToInt(float) body ...");
        ChildResult build = RunChild(s_dotnetHost, new[]
        {
            "build", Path.Combine(repoRoot, "GumCommon", "GumCommon.csproj"),
            "-c", configuration, "-f", "net8.0", "--no-restore", "-nologo", "-v:q",
            "-o", binDirectory,
            "-p:GeneratePackageOnBuild=false",
            "-p:IntermediateOutputPath=" + objDirectory.Replace('\\', '/') + "/",
            "-p:CustomAfterMicrosoftCommonTargets=" + Path.Combine(projectDirectory, "Variant", "swap.targets"),
            "-p:SwapMathFunctionsFile=" + swapFile,
        }, new Dictionary<string, string>(), 240_000, repoRoot, scrub: false);
        string builtDll = Path.Combine(binDirectory, "GumCommon.dll");
        if (build.ExitCode != 0 || !File.Exists(builtDll))
        {
            return "build failed: " + Tail(build.Output + build.Error);
        }

        CopyDirectory(oldDirectory, newDirectory);
        File.Copy(builtDll, Path.Combine(newDirectory, "GumCommon.dll"), overwrite: true);
        return null;
    }

    // ---------------------------------------------------------------- report

    private static string BuildReport(
        Dictionary<Setting, Dictionary<string, Series>> micro,
        Dictionary<Setting, bool> sameResults,
        Dictionary<Setting, (GlyphCell Old, GlyphCell New)> glyph,
        string? glyphNote, bool newVariantAvailable, int glyphRepeats, int microRuns)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("**Gum #6388 tiering repro**");
        text.AppendLine();
        text.AppendLine($"- CPU: {MachineInfo.CpuModel()}");
        text.AppendLine($"- OS: {MachineInfo.Os()}");
        text.AppendLine($"- .NET runtime: {MachineInfo.Runtime()}; SDK {MachineInfo.Sdk(s_dotnetHost)}");
        text.AppendLine($"- Vector support: {MachineInfo.Vectors()}");
        text.AppendLine("- Settings tried (each case runs in its own process with exactly these DOTNET_ variables and no others):");
        foreach (Setting setting in Settings)
        {
            text.AppendLine($"  - {setting.Name}: {setting.Description}");
        }
        List<string> inherited = InheritedRuntimeVariables();
        text.AppendLine(inherited.Count == 0
            ? "- DOTNET_/COMPlus_ variables set when this started: none"
            : "- DOTNET_/COMPlus_ variables set when this started (removed for the cases): " + string.Join(", ", inherited));
        text.AppendLine($"- Runs: tables A and B use {microRuns} processes per setting (best of 9 rounds inside each); table C uses {glyphRepeats} processes per body and setting (best of 9 batches of 200 frames inside each), alternating current and new body. Min and median are over those processes. Spread is slowest over fastest; above 20% the row is marked NOISY.");
        text.AppendLine();

        // Table A
        text.AppendLine("**A. Does a call into precompiled framework code slow down right after JIT code zeroes a block of memory?** (current RoundToInt body, ns per call)");
        text.AppendLine();
        text.AppendLine("| setting | call only (min / median) | after block zero (min / median, zeroing time removed) | slowdown (min / median) | spread | note |");
        text.AppendLine("|---|---|---|---|---|---|");
        Dictionary<Setting, (double RatioMin, double RatioMedian, bool Noisy)> penalties = new Dictionary<Setting, (double, double, bool)>();
        foreach (Setting setting in Settings)
        {
            Dictionary<string, Series> m = micro[setting];
            if (!HasMicro(m, "init", "old-clean", "old-dirty"))
            {
                text.AppendLine($"| {setting.Name} | failed | | | | |");
                continue;
            }
            double cleanMin = m["old-clean"].Min;
            double cleanMedian = m["old-clean"].Median;
            double dirtyMin = m["old-dirty"].Min - m["init"].Min;
            double dirtyMedian = m["old-dirty"].Median - m["init"].Median;
            double ratioMin = dirtyMin / cleanMin;
            double ratioMedian = dirtyMedian / cleanMedian;
            bool noisy = m["old-clean"].IsNoisy || m["old-dirty"].IsNoisy;
            penalties[setting] = (ratioMin, ratioMedian, noisy);
            text.AppendLine($"| {setting.Name} | {cleanMin:F2} / {cleanMedian:F2} | {dirtyMin:F2} / {dirtyMedian:F2} | {ratioMin:F2}x / {ratioMedian:F2}x | {Percent(m["old-clean"].Spread)} / {Percent(m["old-dirty"].Spread)} | {(noisy ? "NOISY" : "")} |");
        }
        text.AppendLine();

        // Table B
        text.AppendLine("**B. RoundToInt(float) alone: current body vs `(int)MathF.Round(x, MidpointRounding.AwayFromZero)`** (ns per call, min / median)");
        text.AppendLine();
        text.AppendLine("| setting | context | current | new | current / new (min) | note |");
        text.AppendLine("|---|---|---|---|---|---|");
        Dictionary<(Setting, bool), (double Ratio, bool Noisy)> isolated = new Dictionary<(Setting, bool), (double, bool)>();
        foreach (Setting setting in Settings)
        {
            Dictionary<string, Series> m = micro[setting];
            if (!HasMicro(m, "init", "old-clean", "new-clean", "old-dirty", "new-dirty"))
            {
                text.AppendLine($"| {setting.Name} | failed | | | | |");
                continue;
            }
            foreach (bool dirty in new[] { false, true })
            {
                string suffix = dirty ? "dirty" : "clean";
                double init = dirty ? m["init"].Min : 0;
                double initMedian = dirty ? m["init"].Median : 0;
                Series oldSeries = m["old-" + suffix];
                Series newSeries = m["new-" + suffix];
                double ratio = (oldSeries.Min - init) / (newSeries.Min - init);
                bool noisy = oldSeries.IsNoisy || newSeries.IsNoisy;
                isolated[(setting, dirty)] = (ratio, noisy);
                string note = (noisy ? "NOISY " : "") + (sameResults[setting] ? "" : "results differ on the test inputs");
                text.AppendLine($"| {setting.Name} | {(dirty ? "after block zero" : "call only")} | {oldSeries.Min - init:F2} / {oldSeries.Median - initMedian:F2} | {newSeries.Min - init:F2} / {newSeries.Median - initMedian:F2} | {ratio:F2}x | {note.Trim()} |");
            }
        }
        text.AppendLine();

        // Table C
        text.AppendLine("**C. Real Gum text drawing** (200 letters in a 300 px Text, hidden MonoGame window; microseconds per glyph draw, min / median over processes)");
        text.AppendLine();
        if (glyph.Count == 0)
        {
            text.AppendLine($"Not measured: {glyphNote}");
        }
        else
        {
            text.AppendLine("| setting | current body | new body | current / new (min) | spread current / new | note |");
            text.AppendLine("|---|---|---|---|---|---|");
            foreach (Setting setting in Settings)
            {
                (GlyphCell oldCell, GlyphCell newCell) = glyph[setting];
                Series o = oldCell.PerGlyphMicroseconds;
                Series n = newCell.PerGlyphMicroseconds;
                string note = "";
                if (o.Count == 0 || (newVariantAvailable && n.Count == 0))
                {
                    note = "run failed: " + (oldCell.Failure ?? newCell.Failure ?? "no result");
                }
                else if (o.IsNoisy || n.IsNoisy)
                {
                    note = "NOISY";
                }
                string oldCellText = o.Count == 0 ? "n/a" : $"{o.Min:F3} / {o.Median:F3}";
                string newCellText = n.Count == 0 ? "n/a" : $"{n.Min:F3} / {n.Median:F3}";
                string ratioText = o.Count == 0 || n.Count == 0 ? "n/a" : $"{o.Min / n.Min:F2}x";
                text.AppendLine($"| {setting.Name} | {oldCellText} | {newCellText} | {ratioText} | {Percent(o.Spread)} / {Percent(n.Spread)} | {note} |");
            }
            if (glyphNote != null)
            {
                text.AppendLine();
                text.AppendLine($"Note: {glyphNote}");
            }
            HashSet<int> oldIl = glyph.Values.SelectMany(g => g.Old.IlBytes).ToHashSet();
            HashSet<int> newIl = glyph.Values.SelectMany(g => g.New.IlBytes).ToHashSet();
            if (newVariantAvailable && (oldIl.Count != 1 || newIl.Count != 1 || oldIl.Overlaps(newIl)))
            {
                text.AppendLine();
                text.AppendLine("WARNING: the two text-drawing variants did not report different RoundToInt bodies; table C is not a valid A/B.");
            }
        }
        text.AppendLine();

        text.AppendLine("**Verdict:** " + Verdict(penalties, isolated, glyph, newVariantAvailable));
        text.AppendLine();
        text.AppendLine($"_Total run time {s_clock.Elapsed.TotalMinutes:F1} min._");
        return text.ToString();
    }

    private static string Verdict(
        Dictionary<Setting, (double RatioMin, double RatioMedian, bool Noisy)> penalties,
        Dictionary<(Setting, bool), (double Ratio, bool Noisy)> isolated,
        Dictionary<Setting, (GlyphCell Old, GlyphCell New)> glyph,
        bool newVariantAvailable)
    {
        Setting on = Settings[0];
        Setting off = Settings[1];
        Setting avx0 = Settings[2];
        Setting width128 = Settings[3];

        string penaltyPart;
        if (!penalties.TryGetValue(off, out (double RatioMin, double RatioMedian, bool Noisy) offPenalty))
        {
            penaltyPart = "penalty observed: unknown (micro case failed)";
        }
        else
        {
            bool yes = offPenalty.RatioMin >= PenaltyRatioThreshold;
            string controls = string.Join(", ", new[] { (avx0, "EnableAVX=0"), (width128, "vector width 128") }
                .Select(c => $"{c.Item2}: {(penalties.TryGetValue(c.Item1, out (double RatioMin, double RatioMedian, bool Noisy) p) ? $"{p.RatioMin:F2}x" : "n/a")}"));
            string onText = penalties.TryGetValue(on, out (double RatioMin, double RatioMedian, bool Noisy) onPenalty) ? $"{onPenalty.RatioMin:F2}x" : "n/a";
            string removed = "";
            if (yes && penalties.TryGetValue(avx0, out (double RatioMin, double RatioMedian, bool Noisy) avxPenalty))
            {
                removed = avxPenalty.RatioMin < PenaltyRemovedRatio ? "; EnableAVX=0 removes it" : "; EnableAVX=0 does not remove it";
            }
            penaltyPart = $"penalty observed: {(yes ? "yes" : "no")}{(offPenalty.Noisy ? " (noisy)" : "")} " +
                $"(cost of the call after a block zero relative to call only: tiering off {offPenalty.RatioMin:F2}x, tiering on {onText}; {controls}{removed})";
        }

        string changePart;
        if (glyph.Count > 0 && newVariantAvailable
            && glyph[off].Old.PerGlyphMicroseconds.Count > 0 && glyph[off].New.PerGlyphMicroseconds.Count > 0)
        {
            string offText = DescribeChange(glyph[off].Old.PerGlyphMicroseconds, glyph[off].New.PerGlyphMicroseconds);
            string onText = glyph[on].Old.PerGlyphMicroseconds.Count > 0 && glyph[on].New.PerGlyphMicroseconds.Count > 0
                ? DescribeChange(glyph[on].Old.PerGlyphMicroseconds, glyph[on].New.PerGlyphMicroseconds)
                : "n/a";
            string isolatedText = isolated.TryGetValue((off, true), out (double Ratio, bool Noisy) isolatedOff) ? $"; isolated call, tiering off: {Faster(isolatedOff.Ratio)}" : "";
            changePart = $"RoundToInt change in real text drawing: tiering off: {offText}; tiering on: {onText}{isolatedText}";
        }
        else if (isolated.TryGetValue((off, true), out (double Ratio, bool Noisy) iso))
        {
            changePart = $"RoundToInt change (isolated call only, real text drawing not measured): tiering off, after block zero: {Faster(iso.Ratio)}";
        }
        else
        {
            changePart = "RoundToInt change: unknown (nothing measured)";
        }
        return penaltyPart + ". " + changePart + ".";
    }

    private static string DescribeChange(Series current, Series changed)
    {
        double percent = (current.Min - changed.Min) / current.Min * 100;
        double medianPercent = (current.Median - changed.Median) / current.Median * 100;
        bool noisy = current.IsNoisy || changed.IsNoisy;
        string text;
        if (Math.Abs(percent) < ChangeThresholdPercent || Math.Sign(percent) != Math.Sign(medianPercent))
        {
            text = $"no change ({percent:F0}% by min, {medianPercent:F0}% by median; {current.Min:F3} -> {changed.Min:F3} us per glyph draw)";
        }
        else
        {
            text = $"{Math.Abs(percent):F0}% {(percent > 0 ? "faster" : "slower")} ({current.Min:F3} -> {changed.Min:F3} us per glyph draw, {Math.Abs(medianPercent):F0}% by median)";
        }
        return noisy ? text + " [noisy]" : text;
    }

    private static string Faster(double ratio)
    {
        double percent = (1 - 1 / ratio) * 100;
        return Math.Abs(percent) < ChangeThresholdPercent
            ? $"no change ({percent:F0}%)"
            : $"{Math.Abs(percent):F0}% {(percent > 0 ? "faster" : "slower")}";
    }

    private static bool HasMicro(Dictionary<string, Series> byName, params string[] names) =>
        names.All(n => byName.TryGetValue(n, out Series? s) && s.Count > 0);

    private static string Percent(double spread) => $"{spread * 100:F0}%";

    // ---------------------------------------------------------------- plumbing

    private static List<string> InheritedRuntimeVariables()
    {
        List<string> names = new List<string>();
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            string name = (string)entry.Key;
            if (IsRuntimeKnob(name))
            {
                names.Add($"{name}={entry.Value}");
            }
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    private static bool IsRuntimeKnob(string name) =>
        (name.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("COMPlus_", StringComparison.OrdinalIgnoreCase))
        && !KeptRuntimeVariablePrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static ChildResult RunChild(string file, IEnumerable<string> arguments, Dictionary<string, string> env,
        int timeoutMilliseconds, string? workingDirectory, bool scrub)
    {
        ProcessStartInfo info = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (workingDirectory != null)
        {
            info.WorkingDirectory = workingDirectory;
        }
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }
        if (scrub)
        {
            foreach (string name in info.Environment.Keys.ToList())
            {
                if (IsRuntimeKnob(name))
                {
                    info.Environment.Remove(name);
                }
            }
        }
        foreach ((string name, string value) in env)
        {
            info.Environment[name] = value;
        }

        using Process process = Process.Start(info)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(timeoutMilliseconds);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        return new ChildResult(exited ? process.ExitCode : -1, output.Result, error.Result, !exited);
    }

    private static string Tail(string text)
    {
        string[] lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        return string.Join(" | ", lines.Skip(Math.Max(0, lines.Length - 3)));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Metadata(string key) =>
        typeof(Runner).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == key).Value ?? "";

    private static int ReadOption(string[] args, string name, int defaultValue)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int value) ? value : defaultValue;
    }

    private static void Log(string message) =>
        Console.WriteLine($"[{s_clock.Elapsed:mm\\:ss}] {message}");
}
