using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.GueDeriving;
using MonoGameGum.Input;
using Gum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ButtonTests : BaseTestClass
{
    [Fact]
    public void Constructor_ShouldAssignVisual()
    {
        Button button = new();

        button.Visual.ShouldNotBeNull();

        button.Visual.ShouldBeOfType<Gum.Forms.DefaultVisuals.DefaultButtonRuntime>();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Button sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void DefaultButtonRuntimeConstructor_ShouldNotThrow_WhenActiveStyleNeverInitialized()
    {
        // Constructing a legacy default-visual class directly ("create a button through its
        // visual type", a documented pattern - see GumFormsSample's FrameworkElementExampleScreen)
        // used to NRE in Styling.ActiveStyle.Colors.Primary whenever the app only ever called
        // InitializeDefaults with V3 (or never called the V1/V2 overload) - nothing else sets the
        // V1/V2-shared Styling.ActiveStyle, so it stayed null.
        var previousActiveStyle = Gum.Forms.DefaultVisuals.Styling.ActiveStyle;
        try
        {
            Gum.Forms.DefaultVisuals.Styling.ActiveStyle = null;

            Should.NotThrow(() => new DefaultButtonRuntime());
        }
        finally
        {
            Gum.Forms.DefaultVisuals.Styling.ActiveStyle = previousActiveStyle;
        }
    }

    [Fact]
    public void UpdateState_ShouldSetPushed_IfPushedWithKeyboard()
    {
        bool wasApplied = false;
        Button button = new();
        button.IsFocused = true;

        var buttonState = button.GetState(FrameworkElement.PushedStateName);
        buttonState.Clear();
        buttonState.Apply = () =>
        {
            // just to make sure it gets called
            wasApplied = true;
        };

        var keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        keyboard
            .Setup(k => k.KeyDown(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        // KeyCombo.IsComboPushed now routes through the shared Gum.Forms.Input.Keys overload.
        keyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter))
            .Returns(true);
        keyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyDown(Gum.Forms.Input.Keys.Enter))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        button.OnFocusUpdate();
        button.UpdateState();

        wasApplied.ShouldBeTrue();
    }


    [Fact]
    public void UpdateState_ShouldNotSetPushed_IfKeyboardDownNoPush()
    {
        bool wasApplied = false;
        Button button = new();
        button.IsFocused = true;

        var buttonState = button.GetState(FrameworkElement.PushedStateName);
        buttonState.Clear();
        buttonState.Apply = () =>
        {
            // just to make sure it gets called
            wasApplied = true;
        };

        var keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(false);
        keyboard
            .Setup(k => k.KeyDown(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        button.OnFocusUpdate();
        button.UpdateState();

        wasApplied.ShouldBeFalse();
    }

    [Fact]
    public void OnFocusUpdate_ShouldClickOnEnter()
    {
        Button button = new ();
        var wasClicked = false;
        button.Click += (sender, args) => wasClicked = true;
        FrameworkElement.KeyboardsForUiControl.Clear();

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter))
            .Returns(true);


        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        button.OnFocusUpdate();

        wasClicked.ShouldBeTrue();
    }

    [Fact]
    public void OnFocusUpdate_ShouldNotClick_OnNonEnterKeyboardPresses()
    {

        Button button = new();
        var wasClicked = false;
        button.Click += (sender, args) => wasClicked = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);

        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Right))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        button.OnFocusUpdate();

        wasClicked.ShouldBeFalse();
    }

    [Fact]
    public void OnFocusUpdate_ShouldClick_CustomClickCombo()
    {
        Button button = new();
        var wasClicked = false;
        button.Click += (sender, args) => wasClicked = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Space))
            .Returns(true);

        FrameworkElement.ClickCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Space
        });

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        button.OnFocusUpdate();

        wasClicked.ShouldBeTrue();
    }

    [Fact]
    public void PushedState_ShouldDependOnKeyCombos()
    {
        FrameworkElement.ClickCombos = new List<KeyCombo>
        {
            new KeyCombo
            {
                PushedKey = Gum.Forms.Input.Keys.Space
            }
        };

        string lastStateSet = string.Empty;
        Button button = new();
        button.IsFocused = true;

        AssignLastSetInState(button.GetState(FrameworkElement.EnabledStateName));
        AssignLastSetInState(button.GetState(FrameworkElement.HighlightedStateName));
        AssignLastSetInState(button.GetState(FrameworkElement.PushedStateName));
        AssignLastSetInState(button.GetState(FrameworkElement.FocusedStateName));

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);

        mockKeyboard
            .Setup(k => k.KeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);
        // KeyCombo.IsComboPushed / IsComboDown now route through the Gum.Forms.Input.Keys overload.
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Space))
            .Returns(true);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyDown(Gum.Forms.Input.Keys.Space))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        button.OnFocusUpdate();

        lastStateSet.ShouldBe(FrameworkElement.PushedStateName, "Because the space key was pushed and is down");


        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(false);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Space))
            .Returns(false);

        button.UpdateState();

        lastStateSet.ShouldBe(FrameworkElement.PushedStateName, "Becuse the button is still focused and the click button is pushed");

        mockKeyboard
            .Setup(k => k.KeyReleased(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);

        mockKeyboard
            .Setup(k => k.KeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(false);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyReleased(Gum.Forms.Input.Keys.Space))
            .Returns(true);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(k => k.KeyDown(Gum.Forms.Input.Keys.Space))
            .Returns(false);

        button.OnFocusUpdate();

        lastStateSet.ShouldBe(FrameworkElement.FocusedStateName, "Because the space key was released and not down, but this button has fous");

        //------------------------------internal methods------------------------------

        void AssignLastSetInState(StateSave stateSave)
        {
            stateSave.Apply = () =>
            {
                lastStateSet = stateSave.Name;
            };
        }
    }
}
