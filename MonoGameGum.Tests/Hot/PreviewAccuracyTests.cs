#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracyWorld;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Preview accuracy sweep: one edit the tool can save, applied to a baseline project, then the hot
/// reloaded tree compared with a fresh load of the saved files and the reload's
/// <see cref="HotReloadResult"/> checked against it. A false negative (the tree differs and the result
/// says it matches) leaves Preview stale without anyone knowing; a false positive (the tree is right and
/// the result asks for a restart) makes Preview rebuild itself for nothing.
/// </summary>
public partial class PreviewAccuracyTests : BaseTestClass
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

    private PreviewVerdict Run(Action<GumProjectSave> edit, string shown = "MainScreen")
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot(shown);
        PreviewVerdict verdict = world.Save(edit);
        return verdict;
    }

    /// <summary>The tree equals a restart and the result agrees: nothing stale, no needless rebuild.</summary>
    private static void ShouldBeCorrect(PreviewVerdict verdict) =>
        (verdict.Differs || !verdict.Result.MatchesRestart).ShouldBeFalse(verdict.Explain());

    /// <summary>Whatever the tree is, the result must not claim a match while it differs.</summary>
    private static void ShouldNotBeStaleSilently(PreviewVerdict verdict) => verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());

    private static void Rename(ElementSave owner, string oldName, string newName)
    {
        owner.GetInstance(oldName)!.Name = newName;
        foreach (VariableSave variable in owner.DefaultState!.Variables)
        {
            if (variable.Name.StartsWith(oldName + "."))
            {
                variable.Name = newName + variable.Name.Substring(oldName.Length);
            }
            if (variable.Name.EndsWith(".Parent") && variable.Value as string == oldName)
            {
                variable.Value = newName;
            }
        }
    }

    private static void Remove(ElementSave owner, string instance)
    {
        owner.Instances.Remove(owner.GetInstance(instance)!);
        owner.DefaultState!.Variables.RemoveAll(variable => variable.Name.StartsWith(instance + "."));
    }

    // ---- Variables, every type and unit, on a screen instance ----

    [Fact]
    public void Variable_Number_X() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Box", "X", 44f)));

    [Fact]
    public void Variable_Units_XUnitsAndOrigin() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Box", "XUnits", "PixelsFromCenterX");
        Std(MainScreen(p), "Box", "XOrigin", "Center");
    }));

    [Fact]
    public void Variable_Units_WidthRelativeToContainer() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Box", "WidthUnits", "PercentageOfParent");
        Std(MainScreen(p), "Box", "Width", 50f);
    }));

    [Fact]
    public void Variable_Color_RedGreenBlueAlpha() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Box", "Red", 200);
        Std(MainScreen(p), "Box", "Green", 100);
        Std(MainScreen(p), "Box", "Blue", 50);
        Std(MainScreen(p), "Box", "Alpha", 128);
    }));

    [Fact]
    public void Variable_Bool_Visible() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Box", "Visible", false)));

    [Fact]
    public void Variable_Number_Rotation() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Box", "Rotation", 30f)));

    [Fact]
    public void Variable_Enum_BlendMode() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Box", "Blend", "Additive")));

    [Fact]
    public void Variable_Removed_ValueGoesBackToTheDefault() => ShouldBeCorrect(Run(p =>
        MainScreen(p).DefaultState!.Variables.RemoveAll(variable => variable.Name == "Box.X")));

    [Fact]
    public void Variable_String_Text() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Label", "Text", "Changed")));

    [Fact]
    public void Variable_Enum_HorizontalAlignment() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Label", "HorizontalAlignment", "Right")));

    [Fact]
    public void Variable_Font_SizeAndSizeToContent() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Label", "FontSize", 40);
        Std(MainScreen(p), "Label", "WidthUnits", "RelativeToChildren");
        Std(MainScreen(p), "Label", "Width", 0f);
    }));

    [Fact]
    public void Variable_Font_NameAndOutline() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Label", "Font", "Courier New");
        Std(MainScreen(p), "Label", "OutlineThickness", 2);
    }));

    [Fact]
    public void Variable_Bool_ClipsChildren() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Panel", "ClipsChildren", true)));

    [Fact]
    public void Variable_Bool_IsRenderTargetAndAlpha() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Panel", "IsRenderTarget", true);
        Std(MainScreen(p), "Panel", "Alpha", 100);
    }));

    [Fact]
    public void Variable_Enum_ChildrenLayoutAndSpacing() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Panel", "ChildrenLayout", "TopToBottomStack");
        Std(MainScreen(p), "Panel", "StackSpacing", 6f);
    }));

    [Fact]
    public void Variable_ParentSize_ReachesAChildInPercent() => ShouldBeCorrect(Run(p =>
    {
        Std(MainScreen(p), "Inner", "WidthUnits", "PercentageOfParent");
        Std(MainScreen(p), "Inner", "Width", 50f);
        Std(MainScreen(p), "Panel", "Width", 300f);
    }));

    [Fact]
    public void Variable_ParentAssignedToAnotherInstance() => ShouldBeCorrect(Run(p => Set(MainScreen(p), "Inner.Parent", "string", "Widget1")));

    [Fact]
    public void Variable_ParentCleared_InstanceMovesBackToTheScreen() => ShouldBeCorrect(Run(p =>
        MainScreen(p).DefaultState!.Variables.RemoveAll(variable => variable.Name == "Inner.Parent")));

    // ---- Instances ----

    [Fact]
    public void Instance_AddedAtTheEnd() => ShouldBeCorrect(Run(p =>
    {
        AddInstance(MainScreen(p), "Extra", "ColoredRectangle");
        Std(MainScreen(p), "Extra", "Red", 90);
    }));

    [Fact]
    public void Instance_AddedAtTheStart_DrawsFirst() => ShouldBeCorrect(Run(p =>
    {
        ScreenSave screen = MainScreen(p);
        InstanceSave behind = new InstanceSave { Name = "Behind", BaseType = "ColoredRectangle", ParentContainer = screen };
        screen.Instances.Insert(0, behind);
    }));

    [Fact]
    public void Instance_Deleted() => ShouldBeCorrect(Run(p => Remove(MainScreen(p), "Label")));

    [Fact]
    public void Instance_DeletedWithItsChild() => ShouldBeCorrect(Run(p =>
    {
        Remove(MainScreen(p), "Inner");
        Remove(MainScreen(p), "Panel");
    }));

    [Fact]
    public void Instance_Renamed() => ShouldBeCorrect(Run(p => Rename(MainScreen(p), "Box", "Crate")));

    [Fact]
    public void Instance_RenamedWhileAChildIsParentedToIt() => ShouldBeCorrect(Run(p => Rename(MainScreen(p), "Panel", "Frame")));

    [Fact]
    public void Instance_Reordered() => ShouldBeCorrect(Run(p =>
    {
        ScreenSave screen = MainScreen(p);
        InstanceSave box = screen.GetInstance("Box")!;
        screen.Instances.Remove(box);
        screen.Instances.Add(box);
    }));

    [Fact]
    public void Instance_MovedIntoAContainer() => ShouldBeCorrect(Run(p => Set(MainScreen(p), "Box.Parent", "string", "Panel")));

    [Fact]
    public void Instance_BaseTypeChanged_ToAnotherStandard() => ShouldBeCorrect(Run(p =>
        MainScreen(p).GetInstance("Tail")!.BaseType = "Container"));

    [Fact]
    public void Instance_BaseTypeChanged_ToAComponent() => ShouldBeCorrect(Run(p =>
        MainScreen(p).GetInstance("Tail")!.BaseType = "Widgets/Widget"));

    [Fact]
    public void Instance_BaseTypeChanged_ToADerivedComponent() => ShouldBeCorrect(Run(p =>
        MainScreen(p).GetInstance("Widget1")!.BaseType = "Widgets/Gadget"));

    [Fact]
    public void Instances_ManyAddedAndOneDeletedInOneSave() => ShouldBeCorrect(Run(p =>
    {
        ScreenSave screen = MainScreen(p);
        for (int i = 0; i < 5; i++)
        {
            AddInstance(screen, "Pasted" + i, "ColoredRectangle");
            Std(screen, "Pasted" + i, "Red", 10 * i);
        }
        Set(screen, "Pasted2.Parent", "string", "Panel");
        Remove(screen, "Tail");
    }));

    // ---- Components ----

    [Fact]
    public void Component_VariableEdited_ReachesTheInstanceAndTheDerivedOne() => ShouldBeCorrect(Run(p =>
        Std(Component(p, "Widgets/Widget"), "Fill", "Red", 111)));

    [Fact]
    public void Component_OwnVariableEdited() => ShouldBeCorrect(Run(p =>
        StdOwn(Component(p, "Widgets/Widget"), "Container", "Width", 123f)));

    [Fact]
    public void Component_InstanceAdded_ReachesDepthThree() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave widget = Component(p, "Widgets/Widget");
        AddInstance(widget, "Badge", "ColoredRectangle");
        Std(widget, "Badge", "Red", 66);
    }));

    [Fact]
    public void Component_InstanceRemoved_ReachesDepthThree() => ShouldBeCorrect(Run(p => Remove(Component(p, "Widgets/Widget"), "Caption")));

    [Fact]
    public void Component_InstanceRenamed() => ShouldBeCorrect(Run(p => Rename(Component(p, "Widgets/Widget"), "Fill", "Background")));

    [Fact]
    public void Component_RootOfTheChainEdited_ReachesDepthThree() => ShouldBeCorrect(Run(p =>
        Component(p, "Widgets/Base").DefaultState!.Variables.Single(variable => variable.Name == "X").Value = 31f));

    [Fact]
    public void Component_RootOfTheChainGainsAnInstance_ReachesDepthThree() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave root = Component(p, "Widgets/Base");
        AddInstance(root, "Dot", "ColoredRectangle");
        Std(root, "Dot", "Red", 12);
    }));

    [Fact]
    public void Component_MiddleOfTheChainChangesItsBase() => ShouldNotBeStaleSilently(Run(p =>
        Component(p, "Widgets/Widget").BaseType = "Container"));

    [Fact]
    public void Component_MovedToAnotherFolder() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave gadget = Component(p, "Widgets/Gadget");
        gadget.Name = "Elsewhere/Gadget";
        MainScreen(p).GetInstance("Gadget1")!.BaseType = "Elsewhere/Gadget";
    }));

    [Fact]
    public void Component_AddedAndUsed() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave badge = NewComponent("Badge");
        AddInstance(badge, "Dot", "ColoredRectangle");
        p.Components.Add(badge);
        AddInstance(MainScreen(p), "Badge1", "Badge");
    }));

    // ---- States and categories ----

    [Fact]
    public void State_AssignedOnAnInstance() => ShouldBeCorrect(Run(p =>
        Set(MainScreen(p), "Widget1.LookState", "Look", "Highlighted")));

    [Fact]
    public void State_ValueEdited_WhileAnInstanceIsInIt() => ShouldBeCorrect(Run(p =>
    {
        // the baseline keeps Highlighted at 40; the edit assigns it and then changes what it sets
        Set(MainScreen(p), "Widget1.LookState", "Look", "Highlighted");
        StateSave highlighted = Component(p, "Widgets/Widget").Categories.Single().States.Single(state => state.Name == "Highlighted");
        highlighted.Variables.Single(variable => variable.Name == "Fill.Red").Value = 77;
    }));

    [Fact]
    public void State_RenamedWhileAnInstanceIsInIt() => ShouldBeCorrect(Run(p =>
    {
        Set(MainScreen(p), "Widget1.LookState", "Look", "Hot");
        Component(p, "Widgets/Widget").Categories.Single().States.Single(state => state.Name == "Highlighted").Name = "Hot";
    }));

    [Fact]
    public void State_AddedToACategory_AndUsed() => ShouldBeCorrect(Run(p =>
    {
        StateSave pressed = new StateSave { Name = "Pressed", ParentContainer = Component(p, "Widgets/Widget") };
        Set(pressed, "Fill.Red", "int", 5);
        Component(p, "Widgets/Widget").Categories.Single().States.Add(pressed);
        Set(MainScreen(p), "Widget1.LookState", "Look", "Pressed");
    }));

    [Fact]
    public void State_Reordered_AndRemoved() => ShouldBeCorrect(Run(p =>
    {
        StateSaveCategory look = Component(p, "Widgets/Widget").Categories.Single();
        look.States.Reverse();
        look.States.RemoveAll(state => state.Name == "Normal");
    }));

    [Fact]
    public void Category_AddedToAScreenWithAState() => ShouldBeCorrect(Run(p =>
    {
        ScreenSave screen = MainScreen(p);
        StateSaveCategory mode = new StateSaveCategory { Name = "Mode" };
        StateSave dark = new StateSave { Name = "Dark", ParentContainer = screen };
        Set(dark, "Box.Red", "int", 3);
        mode.States.Add(dark);
        screen.Categories.Add(mode);
    }));

    // ---- Standard elements, project, behaviors ----

    [Fact]
    public void StandardElement_DefaultEdited_IsHonest() => ShouldNotBeStaleSilently(Run(p =>
        p.StandardElements.Single(standard => standard.Name == "ColoredRectangle").DefaultState!.Variables
            .Single(variable => variable.Name == "Red").Value = 200));

    [Fact]
    public void StandardElement_TextDefaultEdited_IsHonest() => ShouldNotBeStaleSilently(Run(p =>
        p.StandardElements.Single(standard => standard.Name == "Text").DefaultState!.Variables
            .Single(variable => variable.Name == "Text").Value = "Default!"));

    [Fact]
    public void Project_CanvasSizeEdited_ChangesNoElement() => ShouldBeCorrect(Run(p =>
    {
        p.DefaultCanvasWidth = 1280;
        p.DefaultCanvasHeight = 720;
    }));

    [Fact]
    public void Behavior_AddedToTheProject_AndReferencedByAComponent() => ShouldBeCorrect(Run(p =>
    {
        p.Behaviors.Add(new BehaviorSave { Name = "SomeBehavior" });
        Component(p, "Widgets/Widget").Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SomeBehavior" });
    }));

    // ---- Undo, redo and saving twice ----

    [Fact]
    public void Edit_ThenTheSameEditUndone_LeavesTheTreeAsARestartShowsIt()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        world.Save(p => Rename(MainScreen(p), "Box", "Crate"));

        PreviewVerdict undone = world.Save(p => { });
        ShouldBeCorrect(undone);
    }

    [Fact]
    public void Edit_ThenDeletedInstanceRestored_LeavesTheTreeAsARestartShowsIt()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        world.Save(p => Remove(MainScreen(p), "Box"));

        PreviewVerdict undone = world.Save(p => { });
        ShouldBeCorrect(undone);
    }

    [Fact]
    public void TwoSavesInsideTheDebounce_ReloadOnceFromTheLastOne()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        world.Save(p => Std(MainScreen(p), "Box", "X", 10f));
        int results = world.Results.Count;
        // the second save lands before the clock moves past the debounce
        GumProjectSave second = Baseline();
        Std(MainScreen(second), "Box", "X", 20f);
        Std(MainScreen(second), "Label", "Text", "Second");
        SaveAll(second, world.Gumx);

        PreviewVerdict verdict = world.Reload();
        world.Results.Count.ShouldBe(results + 1);
        ShouldBeCorrect(verdict);
    }

    // ---- The shown element is a component (what Preview shows for a selected component) ----

    [Fact]
    public void ShownComponent_VariableEdited() => ShouldBeCorrect(Run(p => Std(Component(p, "Widgets/Widget"), "Fill", "Red", 5), "Widgets/Widget"));

    [Fact]
    public void ShownComponent_InstanceAddedAndReordered() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave widget = Component(p, "Widgets/Widget");
        widget.Instances.Insert(0, new InstanceSave { Name = "Back", BaseType = "ColoredRectangle", ParentContainer = widget });
    }, "Widgets/Widget"));

    [Fact]
    public void ShownComponent_StateEditedAfterBeingSelected() => ShouldBeCorrect(Run(p =>
        Component(p, "Widgets/Widget").Categories.Single().States.Single(state => state.Name == "Highlighted")
            .Variables.Single(variable => variable.Name == "Fill.Red").Value = 90, "Widgets/Widget"));

    [Fact]
    public void ShownComponent_BaseEdited() => ShouldBeCorrect(Run(p =>
        Component(p, "Widgets/Base").DefaultState!.Variables.Single(variable => variable.Name == "X").Value = 50f, "Widgets/Widget"));

    [Fact]
    public void ShownComponent_Renamed_IsReported() =>
        Run(p => Component(p, "Widgets/Widget").Name = "Widgets/Renamed", "Widgets/Widget").Result.MatchesRestart.ShouldBeFalse();

    [Fact]
    public void ShownScreen_Renamed_IsReported() =>
        Run(p => MainScreen(p).Name = "MainRenamed").Result.MatchesRestart.ShouldBeFalse();

    [Fact]
    public void ShownScreen_AnotherScreenEdited_ChangesNothingShown() => ShouldBeCorrect(Run(p =>
        AddInstance(p.Screens.Single(screen => screen.Name == "OtherScreen"), "More", "ColoredRectangle")));

    // ---- A shown screen that inherits from another screen ----

    [Fact]
    public void DerivedScreen_BaseScreenVariableEdited() => ShouldBeCorrect(Run(p => Std(MainScreen(p), "Box", "X", 90f), "DerivedScreen"));

    [Fact]
    public void DerivedScreen_BaseScreenGainsAndLosesInstances() => ShouldBeCorrect(Run(p =>
    {
        AddInstance(MainScreen(p), "FromBase", "ColoredRectangle");
        Remove(MainScreen(p), "Tail");
    }, "DerivedScreen"));

    [Fact]
    public void DerivedScreen_OwnInstanceEditedAndOverridesABaseInstance() => ShouldBeCorrect(Run(p =>
    {
        Std(p.Screens.Single(screen => screen.Name == "DerivedScreen"), "Own", "Red", 77);
        Set(p.Screens.Single(screen => screen.Name == "DerivedScreen"), "Box.Red", "int", 150);
    }, "DerivedScreen"));

    [Fact]
    public void DerivedScreen_BaseScreenChanged() => ShouldNotBeStaleSilently(Run(p =>
        p.Screens.Single(screen => screen.Name == "DerivedScreen").BaseType = "OtherScreen", "DerivedScreen"));

    // ---- Forms behaviors ----

    [Fact]
    public void Behavior_ButtonBehaviorAddedToAShownComponent_FormsControlAppears() => ShouldNotBeStaleSilently(Run(p =>
    {
        p.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });
        Component(p, "Widgets/Widget").Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
    }));

    [Fact]
    public void Behavior_ButtonBehaviorRemovedFromAComponent_FormsControlGoes()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        world.Save(p =>
        {
            p.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });
            Component(p, "Widgets/Widget").Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        });

        PreviewVerdict removed = world.Save(p => { });
        ShouldNotBeStaleSilently(removed);
    }
}
