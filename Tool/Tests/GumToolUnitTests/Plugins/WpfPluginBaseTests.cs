using Gum;
using Gum.Gui.Windows;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Numerics;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Plugins;

// Characterization tests pinning behavior across the WpfPluginBase extraction (#3942): the
// TreeNode -> ITreeNode, InputLibrary.Cursor -> IGumCursorState, and FrameworkElement -> object
// retypes on PluginBase still flow the same instances through, and the Delete-dialog events newly
// declared on WpfPluginBase (rather than PluginBase itself) still forward correctly.
public class WpfPluginBaseTests
{
    [Fact]
    public void CallTreeNodeSelected_InvokesEventWithSameTreeNodeInstance()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        ITreeNode? received = null;
        plugin.TreeNodeSelected += node => received = node;
        FakeTreeNode treeNode = new FakeTreeNode();

        plugin.CallTreeNodeSelected(treeNode);

        received.ShouldBeSameAs(treeNode);
    }

    [Fact]
    public void CallStateWindowTreeNodeSelected_InvokesEventWithSameTreeNodeInstance()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        ITreeNode? received = null;
        plugin.StateWindowTreeNodeSelected += node => received = node;
        FakeTreeNode treeNode = new FakeTreeNode();

        plugin.CallStateWindowTreeNodeSelected(treeNode);

        received.ShouldBeSameAs(treeNode);
    }

    [Fact]
    public void CallGetWorldCursorPosition_InvokesEventWithSameCursorAndReturnsResult()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        IGumCursorState? received = null;
        plugin.GetWorldCursorPosition += cursor =>
        {
            received = cursor;
            return new Vector2(cursor.X, cursor.Y);
        };
        FakeCursorState cursorState = new FakeCursorState { X = 12, Y = 34 };

        Vector2? result = plugin.CallGetWorldCursorPosition(cursorState);

        received.ShouldBeSameAs(cursorState);
        result.ShouldBe(new Vector2(12, 34));
    }

    [Fact]
    public void AddControl_ForwardsArbitraryObjectControlToTabManager()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        Mock<ITabManager> tabManagerMock = new Mock<ITabManager>();
        plugin.TabManager = tabManagerMock.Object;
        // Not a FrameworkElement - proves the retyped `object control` parameter no longer
        // requires a WPF type to flow through to ITabManager.AddControl (which already took object).
        object arbitraryControl = new object();

        plugin.AddControl(arbitraryControl, "My Tab", TabLocation.CenterBottom);

        tabManagerMock.Verify(m => m.AddControl(arbitraryControl, "My Tab", TabLocation.CenterBottom), Times.Once);
    }

    [Fact]
    public void CallDeleteOptionsWindowShow_InvokesEventWithSameArguments()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        DeleteOptionsWindow? receivedWindow = null;
        Array? receivedObjects = null;
        plugin.DeleteOptionsWindowShow += (window, objects) =>
        {
            receivedWindow = window;
            receivedObjects = objects;
        };
        Array objectsToDelete = new object[] { "a" };

        plugin.CallDeleteOptionsWindowShow(null!, objectsToDelete);

        receivedWindow.ShouldBeNull();
        receivedObjects.ShouldBeSameAs(objectsToDelete);
    }

    [Fact]
    public void CallDeleteConfirmed_InvokesEventWithSameArguments()
    {
        TestWpfPlugin plugin = new TestWpfPlugin();
        Array? receivedObjects = null;
        plugin.DeleteConfirmed += (window, objects) => receivedObjects = objects;
        Array deletedObjects = new object[] { "b" };

        plugin.CallDeleteConfirmed(null!, deletedObjects);

        receivedObjects.ShouldBeSameAs(deletedObjects);
    }

    private sealed class TestWpfPlugin : WpfPluginBase
    {
        public override string FriendlyName => "Test Wpf Plugin";
        public override void StartUp() { }
        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

    private sealed class FakeTreeNode : ITreeNode
    {
        public object? Tag => null;
        public string Text { get; set; } = "";
        public string FullPath => "";
        public ITreeNode? Parent => null;
        public IEnumerable<ITreeNode> Children => Array.Empty<ITreeNode>();
        public FilePath GetFullFilePath() => new FilePath(FullPath);
        public void Expand() { }
    }

    private sealed class FakeCursorState : IGumCursorState
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float XChange => 0;
        public float YChange => 0;
        public bool PrimaryDown => false;
        public bool PrimaryPush => false;
        public bool PrimaryClick => false;
        public bool IsInWindow => true;
        public bool PrimaryDoubleClick => false;
        public bool SecondaryPush => false;
        public bool PrimaryDownIgnoringIsInWindow => false;
        public void SetCursor(GumCursorKind kind) { }
    }
}
