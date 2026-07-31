using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab;
using Moq;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Xunit;
using WinFormsTreeNode = System.Windows.Forms.TreeNode;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

// Regression coverage for issue #3965: dragging a Component/Standard/Instance tree node onto the
// wireframe canvas silently did nothing, because the drag payload was detected via an exact-type
// format lookup that never matches a GumTreeNode-boxed single drag (a boxed payload is keyed by its
// *runtime* type name) or a multi-select TreeNode[] drag. These tests pin that the canvas's
// accept/drop routing still sees both shapes, end to end from the OLE data object through
// WpfWireframeDropPayloadReader (the WPF canvas's extraction) into the plugin.
public class MainEditorTabPluginDragDropTests : BaseTestClass
{
    [StaFact]
    public void DecideWireframeDropEffect_MultiSelectTreeNodeArray_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        GumTreeNode first = new GumTreeNode("Circle1");
        GumTreeNode second = new GumTreeNode("Circle2");
        WinFormsTreeNode[] draggedArray = { first, second };

        DragDropEffects effects = plugin.DecideWireframeDropEffect(ReadPayload(draggedArray), reportBlockedReason: true);

        effects.ShouldBe(DragDropEffects.Copy);
    }

    [StaFact]
    public void DecideWireframeDropEffect_SingleGumTreeNodeBoxedAsPlainObject_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        // Mirrors MultiSelectTreeView.Theming.cs's single-node drag start:
        // DoDragDrop((object)nodeToDrag, ...), which boxes the concrete GumTreeNode.
        GumTreeNode draggedNode = new GumTreeNode("Circle1");

        DragDropEffects effects = plugin.DecideWireframeDropEffect(ReadPayload(draggedNode), reportBlockedReason: true);

        effects.ShouldBe(DragDropEffects.Copy);
    }

    [StaFact]
    public void HandleWireframeDrop_MultiSelectTreeNodeArray_CreatesInstanceForEachDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object firstTag = new();
        object secondTag = new();
        GumTreeNode first = new GumTreeNode("Circle1") { Tag = firstTag };
        GumTreeNode second = new GumTreeNode("Circle2") { Tag = secondTag };
        // Mirrors MultiSelectTreeView.Theming.cs's multi-select drag start: DoDragDrop(SelectedNodes.ToArray(), ...).
        WinFormsTreeNode[] draggedArray = { first, second };

        plugin.HandleWireframeDrop(ReadPayload(draggedArray));

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(firstTag), Times.Once);
        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(secondTag), Times.Once);
    }

    [StaFact]
    public void HandleWireframeDrop_SingleGumTreeNodeBoxedAsPlainObject_CreatesInstanceFromDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object draggedTag = new();
        GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = draggedTag };

        plugin.HandleWireframeDrop(ReadPayload(draggedNode));

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(draggedTag), Times.Once);
    }

    [StaFact]
    public void HandleWireframeDrop_DraggedNodeTagIsNull_FallsBackToSearchResultDragPayload()
    {
        // Issue #4123: a search-result drag (FlatSearchListBox) boxes its backing object into
        // TreeNode.Tag, but Tag comes back null after crossing the drag boundary (confirmed
        // empirically: ComponentSave/ElementSave aren't [Serializable], and TreeNode's own
        // serialization only carries Tag across when its type is). SearchResultDragPayload is
        // the in-process fallback that carries the real object instead.
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object backingObject = new();
        SearchResultDragPayload.Current = backingObject;
        try
        {
            GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = null };

            plugin.HandleWireframeDrop(ReadPayload(draggedNode));

            dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(backingObject), Times.Once);
        }
        finally
        {
            SearchResultDragPayload.Current = null;
        }
    }

    private static WireframeDropPayload ReadPayload(object draggedData) =>
        WpfWireframeDropPayloadReader.Read(new DataObject(draggedData));

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
