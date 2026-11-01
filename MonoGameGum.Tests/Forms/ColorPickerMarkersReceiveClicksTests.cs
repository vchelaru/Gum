using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Clicking the marker that shows the current color (in the saturation/value square) or the
/// current hue (in the hue bar) moves the color like a click anywhere else on that area does.
/// A marker that takes the hit for itself leaves the picker's own Push handler silent (#7426).
/// The click lands a few pixels off the marker's center so the picked color visibly changes.
/// </summary>
public class ColorPickerMarkersReceiveClicksTests : BeginnerDriverTestBase
{
    public static TheoryData<string> Themes => new()
    {
        "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95",
    };

    [Fact]
    public void ClickSaturationValueMarker_CodeOnlyVisual_ChangesSaturation()
    {
        ColorPicker picker = AddToRoot(new ColorPicker(), "Picker");
        ClickSaturationValueMarker(picker);
    }

    [Fact]
    public void ClickHueMarker_CodeOnlyVisual_ChangesHue()
    {
        ColorPicker picker = AddToRoot(new ColorPicker(), "Picker");
        ClickHueMarker(picker);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickSaturationValueMarker_ThemeComponent_ChangesSaturation(string theme)
    {
        ColorPicker picker = LoadThemedPicker(theme);
        ClickSaturationValueMarker(picker);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickHueMarker_ThemeComponent_ChangesHue(string theme)
    {
        ColorPicker picker = LoadThemedPicker(theme);
        ClickHueMarker(picker);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickAwayFromMarkers_ThemeComponent_ChangesColor(string theme)
    {
        ColorPicker picker = LoadThemedPicker(theme);
        picker.SelectedColor = System.Drawing.Color.FromArgb(255, 200, 60, 60);
        Driver.Step();
        float saturation = picker.Saturation;
        float hue = picker.Hue;
        (int x, int y) = Driver.CenterOf("SaturationValueContainer");
        Driver.Click(x - 40, y - 40);
        (int hx, int hy) = Driver.CenterOf("HueContainer");
        Driver.Click(hx, hy + 40);

        picker.Saturation.ShouldNotBe(saturation);
        picker.Hue.ShouldNotBe(hue);
    }

    private void ClickSaturationValueMarker(ColorPicker picker)
    {
        picker.SelectedColor = System.Drawing.Color.FromArgb(255, 200, 60, 60);
        Driver.Step();
        float before = picker.Saturation;
        (int x, int y) = Driver.CenterOf("SaturationValueIndicator");

        Driver.Click(x + 4, y);

        picker.Saturation.ShouldNotBe(before);
    }

    private void ClickHueMarker(ColorPicker picker)
    {
        picker.SelectedColor = System.Drawing.Color.FromArgb(255, 200, 60, 60);
        Driver.Step();
        float before = picker.Hue;
        (int x, int y) = Driver.CenterOf("HueIndicator");

        Driver.Click(x, y + 2);

        picker.Hue.ShouldNotBe(before);
    }

    private ColorPicker LoadThemedPicker(string theme)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string baseType = theme.Length == 0 ? "Controls/ColorPickerStandard" : $"{theme}/Controls/ColorPicker";
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiation("Container", () => new Gum.GueDeriving.ContainerRuntime());
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ScreenSave screen = new ScreenSave { Name = "PickerScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Picker", BaseType = baseType, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        screenVisual.UpdateLayout();
        Driver.Step();
        InteractiveGue pickerVisual = (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Picker")!;
        return (ColorPicker)pickerVisual.FormsControlAsObject;
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
