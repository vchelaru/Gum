using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Renderables;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// The per-frame animation walk skips a subtree with nothing to animate, but a raylib Sprite or
/// NineSlice must wake it as soon as Animate turns on (#6177).
/// </summary>
public class AnimateSelfIdleSubtreeTests : BaseTestClass
{
    private static AnimationChainList CreateChains()
    {
        AnimationChain chain = new() { Name = "Chain" };
        chain.Add(new AnimationFrame { FrameLength = 10 });
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    private static void RunFrames(GraphicalUiElement root, int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            root.AnimateSelf(0.1);
        }
    }

    [Fact]
    public void Sprite_AnimateSelf_ShouldAdvanceChain_WhenAnimateIsTurnedOnAfterTheSubtreeWentIdle()
    {
        ContainerRuntime root = new();
        Sprite sprite = new();
        sprite.AnimationLogic.AnimationChains = CreateChains();
        root.AddChild(new GraphicalUiElement(sprite));
        RunFrames(root, 3);

        sprite.Animate = true;
        RunFrames(root, 2);

        sprite.AnimationLogic.TimeIntoAnimation.ShouldBe(0.2, 0.0001);
    }

    [Fact]
    public void NineSlice_AnimateSelf_ShouldAdvanceChain_WhenAnimateIsTurnedOnAfterTheSubtreeWentIdle()
    {
        ContainerRuntime root = new();
        NineSlice nineSlice = new();
        nineSlice.AnimationLogic.AnimationChains = CreateChains();
        root.AddChild(new GraphicalUiElement(nineSlice));
        RunFrames(root, 3);

        nineSlice.AnimationLogic.Animate = true;
        RunFrames(root, 2);

        nineSlice.AnimationLogic.TimeIntoAnimation.ShouldBe(0.2, 0.0001);
    }
}
