#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// A hot reload of a project with a loop of variable references shows what a restart shows: the rows in the loop
// keep their stored values (#6871), and a value that came from a row that no longer applies does not stay on screen.
public partial class PreviewAccuracy2Tests
{
    /// <summary>Replaces the rows <paramref name="reader"/> has in <paramref name="owner"/>; null removes them.</summary>
    private static void SetRow(ElementSave owner, string reader, string? row)
    {
        owner.DefaultState!.VariableLists.RemoveAll(list => list.Name == reader + ".VariableReferences");
        if (row == null)
        {
            return;
        }
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = reader + ".VariableReferences" };
        list.Value.Add(row);
        owner.DefaultState.VariableLists.Add(list);
    }

    /// <summary>Boots with <paramref name="before"/>, saves with <paramref name="after"/> and fails if the save does not finish in time.</summary>
    private PreviewVerdict2 Edit(Action<GumProjectSave> before, Action<GumProjectSave> after)
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: before);
        Task<PreviewVerdict2> save = Task.Run(() => world.Save(after));
        save.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue("the reload did not finish: a loop of references hung it");
        return save.Result;
    }

    [Fact]
    public void Reference_CircularReferenceAdded_DoesNotHangAndMatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project => SetRow(MainScreen(project), "Follower", "Red = Swatch.Red"),
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Follower.Red");
            });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopOfThreeAdded_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Box.Red");
            },
            project => SetRow(MainScreen(project), "Box", "Red = Follower.Red"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_ReadingItselfAdded_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project => SetRow(MainScreen(project), "Follower", "Red = Swatch.Red"),
            project => SetRow(MainScreen(project), "Follower", "Red = Follower.Red"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_ReadingItselfAddedToAnInstanceWithNoRows_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project => { },
            project => SetRow(MainScreen(project), "Follower", "Red = Follower.Red"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_ExistingReferenceEditedIntoALoop_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Box.Red");
            },
            project => SetRow(MainScreen(project), "Swatch", "Red = Follower.Red"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopBroken_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Follower.Red");
            },
            project => SetRow(MainScreen(project), "Swatch", null));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopBrokenByRemovingBothRows_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Follower.Red");
            },
            project =>
            {
                SetRow(MainScreen(project), "Follower", null);
                SetRow(MainScreen(project), "Swatch", null);
            });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopAlreadyInTheProject_UnrelatedEdit_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Follower.Red");
            },
            project => Std(MainScreen(project), "Box", "Red", 99));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopAlreadyInTheProject_NothingEdited_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
                SetRow(MainScreen(project), "Swatch", "Red = Follower.Red");
            },
            project => { });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_RowRemovedWithoutALoop_ValueGoesBackToItsOwn()
    {
        PreviewVerdict2 verdict = Edit(
            project => SetRow(MainScreen(project), "Follower", "Red = Swatch.Red"),
            project => SetRow(MainScreen(project), "Follower", null));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_RowRemoved_TheFollowerNeverStoredAValue_GoesBackToTheDefault()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                MainScreen(project).DefaultState!.Variables.RemoveAll(variable => variable.Name == "Follower.Red");
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
            },
            project => SetRow(MainScreen(project), "Follower", null));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_RowRetargetedWhileTheFollowerNeverStoredAValue_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project =>
            {
                MainScreen(project).DefaultState!.Variables.RemoveAll(variable => variable.Name == "Follower.Red");
                SetRow(MainScreen(project), "Follower", "Red = Swatch.Red");
            },
            project => SetRow(MainScreen(project), "Follower", "Red = Follower.Red"));
        ShouldBeCorrect(verdict);
    }

    // The loop is between a component's instances, one row declared by its base and one by the derived component.
    private static void FancyBadge(GumProjectSave project, string? derivedRow)
    {
        Component(project, "Widgets/Badge").DefaultState!.Variables.RemoveAll(variable => variable.Name == "Pic.Red" || variable.Name == "Name.Red");
        Std(Component(project, "Widgets/Badge"), "Pic", "Red", 40);
        Std(Component(project, "Widgets/Badge"), "Name", "Red", 50);
        SetRow(Component(project, "Widgets/Badge"), "Name", "Red = Pic.Red");

        ComponentSave fancy = NewComponent("Widgets/Fancy", "Widgets/Badge");
        SetRow(fancy, "Pic", derivedRow);
        project.Components.Add(fancy);
        AddInstance(MainScreen(project), "Fancy1", "Widgets/Fancy");
    }

    [Fact]
    public void Reference_LoopThroughAComponentAndItsBaseAdded_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project => FancyBadge(project, derivedRow: null),
            project => SetRow(Component(project, "Widgets/Fancy"), "Pic", "Red = Name.Red"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_LoopThroughAComponentAndItsBaseRemoved_MatchesARestart()
    {
        PreviewVerdict2 verdict = Edit(
            project => FancyBadge(project, derivedRow: "Red = Name.Red"),
            project => SetRow(Component(project, "Widgets/Fancy"), "Pic", null));
        ShouldBeCorrect(verdict);
    }
}
