using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The fixture's guard on check-derived reads: reading banners, the refusal, the folder notice or the OK button after an
/// input change without <c>Settle()</c> throws every time, and the slow-checks switch makes the checks take real time.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiAsyncStateGuardTests
{
    private static SlowExistingCodeProjectDetector SlowDetector() =>
        new SlowExistingCodeProjectDetector(new ExistingCodeProjectDetector(), TimeSpan.Zero);

    [AvaloniaFact]
    public void ReadingTextsWhileACheckIsBlocked_Throws_AndReadingAfterSettleDoesNot()
    {
        SlowExistingCodeProjectDetector detector = SlowDetector();
        using NewProjectUiFixture fixture = new NewProjectUiFixture(detector: detector);
        try
        {
            detector.Gate.Reset();
            fixture.Dialog.TypeName("Other");

            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => fixture.IsShown("anything"));
            exception.Message.ShouldContain("Settle()");
            exception.Message.ShouldContain("still running");
            Should.Throw<InvalidOperationException>(() => fixture.Dialog.OkButton);
        }
        finally
        {
            detector.Gate.Set();
        }

        fixture.Settle();
        fixture.IsShown("anything").ShouldBeFalse();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ReadingTextsAfterACheckFinishedButWasNotWaitedFor_StillThrows()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Other");
        fixture.WaitUntil(() => fixture.ViewModel.ChecksRefresh.IsCompleted, "the checks");

        Should.Throw<InvalidOperationException>(() => fixture.VisibleTexts()).Message.ShouldContain("finished but was not waited for");

        fixture.Settle();
        Should.NotThrow(() => fixture.VisibleTexts());
    }

    [AvaloniaFact]
    public void TheSlowChecksDetector_DelaysEachCheckPass()
    {
        SlowExistingCodeProjectDetector detector = SlowDetector();
        detector.Delay = TimeSpan.FromMilliseconds(300);
        using NewProjectUiFixture fixture = new NewProjectUiFixture(detector: detector);

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        fixture.Dialog.TypeName("Other");
        fixture.Settle();

        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(250);
    }

    [AvaloniaFact]
    public void TheEnvironmentVariable_ParsesToADelay()
    {
        string? original = Environment.GetEnvironmentVariable(SlowExistingCodeProjectDetector.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(SlowExistingCodeProjectDetector.EnvironmentVariable, "200");
            SlowExistingCodeProjectDetector.DelayFromEnvironment().ShouldBe(TimeSpan.FromMilliseconds(200));
            Environment.SetEnvironmentVariable(SlowExistingCodeProjectDetector.EnvironmentVariable, "0");
            SlowExistingCodeProjectDetector.DelayFromEnvironment().ShouldBeNull();
            Environment.SetEnvironmentVariable(SlowExistingCodeProjectDetector.EnvironmentVariable, null);
            SlowExistingCodeProjectDetector.DelayFromEnvironment().ShouldBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(SlowExistingCodeProjectDetector.EnvironmentVariable, original);
        }
    }

    [AvaloniaFact]
    public void OpeningASection_WaitsForThePendingChecks_BeforeItClicksTheHeading()
    {
        SlowExistingCodeProjectDetector detector = SlowDetector();
        using NewProjectUiFixture fixture = new NewProjectUiFixture(detector: detector);
        detector.Gate.Reset();
        using System.Threading.Timer release = new System.Threading.Timer(_ => detector.Gate.Set(), null, 400, Timeout.Infinite);
        try
        {
            fixture.ViewModel.ProjectName = "Other";

            fixture.Dialog.OpenCommandLine();

            fixture.ViewModel.ChecksRefresh.IsCompleted.ShouldBeTrue("a heading moves when the checks land, so the click waits for them");
        }
        finally
        {
            detector.Gate.Set();
        }
    }
}
