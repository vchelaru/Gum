using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// #7005: an instance listed in an element's Instances whose ParentContainer still names another
/// element (a clone of a base's instance) gave the tree refresh a sibling index of -1, which threw
/// and disabled the whole tree plugin for the session.
/// </summary>
public class ElementTreeViewManagerStaleInstanceOwnerTests : BaseTestClass
{
    private static ElementTreeViewManager CreateManager(AutoMocker mocker)
    {
        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        TreeSelectionModel selection = new TreeSelectionModel(nodes, () => TreeModifierKeys.None, _ => { });
        Mock<IElementTreeView> view = mocker.GetMock<IElementTreeView>();
        view.Setup(x => x.Nodes).Returns(nodes);
        view.Setup(x => x.Selection).Returns(selection);
        mocker.GetMock<IElementTreeViewFactory>().Setup(x => x.Create()).Returns(view.Object);
        ElementTreeViewManager manager = mocker.CreateInstance<ElementTreeViewManager>();
        manager.Initialize();
        return manager;
    }

    private static ComponentSave NewComponent(string name, string? baseType = null)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static InstanceSave AddInstance(ComponentSave owner, string name)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = "Container", ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    /// <summary>Adds a copy of each base instance to the derived component, the way the load repair does, leaving the clone's owner as it came.</summary>
    private static void AddCloneKeepingBaseOwner(ComponentSave derived, InstanceSave fromBase)
    {
        InstanceSave clone = fromBase.Clone();
        clone.DefinedByBase = true;
        derived.Instances.Add(clone);
    }

    private static List<string> ChildTexts(GumTreeNode node) => node.Nodes.Select(child => child.Text).ToList();

    private static void ObjectFinderProject(params ComponentSave[] components)
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.AddRange(components);
        ObjectFinder.Self.GumProjectSave = project;
    }

    [Fact]
    public void RefreshUi_InstanceOwnedByTheBaseComponent_DoesNotThrowAndListsEveryInstance()
    {
        ComponentSave baseComponent = NewComponent("Base");
        InstanceSave a = AddInstance(baseComponent, "A");
        InstanceSave b = AddInstance(baseComponent, "B");
        ComponentSave derived = NewComponent("Derived", "Base");
        AddCloneKeepingBaseOwner(derived, a);
        AddCloneKeepingBaseOwner(derived, b);
        ObjectFinderProject(baseComponent, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = derived, Text = "Derived" };

        Should.NotThrow(() => manager.RefreshUi(node));

        ChildTexts(node).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void RefreshUi_InstanceOwnedByTheBaseComponent_PointsItBackAtItsRealElement()
    {
        ComponentSave baseComponent = NewComponent("Base");
        AddCloneKeepingBaseOwner(NewComponent("Throwaway"), AddInstance(baseComponent, "A"));
        ComponentSave derived = NewComponent("Derived", "Base");
        AddCloneKeepingBaseOwner(derived, baseComponent.Instances[0]);
        ObjectFinderProject(baseComponent, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());

        manager.RefreshUi(new GumTreeNode { Tag = derived, Text = "Derived" });

        derived.Instances.Single().ParentContainer.ShouldBeSameAs(derived);
        baseComponent.Instances.Single().ParentContainer.ShouldBeSameAs(baseComponent);
    }

    [Fact]
    public void RefreshUi_InstanceOwnedByTheBaseComponent_NestsItUnderTheParentTheDerivedElementSets()
    {
        // The parent lookup read the stale owner's default state, so a derived-only Parent was ignored.
        ComponentSave baseComponent = NewComponent("Base");
        InstanceSave a = AddInstance(baseComponent, "A");
        ComponentSave derived = NewComponent("Derived", "Base");
        AddCloneKeepingBaseOwner(derived, a);
        AddInstance(derived, "Own");
        derived.DefaultState.SetValue("A.Parent", "Own", "string");
        ObjectFinderProject(baseComponent, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = derived, Text = "Derived" };

        // The tool refreshes more than once; a child listed ahead of its parent nests on the next pass.
        manager.RefreshUi(node);
        manager.RefreshUi(node);

        ChildTexts(node).ShouldBe(new[] { "Own" });
        ChildTexts(node.Nodes[0]).ShouldBe(new[] { "A" });
    }

    [Fact]
    public void RefreshUi_InstanceOwnedByAnUnrelatedElement_DoesNotThrow()
    {
        ComponentSave other = NewComponent("Other");
        InstanceSave stray = AddInstance(other, "Stray");
        ComponentSave derived = NewComponent("Derived");
        AddCloneKeepingBaseOwner(derived, stray);
        AddInstance(derived, "Own");
        ObjectFinderProject(other, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = derived, Text = "Derived" };

        Should.NotThrow(() => manager.RefreshUi(node));

        ChildTexts(node).ShouldBe(new[] { "Stray", "Own" });
    }

    [Fact]
    public void RefreshUi_InstanceWithNoOwner_ListsItInOrder()
    {
        ComponentSave component = NewComponent("Component");
        AddInstance(component, "First");
        InstanceSave orphaned = AddInstance(component, "Second");
        orphaned.ParentContainer = null;
        ObjectFinderProject(component);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = component, Text = "Component" };

        Should.NotThrow(() => manager.RefreshUi(node));

        ChildTexts(node).ShouldBe(new[] { "First", "Second" });
    }

    [Fact]
    public void RefreshUi_RefreshedTwiceWithAStaleOwner_KeepsTheOrder()
    {
        ComponentSave baseComponent = NewComponent("Base");
        InstanceSave a = AddInstance(baseComponent, "A");
        InstanceSave b = AddInstance(baseComponent, "B");
        InstanceSave c = AddInstance(baseComponent, "C");
        ComponentSave derived = NewComponent("Derived", "Base");
        AddCloneKeepingBaseOwner(derived, c);
        AddCloneKeepingBaseOwner(derived, a);
        AddCloneKeepingBaseOwner(derived, b);
        ObjectFinderProject(baseComponent, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = derived, Text = "Derived" };

        manager.RefreshUi(node);
        manager.RefreshUi(node);

        ChildTexts(node).ShouldBe(new[] { "C", "A", "B" });
    }

    [Fact]
    public void RefreshUi_AStaleOwnerOnOneInstanceDoesNotStopTheOthers()
    {
        ComponentSave baseComponent = NewComponent("Base");
        InstanceSave a = AddInstance(baseComponent, "A");
        ComponentSave derived = NewComponent("Derived", "Base");
        AddInstance(derived, "Healthy");
        AddCloneKeepingBaseOwner(derived, a);
        AddInstance(derived, "AlsoHealthy");
        ObjectFinderProject(baseComponent, derived);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = derived, Text = "Derived" };

        manager.RefreshUi(node);

        ChildTexts(node).ShouldBe(new[] { "Healthy", "A", "AlsoHealthy" });
    }
}
