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
    // SVG tags emitted by SkiaSharp's SKSvgCanvas for the various shape draw calls
    // (DrawRoundRect/DrawRect → rect, DrawPath → path, DrawOval/DrawCircle → ellipse/circle,
    // DrawLine → line). A standard shape type that renders nothing produces none of these.
    private static readonly string[] DrawElementTags =
        { "<rect", "<path", "<ellipse", "<circle", "<polygon", "<line", "<image" };

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
        string svg = ExportScreenWithInstance(
            "Rectangle",
            new VariableSave { Name = "ShapeInstance.X", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Y", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Width", Type = "float", Value = 100f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Height", Type = "float", Value = 80f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.IsFilled", Type = "bool", Value = true, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.CornerRadius", Type = "float", Value = 18f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.FillRed", Type = "int", Value = 0, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.FillGreen", Type = "int", Value = 7, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.FillBlue", Type = "int", Value = 255, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });

        bool hasShape = svg.Contains("<rect") || svg.Contains("<path");
        hasShape.ShouldBeTrue(
            $"Expected the exported SVG to contain a rectangle shape element, but it did not.{Environment.NewLine}SVG:{Environment.NewLine}{svg}");
    }

    // Broader guard for the same bug class as #3259 (a shape standard type silently dropped from
    // SVG export because its runtime was never registered in SkiaGum). These are the v3 default
    // standard types that self-render a visible outline from nothing but a size — so the exported
    // SVG must contain a draw element. (The other v3 standards either render nothing by default —
    // Container — or need content a bare instance lacks: a texture for Sprite/NineSlice, points
    // for Polygon, a string for Text. Legacy shapes like ColoredRectangle/Arc aren't part of a v3
    // project, so they have no base element to resolve here.)
    [Theory]
    [InlineData("Rectangle")]
    [InlineData("Circle")]
    public void ExportSvg_ShapeStandardType_ShouldRenderADrawElement(string baseType)
    {
        string svg = ExportScreenWithInstance(
            baseType,
            new VariableSave { Name = "ShapeInstance.X", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Y", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Width", Type = "float", Value = 100f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Height", Type = "float", Value = 80f, SetsValue = true });

        bool hasDrawElement = DrawElementTags.Any(svg.Contains);
        hasDrawElement.ShouldBeTrue(
            $"Expected the exported SVG for a '{baseType}' instance to contain a draw element, but it did not.{Environment.NewLine}SVG:{Environment.NewLine}{svg}");
    }

    /// <summary>
    /// Builds a v3 project on disk (via <see cref="ProjectCreator"/>, which seeds the standard
    /// elements) with a Screen named "Screen" containing a single instance named "ShapeInstance"
    /// of <paramref name="baseType"/> carrying the given variables, exports it to SVG, asserts the
    /// export succeeded, and returns the SVG file contents.
    /// </summary>
    private string ExportScreenWithInstance(string baseType, params VariableSave[] instanceVariables)
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave instance = new InstanceSave
        {
            Name = "ShapeInstance",
            BaseType = baseType,
            ParentContainer = screen,
        };
        screen.Instances.Add(instance);

        foreach (VariableSave variable in instanceVariables)
        {
            defaultState.Variables.Add(variable);
        }

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = "Screen",
            ElementType = ElementType.Screen,
        });

        project.Save(projectPath, saveElements: true);

        string outputPath = Path.Combine(_tempDirectory, baseType + ".svg");
        SkiaGumSvgExportService service = new SkiaGumSvgExportService();
        SvgExportResult result = service.ExportSvg(new SvgExportRequest
        {
            ProjectPath = projectPath,
            ElementName = "Screen",
            OutputPath = outputPath,
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);

        return File.ReadAllText(outputPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
