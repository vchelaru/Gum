using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GumToolUnitTests.DataTypes;

/// <summary>
/// Pins the version-gated standard-element back-fill (FlatRedBall issue #1881). Opening a project
/// runs <see cref="GumProjectSaveExtensionMethods.Initialize"/>, which back-fills each standard
/// element with the tool's current default variables. That back-fill used to be version-unaware, so
/// opening an old project injected the v3 shape surface (gradient / dropshadow / blend / fill+stroke
/// channels) plus newer per-standard variables (RenderTargetTextureSource, SourceShaderFile,
/// IsTilingMiddleSections) into its standard <c>.gutx</c> files and re-saved them. Because FRB1
/// generates its own runtime classes from those standard elements, the regenerated runtime then
/// referenced properties the project's pinned (older) Gum runtime doesn't have — a wall of CS0246.
/// These tests assert a pre-v3 project is left byte-stable while a v3 project still gets the surface.
/// </summary>
public class StandardElementBackfillVersionGateTests : BaseTestClass
{
    private const int PreV3 = (int)GumProjectSave.GumxVersions.AttributeVersion;     // 2
    private const int V3 = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;  // 3

    // Variables added to the plain Circle / Rectangle in the v3 shape-variable expansion
    // (#2929/#2931/#2950). None existed on a pre-v3 shape, and none exist on the older FRB-pinned
    // runtime, so injecting any of them into a pre-v3 project breaks that project's generated code.
    private static readonly string[] V3ShapeVariables =
    {
        // Fill + stroke channels (AddFillAndStrokeVariables)
        "IsFilled", "FillRed", "FillGreen", "FillBlue", "FillAlpha",
        "StrokeWidth", "StrokeRed", "StrokeGreen", "StrokeBlue", "StrokeAlpha",
        // Gradient (AddGradientVariables)
        "UseGradient", "GradientType",
        "GradientX1", "GradientX1Units", "GradientY1", "GradientY1Units",
        "GradientX2", "GradientX2Units", "GradientY2", "GradientY2Units",
        "GradientInnerRadius", "GradientInnerRadiusUnits",
        "GradientOuterRadius", "GradientOuterRadiusUnits",
        "Red2", "Green2", "Blue2", "Alpha2",
        // Dropshadow (AddDropshadowVariables)
        "HasDropshadow", "DropshadowOffsetX", "DropshadowOffsetY", "DropshadowBlur",
        "DropshadowAlpha", "DropshadowRed", "DropshadowGreen", "DropshadowBlue",
        // Blend (AddBlendVariable)
        "Blend",
    };

    // A standard element as an older tool would have saved it: a Default state holding only the
    // pre-v3 surface for that type. The exact classic variables don't matter to these tests (the
    // back-fill adds the rest of the non-gated surface anyway) — what matters is that the gated
    // variables are absent to start, so we can assert the load-time back-fill does not add them.
    private static GumProjectSave MakeProjectWithBareStandard(string standardName, int version,
        params string[] classicVariableNames)
    {
        var variables = classicVariableNames
            .Select(name => new VariableSave { Name = name, Type = "float", Value = 0f, SetsValue = true })
            .ToList();

        var standard = new StandardElementSave
        {
            Name = standardName,
            States = new List<StateSave> { new StateSave { Name = "Default", Variables = variables } }
        };

        var project = new GumProjectSave { Version = version };
        project.StandardElements.Add(standard);
        return project;
    }

    private static StateSave DefaultStateAfterInitialize(GumProjectSave project, string standardName)
    {
        project.Initialize();
        return project.StandardElements.Single(e => e.Name == standardName).DefaultState;
    }

    [Fact]
    public void Initialize_DoesNotInjectV3ShapeVariables_IntoPreV3CircleStandard()
    {
        var project = MakeProjectWithBareStandard("Circle", PreV3, "Width", "Height", "Visible", "Alpha", "Blue");

        var circleDefault = DefaultStateAfterInitialize(project, "Circle");

        foreach (var gatedVariable in V3ShapeVariables)
        {
            circleDefault.Variables.Any(v => v.Name == gatedVariable).ShouldBeFalse(
                $"A pre-v3 project must not have the v3 shape variable '{gatedVariable}' back-filled into its Circle standard.");
        }
    }

    [Fact]
    public void Initialize_DoesNotInjectV3ShapeVariables_IntoPreV3RectangleStandard()
    {
        var project = MakeProjectWithBareStandard("Rectangle", PreV3, "Width", "Height", "Visible");

        var rectangleDefault = DefaultStateAfterInitialize(project, "Rectangle");

        foreach (var gatedVariable in V3ShapeVariables.Append("CornerRadius"))
        {
            rectangleDefault.Variables.Any(v => v.Name == gatedVariable).ShouldBeFalse(
                $"A pre-v3 project must not have the v3 shape variable '{gatedVariable}' back-filled into its Rectangle standard.");
        }
    }

    [Fact]
    public void Initialize_InjectsV3ShapeVariables_IntoV3CircleStandard()
    {
        var project = MakeProjectWithBareStandard("Circle", V3, "Width", "Height", "Visible");

        var circleDefault = DefaultStateAfterInitialize(project, "Circle");

        foreach (var gatedVariable in V3ShapeVariables)
        {
            circleDefault.Variables.Any(v => v.Name == gatedVariable).ShouldBeTrue(
                $"A v3 project should still get the v3 shape variable '{gatedVariable}' on its Circle standard.");
        }
    }

    [Fact]
    public void Initialize_PreservesClassicVariables_OnPreV3CircleStandard()
    {
        var project = MakeProjectWithBareStandard("Circle", PreV3, "Width", "Height", "Visible", "Alpha", "Blue");

        var circleDefault = DefaultStateAfterInitialize(project, "Circle");

        // The classic surface the old runtime DOES support must survive the gated load untouched.
        circleDefault.Variables.Any(v => v.Name == "Width").ShouldBeTrue();
        circleDefault.Variables.Any(v => v.Name == "Visible").ShouldBeTrue();
        circleDefault.Variables.Any(v => v.Name == "Alpha").ShouldBeTrue();
    }

    [Fact]
    public void Initialize_DoesNotInjectRenderTargetTextureSource_IntoPreV3SpriteStandard()
    {
        var project = MakeProjectWithBareStandard("Sprite", PreV3, "Visible");

        var spriteDefault = DefaultStateAfterInitialize(project, "Sprite");

        spriteDefault.Variables.Any(v => v.Name == "RenderTargetTextureSource").ShouldBeFalse();
        // A pre-v3 Sprite still gets the rest of its classic surface back-filled.
        spriteDefault.Variables.Any(v => v.Name == "SourceFile").ShouldBeTrue();
    }

    [Fact]
    public void Initialize_DoesNotInjectIsTilingMiddleSections_IntoPreV3NineSliceStandard()
    {
        var project = MakeProjectWithBareStandard("NineSlice", PreV3, "Visible");

        var nineSliceDefault = DefaultStateAfterInitialize(project, "NineSlice");

        nineSliceDefault.Variables.Any(v => v.Name == "IsTilingMiddleSections").ShouldBeFalse();
        nineSliceDefault.Variables.Any(v => v.Name == "SourceFile").ShouldBeTrue();
    }

    [Fact]
    public void Initialize_DoesNotInjectSourceShaderFile_IntoPreV3ContainerStandard()
    {
        var project = MakeProjectWithBareStandard("Container", PreV3, "Visible");

        var containerDefault = DefaultStateAfterInitialize(project, "Container");

        containerDefault.Variables.Any(v => v.Name == "SourceShaderFile").ShouldBeFalse();
    }

    [Theory]
    // The canonical default-state definitions tag their post-v2 variables with MinimumGumxVersion =
    // v3 so the back-fill can gate them. This pins the mechanism the behavior tests above rely on.
    [InlineData("Circle", "Blend")]
    [InlineData("Circle", "FillRed")]
    [InlineData("Circle", "StrokeWidth")]
    [InlineData("Circle", "HasDropshadow")]
    [InlineData("Circle", "UseGradient")]
    [InlineData("Rectangle", "CornerRadius")]
    [InlineData("Sprite", "RenderTargetTextureSource")]
    [InlineData("NineSlice", "IsTilingMiddleSections")]
    [InlineData("Container", "SourceShaderFile")]
    public void DefaultState_TagsPostV2Variable_WithV3MinimumGumxVersion(string standardName, string variableName)
    {
        StateSave defaultState = StandardElementsManager.Self.DefaultStates[standardName];

        VariableSave variable = defaultState.Variables.First(v => v.Name == variableName);
        variable.MinimumGumxVersion.ShouldBe(V3);
    }

    [Theory]
    // Classic, always-supported variables stay untagged (MinimumGumxVersion 0) so they are always
    // back-filled, even into the oldest projects.
    [InlineData("Circle", "Width")]
    [InlineData("Circle", "Visible")]
    [InlineData("Sprite", "SourceFile")]
    public void DefaultState_LeavesClassicVariable_Untagged(string standardName, string variableName)
    {
        StateSave defaultState = StandardElementsManager.Self.DefaultStates[standardName];

        VariableSave variable = defaultState.Variables.First(v => v.Name == variableName);
        variable.MinimumGumxVersion.ShouldBe(0);
    }

    [Fact]
    public void FixStandardVariables_DoesNotThrow_WhenGatedVariablesAreAbsent_OnPreV3Project()
    {
        // After the gated back-fill the loaded Circle lacks the v3 variables, but the canonical
        // default state still lists them. FixStandardVariables reconciles per-variable metadata by
        // looking each default variable up on the loaded element — it must tolerate the misses.
        var project = MakeProjectWithBareStandard("Circle", PreV3, "Width", "Height", "Visible");
        project.Initialize();

        Should.NotThrow(() => project.FixStandardVariables());
    }
}
