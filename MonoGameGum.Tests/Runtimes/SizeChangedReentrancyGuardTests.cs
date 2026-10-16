using Gum.GueDeriving;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// UpdateLayout raises SizeChanged behind a reentrancy guard. A handler that throws must not leave the
// guard set, or SizeChanged never fires again for that element.
public class SizeChangedReentrancyGuardTests : BaseTestClass
{
    [Fact]
    public void SizeChanged_ShouldStillRaise_AfterAHandlerThrew()
    {
        ContainerRuntime container = new();
        EventHandler throwingHandler = (_, _) => throw new InvalidOperationException("handler failed");
        container.SizeChanged += throwingHandler;

        Should.Throw<InvalidOperationException>(() => container.Width = 123);
        container.SizeChanged -= throwingHandler;
        int raisedCount = 0;
        container.SizeChanged += (_, _) => raisedCount++;
        container.Width = 456;

        raisedCount.ShouldBe(1);
    }
}
