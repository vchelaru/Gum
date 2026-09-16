using System;
using Gum.Diagnostics;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class UiHeartbeatStallDetectorTests
{
    [Fact]
    public void HasNewlyStalled_FalseBeforeThresholdElapses()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        UiHeartbeatStallDetector detector = new UiHeartbeatStallDetector(TimeSpan.FromSeconds(5), start);

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(4)).ShouldBeFalse();
    }

    [Fact]
    public void HasNewlyStalled_TrueOnceThresholdElapsesSinceLastHeartbeat()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        UiHeartbeatStallDetector detector = new UiHeartbeatStallDetector(TimeSpan.FromSeconds(5), start);

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(6)).ShouldBeTrue();
    }

    [Fact]
    public void HasNewlyStalled_FiresOnlyOnceUntilTheNextHeartbeat()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        UiHeartbeatStallDetector detector = new UiHeartbeatStallDetector(TimeSpan.FromSeconds(5), start);

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(6)).ShouldBeTrue();
        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(7)).ShouldBeFalse();
    }

    [Fact]
    public void Heartbeat_ResetsTheStallWindow()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        UiHeartbeatStallDetector detector = new UiHeartbeatStallDetector(TimeSpan.FromSeconds(5), start);

        detector.Heartbeat(start + TimeSpan.FromSeconds(3));

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(7)).ShouldBeFalse();
        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(9)).ShouldBeTrue();
    }

    [Fact]
    public void Heartbeat_AfterAFiredStall_AllowsItToFireAgain()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        UiHeartbeatStallDetector detector = new UiHeartbeatStallDetector(TimeSpan.FromSeconds(5), start);

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(6)).ShouldBeTrue();
        detector.Heartbeat(start + TimeSpan.FromSeconds(7));

        detector.HasNewlyStalled(start + TimeSpan.FromSeconds(13)).ShouldBeTrue();
    }
}
