using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Tests for Tab / Shift+Tab focus navigation on Raylib. Exercises the shared
/// <c>FrameworkElement.HandleKeyboardFocusUpdate</c> path (the
/// <c>#if RAYLIB</c> branch inside that method). This is the code that
/// <c>TextBoxBase.OnFocusUpdate</c>, <c>ScrollViewer.OnFocusUpdate</c>,
/// <c>ListBox.OnFocusUpdate</c>, and <c>ComboBox.OnFocusUpdate</c> now invoke
/// on Raylib after Bucket 2 sub-task 3 widened their guards from
/// platform-specific conjunctions to just <c>!FRB</c>.
///
/// Building a real <see cref="TextBox"/> / <see cref="ListBox"/> / etc. on
/// Raylib requires <see cref="FormsUtilities.InitializeDefaults"/> with a
/// fully-initialized <c>SystemManagers</c> + Renderer + sprite sheet, which
/// the existing Raylib test infrastructure does not set up. Instead, these
/// tests cover the shared focus-update path via a thin
/// <see cref="FrameworkElement"/> harness wired to a
/// <see cref="ContainerRuntime"/> visual.
/// </summary>
public class FocusTabNavigationTests : BaseTestClass
{
    private sealed class FocusUpdateHarness : FrameworkElement, IInputReceiver
    {
        public FocusUpdateHarness(InteractiveGue visual) : base(visual) { }

        public void InvokeHandleKeyboardFocusUpdate() => HandleKeyboardFocusUpdate();

        public IInputReceiver? ParentInputReceiver => null;

        public void OnGainFocus() { }

        public void OnLoseFocus() { }

        public void OnFocusUpdate() { }

        public void OnFocusUpdatePreview(RoutedEventArgs args) { }

        public void DoKeyboardAction(IInputReceiverKeyboard keyboard) { }
    }

    private static FocusUpdateHarness CreateHarness(ContainerRuntime parent)
    {
        ContainerRuntime visual = new ContainerRuntime();
        visual.Parent = parent;
        FocusUpdateHarness harness = new FocusUpdateHarness(visual);
        return harness;
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabPressed_MovesFocusToPreviousFocusableSibling()
    {
        ContainerRuntime root = new ContainerRuntime();

        FocusUpdateHarness harness1 = CreateHarness(root);
        FocusUpdateHarness harness2 = CreateHarness(root);

        harness2.IsFocused = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        harness2.InvokeHandleKeyboardFocusUpdate();

        harness2.IsFocused.ShouldBeFalse();
        harness1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabPressed_MovesFocusToNextFocusableSibling()
    {
        ContainerRuntime root = new ContainerRuntime();

        FocusUpdateHarness harness1 = CreateHarness(root);
        FocusUpdateHarness harness2 = CreateHarness(root);

        harness1.IsFocused = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        harness1.InvokeHandleKeyboardFocusUpdate();

        harness1.IsFocused.ShouldBeFalse();
        harness2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabPressedAtEnd_WrapsToFirst()
    {
        ContainerRuntime grandparent = new ContainerRuntime();
        ContainerRuntime root = new ContainerRuntime();
        root.Parent = grandparent;

        FocusUpdateHarness harness1 = CreateHarness(root);
        FocusUpdateHarness harness2 = CreateHarness(root);

        harness2.IsFocused = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        harness2.InvokeHandleKeyboardFocusUpdate();

        harness2.IsFocused.ShouldBeFalse();
        harness1.IsFocused.ShouldBeTrue();
    }
}
