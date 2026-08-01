using System;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class RefreshCoalescerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private Action? _postedAction;
    private int _refreshCallCount;
    private readonly RefreshCoalescer _sut;

    public RefreshCoalescerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _dispatcherMock.Setup(d => d.Post(It.IsAny<Action>()))
            .Callback<Action>(action => _postedAction = action);

        _sut = new RefreshCoalescer(_dispatcherMock.Object, () => _refreshCallCount++);
    }

    [Fact]
    public void RequestRefresh_CalledOnce_PostsAndDoesNotRunRefreshSynchronously()
    {
        _sut.RequestRefresh();

        _dispatcherMock.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
        _refreshCallCount.ShouldBe(0);
    }

    [Fact]
    public void RequestRefresh_CalledRepeatedlyBeforePostedActionRuns_PostsOnlyOnce()
    {
        _sut.RequestRefresh();
        _sut.RequestRefresh();
        _sut.RequestRefresh();

        _dispatcherMock.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void RequestRefresh_WhenPostedActionRuns_InvokesRefreshExactlyOnce()
    {
        _sut.RequestRefresh();
        _sut.RequestRefresh();
        _sut.RequestRefresh();

        _postedAction!.Invoke();

        _refreshCallCount.ShouldBe(1);
    }

    [Fact]
    public void RequestRefresh_AfterPostedActionRuns_SchedulesAgain()
    {
        _sut.RequestRefresh();
        _postedAction!.Invoke();

        _sut.RequestRefresh();

        _dispatcherMock.Verify(d => d.Post(It.IsAny<Action>()), Times.Exactly(2));
    }
}
