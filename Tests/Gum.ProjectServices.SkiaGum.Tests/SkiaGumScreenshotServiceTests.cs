using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.Screenshot;
using GumRuntime;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.SkiaGum.Tests;

/// <summary>
/// Tests for <see cref="SkiaGumScreenshotService"/>, which backs <c>gumcli screenshot --backend skia</c>
/// for games that draw Gum with Skia (#7965).
/// </summary>
public class SkiaGumScreenshotServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SkiaGumScreenshotServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumSkiaScreenshotTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void TakeScreenshot_FilledRectangleScreen_WritesPngWithRectangleInExpectedRegion()
    {
        string projectPath = CreateProjectWithRedRectangle();
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");

        ScreenshotResult result = new SkiaGumScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
            Width = 200,
            Height = 150,
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);

        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.Width.ShouldBe(200);
        bitmap.Height.ShouldBe(150);

        SKColor inside = bitmap.GetPixel(50, 37);
        inside.Red.ShouldBe((byte)255);
        inside.Green.ShouldBe((byte)0);
        inside.Blue.ShouldBe((byte)0);
        inside.Alpha.ShouldBe((byte)255);

        // Outside the rectangle the default clear stays fully transparent.
        bitmap.GetPixel(150, 120).Alpha.ShouldBe((byte)0);
    }

    // MonoGame and raylib's GumService.Uninitialize clear the process-wide hooks
    // (ElementSaveExtensions.ClearRegistrations), so a Skia render that follows one in the same
    // process must register them again rather than rely on its first initialization.
    [Fact]
    public void TakeScreenshot_AfterAnotherBackendClearedRegistrations_StillRendersElement()
    {
        string projectPath = CreateProjectWithRedRectangle();
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");
        ScreenshotRequest request = new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
            Width = 200,
            Height = 150,
        };
        new SkiaGumScreenshotService().TakeScreenshot(request);

        ElementSaveExtensions.ClearRegistrations();
        ScreenshotResult result = new SkiaGumScreenshotService().TakeScreenshot(request);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.GetPixel(50, 37).ShouldBe(new SKColor(255, 0, 0, 255));
    }

    [Fact]
    public void TakeScreenshot_WithBackgroundColor_ClearsOutsideContentToOpaqueColor()
    {
        string projectPath = CreateProjectWithRedRectangle();
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");

        ScreenshotResult result = new SkiaGumScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
            Width = 200,
            Height = 150,
            BackgroundColor = new ScreenshotColor(0x1E, 0x2A, 0x38, 255),
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);

        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.GetPixel(150, 120).ShouldBe(new SKColor(0x1E, 0x2A, 0x38, 255));
    }

    [Fact]
    public void TakeScreenshot_UnknownElement_Fails()
    {
        string projectPath = CreateProjectWithRedRectangle();

        ScreenshotResult result = new SkiaGumScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Missing",
            OutputPath = Path.Combine(_tempDirectory, "Missing.png"),
        });

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("'Missing' not found");
    }

    private string CreateProjectWithRedRectangle()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        GumProjectSave project = new ProjectCreator().Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        screen.Instances.Add(new InstanceSave
        {
            Name = "RectInstance",
            BaseType = "Rectangle",
            ParentContainer = screen,
        });

        // Covers only the top-left quadrant of the 200x150 render.
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.X", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Y", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Width", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Height", Type = "float", Value = 75f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.IsFilled", Type = "bool", Value = true, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillRed", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillGreen", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillBlue", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Screen", ElementType = ElementType.Screen });
        project.Save(projectPath, saveElements: true);

        return projectPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
