using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Shapes.Tests;

public class ArcRuntimeTests
{
    [Fact]
    public void Alpha_ShouldAssign_ThroughSetProperty()
    {
        ArcRuntime sut = new ArcRuntime();
        sut.SetProperty("Alpha", 128);
        sut.Alpha.ShouldBe(128);
    }
}
