using System.ComponentModel;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How a bound Forms control reacts to the change notifications its source raises: a null or empty
/// property name (every property changed, as in WPF), changes to an intermediate of a dotted path, and
/// sources that raise no notifications at all.
/// </summary>
public class BindingSourceNotificationTests : BaseTestClass
{
    private class Node : INotifyPropertyChanged
    {
        private string? _text;
        private Node? _child;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Text { get => _text; set { _text = value; Raise(nameof(Text)); } }
        public Node? Child { get => _child; set { _child = value; Raise(nameof(Child)); } }

        public void SetTextSilently(string? value) => _text = value;
        public void SetChildSilently(Node? value) => _child = value;
        public void Raise(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private class PlainRoot
    {
        public Node? Child { get; set; }
        public string? Text { get; set; }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PropertyChanged_NullOrEmptyName_UpdatesTarget(string? raisedName)
    {
        Node vm = new() { Text = "before" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(Node.Text));

        vm.SetTextSilently("after");
        vm.Raise(raisedName);

        textBox.Text.ShouldBe("after");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PropertyChanged_NullOrEmptyNameOnRoot_UpdatesNestedPathTarget(string? raisedName)
    {
        Node vm = new() { Child = new Node { Text = "before" } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Text");

        vm.SetChildSilently(new Node { Text = "after" });
        vm.Raise(raisedName);

        textBox.Text.ShouldBe("after");
    }

    [Fact]
    public void NestedPath_IntermediateReplaced_OnlyTheNewIntermediateReachesTarget()
    {
        Node oldChild = new() { Text = "old" };
        Node vm = new() { Child = oldChild };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Text");

        Node newChild = new() { Text = "new" };
        vm.Child = newChild;
        textBox.Text.ShouldBe("new");

        oldChild.Text = "stale";
        textBox.Text.ShouldBe("new");

        newChild.Text = "new changed";
        textBox.Text.ShouldBe("new changed");
    }

    [Fact]
    public void NestedPath_IntermediateReplaced_TargetEditWritesTheNewIntermediate()
    {
        Node oldChild = new() { Text = "old" };
        Node vm = new() { Child = oldChild };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Text");

        Node newChild = new() { Text = "new" };
        vm.Child = newChild;
        textBox.Text = "typed";

        newChild.Text.ShouldBe("typed");
        oldChild.Text.ShouldBe("old");
    }

    [Fact]
    public void DeepPath_FirstIntermediateReplaced_OldBranchNoLongerReachesTarget()
    {
        Node oldGrandChild = new() { Text = "old" };
        Node vm = new() { Child = new Node { Child = oldGrandChild } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Child.Text");

        vm.Child = new Node { Child = new Node { Text = "new" } };
        textBox.Text.ShouldBe("new");

        oldGrandChild.Text = "stale";
        textBox.Text.ShouldBe("new");
    }

    [Fact]
    public void NestedPath_IntermediateNullThenBack_ShowsFallbackThenNewValueAndTakesEdits()
    {
        Node vm = new() { Child = new Node { Text = "first" } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding("Child.Text") { FallbackValue = "none" });

        vm.Child = null;
        textBox.Text.ShouldBe("none");

        Node back = new() { Text = "back" };
        vm.Child = back;
        textBox.Text.ShouldBe("back");

        textBox.Text = "typed";
        back.Text.ShouldBe("typed");

        back.Text = "changed";
        textBox.Text.ShouldBe("changed");
    }

    [Fact]
    public void NestedPath_NonNotifyingRoot_IntermediateReplaced_OldIntermediateNoLongerReachesTarget()
    {
        Node oldGrandChild = new() { Text = "old" };
        PlainRoot root = new() { Child = new Node { Child = oldGrandChild } };
        TextBox textBox = new() { BindingContext = root };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Child.Text");

        root.Child.Child = new Node { Text = "new" };
        textBox.Text.ShouldBe("new");

        oldGrandChild.Text = "stale";
        textBox.Text.ShouldBe("new");
    }

    [Fact]
    public void NonNotifyingSource_ShowsInitialValueAndTakesEdits()
    {
        PlainRoot root = new() { Text = "initial" };
        TextBox textBox = new() { BindingContext = root };
        textBox.SetBinding(nameof(TextBox.Text), nameof(PlainRoot.Text));

        textBox.Text.ShouldBe("initial");

        textBox.Text = "typed";
        root.Text.ShouldBe("typed");
    }

    [Fact]
    public void PropertyChanged_UnrelatedName_LeavesTargetAlone()
    {
        Node vm = new() { Text = "before" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(Node.Text));

        vm.SetTextSilently("after");
        vm.Raise(nameof(Node.Child));

        textBox.Text.ShouldBe("before");
    }
}
