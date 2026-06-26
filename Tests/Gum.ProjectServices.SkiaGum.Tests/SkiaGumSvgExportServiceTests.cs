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
    // SVG export because its runtime was never registered in SkiaGum). These are the shape standard
    // types that ProjectCreator seeds into a v3 project AND that self-render a visible outline from
    // nothing but a size — so the exported SVG must contain a draw element. (Arc/ColoredCircle/
    // RoundedRectangle are current shapes too, but not part of a v3 seed — see the Arc test below
    // for how a project that uses one carries its own standard. Container renders nothing by
    // default; Sprite/NineSlice/Polygon/Text need content a bare instance lacks.)
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

    // Arc is a current shapes-library standard type (NOT deprecated — only ColoredRectangle is, per
    // StandardElementsManager._deprecatedStandardTypeNames). Unlike Rectangle/Circle it is not part
    // of ProjectCreator's v3 seed, so a project that uses an Arc ships its own Arc standard element.
    // Seed one with GetArcState's real defaults (Thickness 10, StartAngle 0, SweepAngle 90) and
    // confirm the Arc renders through SkiaGum's SVG export — i.e. its runtime is registered like
    // Rectangle's now is. (A bare Arc instance with no seeded standard exports nothing, but that is
    // a missing-base-element fixture condition, not a render gap.)
    [Fact]
    public void ExportSvg_ArcInstance_WithSeededStandard_ShouldRenderADrawElement()
    {
        StandardElementSave arcStandard = new StandardElementSave { Name = "Arc" };
        StateSave arcState = new StateSave { Name = "Default", ParentContainer = arcStandard };
        arcStandard.States.Add(arcState);
        arcState.Variables.Add(new VariableSave { Name = "Thickness", Type = "float", Value = 10f, SetsValue = true });
        arcState.Variables.Add(new VariableSave { Name = "StartAngle", Type = "float", Value = 0f, SetsValue = true });
        arcState.Variables.Add(new VariableSave { Name = "SweepAngle", Type = "float", Value = 90f, SetsValue = true });

        string svg = ExportScreenWithInstance(
            "Arc",
            new[] { arcStandard },
            new VariableSave { Name = "ShapeInstance.X", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Y", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Width", Type = "float", Value = 100f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Height", Type = "float", Value = 80f, SetsValue = true });

        bool hasDrawElement = DrawElementTags.Any(svg.Contains);
        hasDrawElement.ShouldBeTrue(
            $"Expected the exported SVG for an Arc instance to contain a draw element, but it did not.{Environment.NewLine}SVG:{Environment.NewLine}{svg}");
    }

    // Regression test for issue #3324: SVG export threw
    // "Could not get the default state for type Line" whenever the project contained a Line
    // standard element, because SkiaGum's SystemManagers wired Arc/ColoredCircle/RoundedRectangle/
    // Canvas/Svg/LottieAnimation into CustomGetDefaultState but omitted Line. Like Arc, Line is not
    // part of ProjectCreator's v3 seed, so a project that uses one ships its own Line standard. Seed
    // one with GetLineState's real defaults (IsRounded false, StrokeWidth 2) and confirm the Line
    // both exports without throwing AND renders a draw element (its runtime is now registered, the
    // same #3259-class "silently dropped" gap that the Rectangle/Arc cases above guard against).
    [Fact]
    public void ExportSvg_LineInstance_WithSeededStandard_ShouldRenderADrawElement()
    {
        StandardElementSave lineStandard = new StandardElementSave { Name = "Line" };
        StateSave lineState = new StateSave { Name = "Default", ParentContainer = lineStandard };
        lineStandard.States.Add(lineState);
        lineState.Variables.Add(new VariableSave { Name = "IsRounded", Type = "bool", Value = false, SetsValue = true });
        lineState.Variables.Add(new VariableSave { Name = "StrokeWidth", Type = "float", Value = 2f, SetsValue = true });

        string svg = ExportScreenWithInstance(
            "Line",
            new[] { lineStandard },
            new VariableSave { Name = "ShapeInstance.X", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Y", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Width", Type = "float", Value = 100f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Height", Type = "float", Value = 80f, SetsValue = true });

        bool hasDrawElement = DrawElementTags.Any(svg.Contains);
        hasDrawElement.ShouldBeTrue(
            $"Expected the exported SVG for a Line instance to contain a draw element, but it did not.{Environment.NewLine}SVG:{Environment.NewLine}{svg}");
    }

    // Full coverage for issue #3324 across EVERY extended standard type — the shapes
    // (Arc/ColoredCircle/RoundedRectangle/Line) and the Skia-only types (Canvas/Svg/LottieAnimation).
    // None are in ProjectCreator's v3 seed, so each is seeded as its own standard, which is the exact
    // condition that triggered the crash: GumProjectSave.Initialize calls GetDefaultStateFor for every
    // standard in the project, and any type missing from the SkiaGum SystemManagers' CustomGetDefaultState
    // switch threw "Could not get the default state for type X", failing the whole export. This pins all
    // seven against that regression. (Export-success is the uniform assertion: Canvas/Svg/LottieAnimation
    // render nothing without children or a SourceFile; per-shape render coverage lives in the tests above.)
    [Theory]
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    [InlineData("Line")]
    [InlineData("Canvas")]
    [InlineData("Svg")]
    [InlineData("LottieAnimation")]
    public void ExportSvg_ExtendedStandardType_WithSeededStandard_ShouldExportWithoutThrowing(string baseType)
    {
        StandardElementSave standard = new StandardElementSave { Name = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = standard };
        standard.States.Add(defaultState);
        defaultState.Variables.Add(new VariableSave { Name = "Visible", Type = "bool", Value = true, SetsValue = true });

        // ExportScreenWithInstance asserts result.Success internally; before the fix this threw
        // InvalidOperationException for any extended type absent from the SystemManagers switch.
        string svg = ExportScreenWithInstance(
            baseType,
            new[] { standard },
            new VariableSave { Name = "ShapeInstance.X", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Y", Type = "float", Value = 10f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Width", Type = "float", Value = 100f, SetsValue = true },
            new VariableSave { Name = "ShapeInstance.Height", Type = "float", Value = 80f, SetsValue = true });

        svg.ShouldNotBeNullOrEmpty();
    }

    private string ExportScreenWithInstance(string baseType, params VariableSave[] instanceVariables) =>
        ExportScreenWithInstance(baseType, standardsToSeed: null, instanceVariables);

    /// <summary>
    /// Builds a v3 project on disk (via <see cref="ProjectCreator"/>, which seeds the standard
    /// elements) plus any extra <paramref name="standardsToSeed"/>, with a Screen named "Screen"
    /// containing a single instance named "ShapeInstance" of <paramref name="baseType"/> carrying
    /// the given variables, exports it to SVG, asserts the export succeeded, and returns the SVG
    /// file contents.
    /// </summary>
    private string ExportScreenWithInstance(
        string baseType,
        IEnumerable<StandardElementSave>? standardsToSeed,
        params VariableSave[] instanceVariables)
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        if (standardsToSeed != null)
        {
            foreach (StandardElementSave standard in standardsToSeed)
            {
                project.StandardElements.Add(standard);
                project.StandardElementReferences.Add(new ElementReference
                {
                    Name = standard.Name,
                    ElementType = ElementType.Standard,
                });
            }
        }

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
