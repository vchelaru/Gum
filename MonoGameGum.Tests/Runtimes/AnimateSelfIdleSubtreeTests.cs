using FlatRedBall.Glue.StateInterpolation;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// The per-frame animation walk skips subtrees with nothing to animate, but must still start
/// animating the moment anything in a skipped subtree becomes active (#6177).
/// </summary>
public class AnimateSelfIdleSubtreeTests : BaseTestClass
{
    #region Helpers

    // A Sprite that counts how often the animation walk reaches it.
    private sealed class CountingSprite : Sprite, IAnimatable
    {
        public int Calls;

        public CountingSprite() : base(null) { }

        bool IAnimatable.AnimateSelf(double secondDifference)
        {
            Calls++;
            return base.AnimateSelf(secondDifference);
        }
    }

    // An animatable that is not a Sprite/NineSlice: Gum cannot tell when it has work, so it must tick every frame.
    private sealed class CustomAnimatable : InvisibleRenderable, IAnimatable
    {
        public int Calls;

        bool IAnimatable.AnimateSelf(double secondDifference)
        {
            Calls++;
            return false;
        }
    }

    private sealed class UpdateEveryFrameControl : IUpdateEveryFrame
    {
        public int Calls;

        public void Activity(double secondDifference) => Calls++;
    }

    private static AnimationChainList CreateChains(bool loops, params float[] frameLengths)
    {
        AnimationChain chain = new() { Name = "Chain", Loop = loops };
        foreach (float frameLength in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = frameLength });
        }
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    private static CountingSprite CreateIdleSprite()
    {
        CountingSprite sprite = new();
        sprite.AnimationLogic.AnimationChains = CreateChains(loops: true, 10f);
        return sprite;
    }

    private static void StartAnimating(CountingSprite sprite) => sprite.Animate = true;

    private static double TimeInto(CountingSprite sprite) => sprite.AnimationLogic.TimeIntoAnimation;

    private static void RunFrames(GraphicalUiElement root, int frameCount, double secondDifference = 0.1)
    {
        for (int i = 0; i < frameCount; i++)
        {
            root.AnimateSelf(secondDifference);
        }
    }

    // A card-like subtree: container -> 4 containers -> 1 sprite each (9 elements, 4 of them animatable).
    private static ContainerRuntime CreateCard(List<CountingSprite> sprites)
    {
        ContainerRuntime card = new();
        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime slot = new();
            CountingSprite sprite = CreateIdleSprite();
            sprites.Add(sprite);
            slot.AddChild(new GraphicalUiElement(sprite));
            card.AddChild(slot);
        }
        return card;
    }

    private static (ContainerRuntime Root, List<CountingSprite> Sprites) CreateBoard(int cardCount)
    {
        ContainerRuntime root = new();
        List<CountingSprite> sprites = new();
        for (int i = 0; i < cardCount; i++)
        {
            root.AddChild(CreateCard(sprites));
        }
        return (root, sprites);
    }

    private static int TotalCalls(List<CountingSprite> sprites)
    {
        int total = 0;
        foreach (CountingSprite sprite in sprites)
        {
            total += sprite.Calls;
        }
        return total;
    }

    private static AnimationRuntime CreateMoveXAnimation(GraphicalUiElement target)
    {
        StateSaveCategory category = new() { Name = "Move" };
        StateSave from = new() { Name = "From" };
        from.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(from);
        StateSave to = new() { Name = "To" };
        to.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(to);
        target.Categories["Move"] = category;

        AnimationRuntime animation = new() { Name = "Move" };
        animation.Keyframes.Add(new KeyframeRuntime
        {
            Time = 0,
            StateName = "Move/From",
            InterpolationType = InterpolationType.Linear
        });
        animation.Keyframes.Add(new KeyframeRuntime { Time = 1, StateName = "Move/To" });
        return animation;
    }

    #endregion

    #region Work count

    [Fact]
    public void AnimateSelf_ShouldNotReachAnyAnimatable_WhenTheTreeHasBeenIdleForAFrame()
    {
        // 52 cards of 9 elements plus the root: the 469-element board from #6177.
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(52);
        root.AnimateSelf(0.1);
        TotalCalls(sprites).ShouldBe(52 * 4, "the first frame has to look at everything to learn it is idle");

        foreach (CountingSprite sprite in sprites)
        {
            sprite.Calls = 0;
        }

        RunFrames(root, 10);

        TotalCalls(sprites).ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_ShouldOnlyReachTheAnimatingCard_WhenOneCardAnimates()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(10);
        RunFrames(root, 2);
        foreach (CountingSprite sprite in sprites)
        {
            sprite.Calls = 0;
        }

        StartAnimating(sprites[5]);
        RunFrames(root, 3);

        sprites[5].Calls.ShouldBe(3);
        TotalCalls(sprites).ShouldBe(3);
    }

    [Fact]
    public void AnimateSelf_ShouldStopReachingAnimatable_AfterNonLoopingChainFinishes()
    {
        ContainerRuntime root = new();
        CountingSprite sprite = new();
        sprite.AnimationLogic.AnimationChains = CreateChains(loops: false, 0.25f, 0.25f);
        sprite.IsAnimationChainLooping = false;
        sprite.Animate = true;
        root.AddChild(new GraphicalUiElement(sprite));

        RunFrames(root, 10, secondDifference: 0.25);
        sprite.Animate.ShouldBeFalse();
        // Past the frame that finished and the one that confirms nothing is left.
        RunFrames(root, 2);
        sprite.Calls = 0;

        RunFrames(root, 5);

        sprite.Calls.ShouldBe(0);
    }

    #endregion

    #region Wake-up: chain animation

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenAnimateIsSetAfterTheSubtreeWentIdle()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        RunFrames(root, 3);

        StartAnimating(sprites[7]);
        RunFrames(root, 2);

        TimeInto(sprites[7]).ShouldBe(0.2, 0.0001);
        TimeInto(sprites[0]).ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenAnimateIsTurnedOffAndBackOn()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(2);
        StartAnimating(sprites[1]);
        RunFrames(root, 2);
        sprites[1].Animate = false;
        RunFrames(root, 3);
        double timeWhenStopped = TimeInto(sprites[1]);

        sprites[1].Animate = true;
        RunFrames(root, 2);

        TimeInto(sprites[1]).ShouldBe(timeWhenStopped + 0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenAnimatingElementIsAddedToAnIdleParent()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        RunFrames(root, 3);

        CountingSprite late = CreateIdleSprite();
        late.Animate = true;
        root.Children[1].AddChild(new GraphicalUiElement(late));
        RunFrames(root, 2);

        TimeInto(late).ShouldBe(0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenAnimatingElementIsReparentedIntoAnIdleSubtree()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        ContainerRuntime elsewhere = new();
        CountingSprite moving = CreateIdleSprite();
        moving.Animate = true;
        GraphicalUiElement movingGue = new(moving);
        elsewhere.AddChild(movingGue);
        root.AddChild(elsewhere);
        RunFrames(root, 3);
        double before = TimeInto(moving);

        // Moves it from an animating spot into a subtree that went idle.
        ((GraphicalUiElement)root.Children[0].Children[0]).AddChild(movingGue);
        RunFrames(root, 2);

        TimeInto(moving).ShouldBe(before + 0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenHiddenSubtreeIsShown()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement hiddenCard = root.Children[1];
        hiddenCard.Visible = false;
        RunFrames(root, 3);

        StartAnimating(sprites[4]);
        RunFrames(root, 2);
        TimeInto(sprites[4]).ShouldBe(0, "a hidden subtree does not animate");

        hiddenCard.Visible = true;
        RunFrames(root, 2);

        TimeInto(sprites[4]).ShouldBe(0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenHiddenLeafIsShown()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement leaf = root.Children[2].Children[1].Children[0];
        leaf.Visible = false;
        StartAnimating(sprites[9]);
        RunFrames(root, 3);
        TimeInto(sprites[9]).ShouldBe(0);

        leaf.Visible = true;
        RunFrames(root, 2);

        TimeInto(sprites[9]).ShouldBe(0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldAdvanceChain_WhenRenderableIsReplaced()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        RunFrames(root, 3);

        CountingSprite replacement = CreateIdleSprite();
        replacement.Animate = true;
        GraphicalUiElement leaf = root.Children[0].Children[0].Children[0];
        leaf.SetContainedObject(replacement);
        RunFrames(root, 2);

        TimeInto(replacement).ShouldBe(0.2, 0.0001);
    }

    [Fact]
    public void AnimateSelf_ShouldTickOnlyRootsThatAreActive_WhenRootsAreAnimatedWithAnimateEach()
    {
        List<GraphicalUiElement> roots = new();
        List<CountingSprite> sprites = new();
        for (int i = 0; i < 4; i++)
        {
            roots.Add(CreateCard(sprites));
        }
        for (int i = 0; i < 3; i++)
        {
            GraphicalUiElement.AnimateEach(roots, 0.1);
        }

        StartAnimating(sprites[10]);
        GraphicalUiElement.AnimateEach(roots, 0.1);
        GraphicalUiElement.AnimateEach(roots, 0.1);

        TimeInto(sprites[10]).ShouldBe(0.2, 0.0001);
    }

    #endregion

    #region Wake-up: state animations

    [Fact]
    public void AnimateSelf_ShouldRunStateAnimation_WhenPlayedAfterTheSubtreeWentIdle()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement card = root.Children[1];
        AnimationRuntime animation = CreateMoveXAnimation(card);
        RunFrames(root, 3);

        card.PlayAnimation(animation);
        RunFrames(root, 1, secondDifference: 0.5);

        card.X.ShouldBe(50, 0.01f);
    }

    [Fact]
    public void AnimateSelf_ShouldRunStateAnimation_WhenPlayedOnAGrandchildOfAnIdleSubtree()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement slot = root.Children[2].Children[3];
        AnimationRuntime animation = CreateMoveXAnimation(slot);
        RunFrames(root, 3);

        slot.PlayAnimation(animation);
        RunFrames(root, 1, secondDifference: 0.5);

        slot.X.ShouldBe(50, 0.01f);
    }

    [Fact]
    public void AnimateSelf_ShouldContinueStateAnimation_WhenResumedAfterPause()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement card = root.Children[0];
        AnimationRuntime animation = CreateMoveXAnimation(card);
        card.PlayAnimation(animation);
        RunFrames(root, 1, secondDifference: 0.25);
        card.AnimationController.Pause();
        RunFrames(root, 3);

        card.AnimationController.Resume();
        RunFrames(root, 1, secondDifference: 0.25);

        card.X.ShouldBe(50, 0.01f);
    }

    [Fact]
    public void AnimateSelf_ShouldRestartStateAnimation_WhenRestartedAfterItFinished()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement card = root.Children[0];
        AnimationRuntime animation = CreateMoveXAnimation(card);
        card.PlayAnimation(animation);
        RunFrames(root, 3, secondDifference: 0.5);
        card.AnimationController.IsStopped.ShouldBeTrue();
        RunFrames(root, 2);

        card.PlayAnimation(animation);
        RunFrames(root, 1, secondDifference: 0.25);

        card.X.ShouldBe(25, 0.01f);
    }

    [Fact]
    public void AnimateSelf_ShouldRunStateAnimation_WhenAnimationCompletedHandlerStartsAnother()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        GraphicalUiElement card = root.Children[0];
        AnimationRuntime first = CreateMoveXAnimation(card);
        AnimationRuntime second = CreateMoveXAnimation(card);
        bool started = false;
        card.AnimationController.OnCompleted += () =>
        {
            if (!started)
            {
                started = true;
                card.PlayAnimation(second);
            }
        };
        card.PlayAnimation(first);
        RunFrames(root, 1, secondDifference: 1);
        RunFrames(root, 1, secondDifference: 0.5);

        card.X.ShouldBe(50, 0.01f);
    }

    #endregion

    #region Wake-up: per-frame callbacks

    [Fact]
    public void AnimateSelf_ShouldKeepTickingAnimatableThatIsNotAChainSprite()
    {
        ContainerRuntime root = new();
        CustomAnimatable custom = new();
        root.AddChild(new GraphicalUiElement(custom));

        RunFrames(root, 5);

        custom.Calls.ShouldBe(5);
    }

    [Fact]
    public void AnimateSelf_ShouldCallUpdateEveryFrame_EveryFrame()
    {
        ContainerRuntime root = new();
        InteractiveGue visual = new(new InvisibleRenderable());
        UpdateEveryFrameControl control = new();
        visual.FormsControlAsObject = control;
        root.AddChild(visual);

        RunFrames(root, 5);

        control.Calls.ShouldBe(5);
    }

    [Fact]
    public void AnimateSelf_ShouldCallUpdateEveryFrame_WhenControlIsAssignedAfterTheSubtreeWentIdle()
    {
        (ContainerRuntime root, List<CountingSprite> sprites) = CreateBoard(3);
        InteractiveGue visual = new(new InvisibleRenderable());
        root.Children[1].Children[2].AddChild(visual);
        RunFrames(root, 3);

        UpdateEveryFrameControl control = new();
        visual.FormsControlAsObject = control;
        RunFrames(root, 2);

        control.Calls.ShouldBe(2);
    }

    #endregion
}
