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

    // Arc/ColoredCircle/RoundedRectangle/Line are plugin-contributed legacy shapes never placed in
    // StandardElementsManager.DefaultStates (mDefaults) -- they're reached through
    // CustomGetDefaultState instead, so tests must go through RegisterExtendedDefaultStates() and
    // the individual Get*State() accessors rather than the DefaultStates indexer used for
    // Circle/Rectangle above.
    private static StateSave GetLegacyShapeDefaultState(string standardName) => standardName switch
    {
        "Arc" => StandardElementsManager.GetArcState(),
        "ColoredCircle" => StandardElementsManager.GetColoredCircleState(),
        "RoundedRectangle" => StandardElementsManager.GetRoundedRectangleState(),
        "Line" => StandardElementsManager.GetLineState(),
        _ => throw new System.ArgumentException($"Unknown legacy shape standard '{standardName}'", nameof(standardName)),
    };

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

    [Theory]
    // #2950 unified the legacy per-axis DropshadowBlurX/DropshadowBlurY into a single scalar
    // DropshadowBlur across every AddDropshadowVariables caller, including these four legacy
    // shapes. Their FRB1-generated runtime (frozen outside this repo -- see
    // SkiaGum.Renderables.RenderableArc and friends) predates that rename and never gained the
    // scalar name, so back-filling "DropshadowBlur" into an old FRB1 project broke the regenerated
    // runtime with CS0246 ('RenderableArc' does not contain a definition for 'DropshadowBlur').
    // These call sites were missed when Circle/Rectangle got the same gate for FRB #1881.
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    [InlineData("Line")]
    public void DefaultState_TagsDropshadowVariables_WithV3MinimumGumxVersion_OnLegacyShapes(string standardName)
    {
        StateSave defaultState = GetLegacyShapeDefaultState(standardName);

        VariableSave variable = defaultState.Variables.First(v => v.Name == "DropshadowBlur");
        variable.MinimumGumxVersion.ShouldBe(V3);
    }

    [Theory]
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    [InlineData("Line")]
    public void Initialize_DoesNotInjectDropshadowBlur_IntoPreV3LegacyShapeStandard(string standardName)
    {
        StandardElementsManager.Self.RegisterExtendedDefaultStates();
        GumProjectSave project = MakeProjectWithBareStandard(standardName, PreV3, "Visible", "Width", "Height");

        StateSave legacyShapeDefault = DefaultStateAfterInitialize(project, standardName);

        legacyShapeDefault.Variables.Any(v => v.Name == "DropshadowBlur").ShouldBeFalse(
            $"A pre-v3 project must not have DropshadowBlur back-filled into its {standardName} standard.");
    }

    [Theory]
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    [InlineData("Line")]
    public void Initialize_InjectsDropshadowBlur_IntoV3LegacyShapeStandard(string standardName)
    {
        StandardElementsManager.Self.RegisterExtendedDefaultStates();
        GumProjectSave project = MakeProjectWithBareStandard(standardName, V3, "Visible", "Width", "Height");

        StateSave legacyShapeDefault = DefaultStateAfterInitialize(project, standardName);

        legacyShapeDefault.Variables.Any(v => v.Name == "DropshadowBlur").ShouldBeTrue(
            $"A v3 project should still get DropshadowBlur back-filled into its {standardName} standard.");
    }

    [Theory]
    // The gate only skips AUTO-injecting a gated variable into a pre-v3 standard. A value the user
    // explicitly set on a v2 project must survive the load untouched — the back-fill never removes
    // or rewrites a variable already present in the loaded element. (FRB #1881 maintainer call:
    // "they should, of course, still work if they were manually set before on V2".)
    [InlineData("Container", "SourceShaderFile", "Effects/Bloom.fx")]
    [InlineData("Sprite", "RenderTargetTextureSource", "BackgroundContainer")]
    public void Initialize_PreservesManuallySetGatedVariable_OnPreV3Project(
        string standardName, string variableName, string value)
    {
        GumProjectSave project = MakeProjectWithBareStandard(standardName, PreV3, "Visible");
        StateSave loadedDefault = project.StandardElements.Single(e => e.Name == standardName).DefaultState;
        loadedDefault.Variables.Add(new VariableSave { Name = variableName, Type = "string", Value = value, SetsValue = true });

        project.Initialize();

        VariableSave preserved = project.StandardElements.Single(e => e.Name == standardName).DefaultState
            .Variables.FirstOrDefault(v => v.Name == variableName);
        preserved.ShouldNotBeNull();
        preserved.Value.ShouldBe(value);
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
