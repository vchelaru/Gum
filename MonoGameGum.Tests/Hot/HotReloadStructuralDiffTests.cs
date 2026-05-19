using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Drives the structural side of hot reload (issue #2848). Each test materializes an
/// in-memory <see cref="GumProjectSave"/> through the normal runtime pipeline, mutates the
/// project's <c>Instances</c> list (the same edits the Gum tool would persist on save),
/// then invokes <see cref="GumHotReloadManager.ApplyDiff"/> and asserts that the live
/// visual tree converged to the new project state.
/// </summary>
public class HotReloadStructuralDiffTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region Helpers

    /// <summary>
    /// Ensures the named standard (e.g. "Container", "ColoredRectangle") is present in the project
    /// so <see cref="ObjectFinder"/> resolves it during instance materialization. Standards are
    /// what drive <c>CustomCreateGraphicalComponentFunc</c> into producing a real renderable.
    /// </summary>
    private static void EnsureStandard(GumProjectSave project, string name)
    {
        if (project.StandardElements.Any(s => s.Name == name))
        {
            return;
        }
        StandardElementSave standard = new StandardElementSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = standard };
        standard.States.Add(defaultState);
        project.StandardElements.Add(standard);
    }

    private static (ScreenSave screen, StateSave defaultState) BuildScreen(
        GumProjectSave project, string name = "TestScreen")
    {
        ScreenSave screen = new ScreenSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        project.Screens.Add(screen);
        return (screen, defaultState);
    }

    private static InstanceSave AddInstance(
        GumProjectSave project, ScreenSave screen, string name, string baseType = "Container")
    {
        EnsureStandard(project, baseType);
        InstanceSave instance = new InstanceSave
        {
            Name = name,
            BaseType = baseType,
            ParentContainer = screen
        };
        screen.Instances.Add(instance);
        return instance;
    }

    private static GraphicalUiElement? FindChildByName(GraphicalUiElement parent, string name)
    {
        return parent.Children.FirstOrDefault(c => c.Name == name);
    }

    #endregion

    [Fact]
    public void Add_NewInstance_AppearsInVisualTree()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        screenGue.Children.Count.ShouldBe(1, "sanity: initial materialization should have one child");

        // Simulate copy/paste in the Gum tool: a new InstanceSave is appended to the screen
        // and the screen's default state gains a qualified positional variable for it.
        AddInstance(project, screen, "Box2");
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "Box2.X",
            Value = 25f,
            Type = "float",
            SetsValue = true
        });

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Count.ShouldBe(2);
        GraphicalUiElement? newChild = FindChildByName(screenGue, "Box2");
        newChild.ShouldNotBeNull();
        newChild!.Tag.ShouldBeOfType<InstanceSave>();
        ((InstanceSave)newChild.Tag!).Name.ShouldBe("Box2");
        newChild.X.ShouldBe(25f, "qualified-name variables on the parent should have flowed through to the new child");
    }

    [Fact]
    public void NullTag_OnDesignTimeChild_IsTreatedAsRuntime_LimitationLocked()
    {
        // Locks in the documented limitation: if user code nulls the Tag, the diff treats
        // the child as runtime-only. A still-present matching InstanceSave does NOT cause
        // a duplicate visual to be created. If we ever revisit this (e.g. fall back to
        // Name+ElementSave matching), this test should change accordingly.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement box1 = screenGue.Children.Single();
        box1.Tag = null;

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Count.ShouldBe(1, "no extra visual should be created when Tag was cleared");
        screenGue.Children.Single().ShouldBeSameAs(box1);
    }

    [Fact]
    public void Preserve_RuntimeAddedChild_AcrossDiff()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();

        // A runtime-added child (no InstanceSave Tag) — represents UI the game code created
        // dynamically, e.g. a list-row generated by an ItemsControl.
        GraphicalUiElement runtimeChild = new GraphicalUiElement(new InvisibleRenderable())
        {
            Name = "RuntimeOnly"
        };
        runtimeChild.Parent = screenGue;

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.ShouldContain(runtimeChild);
    }

    [Fact]
    public void Remove_DeletedInstance_DropsFromVisualTree()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        AddInstance(project, screen, "Box2");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        screenGue.Children.Count.ShouldBe(2);

        // Simulate deletion in the Gum tool: drop Box1 from Instances.
        InstanceSave box1Instance = screen.Instances.Single(i => i.Name == "Box1");
        screen.Instances.Remove(box1Instance);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Count.ShouldBe(1);
        FindChildByName(screenGue, "Box1").ShouldBeNull();
        FindChildByName(screenGue, "Box2").ShouldNotBeNull();
    }

    [Fact]
    public void Reorder_InstancesReorderedInProject_ReordersDesignTimeChildren()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        AddInstance(project, screen, "Box2");
        AddInstance(project, screen, "Box3");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        screenGue.Children.Select(c => c.Name).ToList()
            .ShouldBe(new List<string> { "Box1", "Box2", "Box3" });

        // Simulate reorder in the Gum tool: Box3 → first, others shift down.
        InstanceSave box3 = screen.Instances.Single(i => i.Name == "Box3");
        screen.Instances.Remove(box3);
        screen.Instances.Insert(0, box3);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Select(c => c.Name).ToList()
            .ShouldBe(new List<string> { "Box3", "Box1", "Box2" });
    }

    [Fact]
    public void Reorder_LeavesRuntimeAddedSiblingsInPlace()
    {
        // Minimal expectation: runtime-added children keep their relative order among
        // themselves after a design-time reorder. We do NOT assert a specific interleave
        // between design-time and runtime children — that's an implementation detail.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        AddInstance(project, screen, "Box2");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();

        GraphicalUiElement runtimeA = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RuntimeA" };
        runtimeA.Parent = screenGue;
        GraphicalUiElement runtimeB = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RuntimeB" };
        runtimeB.Parent = screenGue;

        InstanceSave box2 = screen.Instances.Single(i => i.Name == "Box2");
        screen.Instances.Remove(box2);
        screen.Instances.Insert(0, box2);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.ShouldContain(runtimeA);
        screenGue.Children.ShouldContain(runtimeB);

        int idxA = screenGue.Children.ToList().IndexOf(runtimeA);
        int idxB = screenGue.Children.ToList().IndexOf(runtimeB);
        idxA.ShouldBeLessThan(idxB, "runtime-added children should keep their relative order");
    }

    [Fact]
    public void Reparent_InstanceToAnotherInstance_AttachesUnderNewParent()
    {
        // Box1 starts attached to the screen (no Parent variable). After the edit, Box1 is
        // reparented to be a child of Holder1 via the qualified Parent variable.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Holder1");
        AddInstance(project, screen, "Box1");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement holderGue = FindChildByName(screenGue, "Holder1")!;
        GraphicalUiElement box1Gue = FindChildByName(screenGue, "Box1")!;
        box1Gue.Parent.ShouldBe(screenGue);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "Box1.Parent",
            Value = "Holder1",
            Type = "string",
            SetsValue = true
        });

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        box1Gue.Parent.ShouldBe(holderGue);
        holderGue.Children.ShouldContain(box1Gue);
    }

    [Fact]
    public void Retype_BaseTypeChanged_ReplacesVisual()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave _) = BuildScreen(project);
        AddInstance(project, screen, "Item1", baseType: "Container");
        EnsureStandard(project, "ColoredRectangle");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement originalItem = screenGue.Children.Single();
        originalItem.ElementSave!.Name.ShouldBe("Container");

        // Simulate BaseType change in the Gum tool.
        InstanceSave item1 = screen.Instances.Single(i => i.Name == "Item1");
        item1.BaseType = "ColoredRectangle";

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Count.ShouldBe(1);
        GraphicalUiElement newItem = screenGue.Children.Single();
        newItem.Name.ShouldBe("Item1");
        newItem.ElementSave!.Name.ShouldBe("ColoredRectangle");
        newItem.ShouldNotBeSameAs(originalItem);
    }

    [Fact]
    public void VariableChange_OnExistingInstance_StillFlows()
    {
        // Sanity guard: the existing variable-reapply behavior continues to work after we
        // layer structural diffing on top.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Box1");
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "Box1.X",
            Value = 10f,
            Type = "float",
            SetsValue = true
        });
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement box1 = screenGue.Children.Single();
        box1.X.ShouldBe(10f);

        screenDefault.Variables.Single(v => v.Name == "Box1.X").Value = 99f;

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        box1.X.ShouldBe(99f);
        screenGue.Children.Single().ShouldBeSameAs(box1);
    }
}
