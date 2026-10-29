using Gum;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class PreviewReloadPolicyTests
{
    private static HotReloadResult Result(params HotReloadIssue[] issues) => new HotReloadResult(issues);

    private static HotReloadIssue Issue(HotReloadIssueKind kind, string message = "problem") =>
        new HotReloadIssue(kind, message);

    [Fact]
    public void CleanResult_AsksForNothing()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result());

        policy.IsReloadPending.ShouldBeFalse();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Theory]
    [InlineData(HotReloadIssueKind.InstanceRemoved)]
    [InlineData(HotReloadIssueKind.InstanceRebuilt)]
    public void IssuesThatMatchARestart_AskForNothing(HotReloadIssueKind kind)
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(kind)));

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Theory]
    [InlineData(HotReloadIssueKind.MissingFile)]
    [InlineData(HotReloadIssueKind.FontFailure)]
    [InlineData(HotReloadIssueKind.ReloadFailed)]
    [InlineData(HotReloadIssueKind.ShownElementGone)]
    public void IssuesThatDoNotMatchARestart_AskForOneRebuild(HotReloadIssueKind kind)
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(kind)));

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMixOfMatchingAndNonMatchingIssues_AsksForARebuild()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.InstanceRemoved), Issue(HotReloadIssueKind.MissingFile)));

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void ANewerMatchingResult_CancelsTheRebuildAnOlderOneAskedFor()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed)));

        policy.OnReloadFinished(Result());

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void ACleanReload_WhileTheShownElementIsMissing_AsksForARebuild()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(), isShownElementMissing: true);

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMatchingResult_WhileTheElementIsNotMissing_StillAsksForNothing()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.InstanceRebuilt)), isShownElementMissing: false);

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMissingElement_RebuildsAfterEachReload_ButNotBetweenReloads()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(), isShownElementMissing: true);
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.TryTakePendingReload().ShouldBeFalse();
        policy.OnReloadFinished(Result(), isShownElementMissing: true);

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void TheSameNonMatchingResultOnConsecutiveReloads_AsksForARebuildEachTime()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        HotReloadIssue sticky = Issue(HotReloadIssueKind.StandardElementChanged, "Text changed");

        policy.OnReloadFinished(Result(sticky));
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.OnReloadFinished(Result(sticky));
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.OnReloadFinished(Result(sticky));

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void SeveralResultsBeforeTheRebuild_AskForOnlyOne()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed, "first")));
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ShownElementGone)));

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void NoRebuildWithoutAFinishedReload()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile)), isShownElementMissing: true);
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.TryTakePendingReload().ShouldBeFalse();
        policy.TryTakePendingReload().ShouldBeFalse();
    }
}
