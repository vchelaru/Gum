using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Pins issue #4708: AnimationChainLogic seeds IsAnimationChainLooping from the newly-selected
// chain's authored Loop value each time CurrentChainName switches chains, instead of always
// defaulting true, while still allowing a per-instance override afterward.
public class AnimationChainLogicTests
{
    private static AnimationChainList MakeChains(params (string name, bool loop)[] chains)
    {
        var list = new AnimationChainList();
        foreach ((string name, bool loop) in chains)
        {
            var chain = new AnimationChain { Name = name, Loop = loop };
            chain.Add(new AnimationFrame { FrameLength = 0.1f });
            list.Add(chain);
        }
        return list;
    }

    [Fact]
    public void CurrentChainName_Set_SeedsIsAnimationChainLoopingFromChainLoop()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false)) };

        sut.CurrentChainName = "Attack";

        sut.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Fact]
    public void CurrentChainName_Set_ReseedsIsAnimationChainLoopingOnEachChainSwitch()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false), ("Walk", true)) };
        sut.CurrentChainName = "Attack";

        sut.CurrentChainName = "Walk";

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }

    [Fact]
    public void IsAnimationChainLooping_CanStillBeOverridden_AfterCurrentChainNameSeedsIt()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false)) };
        sut.CurrentChainName = "Attack";

        sut.IsAnimationChainLooping = true;

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }
}
