using Gum.Diagnostics;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class UiFreezeWatchdogHookRegistryTests
{
    [Fact]
    public void SuspendScope_WithNoRegistration_DoesNotThrow()
    {
        UiFreezeWatchdogHookRegistry registry = new UiFreezeWatchdogHookRegistry();

        using var scope = registry.SuspendScope();
    }

    [Fact]
    public void SuspendScope_InvokesRegisteredSuspend()
    {
        UiFreezeWatchdogHookRegistry registry = new UiFreezeWatchdogHookRegistry();
        bool suspended = false;
        registry.Register(suspend: () => suspended = true, resume: () => { });

        using var scope = registry.SuspendScope();

        suspended.ShouldBeTrue();
    }

    [Fact]
    public void DisposingSuspendScope_InvokesRegisteredResume()
    {
        UiFreezeWatchdogHookRegistry registry = new UiFreezeWatchdogHookRegistry();
        bool resumed = false;
        registry.Register(suspend: () => { }, resume: () => resumed = true);

        var scope = registry.SuspendScope();
        resumed.ShouldBeFalse();

        scope.Dispose();

        resumed.ShouldBeTrue();
    }

    [Fact]
    public void DisposingSuspendScope_TwiceInvokesResumeOnlyOnce()
    {
        UiFreezeWatchdogHookRegistry registry = new UiFreezeWatchdogHookRegistry();
        int resumeCount = 0;
        registry.Register(suspend: () => { }, resume: () => resumeCount++);

        var scope = registry.SuspendScope();
        scope.Dispose();
        scope.Dispose();

        resumeCount.ShouldBe(1);
    }
}
