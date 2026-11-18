using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Cli.Tests.EndToEnd;
using Shouldly;
using SkiaSharp;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli screenshot --scale</c> (#8246) and <c>--set</c> (#8247), rendered through the headless skia backend.
/// Renders run gumcli as its own process: an in-process render builds its visuals from whatever runtime
/// types another backend's test registered earlier in this process.
/// </summary>
public class ScreenshotScaleAndOverrideTests : BaseTestClass
{
    [Fact]
    public void Screenshot_WithScale_UpscalesWithNearestNeighbor()
    {
        string projectPath = CreatePanelProject(withReference: false);
        string outputPath = Path.Combine(TempDirectory, "Panel.png");

        GumCliProcess result = GumCliProcess.Run(TempDirectory,
            "screenshot", projectPath, "Panel", "--output", outputPath, "--backend", "skia",
            "--background", "000000", "--width", "40", "--height", "16", "--scale", "3");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.Width.ShouldBe(120);
        bitmap.Height.ShouldBe(48);
        // The 32-wide fill ends at source column 31, which covers output columns 93 to 95.
        bitmap.GetPixel(95, 47).ShouldBe(SKColors.White);
        bitmap.GetPixel(96, 0).ShouldBe(SKColors.Black);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("1.5")]
    [InlineData("big")]
    public void Screenshot_WithInvalidScale_ReturnsExitCode2(string scale)
    {
        string projectPath = CreatePanelProject(withReference: false);

        CliTestHelper result = CliTestHelper.Run(
            "screenshot", projectPath, "Panel", "--backend", "skia", "--scale", scale);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain($"Invalid --scale value '{scale}'");
    }

    [Theory]
    [InlineData("Panel", "Width=50", 50)]
    [InlineData("Screen", "Panel.Width=50", 50)]
    [InlineData("Panel", "SizeState=Big", 80)]
    [InlineData("Screen", "Panel.SizeState=Big", 80)]
    [InlineData("FrameScreen", "Frame.Inner.SizeState=Big", 80)]
    [InlineData("FrameScreen", "Frame.Inner.Fill.Width=8", 40)]
    public void Screenshot_WithSet_RendersOverriddenValueWithoutSaving(string element, string set, int expectedWidth)
    {
        string projectPath = CreatePanelProject(withReference: false);
        Dictionary<string, string> filesBefore = ReadProjectFiles();

        GumCliProcess result = RenderWithSets(projectPath, element, out SKBitmap bitmap, set);

        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            bitmap.GetPixel(expectedWidth - 2, 8).ShouldBe(SKColors.White);
            bitmap.GetPixel(expectedWidth + 2, 8).ShouldBe(SKColors.Black);
        }
        ReadProjectFiles().ShouldBe(filesBefore);
    }

    [Theory]
    [InlineData("Screen", "Panel.Fill.Visible=false")]
    [InlineData("Frame", "Inner.Fill.Visible=false")]
    [InlineData("FrameScreen", "Frame.Inner.Fill.Visible=false")]
    public void Screenshot_WithSetThroughNestedInstances_RendersOverriddenValueWithoutSaving(string element, string set)
    {
        string projectPath = CreatePanelProject(withReference: false);
        Dictionary<string, string> filesBefore = ReadProjectFiles();

        GumCliProcess plain = RenderWithSets(projectPath, element, out SKBitmap plainBitmap);
        using (plainBitmap)
        {
            plain.ExitCode.ShouldBe(0, plain.StandardError);
            plainBitmap.GetPixel(10, 8).ShouldBe(SKColors.White);
        }
        GumCliProcess result = RenderWithSets(projectPath, element, out SKBitmap bitmap, set);
        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            bitmap.GetPixel(10, 8).ShouldBe(SKColors.Black);
        }
        ReadProjectFiles().ShouldBe(filesBefore);
    }

    [Theory]
    [InlineData("Frame.Missing.Fill.Visible=false", "Frame has no instance named Missing.")]
    [InlineData("Frame.Inner.Missing.Visible=false", "Panel has no instance named Missing.")]
    [InlineData("Frame.Inner.Fill.Bogus=5", "Rectangle has no variable named Bogus, so Frame.Inner.Fill.Bogus cannot be set.")]
    [InlineData("Frame.Inner.Bogus=5", "Panel has no variable named Bogus, so Frame.Inner.Bogus cannot be set.")]
    [InlineData("Frame.Inner.SizeState=Huge", "Huge is not a state of category Size, so Frame.Inner.SizeState cannot be set.")]
    public void Screenshot_WithNestedSetThatCannotApply_NamesTheBadSegment(string set, string expectedError)
    {
        string projectPath = CreatePanelProject(withReference: false);

        GumCliProcess result = RenderWithSets(projectPath, "FrameScreen", out SKBitmap? bitmap, set);

        bitmap?.Dispose();
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain($"--set {set}: {expectedError}");
    }

    [Fact]
    public void Screenshot_WithSetOnReferencedVariable_UpdatesReferencingVariable()
    {
        string projectPath = CreatePanelProject(withReference: true);

        GumCliProcess result = RenderWithSets(projectPath, "Panel", out SKBitmap bitmap, "BarWidth=60");

        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            // Width = BarWidth, so the stored width of 32 follows BarWidth to 60.
            bitmap.GetPixel(58, 8).ShouldBe(SKColors.White);
            bitmap.GetPixel(62, 8).ShouldBe(SKColors.Black);
        }
    }

    [Fact]
    public void Screenshot_WithSetNumberOnStringVariable_AppliesItAsText()
    {
        string projectPath = CreatePanelProject(withReference: true, label: "");
        GumCliProcess overridden = RenderWithSets(projectPath, "Panel", out SKBitmap overriddenBitmap, "Label=123");
        GumCliProcess empty = RenderWithSets(projectPath, "Panel", out SKBitmap emptyBitmap);
        CreatePanelProject(withReference: true, label: "123");
        GumCliProcess stored = RenderWithSets(projectPath, "Panel", out SKBitmap storedBitmap);

        using (overriddenBitmap)
        using (emptyBitmap)
        using (storedBitmap)
        {
            overridden.ExitCode.ShouldBe(0, overridden.StandardError);
            empty.ExitCode.ShouldBe(0, empty.StandardError);
            stored.ExitCode.ShouldBe(0, stored.StandardError);
            // Label is exposed as Caption.Text, so the override must draw the same text a stored "123" draws.
            overriddenBitmap.Bytes.ShouldBe(storedBitmap.Bytes);
            overriddenBitmap.Bytes.ShouldNotBe(emptyBitmap.Bytes);
        }
    }

    [Theory]
    [InlineData("Missing.Width=5", "Panel has no instance named Missing.")]
    [InlineData("Bogus=5", "Panel has no variable named Bogus.")]
    [InlineData("Fill.Bogus=5", "Rectangle has no variable named Bogus, so Fill.Bogus cannot be set.")]
    [InlineData("Width=wide", "\"wide\" is not a valid float value for Width.")]
    [InlineData("SizeState=Huge", "Huge is not a state of category Size, so SizeState cannot be set.")]
    [InlineData("State=Typo", "Panel has no uncategorized state named Typo, so State cannot be set.")]
    public void Screenshot_WithSetThatCannotApply_ReturnsExitCode1(string set, string expectedError)
    {
        string projectPath = CreatePanelProject(withReference: false);

        GumCliProcess result = RenderWithSets(projectPath, "Panel", out SKBitmap? bitmap, set);

        bitmap?.Dispose();
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain($"--set {set}: {expectedError}");
    }

    [Fact]
    public void Screenshot_WithSetMissingEquals_ReturnsExitCode2()
    {
        string projectPath = CreatePanelProject(withReference: false);

        CliTestHelper result = CliTestHelper.Run(
            "screenshot", projectPath, "Panel", "--backend", "skia", "--set", "Width");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Invalid --set value 'Width'");
    }

    private GumCliProcess RenderWithSets(string projectPath, string element, out SKBitmap bitmap, params string[] sets)
    {
        string outputPath = Path.Combine(TempDirectory, "Render.png");
        List<string> args = new List<string>
        {
            "screenshot", projectPath, element, "--output", outputPath,
            "--backend", "skia", "--background", "000000", "--width", "128", "--height", "16",
        };
        foreach (string set in sets)
        {
            args.Add("--set");
            args.Add(set);
        }

        GumCliProcess result = GumCliProcess.Run(TempDirectory, args.ToArray());
        bitmap = File.Exists(outputPath) ? SKBitmap.Decode(outputPath) : null!;
        return result;
    }

    private Dictionary<string, string> ReadProjectFiles() =>
        Directory.GetFiles(TempDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".png"))
            .ToDictionary(path => path, path => System.Convert.ToBase64String(File.ReadAllBytes(path)));

    /// <summary>
    /// Builds component Panel (Width 32, Height 16, a white filled Rectangle Fill that fills its width, and a
    /// category Size whose state Big sets Width to 80) and a Screen with a Panel instance. With
    /// <paramref name="withReference"/>, Panel also has a float custom variable BarWidth = 32 with the reference
    /// <c>Width = BarWidth</c>, and a Text instance Caption at X 64 whose Text is exposed as Label, set to
    /// <paramref name="label"/>. Component Frame holds a Panel instance Inner, and Screen FrameScreen holds a Frame
    /// instance Frame, for paths through nested instances.
    /// </summary>
    private string CreatePanelProject(bool withReference, string label = "")
    {
        string projectPath = Path.Combine(TempDirectory, "Project.gumx");
        GumProjectSave project = new ProjectCreator().Create(projectPath);

        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave panelState = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(panelState);
        panel.Instances.Add(new InstanceSave { Name = "Fill", BaseType = "Rectangle", ParentContainer = panel });
        panelState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 32f, SetsValue = true });
        panelState.Variables.Add(new VariableSave { Name = "Height", Type = "float", Value = 16f, SetsValue = true });
        panelState.Variables.Add(new VariableSave { Name = "Fill.Width", Type = "float", Value = 0f, SetsValue = true });
        panelState.Variables.Add(new VariableSave
        {
            Name = "Fill.WidthUnits", Type = "DimensionUnitType", Value = DimensionUnitType.RelativeToParent, SetsValue = true,
        });
        panelState.Variables.Add(new VariableSave { Name = "Fill.Height", Type = "float", Value = 16f, SetsValue = true });
        panelState.Variables.Add(new VariableSave { Name = "Fill.IsFilled", Type = "bool", Value = true, SetsValue = true });
        if (withReference)
        {
            panelState.Variables.Add(new VariableSave
            {
                Name = "BarWidth", Type = "float", Value = 32f, IsCustomVariable = true, SetsValue = true,
            });
            panel.Instances.Add(new InstanceSave { Name = "Caption", BaseType = "Text", ParentContainer = panel });
            panelState.Variables.Add(new VariableSave { Name = "Caption.X", Type = "float", Value = 64f, SetsValue = true });
            panelState.Variables.Add(new VariableSave
            {
                Name = "Caption.Text", Type = "string", Value = label, ExposedAsName = "Label", SetsValue = true,
            });
            panelState.VariableLists.Add(new VariableListSave<string>
            {
                Name = "VariableReferences", Type = "string", Value = new List<string> { "Width = BarWidth" },
            });
        }

        StateSaveCategory size = new StateSaveCategory { Name = "Size" };
        StateSave big = new StateSave { Name = "Big", ParentContainer = panel };
        big.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 80f, SetsValue = true });
        size.States.Add(big);
        panel.Categories.Add(size);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave screenState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenState);
        screen.Instances.Add(new InstanceSave { Name = "Panel", BaseType = "Panel", ParentContainer = screen });

        ComponentSave frame = new ComponentSave { Name = "Frame", BaseType = "Container" };
        frame.States.Add(new StateSave { Name = "Default", ParentContainer = frame });
        frame.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Panel", ParentContainer = frame });

        ScreenSave frameScreen = new ScreenSave { Name = "FrameScreen" };
        frameScreen.States.Add(new StateSave { Name = "Default", ParentContainer = frameScreen });
        frameScreen.Instances.Add(new InstanceSave { Name = "Frame", BaseType = "Frame", ParentContainer = frameScreen });

        foreach (ComponentSave component in new[] { panel, frame })
        {
            project.Components.Add(component);
            project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        foreach (ScreenSave screenSave in new[] { screen, frameScreen })
        {
            project.Screens.Add(screenSave);
            project.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
        }
        project.Save(projectPath, saveElements: true);

        return projectPath;
    }
}
