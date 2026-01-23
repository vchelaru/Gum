using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class InteractiveGueTests : BaseTestClass
{
    public InteractiveGueTests() : base()
    {

    }

    #region CurrentInputReceiver
    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfRootIsReset()
    {
        TextBox textBox = new ();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromRoot()
    {
        TextBox textBox = new ();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldNotGetUnset_IfSiblingIsRemovedFromRoot()
    {
        TextBox first = new ();
        first.AddToRoot();

        TextBox second = new();
        second.AddToRoot();
        second.IsFocused = true;

        GumService.Default.Root.Children.Remove(first.Visual);

        second.IsFocused.ShouldBeTrue();

    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfChild_IfRootIsReset()
    {
        Panel panel = new();
        panel.AddToRoot();

        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }


    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromPopup()
    {
        TextBox textBox = new();
        GumService.Default.PopupRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.PopupRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromModal()
    {
        TextBox textBox = new();
        GumService.Default.ModalRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.ModalRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }
    // todo - repeat the tests above for ModalRoot and PopupRoot

    #endregion

    [Fact]
    public void AddNextClickAction_ShouldNotBeRaised_OnSameFrameAdded()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);

        var button = new Button();
        button.AddToRoot();
        bool didClickRun = false;
        button.Click += (_, _) =>
        {
            didClickRun = true;
            InteractiveGue.AddNextClickAction(() =>
            {
                var message = "This should not be run since it is the same frame it was added";
                throw new Exception(message);
            });
        };

        cursor.Setup(x => x.WindowPushed).Returns(button.Visual);

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didClickRun.ShouldBe(true);
    }

    [Fact]
    public void AddNextPushAction_ShouldRegisterWindowOver_ForFrame()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.PrimaryPush).Returns(true);

        Button button = new ();
        button.AddToRoot();

        bool didRunPush = false;

        button.Push += (_, _) =>
        {
            didRunPush = true;
            InteractiveGue.AddNextPushAction(HandleNextPush);
        };

        void HandleNextPush()
        {
            if(cursor.Object.VisualOver != button.Visual)
            {
                throw new Exception("VisualOver was not set correctly");
            }
        }

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didRunPush.ShouldBe(true);

        cursor.Object.VisualOver = null;
        cursor.Object.WindowPushed = null;

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }

    [Fact]
    public void MouseWheelScroll_ShouldRaiseOnChildrenBeforeParents()
    {
        bool didChildRaise = false;
        bool didParentRaise = false;

        Panel parent = new ();
        parent.AddToRoot();
        parent.Width = 10;
        parent.Height = 10;
        parent.Visual.MouseWheelScroll += (_, _) =>
        {
            didParentRaise = true;
            if(!didChildRaise)
            {
                throw new Exception("Parent raised before child, should not be");
            }
        };

        Panel child = new ();
        parent.AddChild(child);
        child.Width = 10;
        child.Height = 10;
        child.Visual.MouseWheelScroll += (_, _) =>
        {
            didChildRaise = true;
            if(didParentRaise)
            {
                throw new Exception("Child raised after parent, should not be");
            }
        };

        Mock<ICursor> cursor = new();
        cursor
            .Setup(x => x.ScrollWheelChange).Returns(1);
        FormsUtilities.SetCursor(cursor.Object);


        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didChildRaise.ShouldBeTrue();
        didParentRaise.ShouldBeTrue();

    }

    [Fact]
    public void HasEvents_SetToTrue_ShouldAssignCursorVisualOver()
    {
        TextRuntime visual = new ();
        visual.Text = "Hello";
        visual.HasEvents = false;
        visual.AddToRoot();

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        ICursor cursor = FrameworkElement.MainCursor;
        cursor.ShouldNotBeNull();
        cursor.VisualOver.ShouldBe(null);

        visual.HasEvents = true;
        visual.Click += (_,_) => { };
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        cursor.VisualOver.ShouldBe(visual);
    }
}
