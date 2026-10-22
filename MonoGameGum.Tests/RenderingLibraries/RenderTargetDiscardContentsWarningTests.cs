using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #6461: baking an IsRenderTarget container switches to its render target and back. If the
/// target Gum switches back to uses <see cref="RenderTargetUsage.DiscardContents"/>, the device clears
/// it on the switch and everything drawn before Gum in the frame disappears. Gum does not change that
/// setting; <see cref="Renderer.TryReportDiscardingRestoreTarget"/> reports it once per renderer
/// through <see cref="CustomSetPropertyOnRenderable.PropertyAssignmentError"/> (mirrored to
/// Console.Error). This is the decision seam: it takes the usage of the restore target (the back buffer,
/// or the user render target bound before the draw) so it needs no GraphicsDevice. The end-to-end draw
/// is pinned in MonoGameGum.IntegrationTests.RenderTargetDiscardContentsWarningTests.
/// </summary>
public class RenderTargetDiscardContentsWarningTests : BaseTestClass
{
    [Fact]
    public void TryReportDiscardingRestoreTarget_DiscardContents_ReportsOnce()
    {
        Renderer renderer = new();

        List<string> reported = Capture(() =>
        {
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents).ShouldBeTrue();
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents).ShouldBeFalse();
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents).ShouldBeFalse();
        });

        reported.Count.ShouldBe(1);
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_Message_NamesTheFix()
    {
        Renderer renderer = new();

        List<string> reported = Capture(() =>
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents));

        reported.Count.ShouldBe(1);
        reported[0].ShouldContain("IsRenderTarget");
        reported[0].ShouldContain("RenderTargetUsage.PreserveContents");
        reported[0].ShouldContain("PreparingDeviceSettings");
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_PreserveContents_DoesNotReport()
    {
        Renderer renderer = new();

        List<string> reported = Capture(() =>
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.PreserveContents).ShouldBeFalse());

        reported.ShouldBeEmpty();
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_PlatformContents_DoesNotReport()
    {
        Renderer renderer = new();

        List<string> reported = Capture(() =>
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.PlatformContents).ShouldBeFalse());

        reported.ShouldBeEmpty();
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_PreserveThenDiscard_StillReportsTheDiscard()
    {
        Renderer renderer = new();

        List<string> reported = Capture(() =>
        {
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.PreserveContents);
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents).ShouldBeTrue();
        });

        reported.Count.ShouldBe(1);
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_SeparateRenderers_ReportIndependently()
    {
        Renderer first = new();
        Renderer second = new();

        List<string> reported = Capture(() =>
        {
            first.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents);
            second.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents);
        });

        reported.Count.ShouldBe(2);
    }

    [Fact]
    public void TryReportDiscardingRestoreTarget_Report_ReachesConsoleError()
    {
        Renderer renderer = new();
        TextWriter originalError = Console.Error;
        StringWriter captured = new();
        Console.SetError(captured);
        try
        {
            renderer.TryReportDiscardingRestoreTarget(RenderTargetUsage.DiscardContents);
        }
        finally
        {
            Console.SetError(originalError);
        }

        captured.ToString().ShouldStartWith("[Gum] An IsRenderTarget container");
    }

    private static List<string> Capture(Action action)
    {
        List<string> reported = new();
        void Handler(string message) => reported.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            action();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
        return reported;
    }
}
