using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
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
        ElementSaveExtensions.Reset();
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
        if (project.StandardElements.Any(s => s.Name == name) || project.Components.Any(c => c.Name == name))
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

    private static ComponentSave AddComponent(GumProjectSave project, string name)
    {
        EnsureStandard(project, "Container");
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        return component;
    }

    private static void SetParent(StateSave state, string instanceName, string parentName)
    {
        state.Variables.Add(new VariableSave
        {
            Name = instanceName + ".Parent",
            Value = parentName,
            Type = "string",
            SetsValue = true
        });
    }

    private static void MoveInstanceToEnd(ScreenSave screen, string name)
    {
        InstanceSave instance = screen.Instances.Single(i => i.Name == name);
        screen.Instances.Remove(instance);
        screen.Instances.Add(instance);
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

    private static void SetFloat(StateSave state, string name, float value)
    {
        VariableSave? existing = state.Variables.FirstOrDefault(v => v.Name == name);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = "float", SetsValue = true });
    }

    private static InstanceSave AddInstanceToComponent(
        GumProjectSave project, ComponentSave container, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = container };
        container.Instances.Add(instance);
        return instance;
    }

    [Fact]
    public void ComponentDefaultStateEdit_ReachesExistingInstancesOfThatComponent()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, _) = BuildScreen(project);
        ComponentSave button = AddComponent(project, "ButtonComponent");
        SetFloat(button.DefaultState, "Width", 100f);
        AddInstance(project, screen, "ButtonInstance", "ButtonComponent");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        FindChildByName(screenGue, "ButtonInstance")!.Width.ShouldBe(100f, "sanity: the instance starts at the component's default width");

        // The user resizes the component in the tool; the screen is not edited.
        SetFloat(button.DefaultState, "Width", 200f);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        FindChildByName(screenGue, "ButtonInstance")!.Width.ShouldBe(200f);
    }

    [Fact]
    public void ComponentDefaultStateEdit_DoesNotOverrideTheScreensOwnValueForThatInstance()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        ComponentSave button = AddComponent(project, "ButtonComponent");
        SetFloat(button.DefaultState, "Width", 100f);
        AddInstance(project, screen, "ButtonInstance", "ButtonComponent");
        SetFloat(screenDefault, "ButtonInstance.Width", 150f);
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        FindChildByName(screenGue, "ButtonInstance")!.Width.ShouldBe(150f, "sanity: the screen's value wins at creation");

        SetFloat(button.DefaultState, "Width", 200f);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        FindChildByName(screenGue, "ButtonInstance")!.Width.ShouldBe(150f);
    }

    [Fact]
    public void ComponentDefaultStateEdit_ReachesInstancesNestedSeveralLevelsDeep()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, _) = BuildScreen(project);
        ComponentSave leaf = AddComponent(project, "LeafComponent");
        SetFloat(leaf.DefaultState, "Width", 10f);
        ComponentSave middle = AddComponent(project, "MiddleComponent");
        AddInstanceToComponent(project, middle, "LeafInstance", "LeafComponent");
        ComponentSave outer = AddComponent(project, "OuterComponent");
        AddInstanceToComponent(project, outer, "MiddleInstance", "MiddleComponent");
        AddInstance(project, screen, "OuterInstance", "OuterComponent");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement GetLeaf() =>
            FindChildByName(FindChildByName(FindChildByName(screenGue, "OuterInstance")!, "MiddleInstance")!, "LeafInstance")!;
        GetLeaf().Width.ShouldBe(10f, "sanity: the leaf starts at its component's default width");

        SetFloat(leaf.DefaultState, "Width", 20f);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        GetLeaf().Width.ShouldBe(20f);
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
        screenGue.Children.Select(c => c.Name!).ToList()
            .ShouldBe(new List<string> { "Box1", "Box2", "Box3" });

        // Simulate reorder in the Gum tool: Box3 → first, others shift down.
        InstanceSave box3 = screen.Instances.Single(i => i.Name == "Box3");
        screen.Instances.Remove(box3);
        screen.Instances.Insert(0, box3);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        screenGue.Children.Select(c => c.Name!).ToList()
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
    public void Add_SiblingUnderNestedParent_DoesNotDuplicateExistingNestedInstance()
    {
        // Box1 lives under Holder1 (not directly under the screen), so it is not in the
        // screen's Children. The diff must still recognize it as already present.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Holder1");
        AddInstance(project, screen, "Box1");
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "Box1.Parent",
            Value = "Holder1",
            Type = "string",
            SetsValue = true
        });
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement holderGue = FindChildByName(screenGue, "Holder1")!;
        GraphicalUiElement box1Gue = FindChildByName(holderGue, "Box1")!;

        AddInstance(project, screen, "Box2");
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "Box2.Parent",
            Value = "Holder1",
            Type = "string",
            SetsValue = true
        });

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        holderGue.Children.Select(c => c.Name!).ShouldBe(new[] { "Box1", "Box2" });
        holderGue.Children[0].ShouldBeSameAs(box1Gue);
        screenGue.Children.Select(c => c.Name!).ShouldBe(new[] { "Holder1" });
        screenGue.ContainedElements.Count.ShouldBe(3);
    }

    [Fact]
    public void Remove_NestedInstance_DropsFromVisualTree()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Holder1");
        InstanceSave box1 = AddInstance(project, screen, "Box1");
        VariableSave box1Parent = new VariableSave
        {
            Name = "Box1.Parent",
            Value = "Holder1",
            Type = "string",
            SetsValue = true
        };
        screenDefault.Variables.Add(box1Parent);
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement holderGue = FindChildByName(screenGue, "Holder1")!;

        screen.Instances.Remove(box1);
        screenDefault.Variables.Remove(box1Parent);

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        holderGue.Children.Count.ShouldBe(0);
        screenGue.ContainedElements.Select(c => c.Name!).ShouldBe(new[] { "Holder1" });
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

    [Fact]
    public void Reorder_InstancesParentedToAnotherInstance_ReordersWithinThatParent()
    {
        // Box1/Box2 sit under Holder1 through a Parent variable, so they are not in the screen's
        // Children. Holder1 is a component instance with its own instance (Inner), which belongs
        // to the component, not the screen, and must keep its slot. Sub1/Sub2 sit one level
        // deeper, under Box1.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave holderComponent = AddComponent(project, "HolderComponent");
        holderComponent.Instances.Add(new InstanceSave
        {
            Name = "Inner",
            BaseType = "Container",
            ParentContainer = holderComponent
        });
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "Holder1", "HolderComponent");
        AddInstance(project, screen, "Box1");
        AddInstance(project, screen, "Box2");
        AddInstance(project, screen, "Sub1");
        AddInstance(project, screen, "Sub2");
        SetParent(screenDefault, "Box1", "Holder1");
        SetParent(screenDefault, "Box2", "Holder1");
        SetParent(screenDefault, "Sub1", "Box1");
        SetParent(screenDefault, "Sub2", "Box1");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        GraphicalUiElement holderGue = FindChildByName(screenGue, "Holder1")!;
        GraphicalUiElement box1Gue = FindChildByName(holderGue, "Box1")!;
        holderGue.Children.Select(c => c.Name!).ShouldBe(new[] { "Inner", "Box1", "Box2" });
        box1Gue.Children.Select(c => c.Name!).ShouldBe(new[] { "Sub1", "Sub2" });

        MoveInstanceToEnd(screen, "Box1");
        MoveInstanceToEnd(screen, "Sub1");

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        holderGue.Children.Select(c => c.Name!).ShouldBe(new[] { "Inner", "Box2", "Box1" });
        box1Gue.Children.Select(c => c.Name!).ShouldBe(new[] { "Sub2", "Sub1" });
    }

    [Fact]
    public void Reorder_ListBoxItemsInListBoxInnerPanel_KeepsItemsAndListBoxItemsInOrder()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        AddComponent(project, "TestListBox");
        AddComponent(project, "TestListBoxItem");
        ElementSaveExtensions.RegisterGueInstantiation(
            "TestListBox", () => (GraphicalUiElement)new ListBox().Visual);
        ElementSaveExtensions.RegisterGueInstantiation(
            "TestListBoxItem", () => (GraphicalUiElement)new ListBoxItem().Visual);
        (ScreenSave screen, StateSave screenDefault) = BuildScreen(project);
        AddInstance(project, screen, "List", "TestListBox");
        AddInstance(project, screen, "Item1", "TestListBoxItem");
        AddInstance(project, screen, "Item2", "TestListBoxItem");
        AddInstance(project, screen, "Item3", "TestListBoxItem");
        SetParent(screenDefault, "Item1", "List.InnerPanelInstance");
        SetParent(screenDefault, "Item2", "List.InnerPanelInstance");
        SetParent(screenDefault, "Item3", "List.InnerPanelInstance");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();
        ListBox listBox = (ListBox)((InteractiveGue)FindChildByName(screenGue, "List")!).FormsControlAsObject!;
        listBox.ListBoxItems.Select(i => i.Visual.Name!).ShouldBe(new[] { "Item1", "Item2", "Item3" });

        MoveInstanceToEnd(screen, "Item1");

        GumHotReloadManager.ApplyDiff(
            new[] { screenGue }, project, SystemManagers.Default);

        string[] expected = new[] { "Item2", "Item3", "Item1" };
        listBox.InnerPanel!.Children.Select(c => c.Name!).ShouldBe(expected);
        listBox.ListBoxItems.Select(i => i.Visual.Name!).ShouldBe(expected);
        listBox.Items!.Cast<ListBoxItem>().Select(i => i.Visual.Name!).ShouldBe(expected);
    }
}
