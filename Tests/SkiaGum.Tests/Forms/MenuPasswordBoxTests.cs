using Gum;
using Gum.Forms.Controls;
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
/// Menu/MenuItem construct their Visual and are asserted end-to-end. PasswordBox is asserted via
/// the DefaultFormsTemplates registration only, not by constructing it here -- the render-only
/// Skia GumService never assigns FrameworkElement.MainCursor (its CreateCursor hook returns null,
/// per the render-only base -- see GumServiceSkiaBase), which TextBoxBase.UpdateState (invoked
/// during construction) dereferences unconditionally, independent of the IFormsText cast this
/// class's remarks used to describe. The IFormsText cast itself no longer throws (#3653) -- see
/// <see cref="SilkNetGum.Tests.Forms.TextBoxPasswordBoxTests"/> for the full end-to-end
/// construction regression test, since SilkNetGum's bootstrap does provide a MainCursor.
/// </summary>
public class MenuPasswordBoxTests
{
    public MenuPasswordBoxTests()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);
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
        // Not constructing a PasswordBox here -- see the class remarks.
        FrameworkElement.DefaultFormsTemplates.ShouldContainKey(typeof(PasswordBox));
    }
}
