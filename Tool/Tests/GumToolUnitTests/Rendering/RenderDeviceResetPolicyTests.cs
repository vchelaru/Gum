using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using XnaAndWinforms;

namespace GumToolUnitTests.Rendering;

public class RenderDeviceResetPolicyTests : BaseTestClass
{
    [Fact]
    public void Evaluate_DeviceLost_ReportsLostAndDoesNotRequestReset()
    {
        RenderDeviceResetPolicy policy = new RenderDeviceResetPolicy();

        RenderDeviceResetDecision decision = policy.Evaluate(
            GraphicsDeviceStatus.Lost, backBufferWidth: 800, backBufferHeight: 600, surfaceWidth: 800, surfaceHeight: 600);

        decision.IsDeviceLost.ShouldBeTrue();
        decision.NeedsReset.ShouldBeFalse();
        decision.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Evaluate_DeviceNotReset_RequestsReset()
    {
        RenderDeviceResetPolicy policy = new RenderDeviceResetPolicy();

        RenderDeviceResetDecision decision = policy.Evaluate(
            GraphicsDeviceStatus.NotReset, backBufferWidth: 800, backBufferHeight: 600, surfaceWidth: 800, surfaceHeight: 600);

        decision.NeedsReset.ShouldBeTrue();
        decision.IsDeviceLost.ShouldBeFalse();
        decision.Message.ShouldNotBeNullOrEmpty();
    }

    // The shared device demand-grows to the largest client, so a surface that still fits inside the
    // existing back buffer must not force a reset - only one that exceeds it on either axis.
    [Theory]
    [InlineData(800, 600, false)]
    [InlineData(400, 300, false)]
    [InlineData(801, 600, true)]
    [InlineData(800, 601, true)]
    public void Evaluate_NormalStatus_RequestsResetOnlyWhenSurfaceExceedsBackBuffer(
        int surfaceWidth, int surfaceHeight, bool expectedNeedsReset)
    {
        RenderDeviceResetPolicy policy = new RenderDeviceResetPolicy();

        RenderDeviceResetDecision decision = policy.Evaluate(
            GraphicsDeviceStatus.Normal, backBufferWidth: 800, backBufferHeight: 600, surfaceWidth, surfaceHeight);

        decision.NeedsReset.ShouldBe(expectedNeedsReset);
        decision.IsDeviceLost.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_ZeroSurfaceSize_ClampsTargetSizeToOne()
    {
        RenderDeviceResetPolicy policy = new RenderDeviceResetPolicy();

        RenderDeviceResetDecision decision = policy.Evaluate(
            GraphicsDeviceStatus.Normal, backBufferWidth: 800, backBufferHeight: 600, surfaceWidth: 0, surfaceHeight: 0);

        decision.TargetWidth.ShouldBe(1);
        decision.TargetHeight.ShouldBe(1);
    }
}
