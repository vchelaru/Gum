using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class NineSliceRuntimeTests
{
    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue()
    {
        NineSliceRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        NineSliceRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }
}
