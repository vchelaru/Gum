using CommonFormsAndControls;
using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class MultiSelectTreeViewHandleExceptionTests : BaseTestClass
{
    private readonly MultiSelectTreeView _treeView;

    public MultiSelectTreeViewHandleExceptionTests()
    {
        _treeView = new MultiSelectTreeView();
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView.Dispose();
    }

    [Fact]
    public void HandleException_ShouldRaiseUnhandledException_WithTheCaughtException()
    {
        // MultiSelectTreeView has no dependency on the host application's dialog service; it
        // hands the exception off via UnhandledException instead (ElementTreeViewManager
        // subscribes and forwards to IDialogService).
        Exception exception = new InvalidOperationException("Something went wrong crawling the tree");
        Exception? raised = null;
        _treeView.UnhandledException += ex => raised = ex;

        MethodInfo? handleExceptionMethod = typeof(MultiSelectTreeView).GetMethod(
            "HandleException", BindingFlags.Instance | BindingFlags.NonPublic);
        handleExceptionMethod.ShouldNotBeNull();

        handleExceptionMethod!.Invoke(_treeView, new object[] { exception });

        raised.ShouldBe(exception);
    }
}
