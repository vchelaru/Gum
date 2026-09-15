using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Pins issue #4708: AnimationChainSave.Loop threads onto the runtime AnimationChain via
// ToAnimationChain, which is what AnimationChainLogic seeds IsAnimationChainLooping from.
public class AnimationChainSaveExtensionMethodsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToAnimationChain_ThreadsLoopFromSave(bool loop)
    {
        AnimationChainSave save = new AnimationChainSave { Name = "Walk", Loop = loop };

        Gum.Graphics.Animation.AnimationChain chain = save.ToAnimationChain(TimeMeasurementUnit.Second);

        chain.Loop.ShouldBe(loop);
    }
}
