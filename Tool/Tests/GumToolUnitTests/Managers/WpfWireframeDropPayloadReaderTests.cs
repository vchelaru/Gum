using Gum.Managers;
using Shouldly;
using System.Windows;
using WinFormsTreeNode = System.Windows.Forms.TreeNode;

namespace GumToolUnitTests.Managers;

// Regression coverage for the same bug MultiSelectTreeViewExtractDraggedNodesTests pins: WinForms
// (and the WPF DataObject that receives the same OLE payload across the WindowsFormsHost boundary)
// keys stored data by the payload's *runtime* type full name, not its static/base type. A dragged
// node is always a GumTreeNode (ElementTreeViewManager never constructs a plain TreeNode) and a
// multi-select drag boxes a WinFormsTreeNode[] array (MultiSelectTreeView.Theming's
// SelectedNodes.ToArray()), not a List<WinFormsTreeNode> - an exact-type format lookup for either
// the wrong container type or the base TreeNode type would silently drop every dragged node.
public class WpfWireframeDropPayloadReaderTests
{
    [StaFact]
    public void Read_FileDropFormatPresent_SetsFiles()
    {
        string[] files = { "C:\\texture.png" };
        DataObject data = new DataObject(DataFormats.FileDrop, files);

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);

        payload.Files.ShouldBe(files);
        payload.StandardElementTypeName.ShouldBeNull();
        payload.NodeTags.ShouldBeNull();
    }

    [StaFact]
    public void Read_NoRecognizedFormats_ReturnsEmptyPayload()
    {
        DataObject data = new DataObject("SomeUnrelatedFormat", "value");

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);

        payload.StandardElementTypeName.ShouldBeNull();
        payload.NodeTags.ShouldBeNull();
        payload.Files.ShouldBeNull();
    }

    [Fact]
    public void Read_NullData_ReturnsEmptyPayload()
    {
        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(null);

        payload.StandardElementTypeName.ShouldBeNull();
        payload.NodeTags.ShouldBeNull();
        payload.Files.ShouldBeNull();
    }

    [StaFact]
    public void Read_SingleGumTreeNodeBoxedAsPlainObject_ReturnsItsTagInNodeTags()
    {
        GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = "InstanceCircle1" };
        // Mirrors MultiSelectTreeView.Theming.cs's single-node drag start: DoDragDrop((object)nodeToDrag, ...).
        DataObject data = new DataObject((object)draggedNode);

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);

        payload.NodeTags.ShouldHaveSingleItem();
        payload.NodeTags![0].ShouldBe("InstanceCircle1");
    }

    [StaFact]
    public void Read_StandardChipFormatPresent_SetsStandardElementTypeName()
    {
        DataObject data = new DataObject(DragDropManager.StandardElementNameDataFormat, "Button");

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);

        payload.StandardElementTypeName.ShouldBe("Button");
        payload.NodeTags.ShouldBeNull();
        payload.Files.ShouldBeNull();
    }

    [StaFact]
    public void Read_TreeNodeArrayBoxed_ReturnsEachNodesTagInOrder()
    {
        GumTreeNode first = new GumTreeNode("Circle1") { Tag = "InstanceCircle1" };
        GumTreeNode second = new GumTreeNode("Circle2") { Tag = "InstanceCircle2" };
        // Mirrors MultiSelectTreeView.Theming.cs's multi-select drag start: DoDragDrop(SelectedNodes.ToArray(), ...).
        WinFormsTreeNode[] dragged = { first, second };
        DataObject data = new DataObject((object)dragged);

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);

        payload.NodeTags.ShouldBe(new object[] { "InstanceCircle1", "InstanceCircle2" });
    }
}
