using CommonFormsAndControls;
using Shouldly;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class MultiSelectTreeViewNavigationTests : BaseTestClass
{
    private readonly MultiSelectTreeView _treeView;

    public MultiSelectTreeViewNavigationTests()
    {
        _treeView = new MultiSelectTreeView
        {
            // Mirrors ElementTreeViewCreator's real configuration (ElementTreeViewCreator.cs),
            // since the mis-select bug only reproduces under this combination.
            IsSelectingOnPush = false,
            MultiSelectBehavior = MultiSelectBehavior.CtrlDown
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView.Dispose();
    }

    private static void RaiseMouseDown(MultiSelectTreeView treeView, MouseButtons button)
    {
        InvokeProtected(treeView, "OnMouseDown", new MouseEventArgs(button, 1, 1, 0, 0));
    }

    /// <summary>
    /// OnMouseDown calls base.OnMouseDown(e), which can force the native window handle to be
    /// created (WinForms mouse-capture handling touches .Handle), requiring an STA thread the
    /// same way TreeNode.Expand()/Collapse() do (see TreeViewStateServiceTests). xUnit's default
    /// runner is MTA.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        Thread thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught != null)
        {
            throw new TargetInvocationException(caught);
        }
    }

    // Does not go through OnMouseUp's GetNodeAt hit-test, which requires a real native window
    // handle - unsafe to force headlessly here (WinForms drag/drop registration needs an STA
    // thread, which this test host does not guarantee, and forcing it crashed the test process).
    private static bool InvokeShouldSelectOnMouseUp(MultiSelectTreeView treeView, TreeNode node, MouseButtons button)
    {
        MethodInfo? method = typeof(MultiSelectTreeView).GetMethod(
            "ShouldSelectOnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        return (bool)method!.Invoke(treeView, new object[] { node, button })!;
    }

    private static void InvokeProtected(object target, string methodName, object arg)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        method!.Invoke(target, new[] { arg });
    }

    [Fact]
    public void ShouldSelectOnMouseUp_XButton1_ReturnsFalse()
    {
        TreeNode node = new TreeNode("SomeNode");
        _treeView.Nodes.Add(node);

        InvokeShouldSelectOnMouseUp(_treeView, node, MouseButtons.XButton1).ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelectOnMouseUp_XButton2_ReturnsFalse()
    {
        TreeNode node = new TreeNode("SomeNode");
        _treeView.Nodes.Add(node);

        InvokeShouldSelectOnMouseUp(_treeView, node, MouseButtons.XButton2).ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelectOnMouseUp_LeftButton_ReturnsTrue()
    {
        TreeNode node = new TreeNode("SomeNode");
        _treeView.Nodes.Add(node);

        InvokeShouldSelectOnMouseUp(_treeView, node, MouseButtons.Left).ShouldBeTrue();
    }

    [Fact]
    public void ShouldSelectOnMouseUp_RightButton_ReturnsFalse()
    {
        // Pins pre-existing behavior: right-click opens the context menu, it must not select.
        TreeNode node = new TreeNode("SomeNode");
        _treeView.Nodes.Add(node);

        InvokeShouldSelectOnMouseUp(_treeView, node, MouseButtons.Right).ShouldBeFalse();
    }

    [Fact]
    public void OnMouseDown_XButton1_RaisesNavigateBackRequested() => RunOnSta(() =>
    {
        var raised = false;
        _treeView.NavigateBackRequested += (_, _) => raised = true;

        RaiseMouseDown(_treeView, MouseButtons.XButton1);

        raised.ShouldBeTrue();
    });

    [Fact]
    public void OnMouseDown_XButton2_RaisesNavigateForwardRequested() => RunOnSta(() =>
    {
        var raised = false;
        _treeView.NavigateForwardRequested += (_, _) => raised = true;

        RaiseMouseDown(_treeView, MouseButtons.XButton2);

        raised.ShouldBeTrue();
    });

    [Fact]
    public void OnMouseDown_LeftButton_DoesNotRaiseNavigationEvents() => RunOnSta(() =>
    {
        var raised = false;
        _treeView.NavigateBackRequested += (_, _) => raised = true;
        _treeView.NavigateForwardRequested += (_, _) => raised = true;

        RaiseMouseDown(_treeView, MouseButtons.Left);

        raised.ShouldBeFalse();
    });
}
