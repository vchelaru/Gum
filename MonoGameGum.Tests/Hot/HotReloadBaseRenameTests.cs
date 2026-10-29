#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracyWorld;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7176: renaming a base component that another derives from (Gadget : Widget : Base, the tool updates
/// Gadget.BaseType and the instances) leaves the tree equal to a restart, so the result must say so, now
/// and on every later reload. Any other base change stays listed.
/// </summary>
public class HotReloadBaseRenameTests : BaseTestClass
{
    private readonly List<PreviewAccuracyWorld> _worlds = new List<PreviewAccuracyWorld>();

    public override void Dispose()
    {
        foreach (PreviewAccuracyWorld world in _worlds)
        {
            world.Manager?.Stop();
            world.Dispose();
        }
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private PreviewAccuracyWorld Boot()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        return world;
    }

    private static void ShouldBeCorrect(PreviewVerdict verdict) =>
        (verdict.Differs || !verdict.Result.MatchesRestart).ShouldBeFalse(verdict.Explain());

    // What the tool saves for a rename of Widgets/Widget: the component, its derived one and the instances.
    private static void RenameWidget(GumProjectSave project)
    {
        Component(project, "Widgets/Widget").Name = "Widgets/Control";
        Component(project, "Widgets/Gadget").BaseType = "Widgets/Control";
        MainScreen(project).GetInstance("Widget1")!.BaseType = "Widgets/Control";
    }

    private static bool Lists(PreviewVerdict verdict) =>
        verdict.Result.Issues.Any(issue => issue.Kind == HotReloadIssueKind.ComponentBaseTypeChanged);

    [Fact]
    public void Component_Renamed_WithItsDerivedAndInstancesUpdated()
    {
        PreviewAccuracyWorld world = Boot();

        PreviewVerdict verdict = world.Save(RenameWidget);

        ShouldBeCorrect(verdict);
        world.Find("Gadget1").ShouldNotBeNull();
    }

    [Fact]
    public void Component_RenamedUnderADerivedOne_DoesNotMakeEveryLaterReloadAskForARestart()
    {
        PreviewAccuracyWorld world = Boot();
        world.Save(RenameWidget);

        PreviewVerdict later = world.Save(project =>
        {
            RenameWidget(project);
            Std(MainScreen(project), "Label", "Text", "Later");
        });
        PreviewVerdict muchLater = world.Save(project =>
        {
            RenameWidget(project);
            Std(MainScreen(project), "Label", "Text", "Much later");
        });

        ShouldBeCorrect(later);
        ShouldBeCorrect(muchLater);
    }

    [Fact]
    public void Component_RenamedAndItsContentEditedInTheSameSave_IsListed()
    {
        PreviewAccuracyWorld world = Boot();

        PreviewVerdict verdict = world.Save(project =>
        {
            RenameWidget(project);
            Std(Component(project, "Widgets/Control"), "Fill", "Red", 77);
        });

        Lists(verdict).ShouldBeTrue(verdict.Explain());
        verdict.Result.MatchesRestart.ShouldBeFalse();
    }

    [Fact]
    public void Component_CopiedUnderANewNameWhileTheOldOneStays_IsListed()
    {
        PreviewAccuracyWorld world = Boot();

        PreviewVerdict verdict = world.Save(project =>
        {
            ComponentSave copy = NewComponent("Widgets/Control", "Widgets/Base");
            AddInstance(copy, "Fill", "ColoredRectangle");
            project.Components.Add(copy);
            Component(project, "Widgets/Gadget").BaseType = "Widgets/Control";
        });

        Lists(verdict).ShouldBeTrue(verdict.Explain());
    }

    [Fact]
    public void Component_SwitchedToABaseThatExistedBefore_IsListed()
    {
        PreviewAccuracyWorld world = Boot();

        PreviewVerdict verdict = world.Save(project => Component(project, "Widgets/Gadget").BaseType = "Widgets/Base");

        Lists(verdict).ShouldBeTrue(verdict.Explain());
    }

    [Fact]
    public void Component_RenamedOntoANameTheOldProjectHad_IsListed()
    {
        PreviewAccuracyWorld world = Boot();

        // Widget takes the name of Base (which is renamed away), so B is not new.
        PreviewVerdict verdict = world.Save(project =>
        {
            Component(project, "Widgets/Base").Name = "Widgets/Base2";
            Component(project, "Widgets/Widget").BaseType = "Widgets/Base2";
            Component(project, "Widgets/Widget").Name = "Widgets/Base";
            Component(project, "Widgets/Gadget").BaseType = "Widgets/Base";
            MainScreen(project).GetInstance("Widget1")!.BaseType = "Widgets/Base";
        });

        Lists(verdict).ShouldBeTrue(verdict.Explain());
    }
}
