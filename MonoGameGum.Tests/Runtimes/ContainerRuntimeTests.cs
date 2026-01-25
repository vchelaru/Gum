using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class ContainerRuntimeTests
{
    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue()
    {
        ContainerRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldDefaultToTrue()
    {
        ContainerRuntime sut = new();
        sut.HasEvents.ShouldBeTrue();
    }
}
