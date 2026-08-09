using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Screenshot;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.MonoGame.Tests;

/// <summary>
/// Tests for <see cref="MonoGameScreenshotService"/>, the monogame-backed (default) implementation
/// of <see cref="IScreenshotService"/> behind <c>gumcli screenshot</c>.
/// </summary>
/// <remarks>
/// <see cref="TakeScreenshot_FilledCircleScreen_WritesPngWithFillColorInExpectedRegion"/> pins the
/// fix for #4403: Circle instances (Apos.Shapes-backed on the monogame runtime) rendered with the
/// renderable's construction defaults — transparent fill, 1px white stroke — instead of the state's
/// actual fill/stroke, because <c>MonoGameScreenshotService</c> never called
/// <c>ShapeRenderer.Self.Initialize()</c>. Rectangle instances were unaffected since a plain
/// (non-rounded) Rectangle doesn't route through Apos.Shapes.
/// </remarks>
public class MonoGameScreenshotServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public MonoGameScreenshotServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumMonoGameScreenshotTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void TakeScreenshot_FilledCircleScreen_WritesPngWithFillColorInExpectedRegion()
    {
        // Self-gated like ScreenshotCommandTests' raylib tests: Apos.Shapes' embedded shader
        // throws "Shader Compilation Failed" under the Windows CI runner's Mesa llvmpipe software
        // GL (this step's own GALLIUM_DRIVER=llvmpipe), even with GraphicsProfile.HiDef set. Real
        // Windows GPU drivers render this correctly (verified locally) - see #4410, tracking the
        // Mesa incompatibility separately from this test.
        if (Environment.GetEnvironmentVariable("GALLIUM_DRIVER") == "llvmpipe")
        {
            return;
        }

        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave instance = new InstanceSave
        {
            Name = "CircleInstance",
            BaseType = "Circle",
            ParentContainer = screen,
        };
        screen.Instances.Add(instance);

        // Covers only the top-left quadrant of the 200x150 render, so a point inside the circle's
        // fill (50,50) and a point clearly outside (150,120) can be distinguished.
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.X", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.Y", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.Width", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.Height", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.IsFilled", Type = "bool", Value = true, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.FillRed", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.FillGreen", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.FillBlue", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "CircleInstance.StrokeWidth", Type = "float", Value = 0f, SetsValue = true });

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = "Screen",
            ElementType = ElementType.Screen,
        });

        project.Save(projectPath, saveElements: true);

        string outputPath = Path.Combine(_tempDirectory, "Screen.png");
        MonoGameScreenshotService service = new MonoGameScreenshotService();

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

        using SKBitmap bitmap = SKBitmap.Decode(outputPath);
        bitmap.Width.ShouldBe(200);
        bitmap.Height.ShouldBe(150);

        SKColor inside = bitmap.GetPixel(50, 50);
        inside.Red.ShouldBeInRange((byte)245, (byte)255);
        inside.Green.ShouldBeLessThan((byte)10);
        inside.Blue.ShouldBeLessThan((byte)10);
        inside.Alpha.ShouldBeInRange((byte)245, (byte)255);

        // Outside the circle the background clear must stay fully transparent.
        SKColor outside = bitmap.GetPixel(150, 120);
        outside.Alpha.ShouldBeLessThan((byte)10);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
