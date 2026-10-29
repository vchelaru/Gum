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
    public void SeveralNonMatchingResultsBeforeTheRebuild_AskForOnlyOne()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed, "first")));
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ShownElementGone)));

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
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
    public void TheSameProblemAfterARebuild_DoesNotRebuildAgain()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void TheSameProblemWithMatchingIssuesComingAndGoing_DoesNotRebuildAgain()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png"), Issue(HotReloadIssueKind.InstanceRebuilt, "Button")));

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void ADifferentProblemAfterARebuild_RebuildsAgain()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png"), Issue(HotReloadIssueKind.ShownElementGone, "Main")));

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void TheProblemComingBackAfterAMatchingResult_RebuildsAgain()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.OnReloadFinished(Result());

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile, "Wall.png")));

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void ARebuildThatFailed_IsRetriedByTheNextSameResult()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed, "locked")));
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.NotifyReloadFailed();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed, "locked")));

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void ASameResultWhileARebuildIsStillPending_KeepsItPending()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ShownElementGone)));

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ShownElementGone)));

        policy.TryTakePendingReload().ShouldBeTrue();
    }
}
