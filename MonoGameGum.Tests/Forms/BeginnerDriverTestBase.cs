using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Base for the first-hour-with-Gum tests: Forms input comes from a scripted cursor and keyboard
/// and frames run through GumService.Update only, the way a beginner's Game1 does.
/// </summary>
public abstract class BeginnerDriverTestBase : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    protected readonly GumDriver Driver;

    protected BeginnerDriverTestBase()
    {
        Driver = GumDriver.ForGumService(Gum.GumService.Default);
        Driver.Install();
    }

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

    /// <summary>Adds a named control the way the tutorials do and lays out one frame.</summary>
    protected T AddToRoot<T>(T control, string name) where T : FrameworkElement
    {
        control.Name = name;
        control.AddToRoot();
        Driver.Step();
        return control;
    }
}
