using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class FrameworkElementTreeExtensionsTests : BaseTestClass
{
    private class SpecialButton : Button { }

    #region FindVisual<T>

    [Fact]
    public void FindVisualOfT_ReturnsVisualUnderControl()
    {
        Button button = new();

        TextRuntime? textInstance = button.FindVisual<TextRuntime>();

        textInstance.ShouldNotBeNull();
        textInstance.ShouldBe(button.Visual.Find<TextRuntime>());
    }

    [Fact]
    public void FindVisualOfT_ReturnsNull_WhenNoMatch()
    {
        Button button = new();

        SpriteRuntime? result = button.FindVisual<SpriteRuntime>();

        result.ShouldBeNull();
    }

    #endregion

    #region FindVisualByName

    [Fact]
    public void FindVisualByName_ReturnsNamedDescendantVisual()
    {
        Button button = new();

        GraphicalUiElement? result = button.FindVisualByName("TextInstance");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("TextInstance");
    }

    #endregion

    #region FindVisual<T>(name)

    [Fact]
    public void FindVisualOfTByName_RequiresBothTypeAndName()
    {
        Button button = new();

        TextRuntime? result = button.FindVisual<TextRuntime>("TextInstance");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("TextInstance");
    }

    #endregion

    #region Find<T>

    [Fact]
    public void FindOfT_FindsNestedFrameworkElement()
    {
        // Window contains Buttons via AddChild — the Button's visual ends up under the
        // window's InnerPanel visual, with FormsControlAsObject pointing back at the Button.
        Window window = new();
        Button button = new();
        window.AddChild(button);

        Button? found = window.Find<Button>();

        found.ShouldBe(button);
    }

    [Fact]
    public void FindOfT_MatchesSubclasses()
    {
        Window window = new();
        SpecialButton special = new();
        window.AddChild(special);

        Button? found = window.Find<Button>();

        found.ShouldBe(special);
    }

    [Fact]
    public void FindOfT_ReturnsShallowestMatch()
    {
        Window outer = new();
        Window inner = new();
        Button shallow = new();
        Button deep = new();
        outer.AddChild(shallow);
        outer.AddChild(inner);
        inner.AddChild(deep);

        Button? found = outer.Find<Button>();

        found.ShouldBe(shallow);
    }

    [Fact]
    public void FindOfT_ReturnsNull_WhenNoMatchingFrameworkElement()
    {
        Window window = new();

        Button? found = window.Find<Button>();

        found.ShouldBeNull();
    }

    #endregion

    #region FindByName

    [Fact]
    public void FindByName_MatchesOnVisualName()
    {
        Window window = new();
        Button button = new();
        button.Visual.Name = "OkButton";
        window.AddChild(button);

        FrameworkElement? found = window.FindByName("OkButton");

        found.ShouldBe(button);
    }

    #endregion

    #region Find<T>(name)

    [Fact]
    public void FindOfTByName_RequiresBothTypeAndName()
    {
        Window window = new();
        Button decoy = new();
        decoy.Visual.Name = "Target";
        Button match = new();
        match.Visual.Name = "Target";
        window.AddChild(decoy);
        window.AddChild(match);

        Button? found = window.Find<Button>("Target");

        // Decoy is added first, so shallowest-first returns it.
        found.ShouldBe(decoy);
    }

    #endregion

    #region Descendants

    [Fact]
    public void Descendants_YieldsOnlyFrameworkElements()
    {
        // The visual tree contains many non-FE nodes (ContainerRuntime panels, TextRuntime,
        // NineSliceRuntime borders). Descendants() must skip those and yield only the FEs.
        Window window = new();
        Button a = new();
        Button b = new();
        window.AddChild(a);
        window.AddChild(b);

        List<FrameworkElement> result = window.Descendants().ToList();

        result.ShouldContain(a);
        result.ShouldContain(b);
        result.ShouldAllBe(x => x is FrameworkElement);
    }

    [Fact]
    public void Descendants_DoesNotIncludeSelf()
    {
        Window window = new();
        Button button = new();
        window.AddChild(button);

        List<FrameworkElement> result = window.Descendants().ToList();

        result.ShouldNotContain(window);
    }

    #endregion

    #region DescendantsAndSelf

    [Fact]
    public void DescendantsAndSelf_StartsWithSelf()
    {
        Window window = new();
        Button button = new();
        window.AddChild(button);

        List<FrameworkElement> result = window.DescendantsAndSelf().ToList();

        result[0].ShouldBe(window);
        result.ShouldContain(button);
    }

    #endregion

    #region Ancestors

    [Fact]
    public void Ancestors_WalksUpToContainingFrameworkElements()
    {
        Window window = new();
        Button button = new();
        window.AddChild(button);

        List<FrameworkElement> result = button.Ancestors().ToList();

        result.ShouldContain(window);
        result.ShouldNotContain(button);
    }

    [Fact]
    public void Ancestors_ReturnsEmpty_WhenNoFrameworkElementAncestors()
    {
        Button button = new();

        IEnumerable<FrameworkElement> result = button.Ancestors();

        result.ShouldBeEmpty();
    }

    #endregion

    #region AncestorsAndSelf

    [Fact]
    public void AncestorsAndSelf_StartsWithSelf()
    {
        Window window = new();
        Button button = new();
        window.AddChild(button);

        List<FrameworkElement> result = button.AncestorsAndSelf().ToList();

        result[0].ShouldBe(button);
        result.ShouldContain(window);
    }

    #endregion
}
