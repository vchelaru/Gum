using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class WindowTests
{
    [Fact]
    public void Constructor_CreatesVisual()
    {
        var window = new Window();

        window.Visual.ShouldNotBeNull();

        window.InnerPanel.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateWindowWithEdgesAndTitleBar()
    {
        var window = new Window();


        window.GetFrameworkElement("BorderTopLeftInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderTopRightInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomLeftInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomRightInstance").ShouldNotBeNull();

        window.GetFrameworkElement("BorderTopInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderBottomInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderRightInstance").ShouldNotBeNull();
        window.GetFrameworkElement("BorderLeftInstance").ShouldNotBeNull();
    }
}
