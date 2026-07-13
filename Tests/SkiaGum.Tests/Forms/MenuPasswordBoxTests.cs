using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// Guards that Menu, MenuItem, and PasswordBox are registered under the V3 default visuals on
/// Skia. Their registrations in <c>FormsUtilities.InitializeDefaults</c> were historically gated
/// <c>#if XNALIKE || FRB || RAYLIB</c>, excluding SKIA (and SILK, which piggybacks the SKIA
/// constant) -- so <c>new Menu()</c> / <c>new MenuItem()</c> / <c>new PasswordBox()</c> previously
/// produced a null Visual on Skia. Mirrors
/// <see cref="RaylibGum.Tests.Forms.MenuPasswordBoxAndImageTests"/>. Issue #3649.
///
/// Calls <see cref="FormsUtilities.InitializeDefaults"/> explicitly rather than relying on
/// <c>GumService.Initialize</c> -- the render-only SkiaGum.Standalone GumService (used here) never
/// calls it, unlike the game-loop Gum.GumService used by SilkNetGum.
///
/// Menu/MenuItem construct their Visual and are asserted end-to-end. PasswordBox is asserted via
/// the DefaultFormsTemplates registration only, not by constructing it -- Skia's Text renderable
/// doesn't implement IFormsText yet, so constructing any text-input control (TextBox too, not just
/// PasswordBox) throws in TextBoxBase.RefreshInternalVisualReferences regardless of this
/// registration fix. See #3653.
/// </summary>
public class MenuPasswordBoxTests
{
    public MenuPasswordBoxTests()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);
    }

    [Fact]
    public void Menu_Visual_IsRegistered_OnV3()
    {
        var menu = new Menu();
        menu.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void MenuItem_Visual_IsRegistered_OnV3()
    {
        var menuItem = new MenuItem();
        menuItem.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void PasswordBox_IsRegistered_OnV3()
    {
        // Not constructing a PasswordBox here -- see the class remarks and #3653.
        FrameworkElement.DefaultFormsTemplates.ShouldContainKey(typeof(PasswordBox));
    }
}
