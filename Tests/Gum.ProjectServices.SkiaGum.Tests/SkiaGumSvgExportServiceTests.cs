using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.SvgExport;
using Shouldly;

namespace Gum.ProjectServices.SkiaGum.Tests;

/// <summary>
/// Tests for <see cref="SkiaGumSvgExportService"/>, the SkiaGum-backed implementation of
/// <see cref="ISvgExportService"/> that <c>gumcli svg</c> and the tool's File ▸ Export use.
/// </summary>
public class SkiaGumSvgExportServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SkiaGumSvgExportServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumSvgExportTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    // Regression test for issue #3259: a Rectangle (filled rounded rectangle) instance was
    // dropped entirely from the exported SVG because SkiaGum never registered a runtime for
    // the "Rectangle" standard base type, so no renderable was created to draw.
    [Fact]
    public void ExportSvg_FilledRectangleScreen_ShouldContainShapeElement()
    {
        string projectPath = CreateProjectWithRectangleScreen("RectScreen");
        string outputPath = Path.Combine(_tempDirectory, "rect.svg");

        SkiaGumSvgExportService service = new SkiaGumSvgExportService();
        SvgExportResult result = service.ExportSvg(new SvgExportRequest
        {
            ProjectPath = projectPath,
            ElementName = "RectScreen",
            OutputPath = outputPath,
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);

        string svg = File.ReadAllText(outputPath);
        bool hasShape = svg.Contains("<rect") || svg.Contains("<path");
        hasShape.ShouldBeTrue(
            $"Expected the exported SVG to contain a rectangle shape element, but it did not.{Environment.NewLine}SVG:{Environment.NewLine}{svg}");
    }

    /// <summary>
    /// Builds a v3 project on disk (via <see cref="ProjectCreator"/>, which seeds the standard
    /// elements including "Rectangle") and adds a Screen containing a single filled, rounded,
    /// blue Rectangle instance. Returns the .gumx path.
    /// </summary>
    private string CreateProjectWithRectangleScreen(string screenName)
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = screenName };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave rectangle = new InstanceSave
        {
            Name = "RectangleInstance",
            BaseType = "Rectangle",
            ParentContainer = screen,
        };
        screen.Instances.Add(rectangle);

        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.X", Type = "float", Value = 10f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Y", Type = "float", Value = 10f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Width", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Height", Type = "float", Value = 80f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.IsFilled", Type = "bool", Value = true, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.CornerRadius", Type = "float", Value = 18f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillRed", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillGreen", Type = "int", Value = 7, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillBlue", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = screenName,
            ElementType = ElementType.Screen,
        });

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
