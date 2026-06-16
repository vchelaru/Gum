using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Guards that the four controls enabled on raylib in issue #3174 build a usable Visual under the
/// V3 default visuals. Menu, MenuItem, and PasswordBox route through
/// <see cref="FrameworkElement.DefaultFormsTemplates"/>, whose registrations in
/// <c>FormsUtilities.InitializeDefaults</c> were historically gated <c>#if XNALIKE || FRB</c> — so on
/// raylib <c>new Menu()</c> / <c>new MenuItem()</c> / <c>new PasswordBox()</c> previously produced a
/// null Visual. Image is not a template entry; it builds its own Visual directly (see its test).
///
/// Mirrors <see cref="TextBoxTests"/>: layers V3 on top of the assembly bootstrap's V2 setup and
/// removes the V3-only registrations in Dispose.
/// </summary>
public class MenuPasswordBoxAndImageTests : BaseTestClass
{
    public MenuPasswordBoxAndImageTests()
    {
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);
    }

    public override void Dispose()
    {
        FrameworkElement.DefaultFormsTemplates.Remove(typeof(Menu));
        FrameworkElement.DefaultFormsTemplates.Remove(typeof(MenuItem));
        FrameworkElement.DefaultFormsTemplates.Remove(typeof(PasswordBox));
        base.Dispose();
        TestAssemblyInitialize.ApplyDefaultTestState();
    }

    [Fact]
    public void Image_Visual_IsBuilt()
    {
        // Image is not a DefaultFormsTemplates entry: it builds its own InteractiveGue wrapping a
        // sprite renderable via IGumService.CreateSpriteRenderable(), which has a live #elif RAYLIB
        // branch. This guards that path produces a usable Visual on raylib.
        var image = new Image();
        image.Visual.ShouldNotBeNull();
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
    public void PasswordBox_Visual_IsRegistered_OnV3()
    {
        var passwordBox = new PasswordBox();
        passwordBox.Visual.ShouldNotBeNull();
    }
}
