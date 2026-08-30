using System;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Gum.Forms.DefaultVisuals.V3;
using Color = System.Drawing.Color;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ColorPickerTests : BaseTestClass
{
    [Fact]
    public void HsvToRgb_ReturnsExpected_ForPrimaryColors()
    {
        ColorPicker.HsvToRgb(0f, 100f, 100f).ShouldBe(((byte)255, (byte)0, (byte)0));
        ColorPicker.HsvToRgb(120f, 100f, 100f).ShouldBe(((byte)0, (byte)255, (byte)0));
        ColorPicker.HsvToRgb(240f, 100f, 100f).ShouldBe(((byte)0, (byte)0, (byte)255));
        ColorPicker.HsvToRgb(0f, 0f, 0f).ShouldBe(((byte)0, (byte)0, (byte)0));
    }

    [Fact]
    public void BuildSaturationValuePixels_HasExpectedCorners_ForRedHue()
    {
        byte[] rgba = ColorPicker.BuildSaturationValuePixels(0f);
        int size = (int)Math.Sqrt(rgba.Length / 4);

        PixelAt(rgba, size, 0, 0).ShouldBe(((byte)255, (byte)255, (byte)255));        // sat 0, value 100 => white
        PixelAt(rgba, size, 0, size - 1).ShouldBe(((byte)0, (byte)0, (byte)0));        // sat 0, value 0 => black
        PixelAt(rgba, size, size - 1, 0).ShouldBe(((byte)255, (byte)0, (byte)0));      // sat 100, value 100, hue 0 => red
    }

    private static (byte R, byte G, byte B) PixelAt(byte[] rgba, int size, int x, int y)
    {
        int i = (y * size + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2]);
    }

    [Fact]
    public void RgbToHsv_ReturnsExpected_ForPrimaryColors()
    {
        ColorPicker.RgbToHsv(255, 0, 0).ShouldBe((0f, 100f, 100f));
        ColorPicker.RgbToHsv(0, 255, 0).ShouldBe((120f, 100f, 100f));
        ColorPicker.RgbToHsv(0, 0, 255).ShouldBe((240f, 100f, 100f));
        ColorPicker.RgbToHsv(0, 0, 0).ShouldBe((0f, 0f, 0f));
    }

    [Fact]
    public void SelectedColor_Set_UpdatesHsvState()
    {
        ColorPickerVisual visual = new();
        ColorPicker picker = visual.FormsControl;

        picker.SelectedColor = Color.FromArgb(255, 0, 0);

        picker.Hue.ShouldBe(0f);
        picker.Saturation.ShouldBe(100f);
        picker.Value.ShouldBe(100f);
    }

    [Fact]
    public void SelectedColor_Set_RaisesSelectedColorChangedOnce()
    {
        ColorPickerVisual visual = new();
        ColorPicker picker = visual.FormsControl;

        int raisedCount = 0;
        picker.SelectedColorChanged += (_, _) => raisedCount++;

        picker.SelectedColor = Color.FromArgb(255, 0, 0);
        picker.SelectedColor = Color.FromArgb(255, 0, 0);

        raisedCount.ShouldBe(1);
    }

    [Fact]
    public void Hue_Set_UpdatesSelectedColor()
    {
        ColorPickerVisual visual = new();
        ColorPicker picker = visual.FormsControl;

        picker.SelectedColor = Color.FromArgb(255, 0, 0);
        picker.Hue = 120f;

        picker.SelectedColor.ShouldBe(Color.FromArgb(0, 255, 0));
    }

    [Fact]
    public void Hue_Set_IsClampedToValidRange()
    {
        ColorPickerVisual visual = new();
        ColorPicker picker = visual.FormsControl;

        picker.Hue = 400f;

        picker.Hue.ShouldBe(360f);
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldCreateFromFileColorPicker_IfElementExists()
    {
        GumProjectSave gumProject = new GumProjectSave();
        var colorPickerComponent = new ComponentSave();
        colorPickerComponent.States.Add(new Gum.DataTypes.Variables.StateSave() { Name = "Default" });
        gumProject.Components.Add(colorPickerComponent);
        colorPickerComponent.Name = "TestColorPickerComponent";
        colorPickerComponent.Behaviors.Add(new Gum.DataTypes.Behaviors.ElementBehaviorReference
        { BehaviorName = "ColorPickerBehavior" });

        ObjectFinder.Self.GumProjectSave = gumProject;

        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        var gue = colorPickerComponent.ToGraphicalUiElement();

        (gue is DefaultFromFileColorPickerRuntime).ShouldBeTrue();
    }

    [Fact]
    public void Resizing_StretchesSaturationValueSquareAndHueBar()
    {
        ColorPickerVisual visual = new();
        visual.Width = 400;
        visual.Height = 300;

        visual.SaturationValueContainer.AbsoluteWidth.ShouldBe(400 - 24);
        visual.SaturationValueContainer.AbsoluteHeight.ShouldBe(300);

        visual.HueContainer.AbsoluteHeight.ShouldBe(300);
        // HueContainer.XOrigin is Right, so AbsoluteX already reports the right edge - it should
        // stay flush against the parent's right edge (400) at any size.
        visual.HueContainer.AbsoluteX.ShouldBe(400);
    }
}
