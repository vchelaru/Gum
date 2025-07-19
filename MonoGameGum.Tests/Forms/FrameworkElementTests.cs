using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using Moq;
using NVorbis.Ogg;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class FrameworkElementTests : BaseTestClass
{
    [Fact]
    public void Loaded_ShouldBeCalled_WhenAddedToRoot()
    {
        Button button = new ();
        bool loadedCalled = false;
        button.Loaded += (_,_) => loadedCalled = true;
        button.AddToRoot();
        loadedCalled.ShouldBeTrue();
    }

    [Fact]
    public void Loaded_ShouldBeCalled_WhenParentIsAddedToRoot()
    {
        Button button = new();
        bool loadedCalled = false;
        button.Loaded += (_, _) => loadedCalled = true;
        Panel parent = new ();
        parent.AddChild(button);
        parent.AddToRoot();
        loadedCalled.ShouldBeTrue();
    }

    [Fact]
    public void Loaded_ShouldBeCalledMultipleTimes_IfAddedMultipleTimes()
    {
        Button button = new();
        int loadCallCount = 0;
        button.Loaded += (_, _) => loadCallCount++;
        Panel parent = new();
        parent.AddToRoot();
        parent.AddChild(button);

        button.Visual.Parent = null;
        parent.AddChild(button);

        loadCallCount.ShouldBe(2);

    }


    [Fact]
    public void EffectiveManagers_ShouldBeSet_IfAddedToRoot()
    {
        Button button = new();
        button.Visual.EffectiveManagers.ShouldBeNull();
        button.AddToRoot();
        button.Visual.EffectiveManagers.ShouldNotBeNull();
        button.Visual.Parent = null;
        button.Visual.EffectiveManagers.ShouldBeNull();
    }

    [Fact]
    public void CursorOver_ShouldBeThis_IfHasEvents()
    {
        var frameworkElement = new FrameworkElement(new ContainerRuntime());
        // so that it has managers:
        frameworkElement.Visual.AddToManagers();

        // So it registers a click:
        frameworkElement.Visual.Click += (_, _) => { };
        frameworkElement.Width = 100;
        frameworkElement.Height = 100;
        GraphicalUiElement.CanvasWidth = 100;
        GraphicalUiElement.CanvasHeight = 100;

        var cursor = new Mock<ICursor>();
        cursor.Setup(x => x.X).Returns(1);
        cursor.Setup(x => x.Y).Returns(1);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            frameworkElement.Visual,
            cursor.Object,
            null,
            0);

        cursor.VerifySet(c => c.WindowOver = frameworkElement.Visual);
    }

    [Fact]
    public void HandleTab_ShouldLoopBackToFirstItem()
    {
        var stack1 = new StackPanel();
        stack1.Name = "Stack1";
        stack1.AddToRoot();
        for (int i = 0; i < 2; i++)
        {
            var textBox = new TextBox();
            textBox.Name = "TextBox1:" + i;
            textBox.Width = 200;
            stack1.AddChild(textBox);
        }

        var stack2 = new StackPanel();
        stack2.Name = "Stack2";
        stack2.AddToRoot();
        stack2.Anchor(Anchor.TopRight);
        for (int i = 0; i < 2; i++)
        {
            var textBox = new TextBox();
            textBox.Name = "TextBox2:" + i;
            textBox.Width = 200;

            stack2.AddChild(textBox);
        }

        stack2.Children[1].IsFocused = true;
        stack2.Children[1].HandleTab(loop:true);

        stack2.Children[0].IsFocused.ShouldBeFalse();
        stack2.Children[1].IsFocused.ShouldBeFalse();
        stack1.Children[0].IsFocused.ShouldBeTrue();
    }
}
