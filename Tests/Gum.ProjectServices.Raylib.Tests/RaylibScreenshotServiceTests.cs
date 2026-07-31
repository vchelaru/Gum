using Gum.Converters;
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
/// <see cref="TakeScreenshot_FilledRectangleScreen_WritesPngWithRectangleInExpectedRegion"/> also
/// pins the fix for #4176: <c>FillRed</c>/<c>FillGreen</c>/<c>FillBlue</c> loaded from a saved
/// project used to silently no-op on raylib (the property dispatcher's
/// <c>TrySetPropertyOnRectangleRuntime</c> was gated <c>#if !RAYLIB</c>, so a Rectangle with a
/// custom fill color rendered with its default white fill instead — indistinguishable from a real
/// rendering bug when only comparing against MonoGame's correct output).
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
        // Also pins #4176's fix: FillRed/FillGreen/FillBlue must apply, not just IsFilled's default.
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
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillRed", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillGreen", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillBlue", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });

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

            Color inside = Raylib_cs.Raylib.GetImageColor(image, 50, 37);
            inside.R.ShouldBeInRange((byte)245, (byte)255);
            inside.G.ShouldBeLessThan((byte)10);
            inside.B.ShouldBeLessThan((byte)10);
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

    // Regression test for #4174's Airpig triage: dialogs positioned via PixelsFromMiddle (the unit
    // a parentless element's X/Y resolve "the middle" against GraphicalUiElement.CanvasWidth/
    // CanvasHeight) rendered pinned to the top-left corner on raylib instead of centered, because
    // RaylibScreenshotService never set CanvasWidth/CanvasHeight the way MonoGameScreenshotService
    // does — so the "middle" it centered against was 0, not the requested render size.
    [Fact]
    public void TakeScreenshot_ElementCenteredViaPixelsFromMiddle_RendersAtCanvasCenter()
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

        // X/Y = 0 with PixelsFromMiddle centers the 50x50 rectangle on whatever CanvasWidth/
        // CanvasHeight resolve to at render time.
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.X", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.XUnits", Type = "GeneralUnitType", Value = GeneralUnitType.PixelsFromMiddle, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Y", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.YUnits", Type = "GeneralUnitType", Value = GeneralUnitType.PixelsFromMiddle, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Width", Type = "float", Value = 50f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectInstance.Height", Type = "float", Value = 50f, SetsValue = true });
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

        Image image = Raylib_cs.Raylib.LoadImage(outputPath);
        try
        {
            // The 50x50 rectangle centered on a 200x150 canvas spans x:[75,125], y:[50,100] —
            // its own center (100, 75) must be filled...
            Color center = Raylib_cs.Raylib.GetImageColor(image, 100, 75);
            center.A.ShouldBeGreaterThan((byte)245);

            // ...and the corner, which the bug pinned the rectangle to, must NOT be.
            Color corner = Raylib_cs.Raylib.GetImageColor(image, 5, 5);
            corner.A.ShouldBeLessThan((byte)10);
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
