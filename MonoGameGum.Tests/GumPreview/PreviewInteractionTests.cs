using Gum.Forms.Controls;
using Gum.Wireframe;
using GumPreview;
using MonoGameGum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

public class PreviewInteractionTests : BaseTestClass
{
    [Fact]
    public void Apply_FocusAndText_FocusesTheTextBoxAndTypesTheText()
    {
        ContainerRuntime root = new();
        TextBox textBox = new();
        textBox.Name = "NameBox";
        root.AddChild(textBox);

        string? error = PreviewInteraction.Apply(root, "NameBox", "Hi there");

        error.ShouldBeNull();
        textBox.IsFocused.ShouldBeTrue();
        textBox.Text.ShouldBe("Hi there");
        textBox.CaretIndex.ShouldBe("Hi there".Length);
    }

    [Fact]
    public void Apply_FocusWithoutText_FocusesAndLeavesTextEmpty()
    {
        ContainerRuntime root = new();
        TextBox textBox = new();
        textBox.Name = "NameBox";
        root.AddChild(textBox);

        string? error = PreviewInteraction.Apply(root, "NameBox", null);

        error.ShouldBeNull();
        textBox.IsFocused.ShouldBeTrue();
        string.IsNullOrEmpty(textBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void Apply_UnknownName_ReturnsAnError()
    {
        ContainerRuntime root = new();

        string? error = PreviewInteraction.Apply(root, "Missing", null);

        error.ShouldNotBeNull();
        error.ShouldContain("Missing");
    }

    [Fact]
    public void Apply_TextOnAControlThatIsNotATextBox_ReturnsAnError()
    {
        ContainerRuntime root = new();
        Button button = new();
        button.Name = "OkButton";
        root.AddChild(button);

        string? error = PreviewInteraction.Apply(root, "OkButton", "Hi");

        error.ShouldNotBeNull();
        error.ShouldContain("OkButton");
    }
}
