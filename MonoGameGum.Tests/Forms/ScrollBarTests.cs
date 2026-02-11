using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ScrollBarTests
{
    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollBar sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ThumbContainer_HasEvents_ShouldBeFalse()
    {
        ScrollBar scrollBar = new();
        InteractiveGue thumbContainer = (InteractiveGue)scrollBar.Visual.Children.First(c => c.Name == "ThumbContainer");
        thumbContainer.HasEvents.ShouldBeFalse();

    }

    [Fact]
    public void ScrollBar_Orientation_ShouldDefaultToVertical()
    {
        ScrollBar scrollBar = new();
        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
    }
}
