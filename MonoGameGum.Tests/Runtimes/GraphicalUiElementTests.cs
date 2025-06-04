using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class GraphicalUiElementTests
{
    [Fact]
    public void FillListWithChildrenByType_ShouldFillRecursively()
    {
        ContainerRuntime sut = new();

        sut.Children.Add(new SpriteRuntime());
        sut.Children.Add(new TextRuntime());
        ContainerRuntime childContainer = new();
        childContainer.Children.Add(new SpriteRuntime());
        sut.Children.Add(childContainer);

        var list = sut.FillListWithChildrenByTypeRecursively<SpriteRuntime>();

        list.Count.ShouldBe(2);
        list[0].ShouldBeOfType<SpriteRuntime>();
        list[1].ShouldBeOfType<SpriteRuntime>();


    }
}
