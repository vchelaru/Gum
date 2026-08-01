using Gum.Managers;
using Shouldly;
using System;
using System.Windows;

namespace GumToolUnitTests.Managers;

// Regression coverage for issues #3965/#4123: a drag out of the element tree (or out of the flat
// search results) puts only a marker format on the data object and carries the dragged items
// themselves in TreeDragPayload. If the reader stops surfacing those items' tags, dropping onto the
// wireframe canvas silently does nothing.
public class WpfWireframeDropPayloadReaderTests : IDisposable
{
    // The payload is a static slot shared by every drag, so a test that sets it must not leak into
    // the next one.
    public void Dispose() => TreeDragPayload.Clear();

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
    public void Read_MultipleDraggedNodes_ReturnsEachNodesTagInOrder()
    {
        GumTreeNode first = new GumTreeNode("Circle1") { Tag = "InstanceCircle1" };
        GumTreeNode second = new GumTreeNode("Circle2") { Tag = "InstanceCircle2" };
        TreeDragPayload.SetNodes(new[] { first, second });

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(CreateTreeDragData());

        payload.NodeTags.ShouldBe(new object[] { "InstanceCircle1", "InstanceCircle2" });
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
    public void Read_SingleDraggedNode_ReturnsItsTagInNodeTags()
    {
        GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = "InstanceCircle1" };
        TreeDragPayload.SetNodes(new[] { draggedNode });

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(CreateTreeDragData());

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
    public void Read_TagsWithoutNodes_ReturnsThoseTags()
    {
        // A search result stands in for a node that may not be realized in the tree, so it publishes
        // tags with no nodes behind them.
        object backingObject = new();
        TreeDragPayload.SetTags(new object?[] { backingObject });

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(CreateTreeDragData());

        payload.NodeTags.ShouldHaveSingleItem();
        payload.NodeTags![0].ShouldBeSameAs(backingObject);
    }

    [StaFact]
    public void Read_TreeDragFormatPresentWithoutPayload_ReturnsNoNodeTags()
    {
        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(CreateTreeDragData());

        payload.NodeTags.ShouldBeNull();
    }

    private static DataObject CreateTreeDragData() => new DataObject(TreeDragPayload.DataFormat, true);
}
