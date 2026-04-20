using Gum.Forms.Controls;
using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Regression guard for <see cref="FrameworkElement.MainKeyboard"/> wiring on Raylib.
/// <c>FormsUtilities.InitializeDefaults</c> (called by the Raylib test assembly bootstrap)
/// assigns <c>MainKeyboard</c> unconditionally; this test pins that behavior so a future
/// re-gate on <c>XNALIKE</c> (or similar) is caught immediately.
/// </summary>
public class MainKeyboardTests : BaseTestClass
{
    [Fact]
    public void MainKeyboard_AfterInitializeDefaults_IsNonNull()
    {
        FrameworkElement.MainKeyboard.ShouldNotBeNull();
    }
}
