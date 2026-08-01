using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Guards that ColorPicker is registered under the V3 default visuals on raylib, and that the
/// per-pixel saturation/value and hue textures the control generates actually reach the display
/// sprites through raylib's <c>PixelDataTextureApplier</c>. Mirrors
/// <see cref="SkiaGum.Tests.Forms.ColorPickerTests"/>. Issue #4241.
/// </summary>
public class ColorPickerTests : BaseTestClass
{
    public ColorPickerTests()
    {
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);
    }

    public override void Dispose()
    {
        FrameworkElement.DefaultFormsTemplates.Remove(typeof(ColorPicker));
        base.Dispose();
        TestAssemblyInitialize.ApplyDefaultTestState();
    }

    [Fact]
    public void ColorPicker_Visual_IsRegistered_OnV3()
    {
        ColorPicker colorPicker = new ColorPicker();

        colorPicker.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void ColorPicker_HueDisplay_ReceivesGeneratedTexture()
    {
        ColorPicker colorPicker = new ColorPicker();

        TextureOf(colorPicker, "HueDisplay").ShouldNotBeNull();
    }

    [Fact]
    public void ColorPicker_SaturationValueDisplay_ReceivesGeneratedTexture()
    {
        ColorPicker colorPicker = new ColorPicker();

        TextureOf(colorPicker, "SaturationValueDisplay").ShouldNotBeNull();
    }

    [Fact]
    public void ColorPicker_SaturationValueDisplay_IsNotSharedBetweenLivePickers()
    {
        ColorPicker first = new ColorPicker();
        ColorPicker second = new ColorPicker();
        // Only a picker still in the visual tree holds its pooled texture; an unparented one is
        // reclaimable, which would let both pickers land on the same entry.
        ContainerRuntime root = new ContainerRuntime();
        root.AddChild(first.Visual);
        root.AddChild(second.Visual);

        first.Hue = 0f;
        second.Hue = 180f;

        TextureOf(first, "SaturationValueDisplay")!.Value.Id
            .ShouldNotBe(TextureOf(second, "SaturationValueDisplay")!.Value.Id);
    }

    private static Texture2D? TextureOf(ColorPicker colorPicker, string spriteName)
    {
        GraphicalUiElement element = colorPicker.Visual.GetGraphicalUiElementByName(spriteName);
        return ((Sprite)element.RenderableComponent).Texture;
    }
}
