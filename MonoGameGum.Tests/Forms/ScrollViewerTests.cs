using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ScrollViewerTests : BaseTestClass
{
    [Fact]
    public void IsFocused_ShouldBeTrue_WhenReceivingTab()
    {
        StackPanel parent = new();

        Button button1 = new();
        parent.AddChild(button1);

        ScrollViewer scrollViewer = new();
        parent.AddChild(scrollViewer);

        Button button2 = new();
        parent.AddChild(button2);

        button1.IsFocused = true;
        button1.HandleTab();

        scrollViewer.IsFocused.ShouldBeTrue();
        (InteractiveGue.CurrentInputReceiver == scrollViewer).ShouldBeTrue();
    }

    [Fact]
    public void ReceiveTab_ShouldSkipInternalItems()
    {
        StackPanel parent = new();

        ScrollViewer scrollViewer = new();
        parent.AddChild(scrollViewer);

        Button button1 = new();
        scrollViewer.AddChild(button1);

        Button button2 = new();
        parent.AddChild(button2);

        scrollViewer.IsFocused = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(m=>m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Tab))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        scrollViewer.OnFocusUpdate();

        scrollViewer.IsFocused.ShouldBeFalse();
        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();

    }

    [Fact]
    public void DoItemsHaveFocus_Set_ShouldGiveFocusToFirstItem()
    {

        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);


        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;
        button1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void EnterInput_ShouldGiveFocusToFirstItem()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);


        scrollViewer.IsFocused = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        scrollViewer.OnFocusUpdate();

        button1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void EscInput_ShouldRemoveFocusFromItems()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);
        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new ();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape))
            .Returns(true);
        mockKeyboard
            .Setup(m => m.KeysTyped)
            .Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        Mock<ICursor> mockCursor = new ();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            scrollViewer.Visual, 
            mockCursor.Object, 
            mockKeyboard.Object, 
            0);

        scrollViewer.IsFocused.ShouldBeTrue();
        scrollViewer.DoItemsHaveFocus.ShouldBeFalse();
    }

    [Fact]
    public void TabInput_ShouldMoveFocusToNextItem()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);
        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;
        button1.IsFocused.ShouldBeTrue();


        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Tab))
            .Returns(true);
        mockKeyboard
            .Setup(m => m.KeysTyped)
            .Returns(new List<Keys>() { Keys.Tab });
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        Mock<ICursor> mockCursor = new();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            scrollViewer.Visual,
            mockCursor.Object,
            mockKeyboard.Object,
            0);


        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();

    }

    [Fact]
    public void DoItemsHaveFoucus_SetToFalse_ShouldRemoveFocusFromItems()
    {
        ScrollViewer scrollViewer = new();

        Button button1 = new();
        scrollViewer.AddChild(button1);

        Button button2 = new();
        scrollViewer.AddChild(button2);

        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;

        button1.IsFocused.ShouldBeTrue();
        scrollViewer.DoItemsHaveFocus = false;

        button1.IsFocused.ShouldBeFalse();
        scrollViewer.IsFocused.ShouldBeTrue();
    }
}
