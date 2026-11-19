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
/// The tree's sibling order comes from <see cref="InstanceSiblingGroups"/>, which must give every
/// instance the same list <see cref="InstanceExtensionMethods.GetSiblingsIncludingThis"/> does.
/// </summary>
public class InstanceSiblingGroupsTests : BaseTestClass
{
    private static ComponentSave NewComponent(string name, string? baseType = null)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static InstanceSave AddInstance(ComponentSave owner, string name, string baseType = "Container")
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    // Top-level A, E; B and D under A (D through "A.Inner"); F under B; G names a parent that does not exist.
    private static ComponentSave MixedParents()
    {
        ComponentSave element = NewComponent("Mixed");
        foreach (string name in new[] { "B", "A", "C", "D", "E", "F", "G" })
        {
            AddInstance(element, name);
        }
        element.States[0].SetValue("B.Parent", "A", "string");
        element.States[0].SetValue("D.Parent", "A.Inner", "string");
        element.States[0].SetValue("F.Parent", "B", "string");
        element.States[0].SetValue("G.Parent", "Ghost", "string");
        element.States[0].SetValue("C.Parent", "A", "string");
        return element;
    }

    [Fact]
    public void SiblingsIncludingThis_MatchesGetSiblingsIncludingThis_ForEveryInstance()
    {
        ComponentSave element = MixedParents();
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(element);

        InstanceSiblingGroups groups = new InstanceSiblingGroups(element);

        foreach (InstanceSave instance in element.Instances)
        {
            groups.SiblingsIncludingThis(instance)!.Select(x => x.Name)
                .ShouldBe(instance.GetSiblingsIncludingThis().Select(x => x.Name), instance.Name);
        }
        groups.SiblingsIncludingThis(element.Instances.Single(x => x.Name == "A"))!.Select(x => x.Name)
            .ShouldBe(new[] { "A", "E", "G" });
        groups.SiblingsIncludingThis(new InstanceSave { Name = "NotInTheElement" }).ShouldBeNull();
    }

    [Fact]
    public void SiblingsIncludingThis_ReadsAParentSetByTheBaseComponent()
    {
        ComponentSave baseComponent = NewComponent("Base");
        AddInstance(baseComponent, "P");
        AddInstance(baseComponent, "Q");
        baseComponent.States[0].SetValue("Q.Parent", "P", "string");
        ComponentSave derived = NewComponent("Derived", "Base");
        AddInstance(derived, "P").DefinedByBase = true;
        AddInstance(derived, "Q").DefinedByBase = true;
        AddInstance(derived, "R");
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(baseComponent);
        ObjectFinder.Self.GumProjectSave.Components.Add(derived);

        InstanceSiblingGroups groups = new InstanceSiblingGroups(derived);

        foreach (InstanceSave instance in derived.Instances)
        {
            groups.SiblingsIncludingThis(instance)!.Select(x => x.Name)
                .ShouldBe(instance.GetSiblingsIncludingThis().Select(x => x.Name), instance.Name);
        }
        groups.SiblingsIncludingThis(derived.Instances[1])!.Select(x => x.Name).ShouldBe(new[] { "Q" });
    }

    [Fact]
    public void RefreshUi_MixedParents_BuildsTheSameTreeAfterOnePass()
    {
        // Pins the tree's contents and order through the sibling lookup (one pass, as on project load).
        ComponentSave element = MixedParents();
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(element);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = element, Text = "Mixed" };

        manager.RefreshUi(node);

        Describe(node).ShouldBe("Mixed(A(B(F) C D) E G)");
    }

    private static string Describe(GumTreeNode node) =>
        node.Nodes.Count == 0
            ? node.Text
            : node.Text + "(" + string.Join(" ", node.Nodes.Cast<GumTreeNode>().Select(Describe)) + ")";

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
}
