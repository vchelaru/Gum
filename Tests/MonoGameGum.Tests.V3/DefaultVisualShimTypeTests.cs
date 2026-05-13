using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// V3 default visuals must continue to expose their child runtimes as the
/// deprecated <c>MonoGameGum.GueDeriving</c> shim types so user code that captures
/// those properties into shim-typed variables keeps compiling. See issue #2715.
/// </summary>
public class DefaultVisualShimTypeTests
{
    [Fact]
    public void V3_ButtonVisual_TextInstance_ShouldBeShimType()
    {
        var sut = new Gum.Forms.DefaultVisuals.V3.ButtonVisual();
#pragma warning disable CS0618
        sut.TextInstance.ShouldBeOfType<MonoGameGum.GueDeriving.TextRuntime>();
        sut.Background.ShouldBeOfType<MonoGameGum.GueDeriving.NineSliceRuntime>();
#pragma warning restore CS0618
    }
}
