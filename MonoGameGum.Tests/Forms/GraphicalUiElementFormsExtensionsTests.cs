using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class GraphicalUiElementFormsExtensionsTests : BaseTestClass
{
    private class SpecialButton : Button { }

    #region FindFormsControl<T>

    [Fact]
    public void FindFormsControlOfT_FindsDescendantFrameworkElement()
    {
        // Window's Visual is a GraphicalUiElement; AddChild places the Button's visual
        // under it with FormsControlAsObject pointing back at the Button. Starting from
        // a raw visual (the typical screen/component root case), FindFormsControl<T> must
        // project descendants through FormsControlAsObject.
        Window window = new();
        Button button = new();
        window.AddChild(button);

        Button? found = window.Visual.FindFormsControl<Button>();

        found.ShouldBe(button);
    }

    [Fact]
    public void FindFormsControlOfT_MatchesSubclasses()
    {
        Window window = new();
        SpecialButton special = new();
        window.AddChild(special);

        Button? found = window.Visual.FindFormsControl<Button>();

        found.ShouldBe(special);
    }

    [Fact]
    public void FindFormsControlOfT_ReturnsShallowestMatch()
    {
        Window outer = new();
        Window inner = new();
        Button shallow = new();
        Button deep = new();
        outer.AddChild(shallow);
        outer.AddChild(inner);
        inner.AddChild(deep);

        Button? found = outer.Visual.FindFormsControl<Button>();

        found.ShouldBe(shallow);
    }

    [Fact]
    public void FindFormsControlOfT_ReturnsNull_WhenNoMatch()
    {
        Window window = new();

        Button? found = window.Visual.FindFormsControl<Button>();

        found.ShouldBeNull();
    }

    #endregion

    #region FindFormsControlByName

    [Fact]
    public void FindFormsControlByName_MatchesOnVisualName()
    {
        Window window = new();
        Button button = new();
        button.Visual.Name = "OkButton";
        window.AddChild(button);

        FrameworkElement? found = window.Visual.FindFormsControlByName("OkButton");

        found.ShouldBe(button);
    }

    [Fact]
    public void FindFormsControlByName_SkipsVisualOnlyDescendantsWithSameName()
    {
        // A bare visual whose Name matches must NOT be returned — only descendants that
        // project to a FrameworkElement count. This is what makes FindFormsControlByName
        // different from GraphicalUiElement.FindByName.
        Window window = new();
        ContainerRuntime decoy = new() { Name = "Target" };
        window.Visual.Children.Add(decoy);
        Button target = new();
        target.Visual.Name = "Target";
        window.AddChild(target);

        FrameworkElement? found = window.Visual.FindFormsControlByName("Target");

        found.ShouldBe(target);
    }

    [Fact]
    public void FindFormsControlByName_ReturnsNull_WhenNoMatch()
    {
        Window window = new();

        FrameworkElement? found = window.Visual.FindFormsControlByName("Missing");

        found.ShouldBeNull();
    }

    #endregion

    #region FindFormsControl<T>(name)

    [Fact]
    public void FindFormsControlOfTByName_RequiresBothTypeAndName()
    {
        Window window = new();
        Button decoy = new();
        decoy.Visual.Name = "Other";
        Button match = new();
        match.Visual.Name = "Target";
        window.AddChild(decoy);
        window.AddChild(match);

        Button? found = window.Visual.FindFormsControl<Button>("Target");

        found.ShouldBe(match);
    }

    [Fact]
    public void FindFormsControlOfTByName_ReturnsNull_WhenNameMatchesButTypeDoesNot()
    {
        Window window = new();
        Button button = new();
        button.Visual.Name = "Target";
        window.AddChild(button);

        CheckBox? found = window.Visual.FindFormsControl<CheckBox>("Target");

        found.ShouldBeNull();
    }

    #endregion
}
