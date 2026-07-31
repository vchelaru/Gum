using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.Screenshot;
using Raylib_cs;
using Shouldly;

namespace Gum.ProjectServices.Raylib.Tests;

/// <summary>
/// Tests for <see cref="RaylibScreenshotService"/>, the raylib-backed implementation of
/// <see cref="IScreenshotService"/> that lets <c>gumcli screenshot --backend raylib</c> render the
/// same project MonoGameScreenshotService renders, for cross-runtime pixel comparison (#4174).
/// </summary>
/// <remarks>
/// Only <c>IsFilled</c> is set here, not <c>FillRed</c>/<c>FillGreen</c>/<c>FillBlue</c> — those
/// silently no-op when loaded from a saved project on raylib today (the property dispatcher's
/// <c>TrySetPropertyOnRectangleRuntime</c> is gated <c>#if !RAYLIB</c>, a real cross-runtime gap
/// tracked separately, not something this test should route around by asserting the broken
/// behavior). Asserting on the well-defined default fill (opaque white) instead still proves the
/// full pipeline: project load, layout, transparent-background render-to-texture, and PNG export.
/// </remarks>
public class RaylibScreenshotServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public RaylibScreenshotServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumRaylibScreenshotTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void TakeScreenshot_FilledRectangleScreen_WritesPngWithRectangleInExpectedRegion()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave instance = new InstanceSave
        {
            Name = "RectInstance",
            BaseType = "Rectangle",
            ParentContainer = screen,
        };
        screen.Instances.Add(instance);

        // Covers only the top-left quadrant of the 200x150 render, so a point inside (50,37) and
        // a point clearly outside (150,120) can be distinguished.
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.X", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Y", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Width", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Height", Type = "float", Value = 75f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.IsFilled", Type = "bool", Value = true, SetsValue = true });

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = "Screen",
            ElementType = ElementType.Screen,
        });

        project.Save(projectPath, saveElements: true);

        string outputPath = Path.Combine(_tempDirectory, "Screen.png");
        RaylibScreenshotService service = new RaylibScreenshotService();

        ScreenshotResult result = service.TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
            Width = 200,
            Height = 150,
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);
        File.Exists(outputPath).ShouldBeTrue();

        Image image = Raylib_cs.Raylib.LoadImage(outputPath);
        try
        {
            image.Width.ShouldBe(200);
            image.Height.ShouldBe(150);

            // Default fill for a v3 Rectangle with IsFilled = true and no explicit fill color is
            // opaque white.
            Color inside = Raylib_cs.Raylib.GetImageColor(image, 50, 37);
            inside.R.ShouldBeInRange((byte)245, (byte)255);
            inside.G.ShouldBeInRange((byte)245, (byte)255);
            inside.B.ShouldBeInRange((byte)245, (byte)255);
            inside.A.ShouldBeInRange((byte)245, (byte)255);

            // Outside the rectangle the background clear must stay fully transparent.
            Color outside = Raylib_cs.Raylib.GetImageColor(image, 150, 120);
            outside.A.ShouldBeLessThan((byte)10);
        }
        finally
        {
            Raylib_cs.Raylib.UnloadImage(image);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
