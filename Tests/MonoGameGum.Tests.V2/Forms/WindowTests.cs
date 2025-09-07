using Gum.Forms;
using Gum.Forms.DefaultVisuals;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class WindowTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var window = new Window();
        window.Visual.ShouldNotBeNull();
        (window.Visual is Gum.Forms.DefaultVisuals.WindowVisual).ShouldBeTrue();
    }

    [Fact]
    public void ResizeMode_ShouldChangeCursorOnEdges_IfCanResize()
    {
        Window window = new();
        window.ResizeMode = ResizeMode.CanResize;

        WindowVisual visual = (WindowVisual)window.Visual;
        visual.BorderTopLeftInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderTopInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderTopRightInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderRightInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderBottomRightInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderBottomInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderBottomLeftInstance.CustomCursor.ShouldNotBeNull();
        visual.BorderLeftInstance.CustomCursor.ShouldNotBeNull();
    }

    [Fact]
    public void ResizeMode_ShouldNotChangeCursor_IfNoResize()
    {
        Window window = new();
        window.ResizeMode = ResizeMode.NoResize;

        WindowVisual visual = (WindowVisual)window.Visual;
        visual.BorderTopLeftInstance.CustomCursor.ShouldBeNull();
        visual.BorderTopInstance.CustomCursor.ShouldBeNull();
        visual.BorderTopRightInstance.CustomCursor.ShouldBeNull();
        visual.BorderRightInstance.CustomCursor.ShouldBeNull();
        visual.BorderBottomRightInstance.CustomCursor.ShouldBeNull();
        visual.BorderBottomInstance.CustomCursor.ShouldBeNull();
        visual.BorderBottomLeftInstance.CustomCursor.ShouldBeNull();
        visual.BorderLeftInstance.CustomCursor.ShouldBeNull();
    }
}
