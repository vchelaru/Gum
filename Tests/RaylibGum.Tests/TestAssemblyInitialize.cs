using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: Xunit.TestFramework("RaylibGum.Tests.TestAssemblyInitialize", "RaylibGum.Tests")]

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace RaylibGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for RaylibGum tests. Mirrors
/// <c>MonoGameGum.TestsCommon.TestAssemblyInitializeBase</c> but for Raylib: opens a
/// hidden Raylib window (required for <c>LoadEmbeddedTexture2d</c> and text measurement),
/// initializes <see cref="SystemManagers"/>, runs <see cref="FormsUtilities.InitializeDefaults"/>
/// so <c>new Button()</c> / <c>new ListBox()</c> / etc. return a control with a valid Visual,
/// and registers the shared Gum keyboard via <c>GumService.Default.UseKeyboardDefaults()</c>.
/// </summary>
public class TestAssemblyInitialize : XunitTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        ApplyDefaultTestState();
    }

    /// <summary>
    /// Sets up (or restores) the assembly-wide test state. Called once from the constructor
    /// and may be called again from tests that tear down <see cref="GumService"/> via
    /// <c>Uninitialize()</c> and need to rebuild the default state for subsequent tests.
    /// </summary>
    public static void ApplyDefaultTestState()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        // Re-establish the font loader here too: tests that tear down GumService null it (GumService
        // sets UpdateFontFromProperties = null), and this bootstrap is re-run after such teardowns.
        GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;

        // Raylib's LoadEmbeddedTexture2d and MeasureTextEx both require an initialized window.
        // The window is kept hidden for the duration of the test run.
        if (!Raylib.IsWindowReady())
        {
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(800, 600, "RaylibGum.Tests");
        }

        // Default SystemManagers mirrors the state a real Raylib game would produce after
        // GumService.Initialize(). We can't call GumService.Initialize() here because the
        // various test base classes call GumService.Default.InitializeForTesting() instead,
        // which flips IsInitialized without wiring up SystemManagers.
        if (SystemManagers.Default == null)
        {
            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize();
            ISystemManagers.Default = SystemManagers.Default;
        }

        // FormsUtilities.InitializeDefaults now creates the cursor/keyboard/gamepad driver through
        // IGumService.Default, so the service must be the active default before that call -- exactly as
        // the real runtime's Initialize orders it (IGumService.Default = this; before InitializeDefaults).
        // Qualified as Gum.GumService (the modern, non-obsolete class) to stay warning-free.
        IGumService.Default = Gum.GumService.Default;

        // Registers V2 DefaultFormsTemplates (Button, ListBox, Slider, ComboBox, ...) so
        // parameterless Forms constructors produce a control with Visual != null on Raylib.
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V2);

        // Wire GumService.Default.Root into the SystemManagers so Forms controls added to
        // the root participate in layout.
        GumService.Default.Root.Dock(Dock.Fill);
        GumService.Default.Root.Name = "Main Root";
        GumService.Default.Root.HasEvents = false;
        GumService.Default.Root.AddToManagers(SystemManagers.Default);

        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        Renderer.Self.Camera.ClientWidth = 800;
        Renderer.Self.Camera.ClientHeight = 600;

        GumService.Default.Root.UpdateLayout();

        // #3066: record the post-bootstrap renderables (Root + Forms defaults) so BaseTestClass.Dispose
        // can sweep anything a test leaks onto the shared layers, keeping draw-call-count tests
        // isolated from each other regardless of run order.
        BaseTestClass.CaptureRenderableBaseline();

        // Intentionally do NOT call GumService.Default.UseKeyboardDefaults() here.
        // KeyCombo.IsComboPushed returns inside the first iteration of
        // FrameworkElement.KeyboardsForUiControl, so leaving the real Raylib keyboard
        // registered would short-circuit tests that mock a keyboard by Add-ing after.
        // Tests add their own Mock<IInputReceiverKeyboard> (after BaseTestClass.Dispose
        // clears the list) which matches the MonoGame test pattern. Real apps still
        // call GumService.Default.UseKeyboardDefaults() at startup — this is test-only.
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
