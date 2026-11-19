using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The tree's error icons for a loaded project are checked after the tree shows, a few elements per
/// UI-thread job, so the tree can be scrolled and clicked while the icons fill in.
/// </summary>
public class DeferredProjectErrorCheckTests
{
    private readonly Queue<Action> _posted = new Queue<Action>();
    private readonly List<ElementSave> _checked = new List<ElementSave>();
    private readonly ManualTimeProvider _time = new ManualTimeProvider();
    private readonly Mock<IProjectState> _projectState = new Mock<IProjectState>();
    private readonly DeferredProjectErrorCheck _sut;
    private TimeSpan _costPerCheck = TimeSpan.Zero;

    public DeferredProjectErrorCheckTests()
    {
        Mock<IErrorChecker> errorChecker = new Mock<IErrorChecker>();
        errorChecker.Setup(checker => checker.GetErrorsFor(It.IsAny<ElementSave?>(), It.IsAny<GumProjectSave>()))
            .Callback<ElementSave?, GumProjectSave>((element, _) =>
            {
                _checked.Add(element!);
                _time.Advance(_costPerCheck);
            })
            .Returns(Array.Empty<ErrorViewModel>());
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(item => item.Post(It.IsAny<Action>())).Callback<Action>(_posted.Enqueue);
        _sut = new DeferredProjectErrorCheck(errorChecker.Object, _projectState.Object, dispatcher.Object, _time);
    }

    [Fact]
    public void Start_ChecksNothingUntilThePostedWorkRuns_ThenEveryElementOnce()
    {
        GumProjectSave project = Project("Menu", "Button", "Label");
        _projectState.Setup(state => state.GumProjectSave).Returns(project);

        _sut.Start();
        _checked.ShouldBeEmpty();
        RunPosted();

        _checked.ShouldBe(project.Screens.Cast<ElementSave>().Concat(project.Components).Concat(project.StandardElements));
    }

    [Fact]
    public void Start_ChecksInSlices_SoInputRunsBetweenThem()
    {
        GumProjectSave project = Project("Menu", "Button", "Label", "Slider", "Toggle");
        _projectState.Setup(state => state.GumProjectSave).Returns(project);
        _costPerCheck = DeferredProjectErrorCheck.SliceLength / 2;

        _sut.Start();
        _posted.Dequeue()();

        _checked.Count.ShouldBe(2);
        _posted.Count.ShouldBe(1);
        RunPosted();
        _checked.Count.ShouldBe(project.AllElements.Count());
    }

    [Fact]
    public void Start_StopsWhenAnotherProjectIsLoadedBeforeItFinishes()
    {
        GumProjectSave first = Project("Menu", "Button", "Label");
        _projectState.Setup(state => state.GumProjectSave).Returns(first);
        _costPerCheck = DeferredProjectErrorCheck.SliceLength;
        _sut.Start();
        _posted.Dequeue()();
        int checkedOfFirst = _checked.Count;

        _projectState.Setup(state => state.GumProjectSave).Returns(Project("Other"));
        RunPosted();

        _checked.Count.ShouldBe(checkedOfFirst);
    }

    [Fact]
    public void Start_ReplacesARunThatHasNotFinished()
    {
        GumProjectSave project = Project("Menu", "Button", "Label");
        _projectState.Setup(state => state.GumProjectSave).Returns(project);
        _costPerCheck = DeferredProjectErrorCheck.SliceLength;
        _sut.Start();
        _posted.Dequeue()();

        _checked.Clear();
        _sut.Start();
        RunPosted();

        _checked.ShouldBe(project.AllElements, ignoreOrder: true);
        _checked.Distinct().Count().ShouldBe(_checked.Count);
    }

    [Fact]
    public void Start_SkipsAnElementRemovedBeforeItsTurn()
    {
        GumProjectSave project = Project("Menu", "Button", "Label");
        _projectState.Setup(state => state.GumProjectSave).Returns(project);
        ComponentSave label = project.Components.Single(component => component.Name == "Label");

        _sut.Start();
        project.Components.Remove(label);
        RunPosted();

        _checked.ShouldNotContain(label);
    }

    private void RunPosted()
    {
        while (_posted.Count > 0)
        {
            _posted.Dequeue()();
        }
    }

    private static GumProjectSave Project(string screenName, params string[] componentNames)
    {
        GumProjectSave project = new GumProjectSave();
        project.Screens.Add(new ScreenSave { Name = screenName });
        foreach (string name in componentNames)
        {
            project.Components.Add(new ComponentSave { Name = name });
        }
        project.StandardElements.Add(new StandardElementSave { Name = "Text" });
        return project;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public void Advance(TimeSpan time) => _ticks += time.Ticks;

        public override long GetTimestamp() => _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    }
}
