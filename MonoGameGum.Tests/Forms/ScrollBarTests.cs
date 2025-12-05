using Gum.Forms.Controls;
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
    public void ScrollBar_Orientation_ShouldDefaultToVertical()
    {
        ScrollBar scrollBar = new();
        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
    }
}
