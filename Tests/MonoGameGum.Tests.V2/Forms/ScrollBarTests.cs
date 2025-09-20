using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ScrollBarTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        ScrollBar scrollBar = new ();
        scrollBar.Visual.ShouldNotBeNull();
        (scrollBar.Visual is ScrollBarVisual).ShouldBeTrue();
    }

    [Fact]
    public void ScrollBar_ThumbWidth_ShouldMatchScrollBarWidth()
    {
        ScrollBar scrollBar = new();
        ScrollBarVisual scrollBarVisual = (ScrollBarVisual)scrollBar.Visual;

        scrollBarVisual.ThumbInstance.GetAbsoluteWidth().ShouldBe(
            scrollBarVisual.GetAbsoluteWidth());
    }

}
