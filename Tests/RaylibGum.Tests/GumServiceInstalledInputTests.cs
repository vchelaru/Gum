using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Which cursor and keyboard the raylib service's accessors return in each install state (#6322).
/// The raylib service shares its source with the MonoGame one, so this mirrors the headless cases in
/// MonoGameGum.Tests (<c>InstalledInputPinningTests</c>).
/// </summary>
public class GumServiceInstalledInputTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public override void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private static Gum.GumService Service => Gum.GumService.Default;

    [Fact]
    public void Cursor_WhenBuiltInCursorIsInstalled_AllAccessorsReturnIt()
    {
        Cursor installed = Cursor.CreateForCurrentPlatform();

        FormsUtilities.SetCursor(installed);

        Service.Cursor.ShouldBeSameAs(installed);
        ((IGumService)Service).Cursor.ShouldBeSameAs(installed);
        FormsUtilities.Cursor.ShouldBeSameAs(installed);
    }

    [Fact]
    public void Cursor_WhenCustomCursorIsInstalled_ConcreteAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        Service.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
    }

    [Fact]
    public void Cursor_WhenCustomCursorIsInstalled_InterfaceAccessorIsNullToday()
    {
        // Pins today's behavior: IGumService.Cursor forwards to the concrete property (#6322 flips it).
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        ((IGumService)Service).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Cursor_WhenReinstallingTheBuiltInCursor_RestoresEveryAccessor()
    {
        Cursor builtIn = Cursor.CreateForCurrentPlatform();
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        FormsUtilities.SetCursor(builtIn);

        Service.Cursor.ShouldBeSameAs(builtIn);
        ((IGumService)Service).Cursor.ShouldBeSameAs(builtIn);
    }

    [Fact]
    public void Keyboard_WhenBuiltInKeyboardIsInstalled_BothAccessorsReturnIt()
    {
        Keyboard installed = Keyboard.CreateForCurrentPlatform();

        FormsUtilities.SetKeyboard(installed);

        Service.Keyboard.ShouldBeSameAs(installed);
        FormsUtilities.Keyboard.ShouldBeSameAs(installed);
    }

    [Fact]
    public void Keyboard_WhenCustomKeyboardIsInstalled_ConcreteAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        Service.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(custom);
    }

    [Fact]
    public void Update_TicksTheInstalledCursorAndKeyboardOnly()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        FormsUtilities.SetCursor(cursor.Object);
        FormsUtilities.SetKeyboard(keyboard.Object);

        Service.Update(5);

        cursor.Verify(c => c.Activity(5), Times.Once());
        keyboard.Verify(k => k.Activity(5), Times.Once());
    }

    [Fact]
    public void SetCursorAndSetKeyboard_DoNotChangeTheDefaultService()
    {
        IGumService? defaultBefore = IGumService.Default;

        FormsUtilities.SetCursor(new Mock<ICursor>().Object);
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        IGumService.Default.ShouldBeSameAs(defaultBefore);
    }
}
