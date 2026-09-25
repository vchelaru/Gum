using EditorTabPlugin_XNA.Services;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CanvasAnimationActivityTests
{
    private static AnimationChainList ChainWithFrames(int frameCount)
    {
        AnimationChain chain = new AnimationChain { Name = "Walk" };
        for (int i = 0; i < frameCount; i++)
        {
            chain.Add(new AnimationFrame { FrameLength = 0.1f });
        }
        AnimationChainList chains = new AnimationChainList();
        chains.Add(chain);
        return chains;
    }

    [Fact]
    public void IsAnimating_IsTrue_OnlyForAVisibleSpritePlayingAMultiFrameChain()
    {
        Sprite sprite = new Sprite(null) { AnimationChains = ChainWithFrames(2), Animate = true };
        GraphicalUiElement parent = new GraphicalUiElement(new InvisibleRenderable());
        GraphicalUiElement spriteGue = new GraphicalUiElement(sprite);
        spriteGue.Parent = parent;

        CanvasAnimationActivity.IsAnimating(parent).ShouldBeTrue();

        parent.Visible = false;
        CanvasAnimationActivity.IsAnimating(parent).ShouldBeFalse("hidden subtrees don't animate");
        parent.Visible = true;

        sprite.Animate = false;
        CanvasAnimationActivity.IsAnimating(parent).ShouldBeFalse("not playing");
        sprite.Animate = true;

        sprite.AnimationChains = ChainWithFrames(1);
        CanvasAnimationActivity.IsAnimating(parent).ShouldBeFalse("a single frame never changes");

        CanvasAnimationActivity.IsAnimating(null).ShouldBeFalse();
    }

    [Fact]
    public void IsAnimating_IsTrue_ForAPlayingNineSlice()
    {
        NineSlice nineSlice = new NineSlice { AnimationChains = ChainWithFrames(3), Animate = true };
        GraphicalUiElement gue = new GraphicalUiElement(nineSlice);

        CanvasAnimationActivity.IsAnimating(gue).ShouldBeTrue();
    }
}
