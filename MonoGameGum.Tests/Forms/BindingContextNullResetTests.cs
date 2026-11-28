using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A bound control property resets when its BindingContext becomes null, as WPF does: the target shows
/// FallbackValue when the binding sets one, otherwise the value the property held when the binding was created
/// (Gum's stand-in for WPF's per-property default). A bound Items is one case of this rule;
/// ItemsCollectionLifetimeTests covers its collection lifetime.
/// </summary>
public class BindingContextNullResetTests : BaseTestClass
{
    [Fact]
    public void BindingContextSetToNull_TextBoxText_ResetsToUnboundValue()
    {
        TestViewModel a = new() { Text = "a" };
        TestViewModel b = new() { Text = "b" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        textBox.BindingContext = b;

        textBox.BindingContext = null;
        b.Text = "b changed";
        a.Text = "a changed again";

        textBox.Text.ShouldBe(ContextResetExpectation.Of(""));
    }

    [Fact]
    public void RemovedFromNonRootParent_InheritedContext_TargetResetsToUnboundValue()
    {
        TestViewModel vm = new() { Text = "first" };
        StackPanel panel = new() { BindingContext = vm };
        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        textBox.Text.ShouldBe("first");

        panel.Visual.Children.Remove(textBox.Visual);
        vm.Text = "second";

        textBox.BindingContext.ShouldBeNull();
        textBox.Text.ShouldBe(ContextResetExpectation.Of(""));
    }

    [Fact]
    public void BindingContextSetToNull_FreshControls_ResetToTheirUnboundValues()
    {
        TestViewModel vm = new() { IsChecked = true, IntValue = 1, Text = "typed" };
        StackPanel panel = new() { BindingContext = vm };
        CheckBox checkBox = new();
        ListBox listBox = new();
        listBox.Items.Add("first");
        listBox.Items.Add("second");
        TextBox textBox = new();
        panel.AddChild(checkBox);
        panel.AddChild(listBox);
        panel.AddChild(textBox);
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(TestViewModel.IsChecked));
        listBox.SetBinding(nameof(ListBox.SelectedIndex), nameof(TestViewModel.IntValue));
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        listBox.SelectedIndex.ShouldBe(1);

        panel.BindingContext = null;

        checkBox.IsChecked.ShouldBe(ContextResetExpectation.Of<bool?>(false));
        listBox.SelectedIndex.ShouldBe(ContextResetExpectation.Of(-1));
        textBox.Text.ShouldBe(ContextResetExpectation.Of(""));
        vm.IsChecked.ShouldBeTrue();
        vm.IntValue.ShouldBe(1);
        vm.Text.ShouldBe("typed");
    }

    [Fact]
    public void BindingContextSetToNull_LabelWithAuthoredText_ShowsAuthoredText()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { Text = "Player name" };
        label.BindingContext = vm;
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));
        label.Text.ShouldBe("Bob");

        label.BindingContext = null;

        label.Text.ShouldBe(ContextResetExpectation.Of("Player name"));
    }

    [Fact]
    public void SetBindingTwice_ContextCleared_ShowsValueFromBeforeTheFirstBinding()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { Text = "Player name", BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));

        label.BindingContext = null;

        label.Text.ShouldBe(ContextResetExpectation.Of("Player name"));
    }

    [Fact]
    public void ClearBindingThenRebind_ContextCleared_ShowsValueFromBeforeTheFirstBinding()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { Text = "Player name", BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));
        label.ClearBinding(nameof(Label.Text));
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));

        label.BindingContext = null;

        label.Text.ShouldBe(ContextResetExpectation.Of("Player name"));
    }

    [Fact]
    public void ClearBinding_ResetsToValueFromBeforeTheBinding()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { Text = "Player name", BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(TestViewModel.Text));
        label.Text.ShouldBe("Bob");

        label.ClearBinding(nameof(Label.Text));

        label.Text.ShouldBe(ContextResetExpectation.Of("Player name"));
    }

    [Fact]
    public void BindingContextSetToNull_ValueTypeTarget_ResetsToUnboundValue()
    {
        TestViewModel vm = new() { IntValue = 7 };
        Slider slider = new() { Minimum = 0, Maximum = 100, Value = 3, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value), nameof(TestViewModel.IntValue));
        slider.Value.ShouldBe(7);

        slider.BindingContext = null;

        slider.Value.ShouldBe(ContextResetExpectation.Of(3d));
        vm.IntValue.ShouldBe(7);
    }

    [Fact]
    public void BindingContextSetToNull_FallbackValueSet_ShowsFallbackValue()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.Text)) { FallbackValue = "nobody" });
        label.Text.ShouldBe("Bob");

        label.BindingContext = null;

        label.Text.ShouldBe("nobody");
    }

    [Fact]
    public void SetBinding_BeforeAnyContext_KeepsAuthoredValue()
    {
        Label label = new() { Text = "Player name" };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.Text)) { FallbackValue = "nobody" });

        label.Text.ShouldBe("Player name");
    }

    [Fact]
    public void BindingContextSetToNull_TargetNullValueSet_IgnoresTargetNullValue()
    {
        TestViewModel vm = new() { Text = "Bob" };
        Label label = new() { Text = "Player name" };
        label.BindingContext = vm;
        label.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.Text)) { TargetNullValue = "(none)" });

        label.BindingContext = null;

        label.Text.ShouldBe(ContextResetExpectation.Of("Player name"));
    }

    [Fact]
    public void BindingContextSetToNull_OneWayToSource_LeavesTargetAlone()
    {
        TestViewModel vm = new() { Text = "from vm" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(TestViewModel.Text)) { Mode = BindingMode.OneWayToSource });
        textBox.Text = "typed";

        textBox.BindingContext = null;

        textBox.Text.ShouldBe("typed");
    }

    [Fact]
    public void BindingContextSetAgain_AfterNull_RestoresValueWithoutWritingDefault()
    {
        TestViewModel a = new() { Text = "a" };
        TestViewModel b = new() { Text = "b" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.BindingContext = null;
        textBox.BindingContext = b;

        textBox.Text.ShouldBe("b");
        b.Text.ShouldBe("b");
        a.Text.ShouldBe("a");
    }

    [Fact]
    public void BindingContextSetToNull_PendingLostFocusEdit_NotWrittenAfterReset()
    {
        TestViewModel a = new() { Text = "a" };
        TestViewModel b = new() { Text = "b" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        textBox.IsFocused = true;
        textBox.Text = "typed";

        // A context change ends the edit, so the pending edit reaches the outgoing view model first.
        textBox.BindingContext = null;
        a.Text.ShouldBe("typed");
        textBox.Text.ShouldBe(ContextResetExpectation.Of(""));

        a.Text = "changed elsewhere";
        textBox.IsFocused = false;
        a.Text.ShouldBe("changed elsewhere");

        textBox.BindingContext = b;
        textBox.Text.ShouldBe("b");
        b.Text.ShouldBe("b");
    }

    [Fact]
    public void BindingContextSetToNull_VisualBinding_KeepsLastValue()
    {
        // Visual (GraphicalUiElement) bindings are a separate engine; they keep the last value today.
        TestViewModel vm = new() { Text = "Bob" };
        GraphicalUiElement element = new() { BindingContext = vm };
        element.SetBinding(nameof(GraphicalUiElement.Name), nameof(TestViewModel.Text));
        element.Name.ShouldBe("Bob");

        element.BindingContext = null;

        element.Name.ShouldBe("Bob");
    }
}
