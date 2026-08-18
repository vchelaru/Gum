using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Screenshot;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.MonoGame.Tests;

/// <summary>
/// Real-GraphicsDevice pixel tests for Apos.Shapes-backed rendering, added ahead of the
/// Apos.Shapes 0.7.10 -> latest version bump tracked in issue #4473. These run against the same
/// Mesa llvmpipe software OpenGL CI uses for this project (see build-and-test.yaml), so they
/// exercise the real shader-compile-and-draw path a plain unit test cannot.
/// </summary>
/// <remarks>
/// <see cref="TakeScreenshot_FilledRoundedRectangle_CutsCorners"/> covers the exact shape from
/// #4473's repro (RectangleRuntime, IsFilled + CornerRadius) that MonoGameScreenshotServiceTests
/// only covers for Circle.
///
/// <see cref="TakeScreenshot_Arc_RendersStrokeAtRequestedThickness"/> guards against a specific
/// known risk of upgrading past 0.7.10: Apos.Shapes 0.7.11's changelog says DrawRing's second
/// radius parameter changed meaning from "total thickness" to "half thickness" ("rings come out
/// twice as thick as before"). Arc.cs's DrawRing call was never updated for that change (the
/// version pin in MonoGameGumShapes.csproj exists specifically to avoid it), so bumping the
/// package without also fixing Arc.cs would silently double every Arc/Ring stroke's rendered
/// width. This test measures the actual rendered band width in pixels so that regression can't
/// land unnoticed.
/// </remarks>
public class AposShapeGeometryTests : IDisposable
{
    private readonly string _tempDirectory;

    public AposShapeGeometryTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumAposShapeGeometryTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    // ProjectCreator only seeds core Gum's own standards (Circle, Rectangle, etc - see
    // ProjectCreator.StandardElementNames); "Arc" is shapes-runtime-only, so a headless project
    // needs its own Standards/Arc.gutx on disk before GumService.Initialize will recognize
    // ArcInstance's BaseType on load. Content matches the real template the Gum tool writes.
    private const string ArcStandardTemplate =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <StandardElementSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <Name>Arc</Name>
          <State>
            <Name>Default</Name>
            <Variable><Type>int</Type><Name>Alpha</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Alpha1</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Alpha2</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>Blend</Type><Name>Blend</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Blue</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Blue1</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Blue2</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>DropshadowAlpha</Name><Value xsi:type="xsd:int">255</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>DropshadowBlue</Name><Value xsi:type="xsd:int">0</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>DropshadowBlurX</Name><Value xsi:type="xsd:float">0</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>DropshadowBlurY</Name><Value xsi:type="xsd:float">3</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>DropshadowGreen</Name><Value xsi:type="xsd:int">0</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>DropshadowOffsetX</Name><Value xsi:type="xsd:float">0</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>DropshadowOffsetY</Name><Value xsi:type="xsd:float">3</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>DropshadowRed</Name><Value xsi:type="xsd:int">0</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>ExposeChildrenEvents</Name><Value xsi:type="xsd:boolean">false</Value><Category>Behavior</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientInnerRadius</Name><Value xsi:type="xsd:float">50</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>DimensionUnitType</Type><Name>GradientInnerRadiusUnits</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientOuterRadius</Name><Value xsi:type="xsd:float">100</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>DimensionUnitType</Type><Name>GradientOuterRadiusUnits</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>GradientType</Type><Name>GradientType</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientX1</Name><Value xsi:type="xsd:float">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>GradientX1Units</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientX2</Name><Value xsi:type="xsd:float">100</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>GradientX2Units</Name><Value xsi:type="xsd:int">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientY1</Name><Value xsi:type="xsd:float">0</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>GradientY1Units</Name><Value xsi:type="xsd:int">1</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>GradientY2</Name><Value xsi:type="xsd:float">100</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>GradientY2Units</Name><Value xsi:type="xsd:int">1</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Green</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Green1</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Green2</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>HasDropshadow</Name><Value xsi:type="xsd:boolean">false</Value><Category>Dropshadow</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>HasEvents</Name><Value xsi:type="xsd:boolean">false</Value><Category>Behavior</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>Height</Name><Value xsi:type="xsd:float">100</Value><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>DimensionUnitType</Type><Name>HeightUnits</Name><Value xsi:type="xsd:int">0</Value><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>IgnoredByParentSize</Name><Value xsi:type="xsd:boolean">false</Value><Category>Parent</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>IsEndRounded</Name><Value xsi:type="xsd:boolean">false</Value><Category>Arc</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float?</Type><Name>MaxHeight</Name><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float?</Type><Name>MaxWidth</Name><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float?</Type><Name>MinHeight</Name><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float?</Type><Name>MinWidth</Name><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>string</Type><Name>Parent</Name><Category>Parent</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Red</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Red1</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>int</Type><Name>Red2</Name><Value xsi:type="xsd:int">255</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>StartAngle</Name><Value xsi:type="xsd:float">0</Value><Category>Arc</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>State</Type><Name>State</Name><Value xsi:type="xsd:string">Default</Value><SetsValue>false</SetsValue></Variable>
            <Variable><Type>float</Type><Name>SweepAngle</Name><Value xsi:type="xsd:float">90</Value><Category>Arc</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>Thickness</Name><Value xsi:type="xsd:float">10</Value><Category>Arc</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>UseGradient</Name><Value xsi:type="xsd:boolean">false</Value><Category>Rendering</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>bool</Type><Name>Visible</Name><Value xsi:type="xsd:boolean">true</Value><Category>States and Visibility</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>Width</Name><Value xsi:type="xsd:float">100</Value><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>DimensionUnitType</Type><Name>WidthUnits</Name><Value xsi:type="xsd:int">0</Value><Category>Dimensions</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>X</Name><Value xsi:type="xsd:float">0</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>HorizontalAlignment</Type><Name>XOrigin</Name><Value xsi:type="xsd:int">0</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>XUnits</Name><Value xsi:type="xsd:int">0</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>float</Type><Name>Y</Name><Value xsi:type="xsd:float">0</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>VerticalAlignment</Type><Name>YOrigin</Name><Value xsi:type="xsd:int">0</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <Variable><Type>PositionUnitType</Type><Name>YUnits</Name><Value xsi:type="xsd:int">1</Value><Category>Position</Category><SetsValue>true</SetsValue></Variable>
            <VariableList xsi:type="VariableListSaveOfString">
              <Type>string</Type>
              <Name>VariableReferences</Name>
              <Category>References</Category>
              <IsFile>false</IsFile>
              <IsHiddenInPropertyGrid>false</IsHiddenInPropertyGrid>
              <Value />
            </VariableList>
          </State>
          <Behaviors />
        </StandardElementSave>
        """;

    private static void WriteArcStandardTemplate(string projectDirectory)
    {
        string standardsDir = Path.Combine(projectDirectory, "Standards");
        Directory.CreateDirectory(standardsDir);
        File.WriteAllText(Path.Combine(standardsDir, "Arc.gutx"), ArcStandardTemplate);
    }

    [Fact]
    public void TakeScreenshot_Arc_RendersStrokeAtRequestedThickness()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);
        WriteArcStandardTemplate(_tempDirectory);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave instance = new InstanceSave
        {
            Name = "ArcInstance",
            BaseType = "Arc",
            ParentContainer = screen,
        };
        screen.Instances.Add(instance);

        // StartAngle/SweepAngle straddle angle 0 (the +X direction from center) so the horizontal
        // scanline through the center cuts the stroke band where it's aligned with the X axis,
        // away from the arc's end caps.
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.X", Type = "float", Value = 20f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Y", Type = "float", Value = 20f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Width", Type = "float", Value = 160f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Height", Type = "float", Value = 160f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Thickness", Type = "float", Value = 16f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.StartAngle", Type = "float", Value = -10f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.SweepAngle", Type = "float", Value = 20f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.IsEndRounded", Type = "bool", Value = false, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Red", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Green", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Blue", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "ArcInstance.Alpha", Type = "int", Value = 255, SetsValue = true });

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = "Screen",
            ElementType = ElementType.Screen,
        });
        // ProjectCreator only seeds core Gum's own standards (Circle, Rectangle, etc); "Arc" is
        // shapes-runtime-only, so it must be registered explicitly for a headless project to
        // recognize ArcInstance's BaseType on load.
        project.StandardElementReferences.Add(new ElementReference
        {
            Name = "Arc",
            ElementType = ElementType.Standard,
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
            Height = 200,
        });

        result.Success.ShouldBeTrue(result.ErrorMessage);

        using SKBitmap bitmap = SKBitmap.Decode(outputPath);

        int bandStart = -1;
        int bandEnd = -1;
        for (int x = 140; x < 200; x++)
        {
            bool isOpaque = bitmap.GetPixel(x, 100).Alpha > 128;
            if (isOpaque && bandStart == -1)
            {
                bandStart = x;
            }
            if (isOpaque)
            {
                bandEnd = x;
            }
        }

        bandStart.ShouldBeGreaterThan(-1, "expected an opaque stroke band along the scanline but found none");

        int bandWidth = bandEnd - bandStart + 1;
        // Allows slack for antialiasing around the requested Thickness (16), while staying well
        // under double that -- what 0.7.11's "half thickness" DrawRing semantics would produce.
        bandWidth.ShouldBeInRange(10, 22);
    }

    [Fact]
    public void TakeScreenshot_FilledRoundedRectangle_CutsCorners()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave instance = new InstanceSave
        {
            Name = "RectangleInstance",
            BaseType = "Rectangle",
            ParentContainer = screen,
        };
        screen.Instances.Add(instance);

        // Same shape as #4473's repro: IsFilled + CornerRadius on a 100x100 RectangleRuntime.
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.X", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Y", Type = "float", Value = 0f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Width", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Height", Type = "float", Value = 100f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.IsFilled", Type = "bool", Value = true, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.CornerRadius", Type = "float", Value = 10f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillRed", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillGreen", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillBlue", Type = "int", Value = 0, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.FillAlpha", Type = "int", Value = 255, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.StrokeWidth", Type = "float", Value = 0f, SetsValue = true });

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

        // Center of the rectangle: opaque fill.
        SKColor center = bitmap.GetPixel(50, 50);
        center.Red.ShouldBeInRange((byte)245, (byte)255);
        center.Green.ShouldBeLessThan((byte)10);
        center.Blue.ShouldBeLessThan((byte)10);
        center.Alpha.ShouldBeInRange((byte)245, (byte)255);

        // Edge midpoint, away from any corner: still square here, so still opaque fill.
        SKColor edgeMidpoint = bitmap.GetPixel(50, 1);
        edgeMidpoint.Alpha.ShouldBeInRange((byte)245, (byte)255);

        // Tip of the top-left corner: CornerRadius=10 cuts this away, so it must be background
        // (transparent), not fill color. If corner rounding regresses back to a square rect this
        // pixel goes opaque and catches it.
        SKColor cornerTip = bitmap.GetPixel(1, 1);
        cornerTip.Alpha.ShouldBeLessThan((byte)10);

        // Fully outside the rectangle: background clear must stay fully transparent.
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
