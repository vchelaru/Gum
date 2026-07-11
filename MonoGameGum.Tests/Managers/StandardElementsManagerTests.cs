using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Managers;
public class StandardElementsManagerTests
{
    [Fact]
    public void AddNewStandardElementTypes_ShouldNotReAddColoredRectangle()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        GumProjectSave project = new GumProjectSave();
        project.AddNewStandardElementTypes();

        project.StandardElements.ShouldContain(e => e.Name == "Rectangle");
        project.StandardElements.ShouldNotContain(e => e.Name == "ColoredRectangle");
    }

    [Fact]
    public void AddStandardElementSaveInstance_ShouldThrowArgumentException_ForPluginStandard()
    {
        // The Skia shapes (Arc, Canvas, Line, Svg, LottieAnimation) are added by a plugin and are
        // never placed in the built-in defaults, so recreating one through this method must surface a
        // clear, typed error instead of a cryptic KeyNotFoundException from the mDefaults indexer (#3373).
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        GumProjectSave project = new GumProjectSave();

        Should.Throw<ArgumentException>(() => self.AddStandardElementSaveInstance(project, "Arc"));
    }

    [Fact]
    public void CircleDefault_ShouldExposeWidthAndHeight_NotRadius()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        var circle = self.DefaultStates["Circle"];

        circle.Variables.ShouldNotContain(v => v.Name == "Radius");

        var width = circle.Variables.First(v => v.Name == "Width");
        width.IsHiddenInPropertyGrid.ShouldBeFalse();
        circle.Variables.ShouldContain(v => v.Name == "Height" && !v.IsHiddenInPropertyGrid);
        circle.Variables.ShouldContain(v => v.Name == "WidthUnits");
        circle.Variables.ShouldContain(v => v.Name == "HeightUnits");
    }

    [Fact]
    public void DefaultStates_Circle_ShouldDefaultTo32x32_MatchingCircleRuntime()
    {
        // The in-tool standard default size must match the runtime constructor so a Circle
        // created in code matches one created in the tool. CircleRuntime's ctor seeds 32x32
        // (Radius 16). Reconciled in #2947/PR#2976.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        StateSave circle = self.DefaultStates["Circle"];

        circle.Variables.First(v => v.Name == "Width").Value.ShouldBe(32f);
        circle.Variables.First(v => v.Name == "Height").Value.ShouldBe(32f);
    }

    [Fact]
    public void DefaultStates_Circle_ShouldNotIncludeCornerRadius()
    {
        // A circle has no corners; CornerRadius belongs only on Rectangle.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.DefaultStates["Circle"].Variables
            .ShouldNotContain(v => v.Name == "CornerRadius");
    }

    [Fact]
    public void DefaultStates_Rectangle_ShouldDefaultTo50x50_MatchingRectangleRuntime()
    {
        // The in-tool standard default size must match the runtime constructor so a Rectangle
        // created in code matches one created in the tool. RectangleRuntime's ctor seeds 50x50.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        StateSave rectangle = self.DefaultStates["Rectangle"];

        rectangle.Variables.First(v => v.Name == "Width").Value.ShouldBe(50f);
        rectangle.Variables.First(v => v.Name == "Height").Value.ShouldBe(50f);
    }

    [Fact]
    public void DefaultStates_Rectangle_ShouldIncludeCornerRadius()
    {
        // v3 Rectangle absorbs RoundedRectangle's rounded-corner surface so the legacy
        // RoundedRectangle standard can be retired.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.DefaultStates["Rectangle"].Variables
            .ShouldContain(v => v.Name == "CornerRadius");
    }

    [Fact]
    public void DefaultStates_Rectangle_ShouldIncludeCustomRadiusOverrides()
    {
        // Issue #3617 — per-corner overrides for CornerRadius, mirroring RectangleRuntime's
        // CustomRadius* runtime properties so the tool's variable grid can expose them.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        List<VariableSave> variables = self.DefaultStates["Rectangle"].Variables;

        variables.ShouldContain(v => v.Name == "CustomRadiusTopLeft" && v.Type == "float?" && v.Value == null);
        variables.ShouldContain(v => v.Name == "CustomRadiusTopRight" && v.Type == "float?" && v.Value == null);
        variables.ShouldContain(v => v.Name == "CustomRadiusBottomLeft" && v.Type == "float?" && v.Value == null);
        variables.ShouldContain(v => v.Name == "CustomRadiusBottomRight" && v.Type == "float?" && v.Value == null);
    }

    [Fact]
    public void DefaultStates_Circle_ShouldNotIncludeCustomRadiusOverrides()
    {
        // A circle has no corners; the per-corner overrides belong only on Rectangle.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.DefaultStates["Circle"].Variables
            .ShouldNotContain(v => v.Name == "CustomRadiusTopLeft");
    }

    [Fact]
    public void DefaultStates_ShouldStillIncludeColoredRectangle_ForLegacyLoad()
    {
        // ColoredRectangle is no longer seeded into new projects, but it must remain in the
        // defaults so legacy projects that already contain it still load with correct values.
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.DefaultStates.ContainsKey("ColoredRectangle").ShouldBeTrue();
    }

    [Fact]
    public void Initialize_ShouldCreateStandardTypes()
    {
        // StandardElmementsManager is used as a singleton in a variety of places in both FRB
        // and Gum. This isn't great, but this is a delecate refactor requiring testing all Gum
        // runtimes including the one-off FlatRedBall implementation. Until that can be carefully
        // refactored, this must stay as a proper singleton. This test enforces that. Do not change
        // this from self access, and do not explicitly instantiate a StandardElementsManager! Doing
        // so will no longer reflect how runtimes interact with StandardElementsManager.
        var self = StandardElementsManager.Self;

        self.RefreshDefaults();

        self.DefaultTypes.Count().ShouldBeGreaterThan(0);

        self.DefaultStates["Circle"].ShouldNotBeNull();
        self.DefaultStates["ColoredRectangle"].ShouldNotBeNull();
        self.DefaultStates["Component"].ShouldNotBeNull();
        self.DefaultStates["Container"].ShouldNotBeNull();
        self.DefaultStates["NineSlice"].ShouldNotBeNull();
        self.DefaultStates["Polygon"].ShouldNotBeNull();
        self.DefaultStates["Rectangle"].ShouldNotBeNull();
        self.DefaultStates["Screen"].ShouldNotBeNull();
        self.DefaultStates["Sprite"].ShouldNotBeNull();
        self.DefaultStates["Text"].ShouldNotBeNull();
    }

    [Fact]
    public void PopulateProjectWithDefaultStandards_ShouldNotSeedColoredRectangle()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        GumProjectSave project = new GumProjectSave();
        self.PopulateProjectWithDefaultStandards(project);

        project.StandardElements.ShouldNotContain(e => e.Name == "ColoredRectangle");
    }

    [Fact]
    public void PopulateProjectWithDefaultStandards_ShouldSeedRectangle()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        GumProjectSave project = new GumProjectSave();
        self.PopulateProjectWithDefaultStandards(project);

        project.StandardElements.ShouldContain(e => e.Name == "Rectangle");
    }

    [Fact]
    public void SeedableStandardTypes_ShouldIncludeRectangle()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.SeedableStandardTypes.ShouldContain("Rectangle");
    }

    [Fact]
    public void SeedableStandardTypes_ShouldNotIncludeColoredRectangle()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.SeedableStandardTypes.ShouldNotContain("ColoredRectangle");
    }

    [Fact]
    public void SeedableStandardTypes_ShouldNotIncludeScreen()
    {
        StandardElementsManager self = StandardElementsManager.Self;
        self.RefreshDefaults();

        self.SeedableStandardTypes.ShouldNotContain("Screen");
    }
}
