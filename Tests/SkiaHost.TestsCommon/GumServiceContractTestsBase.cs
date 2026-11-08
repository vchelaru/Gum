using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// What every interactive Skia host's <c>GumService</c> promises once the assembly bootstrap has
/// initialized it: it is the runtime default, Forms controls construct with a Visual, <c>Update</c>
/// ticks the installed cursor, and a custom cursor or keyboard hides the host's typed accessor. A
/// host's test project derives from this class and supplies its typed accessors and its concrete
/// service. The class puts the cursor, keyboard and Root back itself, so it needs no host base class.
/// </summary>
public abstract class GumServiceContractTestsBase : IDisposable
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public void Dispose()
    {
        ConcreteService.Root.Children.Clear();
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
    }

    /// <summary>The host's typed <c>GumService.Default.Cursor</c>.</summary>
    protected abstract object? ConcreteCursor { get; }

    /// <summary>The host's typed <c>GumService.Default.Keyboard</c>.</summary>
    protected abstract object? ConcreteKeyboard { get; }

    /// <summary>The host's concrete <c>GumService.Default</c>.</summary>
    protected abstract IGumService ConcreteService { get; }

    [Fact]
    public void Bootstrap_SetsIGumServiceDefault()
    {
        IGumService.Default.ShouldNotBeNull();
        IGumService.Default.ShouldBeSameAs(ConcreteService);
        ConcreteService.Root.ShouldNotBeNull();
    }

    [Fact]
    public void Button_ConstructsWithVisual_OnSkia()
    {
        // Proves the whole Forms-on-Skia path wired by this runtime: FormsUtilities.InitializeDefaults
        // registered the V3 ButtonVisual, so the parameterless control has a non-null Visual.
        Button button = new Button();

        button.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void FrameworkElement_AddToRoot_AddsVisualToRoot()
    {
        ContainerRuntime visual = new ContainerRuntime();
        FrameworkElement element = new FrameworkElement(visual);

        element.AddToRoot();

        ConcreteService.Root.Children.ShouldContain(visual);
    }

    [Fact]
    public void Update_PumpsFormsInput_ThroughTheCursor()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);

        ConcreteService.Update(5);

        cursor.Verify(c => c.Activity(5), Times.Once());
    }

    [Fact]
    public void Cursor_IsNull_WhileACustomCursorIsInstalled()
    {
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        ConcreteCursor.ShouldBeNull();
    }

    [Fact]
    public void Keyboard_IsNull_WhileACustomKeyboardIsInstalled()
    {
        IInputReceiverKeyboard original = FormsUtilities.Keyboard!;
        try
        {
            FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

            ConcreteKeyboard.ShouldBeNull();
        }
        finally
        {
            FormsUtilities.SetKeyboard(original);
        }
    }

    [Fact]
    public void Default_IsOneInstance()
    {
        ConcreteService.ShouldBeSameAs(ConcreteService);
    }
}
