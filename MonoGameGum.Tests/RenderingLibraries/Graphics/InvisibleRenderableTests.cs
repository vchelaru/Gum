using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;
public class InvisibleRenderable : BaseTestClass
{
    [Fact]
    public void Clone_ShouldCreateClonedInvisibleRenderable()
    {
        RenderingLibrary.Graphics.InvisibleRenderable sut = new();
        var clone = sut.Clone() as RenderingLibrary.Graphics.InvisibleRenderable;
        clone.ShouldNotBeNull();
    }
}
