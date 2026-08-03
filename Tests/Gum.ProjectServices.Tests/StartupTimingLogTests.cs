using System;
using System.Collections.Generic;
using Gum.Diagnostics;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class StartupTimingLogTests
{
    [Fact]
    public void Mark_RecordsElapsedAndDeltaSincePreviousMark()
    {
        List<string> written = new List<string>();
        long now = 0;
        StartupTimingLog log = new StartupTimingLog(written.Add, () => now);

        now = 100;
        log.Mark("first");
        now = 250;
        log.Mark("second");

        written.Count.ShouldBe(2);
        written[0].ShouldContain("100");
        written[0].ShouldContain("first");
        written[1].ShouldContain("+   150");
        written[1].ShouldContain("second");
    }

    [Fact]
    public void MarkOnce_WritesOnlyTheFirstTimeForALabel()
    {
        List<string> written = new List<string>();
        long now = 0;
        StartupTimingLog log = new StartupTimingLog(written.Add, () => now);

        log.MarkOnce("first refresh");
        now = 500;
        log.MarkOnce("first refresh");

        written.Count.ShouldBe(1);
    }

    [Fact]
    public void Time_RecordsTheDurationOfTheScope()
    {
        List<string> written = new List<string>();
        long now = 0;
        StartupTimingLog log = new StartupTimingLog(written.Add, () => now);

        using (log.Time("slow step"))
        {
            now = 420;
        }

        written.Count.ShouldBe(1);
        written[0].ShouldContain("+   420");
        written[0].ShouldContain("slow step");
    }

    [Fact]
    public void DisabledLog_WritesNothingAndDoesNotThrow()
    {
        StartupTimingLog log = new StartupTimingLog(sink: null, () => 0);

        log.IsEnabled.ShouldBeFalse();
        log.Mark("a");
        log.MarkOnce("b");
        log.Log("c");
        using (log.Time("d"))
        {
        }
    }
}
