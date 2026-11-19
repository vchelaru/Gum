using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// After a load the project's fonts are collected a slice at a time, and the project-wide passes queued
/// meanwhile run once every element is collected, so none of them collects in one long block.
/// </summary>
public class ProjectFontWarmUpTests : BaseTestClass
{
    private readonly Queue<Action> _posted = new Queue<Action>();
    private readonly ManualTimeProvider _time = new ManualTimeProvider();
    private readonly ProjectChangeVersion _version = new ProjectChangeVersion();
    private readonly ProjectFontReferences _references;
    private readonly ProjectFontWarmUp _sut;
    private readonly GumProjectSave _project = new GumProjectSave();

    public ProjectFontWarmUpTests()
    {
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(item => item.Post(It.IsAny<Action>())).Callback<Action>(_posted.Enqueue);
        _references = new ProjectFontReferences(_version);
        _sut = new ProjectFontWarmUp(_references, dispatcher.Object, _time);
        ObjectFinder.Self.GumProjectSave = _project;
    }

    [Fact]
    public void PostAfterWarmUp_RunsAPassOnceEveryElementIsCollected()
    {
        AddScreens("Menu", "Options");
        // One element per slice, so a pass that didn't wait would run between slices.
        _time.AdvancePerRead = ProjectFontWarmUp.SliceLength;
        int collectedWhenThePassRan = -1;

        _sut.Start(_project);
        _sut.PostAfterWarmUp(() => collectedWhenThePassRan = _references.For(_project).CollectionCount);
        RunPosted();

        collectedWhenThePassRan.ShouldBe(_project.AllElements.Count());
        _sut.CompletedCount.ShouldBe(1);
    }

    [Fact]
    public void PostAfterWarmUp_PostsAtOnce_WhenNothingIsCollecting()
    {
        bool ran = false;

        _sut.PostAfterWarmUp(() => ran = true);

        _posted.Count.ShouldBe(1);
        _posted.Dequeue()();
        ran.ShouldBeTrue();
    }

    [Fact]
    public void Start_CollectsInSlices_SoInputRunsBetweenThem()
    {
        AddScreens("Menu", "Options", "Credits");
        // Every timestamp read moves the clock, so a slice ends after its first element.
        _time.AdvancePerRead = ProjectFontWarmUp.SliceLength;

        _sut.Start(_project);
        _posted.Dequeue()();

        _references.For(_project).CollectionCount.ShouldBe(1);
        _posted.Count.ShouldBe(1);
    }

    // An edit discards what was collected; waiting for a collection that keeps restarting would hold
    // the passes back for as long as the user edits.
    [Fact]
    public void PostAfterWarmUp_RunsThePass_WhenAnEditDiscardsTheCollection()
    {
        AddScreens("Menu", "Options", "Credits");
        _time.AdvancePerRead = ProjectFontWarmUp.SliceLength;
        bool ran = false;
        _sut.Start(_project);
        _sut.PostAfterWarmUp(() => ran = true);
        _posted.Dequeue()();

        _version.MarkChanged();
        RunPosted();

        ran.ShouldBeTrue();
        _sut.IsRunning.ShouldBeFalse();
        _sut.CompletedCount.ShouldBe(0);
    }

    [Fact]
    public void Start_ReplacesAnUnfinishedRun_AndItsPassesWaitForTheNewOne()
    {
        AddScreens("Menu", "Options");
        _time.AdvancePerRead = ProjectFontWarmUp.SliceLength;
        int passes = 0;
        _sut.Start(_project);
        _sut.PostAfterWarmUp(() => passes++);
        _posted.Dequeue()();

        _sut.Start(_project);
        _sut.IsRunning.ShouldBeTrue();
        RunPosted();

        passes.ShouldBe(1);
        _sut.CompletedCount.ShouldBe(1);
    }

    [Fact]
    public void ThrowingSlice_StillReleasesWaitingPasses()
    {
        AddScreens("Menu");
        Mock<Gum.ProjectServices.IFontReferenceCacheSource> source = new Mock<Gum.ProjectServices.IFontReferenceCacheSource>();
        source.Setup(s => s.For(It.IsAny<GumProjectSave>())).Throws(new InvalidOperationException("boom"));
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(item => item.Post(It.IsAny<Action>())).Callback<Action>(_posted.Enqueue);
        ProjectFontWarmUp sut = new ProjectFontWarmUp(source.Object, dispatcher.Object, _time);
        bool ran = false;
        sut.Start(_project);
        sut.PostAfterWarmUp(() => ran = true);

        Should.Throw<InvalidOperationException>(() => _posted.Dequeue()());
        RunPosted();

        ran.ShouldBeTrue();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Start_StopsWhenAnotherProjectReplacesIt()
    {
        AddScreens("Menu", "Options", "Credits");
        _time.AdvancePerRead = ProjectFontWarmUp.SliceLength;
        bool ran = false;
        _sut.Start(_project);
        _sut.PostAfterWarmUp(() => ran = true);
        _posted.Dequeue()();
        int collected = _references.For(_project).CollectionCount;

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        RunPosted();

        _references.For(_project).CollectionCount.ShouldBe(collected);
        ran.ShouldBeTrue();
        _sut.IsRunning.ShouldBeFalse();
    }

    private void AddScreens(params string[] names)
    {
        foreach (string name in names)
        {
            ScreenSave screen = new ScreenSave { Name = name };
            StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
            screen.States.Add(state);
            _project.Screens.Add(screen);
        }
    }

    private void RunPosted()
    {
        while (_posted.Count > 0)
        {
            _posted.Dequeue()();
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public TimeSpan AdvancePerRead { get; set; }

        public override long GetTimestamp()
        {
            long now = _ticks;
            _ticks += AdvancePerRead.Ticks;
            return now;
        }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    }
}
