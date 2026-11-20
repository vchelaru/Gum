using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins;

public class DeferredProjectLoadTests
{
    private readonly List<Action> _posted = new List<Action>();
    private readonly Mock<IProjectState> _projectState = new Mock<IProjectState>();
    private readonly DeferredProjectLoad _sut;

    public DeferredProjectLoadTests()
    {
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(d => d.Post(It.IsAny<Action>())).Callback<Action>(_posted.Add);
        _sut = new DeferredProjectLoad(dispatcher.Object, _projectState.Object);
    }

    [Fact]
    public void Post_RunsTheHandlerInALaterJob_WithTheLoadedProject()
    {
        GumProjectSave project = new GumProjectSave();
        _projectState.Setup(p => p.GumProjectSave).Returns(project);
        List<GumProjectSave> handled = new List<GumProjectSave>();

        _sut.Post(project, "Test", handled.Add);

        handled.ShouldBeEmpty();
        RunPosted();
        handled.ShouldBe(new[] { project });
    }

    [Fact]
    public void Post_DropsTheWaitingWork_WhenAnotherLoadPostsBeforeItRuns()
    {
        GumProjectSave first = new GumProjectSave();
        GumProjectSave second = new GumProjectSave();
        List<GumProjectSave> handled = new List<GumProjectSave>();

        _projectState.Setup(p => p.GumProjectSave).Returns(first);
        _sut.Post(first, "Test", handled.Add);
        _projectState.Setup(p => p.GumProjectSave).Returns(second);
        _sut.Post(second, "Test", handled.Add);
        RunPosted();

        handled.ShouldBe(new[] { second });
    }

    [Fact]
    public void Post_DropsTheWork_WhenTheProjectIsNoLongerLoadedWhenItRuns()
    {
        GumProjectSave project = new GumProjectSave();
        _projectState.Setup(p => p.GumProjectSave).Returns(project);
        bool ran = false;

        _sut.Post(project, "Test", _ => ran = true);
        // Closed, or replaced by a load that did not go through this instance.
        _projectState.Setup(p => p.GumProjectSave).Returns((GumProjectSave?)null);
        RunPosted();

        ran.ShouldBeFalse();
    }

    private void RunPosted()
    {
        List<Action> posted = new List<Action>(_posted);
        _posted.Clear();
        foreach (Action action in posted)
        {
            action();
        }
    }
}
