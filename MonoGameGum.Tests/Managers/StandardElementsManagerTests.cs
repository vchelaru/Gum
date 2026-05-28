using Gum.DataTypes;
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
