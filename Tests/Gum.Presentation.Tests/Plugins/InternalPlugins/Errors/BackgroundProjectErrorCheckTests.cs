using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.Errors;
using Gum.ProjectServices;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Errors;

/// <summary>
/// The Errors tab's project check reads the project in UI-thread slices, checks the disk on a worker
/// thread, and applies its rows only while the project is unchanged; these pin that what it applies is
/// what the synchronous check reports for the project at that moment.
/// </summary>
public class BackgroundProjectErrorCheckTests : BaseTestClass
{
    private readonly Queue<Action> _posted = new Queue<Action>();
    private readonly List<(Func<IReadOnlyList<ErrorResult>> Work, TaskCompletionSource<IReadOnlyList<ErrorResult>> Done)> _offThread = new();
    private readonly ProjectChangeVersion _version = new ProjectChangeVersion();
    private readonly HeadlessErrorChecker _headless = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object);
    private readonly List<string[]> _applied = new List<string[]>();
    private GumProjectSave _project = new GumProjectSave();
    private readonly BackgroundProjectErrorCheck _sut;

    public BackgroundProjectErrorCheckTests()
    {
        _project.BehaviorReferences.Add(new BehaviorReference { Name = "IGone" });
        _project.Behaviors.Add(new BehaviorSave { Name = "IGone", IsSourceFileMissing = true });

        Mock<IErrorChecker> errorChecker = new Mock<IErrorChecker>();
        errorChecker.Setup(checker => checker.BeginProjectErrors(It.IsAny<GumProjectSave>()))
            .Returns<GumProjectSave>(project => _headless.BeginProjectErrors(project));
        errorChecker.Setup(checker => checker.ToViewModels(It.IsAny<IReadOnlyList<ErrorResult>>()))
            .Returns<IReadOnlyList<ErrorResult>>(rows => rows.Select(row => new ErrorViewModel { Message = row.Message }).ToArray());
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(item => item.Post(It.IsAny<Action>())).Callback<Action>(_posted.Enqueue);
        _sut = new BackgroundProjectErrorCheck(errorChecker.Object, _version, dispatcher.Object, () => _project,
            work =>
            {
                TaskCompletionSource<IReadOnlyList<ErrorResult>> done = new TaskCompletionSource<IReadOnlyList<ErrorResult>>();
                _offThread.Add((work, done));
                return done.Task;
            });
    }

    [Fact]
    public void Start_AppliesWhatTheSynchronousCheckReports_OnceTheWorkerIsDone()
    {
        string[] expected = _headless.GetProjectErrors(_project).Select(row => row.Message).ToArray();

        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        RunPosted();
        _applied.ShouldBeEmpty();
        FinishOffThreadWork();
        RunPosted();

        expected.ShouldHaveSingleItem().ShouldContain("IGone");
        _applied.ShouldHaveSingleItem().ShouldBe(expected);
    }

    [Fact]
    public void Start_DiscardsTheRowsAndChecksAgain_WhenTheProjectChangesBeforeThey()
    {
        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        RunPosted();

        // An edit between the walk and the apply.
        _project.BehaviorReferences.Clear();
        _version.MarkChanged();
        FinishOffThreadWork();
        RunPosted();
        FinishOffThreadWork();
        RunPosted();

        _sut.DiscardedCount.ShouldBe(1);
        _applied.ShouldHaveSingleItem().ShouldBe(_headless.GetProjectErrors(_project).Select(row => row.Message));
        _applied[0].ShouldBeEmpty();
    }

    [Fact]
    public void Start_ChecksTheNewProject_WhenAnotherIsLoadedWhileItReads()
    {
        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        _project = new GumProjectSave();
        RunPosted();
        FinishOffThreadWork();
        RunPosted();

        _applied.ShouldHaveSingleItem().ShouldBeEmpty();
    }

    [Fact]
    public void Start_ReplacesARunThatHasNotApplied()
    {
        int firstApplied = 0;
        _sut.Start(_ => firstApplied++);
        RunPosted();

        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        RunPosted();
        FinishOffThreadWork();
        RunPosted();

        firstApplied.ShouldBe(0);
        _applied.ShouldHaveSingleItem().ShouldNotBeEmpty();
    }

    // A disk error on the worker (a folder the case check may not list) goes to the tool log, not to the
    // UI thread's unhandled-exception hook, and the next check still applies.
    [Fact]
    public void Start_AppliesALaterCheck_AfterTheWorkerFailed()
    {
        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        RunPosted();
        _offThread.ShouldHaveSingleItem().Done.SetException(new UnauthorizedAccessException("Access to the path is denied."));
        _offThread.Clear();
        Should.NotThrow(RunPosted);
        _applied.ShouldBeEmpty();
        _sut.FailedCount.ShouldBe(1);

        _sut.Start(rows => _applied.Add(rows.Select(row => row.Message).ToArray()));
        RunPosted();
        FinishOffThreadWork();
        RunPosted();

        _applied.ShouldHaveSingleItem().ShouldNotBeEmpty();
    }

    private void RunPosted()
    {
        while (_posted.Count > 0)
        {
            _posted.Dequeue()();
        }
    }

    private void FinishOffThreadWork()
    {
        foreach ((Func<IReadOnlyList<ErrorResult>> work, TaskCompletionSource<IReadOnlyList<ErrorResult>> done) in _offThread.ToList())
        {
            _offThread.Remove((work, done));
            done.SetResult(Task.Run(work).GetAwaiter().GetResult());
        }
    }
}
