using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class RangeBaseTests : BaseTestClass
{
    class ValueOnlyRange : RangeBase
    {
        public ValueOnlyRange(InteractiveGue visual) : base(visual) { }
    }

    [Fact]
    public void Value_ShouldClamp_WhenVisualHasNoThumbOrTrack()
    {
        ValueOnlyRange range = new(new ContainerRuntime());

        range.Value = 150;
        range.Value.ShouldBe(100);

        range.Value = -5;
        range.Value.ShouldBe(0);
    }
}
