using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab;
using Moq;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

// Regression coverage for issue #3965: dragging a Component/Standard/Instance tree node onto the
// wireframe canvas silently did nothing. OnWireframeDragEnter/OnWireframeDrop detected dragged nodes
// via DragEventArgsExt.HasData<TreeNode>()/GetData<TreeNode>(), an exact-type format lookup that
// never matches a GumTreeNode-boxed single drag (WinForms keys a boxed payload by its *runtime* type
// name) or a multi-select TreeNode[] drag (the code checked List<TreeNode> first, which is never the
// shape MultiSelectTreeView.Theming actually boxes). MultiSelectTreeView.ExtractDraggedNodes already
// fixes this for the tree's own internal drag-reorder by scanning formats instead of doing an
// exact-type lookup; these tests pin that MainEditorTabPlugin now routes through it too.
public class MainEditorTabPluginDragDropTests : BaseTestClass
{
    [Fact]
    public void OnWireframeDragEnter_MultiSelectTreeNodeArray_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        GumTreeNode first = new GumTreeNode("Circle1");
        GumTreeNode second = new GumTreeNode("Circle2");
        TreeNode[] draggedArray = { first, second };
        DataObject dataObject = new DataObject((object)draggedArray);
        DragEventArgs args = new DragEventArgs(dataObject, 0, 0, 0, DragDropEffects.Copy | DragDropEffects.Move, DragDropEffects.None);

        plugin.OnWireframeDragEnter(null, args);

        args.Effect.ShouldBe(DragDropEffects.Copy);
    }

    [Fact]
    public void OnWireframeDragEnter_SingleGumTreeNodeBoxedAsPlainObject_AcceptsCopyEffect()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);
        dragDropManager
            .Setup(x => x.DecideWireframeDragEffect(false, true))
            .Returns(new DragAcceptDecision(true, null));

        // Mirrors MultiSelectTreeView.Theming.cs's single-node drag start:
        // DoDragDrop((object)nodeToDrag, ...), which boxes the concrete GumTreeNode.
        GumTreeNode draggedNode = new GumTreeNode("Circle1");
        DataObject dataObject = new DataObject((object)draggedNode);
        DragEventArgs args = new DragEventArgs(dataObject, 0, 0, 0, DragDropEffects.Copy | DragDropEffects.Move, DragDropEffects.None);

        plugin.OnWireframeDragEnter(null, args);

        args.Effect.ShouldBe(DragDropEffects.Copy);
    }

    [Fact]
    public void OnWireframeDrop_MultiSelectTreeNodeArray_CreatesInstanceForEachDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object firstTag = new();
        object secondTag = new();
        GumTreeNode first = new GumTreeNode("Circle1") { Tag = firstTag };
        GumTreeNode second = new GumTreeNode("Circle2") { Tag = secondTag };
        // Mirrors MultiSelectTreeView.Theming.cs's multi-select drag start: DoDragDrop(SelectedNodes.ToArray(), ...).
        TreeNode[] draggedArray = { first, second };
        DataObject dataObject = new DataObject((object)draggedArray);
        DragEventArgs args = new DragEventArgs(dataObject, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);

        plugin.OnWireframeDrop(null, args);

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(firstTag), Times.Once);
        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(secondTag), Times.Once);
    }

    [Fact]
    public void OnWireframeDrop_SingleGumTreeNodeBoxedAsPlainObject_CreatesInstanceFromDraggedTag()
    {
        MainEditorTabPlugin plugin = CreatePlugin(out Mock<IDragDropManager> dragDropManager);

        object draggedTag = new();
        GumTreeNode draggedNode = new GumTreeNode("Circle1") { Tag = draggedTag };
        DataObject dataObject = new DataObject((object)draggedNode);
        DragEventArgs args = new DragEventArgs(dataObject, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);

        plugin.OnWireframeDrop(null, args);

        dragDropManager.Verify(x => x.OnNodeObjectDroppedInWireframe(draggedTag), Times.Once);
    }

    // Stubs MainEditorTabPlugin headlessly without running its ~20-argument constructor (which stands
    // up a WireframeEditorFactory, SelectionManager, ScreenshotService, etc.) - see the
    // "Plugin/DI composition tests" entry in the gum-unit-tests skill. OnWireframeDragEnter/
    // OnWireframeDrop only touch _dragDropManager (and _guiCommands on the rejected-drop path, which
    // these tests don't exercise), so only that field needs to be wired up.
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
