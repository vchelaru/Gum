#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
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
/// Red tests of the round 3 sweep (kept on the branch preview-accuracy-red-3 until their fix lands): the state Preview
/// applies on top of the shown element is not undone when the saved state stops setting that variable, so the running
/// tree keeps the old value while the result says it matches a restart.
/// </summary>
public class PreviewAccuracy3RedTests : BaseTestClass
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

    private PreviewAccuracy3World Boot()
    {
        PreviewAccuracy3World world = new PreviewAccuracy3World
        {
            Selection = new PreviewSelectionMessage("Widgets/Widget") { CategoryName = "Look", StateName = "Highlighted" }
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
}
