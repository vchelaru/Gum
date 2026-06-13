using Gum.Forms.DefaultVisuals;
using Shouldly;

namespace MonoGameGum.Tests.V2.Forms;

/// <summary>
/// V1/V2 default visuals must continue to expose their child runtimes as the
/// deprecated <c>MonoGameGum.GueDeriving</c> shim types so user code that captures
/// those properties into shim-typed variables keeps compiling. See issue #2715.
/// </summary>
public class DefaultVisualShimTypeTests : BaseTestClass
{
    [Fact]
    public void V1_DefaultButtonRuntime_TextInstance_ShouldBeShimType()
    {
        var sut = new Gum.Forms.DefaultVisuals.DefaultButtonRuntime();
#pragma warning disable CS0618
        sut.TextInstance.ShouldBeOfType<MonoGameGum.GueDeriving.TextRuntime>();
#pragma warning restore CS0618
    }

    [Fact]
    public void V2_ButtonVisual_TextInstance_ShouldBeShimType()
    {
        var sut = new ButtonVisual();
#pragma warning disable CS0618
        sut.TextInstance.ShouldBeOfType<MonoGameGum.GueDeriving.TextRuntime>();
        sut.Background.ShouldBeOfType<MonoGameGum.GueDeriving.NineSliceRuntime>();
#pragma warning restore CS0618
    }
}
