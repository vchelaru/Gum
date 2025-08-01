using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ScrollViewerTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var scrollViewer = new MonoGameGum.Forms.Controls.ScrollViewer();
        scrollViewer.Visual.ShouldNotBeNull();
        (scrollViewer.Visual is Gum.Forms.DefaultVisuals.ScrollViewerVisual).ShouldBeTrue();
    }
}
