using Gum;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
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

        policy.OnReloadFinished(Result(), isStateSelected: false);

        policy.IsReloadPending.ShouldBeFalse();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void CleanResult_WithAStateSelected_AsksForOneRebuild()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(), isStateSelected: true);

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void CleanResult_WithNoStateSelected_AsksForNothing()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void ReloadFailedWithAStateSelected_AsksForOneRebuildPerFinishedResult()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed)), isStateSelected: true);
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed)), isStateSelected: true);
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void IsStateSelected_SelectionWithAState_IsTrue()
    {
        PreviewSelectionMessage selection = new PreviewSelectionMessage("Widget") { CategoryName = "Look", StateName = "Highlighted" };

        PreviewReloadPolicy.IsStateSelected(selection).ShouldBeTrue();
    }

    [Fact]
    public void IsStateSelected_SelectionWithNoState_IsFalse()
    {
        PreviewReloadPolicy.IsStateSelected(new PreviewSelectionMessage("Widget")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HotReloadIssueKind.InstanceRemoved)]
    [InlineData(HotReloadIssueKind.InstanceRebuilt)]
    public void IssuesThatMatchARestart_AskForNothing(HotReloadIssueKind kind)
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(kind)), isStateSelected: false);

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

        policy.OnReloadFinished(Result(Issue(kind)), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMixOfMatchingAndNonMatchingIssues_AsksForARebuild()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.InstanceRemoved), Issue(HotReloadIssueKind.MissingFile)), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void ANewerMatchingResult_CancelsTheRebuildAnOlderOneAskedFor()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed)), isStateSelected: false);

        policy.OnReloadFinished(Result(), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void ACleanReload_WhileTheShownElementIsMissing_AsksForARebuild()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(), isStateSelected: false, isShownElementMissing: true);

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMatchingResult_WhileTheElementIsNotMissing_StillAsksForNothing()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.InstanceRebuilt)), isStateSelected: false, isShownElementMissing: false);

        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void AMissingElement_RebuildsAfterEachReload_ButNotBetweenReloads()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(), isStateSelected: false, isShownElementMissing: true);
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.TryTakePendingReload().ShouldBeFalse();
        policy.OnReloadFinished(Result(), isStateSelected: false, isShownElementMissing: true);

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void TheSameNonMatchingResultOnConsecutiveReloads_AsksForARebuildEachTime()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        HotReloadIssue sticky = Issue(HotReloadIssueKind.StandardElementChanged, "Text changed");

        policy.OnReloadFinished(Result(sticky), isStateSelected: false);
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.OnReloadFinished(Result(sticky), isStateSelected: false);
        policy.TryTakePendingReload().ShouldBeTrue();
        policy.OnReloadFinished(Result(sticky), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeTrue();
    }

    [Fact]
    public void SeveralResultsBeforeTheRebuild_AskForOnlyOne()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();

        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ReloadFailed, "first")), isStateSelected: false);
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.ShownElementGone)), isStateSelected: false);

        policy.TryTakePendingReload().ShouldBeTrue();
        policy.TryTakePendingReload().ShouldBeFalse();
    }

    [Fact]
    public void NoRebuildWithoutAFinishedReload()
    {
        PreviewReloadPolicy policy = new PreviewReloadPolicy();
        policy.OnReloadFinished(Result(Issue(HotReloadIssueKind.MissingFile)), isStateSelected: false, isShownElementMissing: true);
        policy.TryTakePendingReload().ShouldBeTrue();

        policy.TryTakePendingReload().ShouldBeFalse();
        policy.TryTakePendingReload().ShouldBeFalse();
    }
}
