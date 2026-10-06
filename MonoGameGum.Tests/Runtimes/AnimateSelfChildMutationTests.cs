using System;
using System.Collections.Generic;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// An animation chain event handler can add or remove elements while the tree is animating. Every
/// element that stays in the tree must still advance exactly once that frame (#5829).
/// </summary>
public class AnimateSelfChildMutationTests : BaseTestClass
{
    // A sprite whose looping chain never changes frame during the test, so its TimeIntoAnimation
    // shows how many times it advanced.
    private static SpriteRuntime CreateObserver()
    {
        return CreateSprite(isLooping: true, 10f);
    }

    private static SpriteRuntime CreateSprite(bool isLooping, params float[] frameLengths)
    {
        AnimationChain chain = new() { Name = "Chain" };
        foreach (float frameLength in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = frameLength });
        }
        AnimationChainList chains = new();
        chains.Add(chain);

        SpriteRuntime sprite = new();
        sprite.AnimationChains = chains;
        sprite.IsAnimationChainLooping = isLooping;
        sprite.Animate = true;
        return sprite;
    }

    [Theory]
    [InlineData("self")]
    [InlineData("earlier")]
    [InlineData("later")]
    public void AnimateSelf_ShouldAdvanceRemainingChildrenOnce_WhenFinishedHandlerRemovesAChild(string removed)
    {
        ContainerRuntime parent = new();
        SpriteRuntime earlier = CreateObserver();
        // Two frames, so finishing also changes the frame and applies it after the handler ran.
        SpriteRuntime remover = CreateSprite(isLooping: false, 0.25f, 0.25f);
        SpriteRuntime later = CreateObserver();
        SpriteRuntime last = CreateObserver();
        parent.AddChild(earlier);
        parent.AddChild(remover);
        parent.AddChild(later);
        parent.AddChild(last);
        SpriteRuntime toRemove = removed switch
        {
            "self" => remover,
            "earlier" => earlier,
            _ => later
        };
        // Parent = null is what RemoveFromRoot does.
        remover.AnimationChainFinished += () => toRemove.Parent = null;

        Should.NotThrow(() => parent.AnimateSelf(1));

        parent.Children.ShouldNotContain(toRemove);
        foreach (SpriteRuntime observer in new[] { earlier, later, last })
        {
            if (observer != toRemove)
            {
                observer.AnimationChainTime.ShouldBe(1);
            }
        }
    }

    [Fact]
    public void AnimateSelf_ShouldNotAdvanceAChildTwice_WhenCycledHandlerInsertsBeforeIt()
    {
        ContainerRuntime parent = new();
        SpriteRuntime first = CreateObserver();
        SpriteRuntime inserter = CreateSprite(isLooping: true, 0.75f);
        SpriteRuntime after = CreateObserver();
        parent.AddChild(first);
        parent.AddChild(inserter);
        parent.AddChild(after);
        bool hasInserted = false;
        inserter.AnimationChainCycled += () =>
        {
            // Insert once, so a revisit shows up as a double advance instead of a loop that never ends.
            if (!hasInserted)
            {
                hasInserted = true;
                parent.Children.Insert(0, new ContainerRuntime());
            }
        };

        parent.AnimateSelf(1);

        inserter.AnimationChainTime.ShouldBe(0.25, tolerance: 0.0001);
        first.AnimationChainTime.ShouldBe(1);
        after.AnimationChainTime.ShouldBe(1);
    }

    [Fact]
    public void Update_ShouldAdvanceRemainingRootsOnce_WhenFinishedHandlerRemovesItsRoot()
    {
        SpriteRuntime remover = CreateSprite(isLooping: false, 0.25f, 0.25f);
        SpriteRuntime next = CreateObserver();
        List<GraphicalUiElement> roots = new() { remover, next };
        remover.AnimationChainFinished += () => roots.Remove(remover);

        Should.NotThrow(() => global::Gum.GumService.Default.Update(
            new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), roots));

        roots.ShouldNotContain(remover);
        next.AnimationChainTime.ShouldBe(1);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("self")]
    [InlineData("earlier")]
    [InlineData("selfAndNext")]
    [InlineData("insertBefore")]
    public void AnimateEach_ShouldAdvanceEveryRemainingElementOnce_WhenAHandlerChangesTheList(string change)
    {
        SpriteRuntime first = CreateObserver();
        SpriteRuntime actor = CreateSprite(isLooping: false, 0.25f, 0.25f);
        SpriteRuntime second = CreateObserver();
        SpriteRuntime third = CreateObserver();
        List<GraphicalUiElement> elements = new() { first, actor, second, third };
        actor.AnimationChainFinished += () =>
        {
            switch (change)
            {
                case "self": elements.Remove(actor); break;
                case "earlier": elements.Remove(first); break;
                case "selfAndNext": elements.Remove(actor); elements.Remove(second); break;
                case "insertBefore": elements.Insert(0, CreateObserver()); break;
            }
        };

        GraphicalUiElement.AnimateEach(elements, 1);

        first.AnimationChainTime.ShouldBe(1);
        if (change != "selfAndNext")
        {
            second.AnimationChainTime.ShouldBe(1);
        }
        third.AnimationChainTime.ShouldBe(1);
    }

    [Fact]
    public void AnimateEach_ShouldNotThrow_WhenTheLastElementRemovesItself()
    {
        SpriteRuntime first = CreateObserver();
        SpriteRuntime actor = CreateSprite(isLooping: false, 0.25f);
        List<GraphicalUiElement> elements = new() { first, actor };
        actor.AnimationChainFinished += () => elements.Remove(actor);

        Should.NotThrow(() => GraphicalUiElement.AnimateEach(elements, 1));

        elements.ShouldBe(new GraphicalUiElement[] { first });
        first.AnimationChainTime.ShouldBe(1);
    }
}
