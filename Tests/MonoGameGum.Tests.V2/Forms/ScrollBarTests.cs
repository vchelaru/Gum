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
        var scrollBar = new MonoGameGum.Forms.Controls.ScrollBar();
        scrollBar.Visual.ShouldNotBeNull();
        (scrollBar.Visual is MonoGameGum.Forms.DefaultVisuals.ScrollBarVisual).ShouldBeTrue();
    }
}
