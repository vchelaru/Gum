using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab;
using Moq;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

// Regression coverage for issues #3965/#4123: dragging a Component/Standard/Instance tree node - or
// a Project-panel search result - onto the wireframe canvas silently did nothing, because the drag
// payload was detected by scanning the data object's formats for a widget type. These tests pin that
// the canvas's accept/drop routing still sees a single-node drag, a multi-select drag, and a search
// result, end to end from the OLE data object through WpfWireframeDropPayloadReader (the WPF
// canvas's extraction) into the plugin.
public class MainEditorTabPluginDragDropTests : BaseTestClass
{
    // The payload is a static slot shared by every drag, so a test that sets it must not leak into
    // the next one.
    public override void Dispose()
    {
        TreeDragPayload.Clear();
        base.Dispose();
    }

    [StaFact]
    public void DecideWireframeDropEffect_MultipleDraggedNodes_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        GumTreeNode first = new GumTreeNode("Circle1");
        GumTreeNode second = new GumTreeNode("Circle2");

        DragDropEffects effects = plugin.DecideWireframeDropEffect(
            ReadNodePayload(first, second), reportBlockedReason: true);

        effects.ShouldBe(DragDropEffects.Copy);
    }

    [StaFact]
    public void DecideWireframeDropEffect_SingleDraggedNode_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        GumTreeNode draggedNode = new GumTreeNode("Circle1");

        DragDropEffects effects = plugin.DecideWireframeDropEffect(
            ReadNodePayload(draggedNode), reportBlockedReason: true);

        effects.ShouldBe(DragDropEffects.Copy);
    }

    [StaFact]
    public void HandleWireframeDrop_MultipleDraggedNodes_CreatesInstanceForEachDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object firstTag = new();
        object secondTag = new();
        GumTreeNode first = new GumTreeNode("Circle1") { Tag = firstTag };
        GumTreeNode second = new GumTreeNode("Circle2") { Tag = secondTag };

        plugin.HandleWireframeDrop(ReadNodePayload(first, second));

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(firstTag), Times.Once);
        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(secondTag), Times.Once);
    }

    [StaFact]
    public void HandleWireframeDrop_SearchResultDrag_CreatesInstanceFromBackingObject()
    {
        // Issue #4123: a search result has no tree node behind it, so it publishes its backing object
        // as a tag on its own. It has to reach the canvas the same way a dragged node's tag does.
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object backingObject = new();
        TreeDragPayload.SetTags(new object?[] { backingObject });

        plugin.HandleWireframeDrop(WpfWireframeDropPayloadReader.Read(CreateTreeDragData()));

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(backingObject), Times.Once);
    }

    [StaFact]
    public void HandleWireframeDrop_SingleDraggedNode_CreatesInstanceFromDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object draggedTag = new();
        GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = draggedTag };

        plugin.HandleWireframeDrop(ReadNodePayload(draggedNode));

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(draggedTag), Times.Once);
    }

    private static WireframeDropPayload ReadNodePayload(params GumTreeNode[] draggedNodes)
    {
        TreeDragPayload.SetNodes(draggedNodes);
        return WpfWireframeDropPayloadReader.Read(CreateTreeDragData());
    }

    private static DataObject CreateTreeDragData() => new DataObject(TreeDragPayload.DataFormat, true);

    // Stubs MainEditorTabPlugin headlessly without running its ~20-argument constructor (which stands
    // up a WireframeEditorFactory, SelectionManager, ScreenshotService, etc.) - see the
    // "Plugin/DI composition tests" entry in the gum-unit-tests skill. DecideWireframeDropEffect/
    // HandleWireframeDrop only touch _dragDropManager (and _guiCommands on the rejected-drop path,
    // which these tests don't exercise), so only that field needs to be wired up.
    private static MainEditorTabPlugin CreatePlugin(out Mock<IDragDropManager> dragDropManager)
    {
        MainEditorTabPlugin plugin = (MainEditorTabPlugin)RuntimeHelpers.GetUninitializedObject(typeof(MainEditorTabPlugin));

        dragDropManager = new Mock<IDragDropManager>();
        SetField(plugin, "_dragDropManager", dragDropManager.Object);

        return plugin;
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = typeof(MainEditorTabPlugin).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
