using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using Gum.Input;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class InteractiveGueTests : BaseTestClass
{
    Mock<ICursor> _cursor;
    public InteractiveGueTests()
    {
        _cursor = new Mock<ICursor>();
        _cursor.SetupProperty(x => x.VisualOver);
        _cursor.SetupProperty(x => x.WindowPushed);
        FormsUtilities.SetCursor(_cursor.Object);
    }


    [Fact]
    public void AddNextClickAction_ShouldNotBeRaised_OnSameFrameAdded()
    {
        _cursor.Setup(x => x.PrimaryClick).Returns(true);

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

        _cursor.Setup(x => x.WindowPushed).Returns(button.Visual);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didClickRun.ShouldBe(true);
    }

    [Fact]
    public void AddNextPushAction_ShouldRegisterVisualOver_ForFrame()
    {
        _cursor.Setup(x => x.PrimaryClick).Returns(true);
        _cursor.Setup(x => x.PrimaryPush).Returns(true);

        Button button = new();
        button.AddToRoot();

        bool didRunPush = false;

        button.Push += (_, _) =>
        {
            didRunPush = true;
            InteractiveGue.AddNextPushAction(HandleNextPush);
        };

        void HandleNextPush()
        {
            if (_cursor.Object.VisualOver != button.Visual)
            {
                throw new Exception("VisualOver was not set correctly");
            }
        }

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didRunPush.ShouldBe(true);

        _cursor.Object.VisualOver = null;
        _cursor.Object.WindowPushed = null;

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }

    // Game time must advance between frames: TextBox ignores a push on the same game time it was focused.
    // The clock keeps counting across tests so a frame never lands on the time a previous test left behind.
    private static double _seconds = 100;

    private static void UpdateTwoFrames()
    {
        for (int i = 0; i < 2; i++)
        {
            _seconds += 1;
            Gum.GumService.Default.Update(
                new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(_seconds), TimeSpan.FromSeconds(1)));
        }
    }

    [Fact]
    public void AddNextPushAndClickAction_ShouldRunOnlyWhileOwnerIsInRoot()
    {
        _cursor.Setup(x => x.PrimaryPush).Returns(true);
        _cursor.Setup(x => x.PrimaryClick).Returns(true);

        Button inRoot = new();
        inRoot.AddToRoot();
        Button removed = new();
        removed.AddToRoot();
        Gum.GumService.Default.Root.Children!.Remove(removed.Visual);

        int pushCount = 0;
        int clickCount = 0;
        InteractiveGue.AddNextPushAction(() => pushCount++, inRoot.Visual);
        InteractiveGue.AddNextClickAction(() => clickCount++, inRoot.Visual);
        InteractiveGue.AddNextPushAction(() => pushCount += 100, removed.Visual);
        InteractiveGue.AddNextClickAction(() => clickCount += 100, removed.Visual);
        UpdateTwoFrames();

        pushCount.ShouldBe(1);
        clickCount.ShouldBe(1);
    }

    [Fact]
    public void NextPushAction_ShouldNotRun_IfOwningControlWasRemovedFromRoot()
    {
        _cursor.Setup(x => x.PrimaryPush).Returns(true);

        TextBox box = new();
        box.AddToRoot();
        box.IsFocused = true;
        Gum.GumService.Default.Root.Children!.Clear();
        InteractiveGue.CurrentInputReceiver = null;
        // a host that tears down its scene can leave the removed box still flagged as focused
        box.IsFocused = true;
        InteractiveGue.CurrentInputReceiver = null;

        int enabledApplyCount = 0;
        Gum.Forms.DefaultVisuals.V3.TextBoxVisual boxVisual = (Gum.Forms.DefaultVisuals.V3.TextBoxVisual)box.Visual;
        Action enabledApply = boxVisual.States.Enabled.Apply!;
        boxVisual.States.Enabled.Apply = () =>
        {
            enabledApplyCount++;
            enabledApply();
        };

        Button other = new();
        other.AddToRoot();
        UpdateTwoFrames();

        enabledApplyCount.ShouldBe(0);
    }

    [Fact]
    public void NextPushAction_ShouldNotThrow_IfOpenMenuWasRemovedFromRoot()
    {
        _cursor.Setup(x => x.PrimaryPush).Returns(true);

        Menu menu = new();
        menu.AddToRoot();
        MenuItem file = new();
        file.Header = "File";
        file.Items!.Add("New");
        menu.Items!.Add(file);
        file.IsSelected = true;
        Gum.GumService.Default.Root.Children!.Clear();

        Button other = new();
        other.AddToRoot();

        Should.NotThrow(() => UpdateTwoFrames());
    }

    [Fact]
    public void NextPushAction_ShouldStillRun_IfOwningControlIsInRoot()
    {
        _cursor.Setup(x => x.PrimaryPush).Returns(true);

        TextBox box = new();
        box.AddToRoot();
        box.Visual.Y = 300;
        box.IsFocused = true;
        box.IsFocused.ShouldBeTrue();

        // the mock cursor sits at 0,0 so this push lands on 'other', not on the box
        Button other = new();
        other.AddToRoot();
        UpdateTwoFrames();

        box.IsFocused.ShouldBeFalse();
    }

    #region CurrentInputReceiver
    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfRootIsReset()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        Gum.GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromRoot()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        Gum.GumService.Default.Root.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldNotGetUnset_IfSiblingIsRemovedFromRoot()
    {
        TextBox first = new();
        first.AddToRoot();

        TextBox second = new();
        second.AddToRoot();
        second.IsFocused = true;

        Gum.GumService.Default.Root.Children.Remove(first.Visual);

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

        Gum.GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }


    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromPopup()
    {
        TextBox textBox = new();
        Gum.GumService.Default.PopupRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        Gum.GumService.Default.PopupRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromModal()
    {
        TextBox textBox = new();
        Gum.GumService.Default.ModalRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        Gum.GumService.Default.ModalRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }
    // todo - repeat the tests above for ModalRoot and PopupRoot

    #endregion

    #region LosePush

    [Fact]
    public void LosePush_ShouldNotRaise_IfCursorWasNeverPushed()
    {
        InteractiveGue gue = new ContainerRuntime();
        gue.AddToRoot();
        gue.HasEvents = true;
        gue.LosePush += (_, _) =>
        {
            throw new Exception("LosePush should not be raised if the cursor was never pushed");
        };

        SetCursor(1, 1);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        var isEither =
            _cursor.Object.VisualOver == gue;
        isEither.ShouldBeTrue();

        SetCursor(1000, 1);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        _cursor.Object.VisualOver.ShouldBeNull();
    }


    [Fact]
    public void LosePush_ShouldRaise_OnPushAndClick()
    {
        InteractiveGue gue = new ContainerRuntime();
        gue.AddToRoot();
        gue.HasEvents = true;
        bool wasCalled = false;
        gue.LosePush += (_, _) =>
        {
            wasCalled = true;
        };

        SetCursor(1, 1);
        _cursor.Setup(x => x.PrimaryPush).Returns(true);
        _cursor.Setup(x => x.PrimaryDown).Returns(true);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        var isEither =
            _cursor.Object.VisualOver == gue;

        isEither.ShouldBeTrue();

        _cursor.Setup(x => x.PrimaryClick).Returns(true);
        _cursor.Setup(x => x.PrimaryPush).Returns(false);
        _cursor.Setup(x => x.PrimaryDown).Returns(false);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void LosePush_ShouldRaise_OnPushAndMoveOff()
    {
        InteractiveGue gue = new ContainerRuntime();
        gue.AddToRoot();
        gue.HasEvents = true;

        bool wasCalled = false;
        gue.LosePush += (_, _) =>
        {
            wasCalled = true;
        };

        SetCursor(1, 1);
        _cursor.Setup(x => x.PrimaryPush).Returns(true);
        _cursor.Setup(x => x.PrimaryDown).Returns(true);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        var isEither =
            _cursor.Object.VisualOver == gue;

        isEither.ShouldBeTrue();

        _cursor.Setup(x => x.PrimaryPush).Returns(false);
        SetCursor(1000, 1);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        wasCalled.ShouldBeTrue();
    }

    #endregion


    [Fact]
    public void MouseWheelScroll_ShouldRaiseOnChildrenBeforeParents()
    {
        bool didChildRaise = false;
        bool didParentRaise = false;

        Panel parent = new();
        parent.AddToRoot();
        parent.Width = 10;
        parent.Height = 10;
        parent.Visual.MouseWheelScroll += (_, _) =>
        {
            didParentRaise = true;
            if (!didChildRaise)
            {
                throw new Exception("Parent raised before child, should not be");
            }
        };

        Panel child = new();
        parent.AddChild(child);
        child.Width = 10;
        child.Height = 10;
        child.Visual.MouseWheelScroll += (_, _) =>
        {
            didChildRaise = true;
            if (didParentRaise)
            {
                throw new Exception("Child raised after parent, should not be");
            }
        };

        _cursor
            .Setup(x => x.ScrollWheelChange).Returns(1);


        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        didChildRaise.ShouldBeTrue();
        didParentRaise.ShouldBeTrue();

    }

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        InteractiveGue sut = new();
        sut.HasEvents.ShouldBeFalse();
    }

    [Fact]
    public void HasEvents_SetToTrue_ShouldAssignCursorVisualOver()
    {
        TextRuntime visual = new ();
        visual.Text = "Hello";
        visual.HasEvents = false;
        visual.AddToRoot();

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        ICursor cursor = FrameworkElement.MainCursor;
        cursor.ShouldNotBeNull();
        cursor.VisualOver.ShouldBe(null);

        visual.HasEvents = true;
        visual.Click += (_,_) => { };
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        cursor.VisualOver.ShouldBe(visual);
    }

    [Fact]
    public void RollOn_ShouldRaise_OnMoveOn()
    {
        InteractiveGue gue = new ContainerRuntime();
        gue.AddToRoot();
        gue.HasEvents = true;

        bool wasCalled = false;
        gue.RollOn += (_, _) =>
        {
            wasCalled = true;
        };

        SetCursor(1000, 1);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        var isEither =
            _cursor.Object.VisualOver == gue;
        isEither.ShouldBeFalse();

        SetCursor(1, 1);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
  
        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void RollOff_ShouldRaise_OnMoveOff()
    {
        InteractiveGue gue = new ContainerRuntime();
        gue.AddToRoot();
        gue.HasEvents = true;

        bool wasCalled = false;
        gue.RollOff += (_, _) =>
        {
            wasCalled = true;
        };

        SetCursor(1, 1);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        var isEither =
            _cursor.Object.VisualOver == gue;
        isEither.ShouldBeTrue();

        _cursor.Setup(x => x.PrimaryPush).Returns(false);

        SetCursor(1000, 1);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void Dragging_ShouldBeRaised_WhilePushedAndCursorIsStationary()
    {
        Button button = new();
        button.AddToRoot();
        _cursor.Setup(x => x.PrimaryDown).Returns(true);
        _cursor.Setup(x => x.XChange).Returns(0);
        _cursor.Setup(x => x.YChange).Returns(0);
        _cursor.Object.WindowPushed = button.Visual;

        int draggingCount = 0;
        button.Visual.Dragging += (_, _) => draggingCount++;

        UpdateTwoFrames();

        draggingCount.ShouldBe(2);
    }

    [Fact]
    public void Dragging_ShouldNotBeRaised_WhenPrimaryButtonIsNotDown()
    {
        Button button = new();
        button.AddToRoot();
        _cursor.Setup(x => x.PrimaryDown).Returns(false);
        _cursor.Setup(x => x.XChange).Returns(5);
        _cursor.Object.WindowPushed = button.Visual;

        int draggingCount = 0;
        button.Visual.Dragging += (_, _) => draggingCount++;

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        draggingCount.ShouldBe(0);
    }

    #region Utilities

    void SetCursor(float x, float y)
    {
        _cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(x);
        _cursor.Setup(x => x.X).Returns((int)x);
        _cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(y);
        _cursor.Setup(x => x.Y).Returns((int)y);

    }

    #endregion
}
