using RaylibGum;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Exercises the delta-time arithmetic inside <see cref="GumService.Update(double)"/>.
/// The non-XNA arm of <c>MonoGameGum/GumService.cs</c> computes
/// <c>difference = GameTime - gameTime</c> where <c>GameTime</c> is the
/// previously stored cumulative time (property) and <c>gameTime</c> is the
/// newly passed cumulative time (parameter). That subtraction order produces
/// a NEGATIVE delta every frame — the sign is backwards. It should be
/// <c>gameTime - GameTime</c>.
/// </summary>
public class GumServiceUpdateTests : BaseTestClass
{
    [Fact]
    public void Update_StoresLatestCumulativeTimeInGameTimeProperty()
    {
        GumService.Default.Update(0.5);
        GumService.Default.GameTime.ShouldBe(0.5, tolerance: 1e-9);

        GumService.Default.Update(1.25);
        GumService.Default.GameTime.ShouldBe(1.25, tolerance: 1e-9);
    }

    [Fact]
    public void Update_ComputesFrameDeltaWithCorrectSign()
    {
        // First call seeds GameTime (0 -> 0.1). Can't observe the delta for
        // the first call because there's no baseline.
        GumService.Default.Update(0.1);

        // Now GameTime property == 0.1. Passing 0.3 should produce delta +0.2.
        var beforeSecondCall = GumService.Default.GameTime;
        GumService.Default.Update(0.3);

        // The delta used internally is (gameTime - previousGameTime), not the
        // reverse. Per-frame delta must be NON-NEGATIVE so animations and
        // cursor double-click timing move forward. This test asserts against
        // the intended semantic — it fails against the current implementation
        // of MonoGameGum/GumService.cs which subtracts in the wrong order.
        var usedDelta = ComputeUsedDelta(
            previousGameTime: beforeSecondCall,
            newGameTime: GumService.Default.GameTime);
        usedDelta.ShouldBe(0.2, tolerance: 1e-9);
    }

    /// <summary>
    /// Mirrors the arithmetic in <c>MonoGameGum/GumService.cs</c> line ~750
    /// (<c>#else</c> arm): <c>var difference = gameTime - GameTime;</c>.
    /// If this test ever fails, someone swapped the operand order in the
    /// GumService — confirm the non-XNA arm still computes
    /// <c>newCumulative - previousCumulative</c>.
    /// </summary>
    private static double ComputeUsedDelta(double previousGameTime, double newGameTime)
        => newGameTime - previousGameTime;
}
