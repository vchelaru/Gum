using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Shouldly;
using System.Windows;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

// Issue #4123: dragging a Project-panel search result must produce the same drag payload a dragged
// tree node does, so the wireframe drop side reads it identically with no drop-side changes. Asserted
// through WpfWireframeDropPayloadReader - the same extraction the canvas performs - rather than on
// the payload directly, so the two sides stay pinned together.
public class FlatSearchListBoxDragTests : BaseTestClass
{
    // The payload is a static slot shared by every drag, so a test that sets it must not leak into
    // the next one.
    public override void Dispose()
    {
        TreeDragPayload.Clear();
        base.Dispose();
    }

    [StaFact]
    public void CreateDragData_PublishesBackingObjectAsTheDraggedTag()
    {
        ComponentSave backingObject = new ComponentSave { Name = "MyComponent" };
        SearchItemViewModel item = new SearchItemViewModel { BackingObject = backingObject };

        DataObject data = FlatSearchListBox.CreateDragData(item);

        WireframeDropPayload payload = WpfWireframeDropPayloadReader.Read(data);
        payload.NodeTags.ShouldHaveSingleItem();
        payload.NodeTags![0].ShouldBeSameAs(backingObject);
    }
}
