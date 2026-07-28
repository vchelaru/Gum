using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// #4057 (Raylib): mirrors MonoGameGum.Tests.V2.GueDeriving.TextRuntimeDropshadowPlumbingTests'
// renderable-forwarding cases. ApplyDropshadowToRenderable was XNALIKE-only until this issue;
// pins that it now also forwards onto the Raylib Text renderable.
public class TextRuntimeDropshadowPlumbingTests : BaseTestClass
{
    [Fact]
    public void SettingDropshadowProperties_ForwardsToTextRenderable()
    {
        TextRuntime text = new TextRuntime();
        text.HasDropshadow = true;
        text.DropshadowOffsetX = 4f;
        text.DropshadowOffsetY = 5f;
        text.DropshadowColor = new Raylib_cs.Color(10, 20, 30, 128);

        Gum.Renderables.Text renderable = (Gum.Renderables.Text)text.RenderableComponent;
        renderable.HasDropshadow.ShouldBeTrue();
        renderable.DropshadowOffsetX.ShouldBe(4f);
        renderable.DropshadowOffsetY.ShouldBe(5f);
        renderable.DropshadowColor.R.ShouldBe((byte)10);
        renderable.DropshadowColor.G.ShouldBe((byte)20);
        renderable.DropshadowColor.B.ShouldBe((byte)30);
        renderable.DropshadowColor.A.ShouldBe((byte)128);
    }

    [Fact]
    public void ClearingHasDropshadow_ForwardsFalseToTextRenderable()
    {
        TextRuntime text = new TextRuntime();
        text.HasDropshadow = true;
        text.HasDropshadow = false;

        Gum.Renderables.Text renderable = (Gum.Renderables.Text)text.RenderableComponent;
        renderable.HasDropshadow.ShouldBeFalse();
    }
}
