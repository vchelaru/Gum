using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class UnattendedPreviewRunTests
{
    [Fact]
    public void TryClaimCapture_BeforeLoaded_IsFalse()
    {
        UnattendedPreviewRun run = new UnattendedPreviewRun(exitAfterSeconds: 30);

        run.TryClaimCapture().ShouldBeFalse();
    }

    [Fact]
    public void TryClaimCapture_AfterLoaded_ClaimsOnce_AndThenTheDeadlineIsIgnored()
    {
        UnattendedPreviewRun run = new UnattendedPreviewRun(exitAfterSeconds: 30);
        run.MarkLoaded();

        run.TryClaimCapture().ShouldBeTrue();

        run.TryClaimCapture().ShouldBeFalse();
        run.TryExpire().ShouldBeNull();
    }

    [Fact]
    public void TryExpire_BeforeLoaded_NamesTheDeadlineAndTheLoadStage_AndBlocksCapture()
    {
        UnattendedPreviewRun run = new UnattendedPreviewRun(exitAfterSeconds: 30);

        string? message = run.TryExpire();

        message.ShouldNotBeNull();
        message.ShouldContain("--exit-after 30 s");
        message.ShouldContain("loading");
        run.MarkLoaded();
        run.TryClaimCapture().ShouldBeFalse();
    }

    [Fact]
    public void TryExpire_AfterLoadedWithNoFrame_NamesTheFrameStage()
    {
        UnattendedPreviewRun run = new UnattendedPreviewRun(exitAfterSeconds: 30);
        run.MarkLoaded();

        string? message = run.TryExpire();

        message.ShouldNotBeNull();
        message.ShouldContain("frame");
    }

    [Fact]
    public void TryFail_ClaimsTheFinish_WithTheReason()
    {
        UnattendedPreviewRun run = new UnattendedPreviewRun(exitAfterSeconds: 30);

        string? message = run.TryFail("no screen or component named 'X'");

        message.ShouldNotBeNull();
        message.ShouldContain("no screen or component named 'X'");
        run.TryExpire().ShouldBeNull();
    }
}
