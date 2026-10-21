using Gum;
using Gum.Forms;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// Pins which cursor and keyboard the service's accessors return in each install state (#6322). Every
/// interactive Skia host has its own concrete <c>Cursor</c> and <c>Keyboard</c> types, so a host's test
/// project derives from this class and supplies the typed <c>GumService.Default.Cursor</c> and
/// <c>Keyboard</c> as objects. The accessors on <see cref="IGumService"/> and <see cref="FormsUtilities"/>
/// need no host type.
/// </summary>
public abstract class InstalledInputTestsBase : IDisposable
{
    private static GumServiceSkiaBase Service => (GumServiceSkiaBase)IGumService.Default!;

    /// <summary>The host's typed <c>GumService.Default.Cursor</c>.</summary>
    protected abstract object? ConcreteCursor { get; }

    /// <summary>The host's typed <c>GumService.Default.Keyboard</c>.</summary>
    protected abstract object? ConcreteKeyboard { get; }

    // The class puts the bootstrap's cursor and keyboard back itself, so it needs no host base class.
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
    }

    [Fact]
    public void Bootstrap_InstallsTheHostsCursorAndKeyboard_ReturnedByTheTypedAccessors()
    {
        ConcreteCursor.ShouldNotBeNull();
        ConcreteKeyboard.ShouldNotBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(ConcreteCursor);
        FormsUtilities.Keyboard.ShouldBeSameAs(ConcreteKeyboard);
    }

    [Fact]
    public void SetCursor_WithCustomCursor_TypedAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        ConcreteCursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
    }

    [Fact]
    public void SetKeyboard_WithCustomKeyboard_TypedAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        ConcreteKeyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(custom);
    }

    [Fact]
    public void SetCursor_ReinstallingTheHostsCursor_RestoresTheTypedAccessor()
    {
        object original = ConcreteCursor!;
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        FormsUtilities.SetCursor((ICursor)original);

        ConcreteCursor.ShouldBeSameAs(original);
        FormsUtilities.Cursor.ShouldBeSameAs(original);
    }

    [Fact]
    public void SetKeyboard_ReinstallingTheHostsKeyboard_RestoresTheTypedAccessor()
    {
        object original = ConcreteKeyboard!;
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        FormsUtilities.SetKeyboard((IInputReceiverKeyboard)original);

        ConcreteKeyboard.ShouldBeSameAs(original);
        FormsUtilities.Keyboard.ShouldBeSameAs(original);
    }

    [Fact]
    public void SetCursor_DoesNotReplaceTheKeyboard_AndSetKeyboardDoesNotReplaceTheCursor()
    {
        object cursor = ConcreteCursor!;
        object keyboard = ConcreteKeyboard!;

        FormsUtilities.SetCursor(new Mock<ICursor>().Object);
        FormsUtilities.Keyboard.ShouldBeSameAs(keyboard);

        FormsUtilities.SetCursor((ICursor)cursor);
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);
        FormsUtilities.Cursor.ShouldBeSameAs(cursor);
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
    public void InterfaceAccessors_AfterBootstrap_ReturnTheHostsCursorAndKeyboard()
    {
        IGumService service = IGumService.Default!;

        service.Cursor.ShouldBeSameAs(ConcreteCursor);
        service.Keyboard.ShouldBeSameAs(ConcreteKeyboard);
    }

    [Fact]
    public void InterfaceAccessors_WithCustomInstalled_ReturnTheCustomObjects()
    {
        IGumService service = IGumService.Default!;
        ICursor customCursor = new Mock<ICursor>().Object;
        IInputReceiverKeyboard customKeyboard = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetCursor(customCursor);
        FormsUtilities.SetKeyboard(customKeyboard);

        service.Cursor.ShouldBeSameAs(customCursor);
        service.Keyboard.ShouldBeSameAs(customKeyboard);
        ConcreteCursor.ShouldBeNull("the typed accessors keep returning null under a custom implementation");
        ConcreteKeyboard.ShouldBeNull();
    }

    [Fact]
    public void InterfaceAccessors_ReinstallingTheHostsInput_ReturnsIt()
    {
        IGumService service = IGumService.Default!;
        object cursor = ConcreteCursor!;
        object keyboard = ConcreteKeyboard!;
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        FormsUtilities.SetCursor((ICursor)cursor);
        FormsUtilities.SetKeyboard((IInputReceiverKeyboard)keyboard);

        service.Cursor.ShouldBeSameAs(cursor);
        service.Keyboard.ShouldBeSameAs(keyboard);
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
