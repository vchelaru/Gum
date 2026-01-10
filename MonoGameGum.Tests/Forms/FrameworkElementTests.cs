using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
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
    #region Loaded

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

    #endregion

    [Fact]
    public void AddToRoot_ShouldAddToRootCorrectly()
    {
        Button child = new();
        child.AddToRoot();

        child.Visual.Parent.ShouldBe(GumService.Default.Root);
        GumService.Default.Root.Children.ShouldContain(child.Visual);
    }

    [Fact]
    public void CursorOver_ShouldBeThis_IfHasEvents()
    {
        var frameworkElement = new FrameworkElement(new ContainerRuntime());
        // so that it has managers which is needed for proper hit detection:
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
            null!,
            0);

        cursor.VerifySet(c => c.WindowOver = frameworkElement.Visual);
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

    // CustomCursor cannot be properly tested because it requires a concrete Cursor class.

    #region HandleTab

    [Fact]
    public void HandleTab_ShouldSelectNextItem_InSameContainer()
    {
        StackPanel stackPanel = new();
        stackPanel.AddToRoot();

        Button button1 = new();
        stackPanel.AddChild(button1);
        Button button2 = new();
        stackPanel.AddChild(button2);

        button1.IsFocused = true;
        button1.HandleTab();
        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSelectNextItem_InDifferentSameContainers()
    {
        StackPanel stackPanel1 = new();
        stackPanel1.AddToRoot();

        StackPanel stackPanel2 = new();
        stackPanel2.AddToRoot();

        Button button1 = new();
        stackPanel1.AddChild(button1);
        Button button2 = new();
        stackPanel2.AddChild(button2);

        button1.IsFocused = true;
        button1.HandleTab();
        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();
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


    [Fact]
    public void HandleTab_ShouldNotUnfocus_OnTabOfOnlyElement()
    {
        Button playButton = new ();
        playButton.AddToRoot();

        playButton.IsFocused = true;

        playButton.HandleTab(loop: true);

        playButton.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldNotUnfocus_OnTabOfOnlyElementInStack()
    {
        StackPanel mainPanel = new ();
        mainPanel.AddToRoot();

        Button playButton = new ();
        mainPanel.AddChild(playButton);

        playButton.IsFocused = true;

        playButton.HandleTab(loop: true);

        playButton.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region OnFocusUpdate

    [Fact]
    public void OnFocusUpdate_ShouldFocusNextItem_IfTabIsPressed()
    {
        // Many controls support focus, but they all implement their own logic so
        // we have to use a specific control and not just FrameworkElement.
        TextBox textBox1 = new ();
        textBox1.AddToRoot();

        TextBox textBox2 = new ();
        textBox2.AddToRoot();

        textBox1.IsFocused = true;

        var keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Tab))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        textBox1.OnFocusUpdate();

        textBox1.IsFocused.ShouldBeFalse();
        textBox2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OnFocusUpdate_ShouldFocusNextItem_IfGamepadDownIsPressed()
    {
        // Many controls support focus, but they all implement their own logic so
        // we have to use a specific control and not just FrameworkElement.
        TextBox textBox1 = new();
        textBox1.AddToRoot();

        TextBox textBox2 = new();
        textBox2.AddToRoot();
        textBox1.IsFocused = true;

        GamePad gamepad = new GamePad();
        gamepad.Activity(new Microsoft.Xna.Framework.Input.GamePadState(), 0);

        var newGamepadState = new Microsoft.Xna.Framework.Input.GamePadState(
            new Microsoft.Xna.Framework.Input.GamePadThumbSticks(),
            new Microsoft.Xna.Framework.Input.GamePadTriggers(),
            new Microsoft.Xna.Framework.Input.GamePadButtons(),
            new Microsoft.Xna.Framework.Input.GamePadDPad(
                Microsoft.Xna.Framework.Input.ButtonState.Released,
                // down state is pressed:
                Microsoft.Xna.Framework.Input.ButtonState.Pressed,
                Microsoft.Xna.Framework.Input.ButtonState.Released,
                Microsoft.Xna.Framework.Input.ButtonState.Released));
        gamepad.Activity(
            newGamepadState, 
            .0667);

        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        textBox1.OnFocusUpdate();

        textBox1.IsFocused.ShouldBeFalse();
        textBox2.IsFocused.ShouldBeTrue();

    }

    #endregion

    [Fact]
    public void PositionValues_ShouldApplyToVisual()
    {
        Button button = new ();

        button.X = 12;
        button.Y = 14;

        button.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        button.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        button.Visual.X.ShouldBe(12);
        button.Visual.Y.ShouldBe(14);
        button.Visual.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        button.Visual.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
    }


    [Fact]
    public void RemoveFromRoot_ShouldRemoveFromRootCorrectly()
    {
        Button child = new();
        child.AddToRoot();
        child.RemoveFromRoot();
        child.Visual.Parent.ShouldBeNull();
        GumService.Default.Root.Children.ShouldNotContain(child.Visual);
    }

    [Fact]
    public void SizeValues_ShouldApplyToVisual()
    {
        Button button = new();

        button.Width = 123;
        button.Height = 456;

        button.MinWidth = 12;
        button.MaxWidth = 2345;

        button.MinHeight = 14;
        button.MaxHeight = 3456;

        button.WidthUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        button.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        button.Visual.Width.ShouldBe(123);
        button.Visual.Height.ShouldBe(456);
        button.Visual.MinWidth.ShouldBe(12);
        button.Visual.MaxWidth.ShouldBe(2345);
        button.Visual.MinHeight.ShouldBe(14);
        button.Visual.MaxHeight.ShouldBe(3456);
        button.Visual.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.Ratio);
        button.Visual.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToParent);
    }
}
