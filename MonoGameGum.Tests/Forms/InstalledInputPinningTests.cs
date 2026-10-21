using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins what the cursor and keyboard accessors return in each install state, and what installing one
/// does and does not touch (#6322). Installing is a plain assignment of <see cref="FrameworkElement.MainCursor"/>
/// / <see cref="FrameworkElement.MainKeyboard"/>: no event is raised, and the only readers are the accessors
/// below and the next <c>Update</c>. The multi-service and context cases live in
/// MonoGameGum.IntegrationTests (<c>GumServiceInstalledInputTests</c>).
/// </summary>
public class InstalledInputPinningTests : BaseTestClass
{
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public override void Dispose()
    {
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private static Gum.GumService Service => Gum.GumService.Default;

    #region Cursor accessors in each state

    [Fact]
    public void Cursor_WhenBuiltInCursorIsInstalled_AllAccessorsReturnIt()
    {
        Cursor installed = new Cursor(null);

        FormsUtilities.SetCursor(installed);

        Service.Cursor.ShouldBeSameAs(installed);
        ((IGumService)Service).Cursor.ShouldBeSameAs(installed);
        FormsUtilities.Cursor.ShouldBeSameAs(installed);
        FrameworkElement.MainCursor.ShouldBeSameAs(installed);
    }

    [Fact]
    public void Cursor_WhenCustomCursorIsInstalled_ConcreteAccessorIsNullAndActiveAccessorsReturnIt()
    {
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        Service.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
        FrameworkElement.MainCursor.ShouldBeSameAs(custom);
    }

    [Fact]
    public void Cursor_WhenNothingIsInstalled_AllAccessorsAreNull()
    {
        FrameworkElement.MainCursor = null!;

        Service.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeNull();
        FrameworkElement.MainCursor.ShouldBeNull();
        // The interface member is declared non-null; an absent cursor is null at runtime.
        ((IGumService)Service).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Cursor_WhenReplacedRepeatedly_AccessorsFollowTheLatestInstall()
    {
        Cursor builtInA = new Cursor(null);
        ICursor customA = new Mock<ICursor>().Object;
        Cursor builtInB = new Cursor(null);
        ICursor customB = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(builtInA);
        Service.Cursor.ShouldBeSameAs(builtInA);

        FormsUtilities.SetCursor(customA);
        Service.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(customA);

        FormsUtilities.SetCursor(customB);
        Service.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(customB);

        FormsUtilities.SetCursor(builtInB);
        Service.Cursor.ShouldBeSameAs(builtInB);
        ((IGumService)Service).Cursor.ShouldBeSameAs(builtInB);
        FormsUtilities.Cursor.ShouldBeSameAs(builtInB);

        // Re-installing the same instance is harmless.
        FormsUtilities.SetCursor(builtInB);
        FormsUtilities.Cursor.ShouldBeSameAs(builtInB);
    }

    [Fact]
    public void SetCursor_AssignedDirectlyToMainCursor_IsSeenByAllAccessors()
    {
        Cursor installed = new Cursor(null);

        FrameworkElement.MainCursor = installed;

        Service.Cursor.ShouldBeSameAs(installed);
        FormsUtilities.Cursor.ShouldBeSameAs(installed);
    }

    #endregion

    #region Keyboard accessors in each state

    [Fact]
    public void Keyboard_WhenBuiltInKeyboardIsInstalled_AllAccessorsReturnIt()
    {
        Keyboard installed = new Keyboard();

        FormsUtilities.SetKeyboard(installed);

        Service.Keyboard.ShouldBeSameAs(installed);
        FormsUtilities.Keyboard.ShouldBeSameAs(installed);
        FrameworkElement.MainKeyboard.ShouldBeSameAs(installed);
    }

    [Fact]
    public void Keyboard_WhenCustomKeyboardIsInstalled_ConcreteAccessorIsNullAndActiveAccessorsReturnIt()
    {
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        Service.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(custom);
        FrameworkElement.MainKeyboard.ShouldBeSameAs(custom);
    }

    [Fact]
    public void Keyboard_WhenNothingIsInstalled_AllAccessorsAreNull()
    {
        FrameworkElement.MainKeyboard = null!;

        Service.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeNull();
        FrameworkElement.MainKeyboard.ShouldBeNull();
    }

    [Fact]
    public void Keyboard_WhenReplacedRepeatedly_AccessorsFollowTheLatestInstall()
    {
        Keyboard builtInA = new Keyboard();
        IInputReceiverKeyboard customA = new Mock<IInputReceiverKeyboard>().Object;
        Keyboard builtInB = new Keyboard();

        FormsUtilities.SetKeyboard(builtInA);
        Service.Keyboard.ShouldBeSameAs(builtInA);

        FormsUtilities.SetKeyboard(customA);
        Service.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(customA);

        FormsUtilities.SetKeyboard(builtInB);
        Service.Keyboard.ShouldBeSameAs(builtInB);
        FormsUtilities.Keyboard.ShouldBeSameAs(builtInB);

        FormsUtilities.SetKeyboard(builtInB);
        FormsUtilities.Keyboard.ShouldBeSameAs(builtInB);
    }

    [Fact]
    public void SetKeyboard_Null_ThrowsAndKeepsTheInstalledKeyboard()
    {
        Keyboard installed = new Keyboard();
        FormsUtilities.SetKeyboard(installed);

        Should.Throw<ArgumentNullException>(() => FormsUtilities.SetKeyboard(null!));

        FormsUtilities.Keyboard.ShouldBeSameAs(installed);
        Service.Keyboard.ShouldBeSameAs(installed);
    }

    #endregion

    #region IGumService returns whichever cursor and keyboard is installed (#6322)

    [Fact]
    public void InterfaceCursor_WhenCustomCursorIsInstalled_ReturnsIt()
    {
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        ((IGumService)Service).Cursor.ShouldBeSameAs(custom);
        Service.Cursor.ShouldBeNull("the concrete accessor keeps returning null under a custom cursor");
    }

    [Fact]
    public void InterfaceCursor_WhenReplacedRepeatedly_FollowsTheLatestInstall()
    {
        ICursor customA = new Mock<ICursor>().Object;
        ICursor customB = new Mock<ICursor>().Object;
        Cursor builtIn = new Cursor(null);
        IGumService service = Service;

        FormsUtilities.SetCursor(customA);
        service.Cursor.ShouldBeSameAs(customA);
        FormsUtilities.SetCursor(customB);
        service.Cursor.ShouldBeSameAs(customB);
        FormsUtilities.SetCursor(builtIn);
        service.Cursor.ShouldBeSameAs(builtIn);
    }

    [Fact]
    public void InterfaceKeyboard_WhenBuiltInKeyboardIsInstalled_ReturnsIt()
    {
        Keyboard installed = new Keyboard();

        FormsUtilities.SetKeyboard(installed);

        ((IGumService)Service).Keyboard.ShouldBeSameAs(installed);
    }

    [Fact]
    public void InterfaceKeyboard_WhenCustomKeyboardIsInstalled_ReturnsIt()
    {
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        ((IGumService)Service).Keyboard.ShouldBeSameAs(custom);
        Service.Keyboard.ShouldBeNull("the concrete accessor keeps returning null under a custom keyboard");
    }

    [Fact]
    public void InterfaceKeyboard_WhenNothingIsInstalled_IsNull()
    {
        FrameworkElement.MainKeyboard = null!;

        ((IGumService)Service).Keyboard.ShouldBeNull();
    }

    [Fact]
    public void InterfaceKeyboard_WhenReplacedRepeatedly_FollowsTheLatestInstall()
    {
        IInputReceiverKeyboard customA = new Mock<IInputReceiverKeyboard>().Object;
        IInputReceiverKeyboard customB = new Mock<IInputReceiverKeyboard>().Object;
        Keyboard builtIn = new Keyboard();
        IGumService service = Service;

        FormsUtilities.SetKeyboard(customA);
        service.Keyboard.ShouldBeSameAs(customA);
        FormsUtilities.SetKeyboard(customB);
        service.Keyboard.ShouldBeSameAs(customB);
        FormsUtilities.SetKeyboard(builtIn);
        service.Keyboard.ShouldBeSameAs(builtIn);
    }

    [Fact]
    public void InterfaceAccessors_DoNotCallMembersOnTheInstalledObjects()
    {
        Mock<ICursor> cursor = new Mock<ICursor>(MockBehavior.Strict);
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>(MockBehavior.Strict);
        FormsUtilities.SetCursor(cursor.Object);
        FormsUtilities.SetKeyboard(keyboard.Object);

        _ = ((IGumService)Service).Cursor;
        _ = ((IGumService)Service).Keyboard;

        cursor.VerifyNoOtherCalls();
        keyboard.VerifyNoOtherCalls();
    }

    [Fact]
    public void InterfaceAccessors_TwoServicesWithoutContexts_ReadTheSameInstalledObjects()
    {
        IGumService? original = IGumService.Default;
        Gum.GumService other = new Gum.GumService();
        ICursor customCursor = new Mock<ICursor>().Object;
        IInputReceiverKeyboard customKeyboard = new Mock<IInputReceiverKeyboard>().Object;
        FormsUtilities.SetCursor(customCursor);
        FormsUtilities.SetKeyboard(customKeyboard);
        try
        {
            other.InitializeForTesting();

            ((IGumService)other).Cursor.ShouldBeSameAs(customCursor);
            ((IGumService)other).Keyboard.ShouldBeSameAs(customKeyboard);
            ((IGumService)Service).Cursor.ShouldBeSameAs(customCursor);
            ((IGumService)Service).Keyboard.ShouldBeSameAs(customKeyboard);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    #endregion

    #region Installing touches only the installed slot

    [Fact]
    public void SetCursor_DoesNotTouchTheKeyboardRootsOrFocus()
    {
        IInputReceiverKeyboard keyboardBefore = FormsUtilities.Keyboard!;
        InteractiveGue popupRoot = FrameworkElement.PopupRoot;
        InteractiveGue modalRoot = FrameworkElement.ModalRoot;
        Gum.Input.GamePad[] gamepads = FormsUtilities.Gamepads;
        IGumService? defaultBefore = IGumService.Default;
        TextBox focused = new TextBox();
        InteractiveGue.CurrentInputReceiver = focused;
        FrameworkElement.KeyboardsForUiControl.Add(keyboardBefore);

        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        FormsUtilities.Keyboard.ShouldBeSameAs(keyboardBefore);
        FrameworkElement.KeyboardsForUiControl.ShouldBe(new[] { keyboardBefore });
        FrameworkElement.PopupRoot.ShouldBeSameAs(popupRoot);
        FrameworkElement.ModalRoot.ShouldBeSameAs(modalRoot);
        FormsUtilities.Gamepads.ShouldBeSameAs(gamepads);
        IGumService.Default.ShouldBeSameAs(defaultBefore);
        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(focused);
    }

    [Fact]
    public void SetKeyboard_DoesNotTouchTheCursorRootsOrFocus()
    {
        ICursor cursorBefore = FormsUtilities.Cursor!;
        InteractiveGue popupRoot = FrameworkElement.PopupRoot;
        InteractiveGue modalRoot = FrameworkElement.ModalRoot;
        Gum.Input.GamePad[] gamepads = FormsUtilities.Gamepads;
        IGumService? defaultBefore = IGumService.Default;
        TextBox focused = new TextBox();
        InteractiveGue.CurrentInputReceiver = focused;

        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        FormsUtilities.Cursor.ShouldBeSameAs(cursorBefore);
        FrameworkElement.PopupRoot.ShouldBeSameAs(popupRoot);
        FrameworkElement.ModalRoot.ShouldBeSameAs(modalRoot);
        FormsUtilities.Gamepads.ShouldBeSameAs(gamepads);
        IGumService.Default.ShouldBeSameAs(defaultBefore);
        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(focused);
    }

    [Fact]
    public void SetKeyboard_SwapsTheOldKeyboardInKeyboardsForUiControlOnly()
    {
        Keyboard first = new Keyboard();
        IInputReceiverKeyboard second = new Mock<IInputReceiverKeyboard>().Object;
        IInputReceiverKeyboard unrelated = new Mock<IInputReceiverKeyboard>().Object;
        FormsUtilities.SetKeyboard(first);
        FrameworkElement.KeyboardsForUiControl.Add(unrelated);
        FrameworkElement.KeyboardsForUiControl.Add(first);

        FormsUtilities.SetKeyboard(second);

        FrameworkElement.KeyboardsForUiControl.ShouldBe(new[] { unrelated, second });
    }

    [Fact]
    public void SetCursor_DoesNotChangeKeyboardsForUiControl()
    {
        IInputReceiverKeyboard registered = new Mock<IInputReceiverKeyboard>().Object;
        FrameworkElement.KeyboardsForUiControl.Add(registered);

        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        FrameworkElement.KeyboardsForUiControl.ShouldBe(new[] { registered });
    }

    #endregion

    #region No event is raised on install

    [Fact]
    public void SetCursorAndSetKeyboard_DoNotRaiseDefaultChanged()
    {
        int raised = 0;
        Action<IGumService?> handler = _ => raised++;
        IGumService.DefaultChanged += handler;
        try
        {
            FormsUtilities.SetCursor(new Mock<ICursor>().Object);
            FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);
            FormsUtilities.SetCursor(new Cursor(null));
            FormsUtilities.SetKeyboard(new Keyboard());
        }
        finally
        {
            IGumService.DefaultChanged -= handler;
        }

        raised.ShouldBe(0);
    }

    [Fact]
    public void SetCursorAndSetKeyboard_DoNotCallMembersOnTheInstalledObjects()
    {
        // Installing is an assignment: nothing is read from, subscribed to, or ticked on the new object.
        Mock<ICursor> cursor = new Mock<ICursor>(MockBehavior.Strict);
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>(MockBehavior.Strict);

        Should.NotThrow(() => FormsUtilities.SetCursor(cursor.Object));
        Should.NotThrow(() => FormsUtilities.SetKeyboard(keyboard.Object));

        cursor.VerifyNoOtherCalls();
        keyboard.VerifyNoOtherCalls();
    }

    [Fact]
    public void DefaultAssignment_RaisesDefaultChangedOnceWithTheNewService()
    {
        IGumService? original = IGumService.Default;
        List<IGumService?> seen = new List<IGumService?>();
        Action<IGumService?> handler = seen.Add;
        Gum.GumService other = new Gum.GumService();
        IGumService.DefaultChanged += handler;
        try
        {
            IGumService.Default = other;
        }
        finally
        {
            IGumService.DefaultChanged -= handler;
            IGumService.Default = original;
        }

        seen.ShouldBe(new IGumService?[] { other });
    }

    #endregion

    #region Update reads the installed objects

    [Fact]
    public void Update_TicksTheInstalledCursorAndKeyboardOnly()
    {
        Mock<ICursor> oldCursor = new Mock<ICursor>();
        Mock<ICursor> newCursor = new Mock<ICursor>();
        Mock<IInputReceiverKeyboard> oldKeyboard = new Mock<IInputReceiverKeyboard>();
        Mock<IInputReceiverKeyboard> newKeyboard = new Mock<IInputReceiverKeyboard>();
        FormsUtilities.SetCursor(oldCursor.Object);
        FormsUtilities.SetKeyboard(oldKeyboard.Object);
        Service.Update(new GameTime());
        oldCursor.Invocations.Clear();
        oldKeyboard.Invocations.Clear();

        FormsUtilities.SetCursor(newCursor.Object);
        FormsUtilities.SetKeyboard(newKeyboard.Object);
        Service.Update(new GameTime());

        newCursor.Verify(c => c.Activity(It.IsAny<double>()), Times.Once());
        newKeyboard.Verify(k => k.Activity(It.IsAny<double>()), Times.Once());
        oldCursor.Verify(c => c.Activity(It.IsAny<double>()), Times.Never());
        oldKeyboard.Verify(k => k.Activity(It.IsAny<double>()), Times.Never());
    }

    [Fact]
    public void Update_WithNoCursor_ThrowsInvalidOperation()
    {
        FrameworkElement.MainCursor = null!;

        Should.Throw<InvalidOperationException>(() => Service.Update(new GameTime()));
    }

    [Fact]
    public void Update_WithNoKeyboard_ThrowsInvalidOperation()
    {
        FrameworkElement.MainKeyboard = null!;

        Should.Throw<InvalidOperationException>(() => Service.Update(new GameTime()));
    }

    [Fact]
    public void Update_AfterReplacingTheCursor_HoverGoesToTheNewCursor()
    {
        Mock<ICursor> oldCursor = CreateCursorAt(500, 500);
        Button button = new Button();
        button.AddToRoot();
        button.X = 0;
        button.Y = 0;
        button.Width = 100;
        button.Height = 100;
        button.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        button.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Service.Update(new GameTime());
        oldCursor.Object.VisualOver.ShouldBeNull("setup: the old cursor is away from the button");

        Mock<ICursor> newCursor = CreateCursorAt(10, 10);
        Service.Update(new GameTime());

        newCursor.Object.VisualOver.ShouldBeSameAs(button.Visual);
        oldCursor.Object.VisualOver.ShouldBeNull();
    }

    private static Mock<ICursor> CreateCursorAt(int x, int y)
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.CustomCursor);
        cursor.Setup(c => c.X).Returns(x);
        cursor.Setup(c => c.Y).Returns(y);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        FormsUtilities.SetCursor(cursor.Object);
        return cursor;
    }

    #endregion

    #region Services that have no context of their own share the process statics

    [Fact]
    public void Cursor_TwoServicesWithoutContexts_ReadTheSameInstalledCursor()
    {
        IGumService? original = IGumService.Default;
        Gum.GumService other = new Gum.GumService();
        Cursor installed = new Cursor(null);
        FormsUtilities.SetCursor(installed);
        try
        {
            other.InitializeForTesting();
            IGumService.Default.ShouldBeSameAs(other, "setup");

            other.Cursor.ShouldBeSameAs(installed);
            ((IGumService)other).Cursor.ShouldBeSameAs(installed);
            Service.Cursor.ShouldBeSameAs(installed);

            IGumService.Default = original;
            other.Cursor.ShouldBeSameAs(installed);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    [Fact]
    public void Default_Switching_DoesNotChangeTheInstalledCursorOrKeyboard()
    {
        IGumService? original = IGumService.Default;
        Gum.GumService other = new Gum.GumService();
        Cursor installedCursor = new Cursor(null);
        Keyboard installedKeyboard = new Keyboard();
        FormsUtilities.SetCursor(installedCursor);
        FormsUtilities.SetKeyboard(installedKeyboard);
        try
        {
            other.InitializeForTesting();
            IGumService.Default = original;

            FormsUtilities.Cursor.ShouldBeSameAs(installedCursor);
            FormsUtilities.Keyboard.ShouldBeSameAs(installedKeyboard);
            IGumService.Default.ShouldBeSameAs(original);
        }
        finally
        {
            IGumService.Default = original;
        }
    }

    #endregion
}
