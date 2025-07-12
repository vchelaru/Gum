using Gum.Wireframe;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class WindowTests : BaseTestClass
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

    [Fact]
    public void Dragging_ShouldMoveWindow()
    {
        Mock<ICursor> cursor = new ();
        Window sut = new();
        FrameworkElement.MainCursor = cursor.Object;


        var titleBar = (InteractiveGue)sut.GetVisual("TitleBarInstance")!;
        titleBar.TryCallPush();
        titleBar.TryCallDragging();

        sut.X.ShouldBe(0);
        sut.Y.ShouldBe(0);

        cursor.SetupGet(c => c.X).Returns(10);
        cursor.SetupGet(c => c.Y).Returns(20);

        titleBar.TryCallDragging();

        sut.X.ShouldBe(10);
        sut.Y.ShouldBe(20);

    }
}
