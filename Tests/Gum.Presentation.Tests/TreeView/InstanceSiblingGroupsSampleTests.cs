using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests;

public class InstanceSiblingGroupsSampleTests : BaseTestClass
{
    [Theory]
    [InlineData("Samples/GameUiSamples/Content/GumProject/GameUiSamplesGumProject.gumx")]
    [InlineData("Samples/GumFormsSample/MonoGameGumFormsSample/Content/FormsGumProject/GumProject.gumx")]
    [InlineData("Samples/FnaGum/FnaSample/Content/GumProject/GumProject.gumx")]
    public void EverySampleElement_MatchesOldSiblings(string relative)
    {
        string dir = System.AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "Gum.slnx"))) dir = Path.GetDirectoryName(dir)!;
        GumProjectSave project = GumProjectSave.Load(Path.Combine(dir, relative), out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        int compared = 0;
        foreach (ElementSave element in project.AllElements)
        {
            if (element.DefaultState == null) continue;
            foreach (InstanceSave i in element.Instances) i.ParentContainer = element;
            InstanceSiblingGroups groups = new InstanceSiblingGroups(element);
            foreach (InstanceSave instance in element.Instances)
            {
                groups.SiblingsIncludingThis(instance)!.ShouldBe(instance.GetSiblingsIncludingThis(), element.Name + "." + instance.Name);
                compared++;
            }
        }
        compared.ShouldBeGreaterThan(50);
    }

    [Fact]
    public void RefreshUi_AfterReparentRenameDelete_NothingStale()
    {
        ComponentSave element = new ComponentSave { Name = "Mixed" };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        foreach (string name in new[] { "B", "A", "C", "D", "E", "F", "G" })
            element.Instances.Add(new InstanceSave { Name = name, BaseType = "Container", ParentContainer = element });
        element.States[0].SetValue("B.Parent", "A", "string");
        element.States[0].SetValue("D.Parent", "A.Inner", "string");
        element.States[0].SetValue("F.Parent", "B", "string");
        element.States[0].SetValue("G.Parent", "Ghost", "string");
        element.States[0].SetValue("C.Parent", "A", "string");
        element.States[0].SetValue("A.Parent", "A", "string"); // self-cycle
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(element);
        ElementTreeViewManager manager = CreateManager(new AutoMocker());
        GumTreeNode node = new GumTreeNode { Tag = element, Text = "Mixed" };
        manager.RefreshUi(node);
        string first = Describe(node);

        element.Instances.RemoveAll(x => x.Name == "C");
        element.States[0].SetValue("F.Parent", "A", "string");
        element.States[0].SetValue("D.Parent", "E", "string");
        manager.RefreshUi(node);
        string second = Describe(node);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "verify8294-tree.txt"), first + "\n" + second);
        second.ShouldBe(SecondExpected);
    }

    // filled per branch; same value must pass on main and PR
    private const string SecondExpected = "Mixed(E(D) G A(B F))";

    private static string Describe(GumTreeNode node) =>
        node.Nodes.Count == 0 ? node.Text
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
