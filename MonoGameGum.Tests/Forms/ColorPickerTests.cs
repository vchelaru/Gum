using Gum.Forms.Controls;
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
}
