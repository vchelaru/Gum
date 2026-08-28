using HtmlToGumPlugin;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace GumToolUnitTests.Plugins.HtmlToGumPlugin;

public class ImportPhaseRecorderTests
{
    /// <summary>Elapsed-time stub returning each queued reading in turn.</summary>
    private static Func<TimeSpan> Clock(params int[] readingsMs)
    {
        int index = 0;
        return () => TimeSpan.FromMilliseconds(readingsMs[index++]);
    }

    [Fact]
    public async Task Measure_RecordsPhasesInOrderAndReturnsResult()
    {
        ImportPhaseRecorder recorder = new(Clock(10, 30, 30, 130));

        int deserialized = recorder.Measure("deserialize", () => 7);
        string imported = await recorder.MeasureAsync("ImportScreen", () => Task.FromResult("screen"));

        deserialized.ShouldBe(7);
        imported.ShouldBe("screen");
        recorder.Phases.Select(p => $"{p.Name}={p.Milliseconds}")
            .ShouldBe(["deserialize=20", "ImportScreen=100"]);
        recorder.Total.ShouldBe(130);
    }

    [Fact]
    public void Measure_RecordsThePhaseEvenWhenTheWorkThrows()
    {
        ImportPhaseRecorder recorder = new(Clock(0, 45));

        Should.Throw<InvalidOperationException>(
            () => recorder.Measure("LoadProject", () => throw new InvalidOperationException("boom")));

        recorder.Phases.Single().Milliseconds.ShouldBe(45);
    }
}

public class HtmlImportTimingLogTests
{
    /// <summary>Collapses column padding so assertions pin content, not alignment.</summary>
    private static string Collapse(string line) => Regex.Replace(line, @"\s+", " ").Trim();

    [Fact]
    public void ParseConverterTimings_ReadsPhasesCountsAndTotal()
    {
        string json = """
            {
              "phases": [ { "name": "browser launch", "ms": 412 }, { "name": "primary.goto", "ms": 5100 } ],
              "counts": { "nodes": 1520, "instances": 812 },
              "totalMs": 36000
            }
            """;

        ConverterTimings? parsed = HtmlImportTimingLog.ParseConverterTimings(json);

        parsed.ShouldNotBeNull();
        parsed.Phases.Select(p => $"{p.Name}={p.Milliseconds}")
            .ShouldBe(["browser launch=412", "primary.goto=5100"]);
        parsed.Counts["nodes"].ShouldBe(1520);
        parsed.TotalMilliseconds.ShouldBe(36000);
    }

    [Fact]
    public void ParseConverterTimings_ReturnsNullForUnparseableJson()
    {
        HtmlImportTimingLog.ParseConverterTimings("not json").ShouldBeNull();
    }

    [Fact]
    public void Format_ListsConverterThenPluginPhasesWithTotalsAndCounts()
    {
        ImportTimingRun run = new()
        {
            TimestampUtc = new DateTime(2026, 8, 27, 14, 3, 22, DateTimeKind.Utc),
            Source = "https://example.com",
            ScreenName = "Example",
            ViewportWidth = 800,
            ViewportHeight = 600,
            Responsive = true,
            PluginPhases =
            [
                new ImportTimingPhase { Name = "converter process", Milliseconds = 37200 },
                new ImportTimingPhase { Name = "LoadProject", Milliseconds = 110000 },
            ],
            PluginTotalMilliseconds = 155700,
            ConverterTimings = new ConverterTimings
            {
                Phases = [new ImportTimingPhase { Name = "browser launch", Milliseconds = 412 }],
                Counts = new Dictionary<string, long> { ["instances"] = 812, ["nodes"] = 1520 },
                TotalMilliseconds = 36000,
            },
        };

        string[] lines = HtmlImportTimingLog.Format(run).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        lines.Select(Collapse).ShouldBe(
        [
            "=== 2026-08-27 14:03:22Z screen=Example viewport=800x600 responsive=on",
            "source=https://example.com",
            "converter browser launch 412 ms",
            "converter (total) 36000 ms",
            "plugin converter process 37200 ms",
            "plugin LoadProject 110000 ms",
            "plugin (total) 155700 ms",
            "counts instances=812 nodes=1520",
        ]);
    }

    [Fact]
    public void Format_SaysSoWhenTheConverterWroteNoTimings()
    {
        ImportTimingRun run = new()
        {
            TimestampUtc = new DateTime(2026, 8, 27, 14, 3, 22, DateTimeKind.Utc),
            Source = @"C:\pages\local.html",
            ScreenName = "Local",
            ViewportWidth = 800,
            ViewportHeight = 600,
            Responsive = false,
            PluginPhases = [new ImportTimingPhase { Name = "converter process", Milliseconds = 900 }],
            PluginTotalMilliseconds = 900,
            ConverterTimings = null,
        };

        string formatted = HtmlImportTimingLog.Format(run);

        formatted.ShouldContain("responsive=off");
        formatted.ShouldContain("converter  (no timings.json — converter did not finish)");
        formatted.ShouldNotContain("counts");
    }
}
