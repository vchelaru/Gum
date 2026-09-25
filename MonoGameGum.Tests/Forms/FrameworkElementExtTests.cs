using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class FrameworkElementExtTests : BaseTestClass
{
    [Fact]
    public void GetFrameworkElement_ReturnsNull_WhenNameMissing()
    {
        Window window = new();

        window.GetFrameworkElement("Missing").ShouldBeNull();
        window.GetFrameworkElement<Button>("Missing").ShouldBeNull();
    }

    [Fact]
    public void GetFrameworkElementOfT_SkipsWrongTypedMatch()
    {
        Window outer = new();
        Label label = new();
        label.Visual.Name = "Item";
        outer.AddChild(label);
        Window inner = new();
        outer.AddChild(inner);
        Button button = new();
        button.Visual.Name = "Item";
        inner.AddChild(button);

        Button? found = outer.GetFrameworkElement<Button>("Item");

        found.ShouldBe(button);
    }
}
