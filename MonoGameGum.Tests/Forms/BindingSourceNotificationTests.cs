using System.ComponentModel;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How a binding reacts to a PropertyChanged raised with a null or empty property name, which means
/// every property changed (the .NET and WPF convention), on Forms controls and on visuals.
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
    public void PropertyChanged_NullNameOnIntermediate_UpdatesTargetAndFollowsNewBranch()
    {
        Node child = new() { Child = new Node { Text = "before" } };
        Node vm = new() { Child = child };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Child.Child.Text");

        Node newGrandChild = new() { Text = "after" };
        child.SetChildSilently(newGrandChild);
        child.Raise(null);
        textBox.Text.ShouldBe("after");

        newGrandChild.Text = "changed";
        textBox.Text.ShouldBe("changed");
    }

    [Theory]
    [InlineData("Text", null)]
    [InlineData("Text", "")]
    [InlineData("Child.Text", null)]
    [InlineData("Child.Text", "")]
    public void Visual_PropertyChanged_NullOrEmptyName_UpdatesTarget(string path, string? raisedName)
    {
        Node vm = new() { Text = "before", Child = new Node { Text = "before" } };
        TextRuntime text = new() { BindingContext = vm };
        text.SetBinding(nameof(TextRuntime.Text), path);

        vm.SetTextSilently("after");
        vm.SetChildSilently(new Node { Text = "after" });
        vm.Raise(raisedName);

        text.Text.ShouldBe("after");
    }

    [Fact]
    public void Visual_PropertyChanged_NullName_UpdatesChildBindingContextBinding()
    {
        Node vm = new() { Text = "root", Child = new Node { Text = "before" } };
        ContainerRuntime parent = new() { BindingContext = vm };
        parent.SetBinding(nameof(ContainerRuntime.Name), nameof(Node.Text));
        TextRuntime text = new();
        parent.Children.Add(text);
        text.SetBinding(nameof(TextRuntime.BindingContext), nameof(Node.Child));
        text.SetBinding(nameof(TextRuntime.Text), nameof(Node.Text));
        text.Text.ShouldBe("before");

        vm.SetChildSilently(new Node { Text = "after" });
        vm.Raise(null);

        text.Text.ShouldBe("after");
    }
}
