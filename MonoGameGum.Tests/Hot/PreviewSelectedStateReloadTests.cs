#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracyWorld;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// The state Preview applies on top of the shown element (#7187): a reload only compares default states, so the
/// running tree has to be rebuilt (<see cref="PreviewReloadPolicy"/>) or it keeps a value the saved state no longer sets.
/// Each test compares the tree after the save with a fresh load of the saved folder in the same selected state.
/// </summary>
public class PreviewSelectedStateReloadTests : BaseTestClass
{
    private readonly List<PreviewAccuracy3World> _worlds = new List<PreviewAccuracy3World>();

    public override void Dispose()
    {
        foreach (PreviewAccuracy3World world in _worlds)
        {
            world.Dispose();
        }
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private PreviewAccuracy3World Boot(string? stateName = "Highlighted")
    {
        PreviewAccuracy3World world = new PreviewAccuracy3World
        {
            Selection = new PreviewSelectionMessage("Widgets/Widget") { CategoryName = stateName == null ? null : "Look", StateName = stateName }
        };
        _worlds.Add(world);
        world.Boot(Baseline(), "Widgets/Widget");
        return world;
    }

    private static StateSave LookState(GumProjectSave project, string name) =>
        Component(project, "Widgets/Widget").Categories.Single(category => category.Name == "Look").States.Single(state => state.Name == name);

    [Fact]
    public void SelectedState_TheVariableItSetsIsRemovedFromIt()
    {
        PreviewVerdict2 verdict = Boot().Save(project => LookState(project, "Highlighted").Variables.RemoveAll(variable => variable.Name == "Fill.Red"));

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void SelectedState_TheStateIsDeleted()
    {
        PreviewVerdict2 verdict = Boot().Save(project =>
            Component(project, "Widgets/Widget").Categories.Single(category => category.Name == "Look").States.RemoveAll(state => state.Name == "Highlighted"));

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void SelectedState_TheStateIsRenamed()
    {
        PreviewVerdict2 verdict = Boot().Save(project => LookState(project, "Highlighted").Name = "Hover");

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void SelectedState_TheCategoryIsDeleted()
    {
        PreviewVerdict2 verdict = Boot().Save(project => Component(project, "Widgets/Widget").Categories.RemoveAll(category => category.Name == "Look"));

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void SelectedState_TheValueItSetsChanges()
    {
        PreviewAccuracy3World world = Boot();

        PreviewVerdict2 verdict = world.Save(project => LookState(project, "Highlighted").Variables.Single(variable => variable.Name == "Fill.Red").Value = 50);

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
        verdict.Hot.ShouldContain("Red=50");
    }

    [Fact]
    public void SelectedState_TheDefaultOfTheVariableItSetsChanges()
    {
        PreviewVerdict2 verdict = Boot().Save(project => Component(project, "Widgets/Widget").DefaultState.Variables.Single(variable => variable.Name == "Fill.Red").Value = 20);

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void SelectedState_EditUndone_ShowsTheStateAgain()
    {
        PreviewAccuracy3World world = Boot();
        world.Save(project => LookState(project, "Highlighted").Variables.RemoveAll(variable => variable.Name == "Fill.Red"));

        PreviewVerdict2 verdict = world.Save(project => LookState(project, "Highlighted").Variables.Add(
            new VariableSave { Name = "Fill.Red", Type = "int", Value = 40, SetsValue = true }));

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
        verdict.Hot.ShouldContain("Red=40");
    }

    [Fact]
    public void SelectedState_AnotherCategoryStateIsEdited()
    {
        PreviewVerdict2 verdict = Boot().Save(project => LookState(project, "Normal").Variables.Add(
            new VariableSave { Name = "Fill.Green", Type = "int", Value = 9, SetsValue = true }));

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
    }

    [Fact]
    public void DefaultStateSelected_NothingIsRebuiltAndTheTreeMatches()
    {
        PreviewAccuracy3World world = Boot(stateName: null);
        GraphicalUiElement before = world.Screen;

        PreviewVerdict2 verdict = world.Save(project => Component(project, "Widgets/Widget").DefaultState.Variables.Single(variable => variable.Name == "Fill.Red").Value = 20);

        verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());
        world.Screen.ShouldBeSameAs(before);
    }
}
