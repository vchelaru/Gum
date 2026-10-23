using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using FlatRedBall.Glue.StateInterpolation;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Splash-damage sweep (#6644) of the per-frame update contract: <see cref="IUpdateEveryFrame"/> and its
/// IsActivityNeeded member, together with the idle-subtree skip in GraphicalUiElement.AnimateSelf.
/// </summary>
public class UpdateEveryFrameSplashTests : BaseTestClass
{
    #region Helpers

    private sealed class NeedsToggleControl : IUpdateEveryFrame
    {
        public int Calls;
        public bool Needed = true;
        public Action? OnActivity;

        public void Activity(double secondDifference)
        {
            Calls++;
            OnActivity?.Invoke();
        }

        bool IUpdateEveryFrame.IsActivityNeeded => Needed;
    }

    // Does not mention IsActivityNeeded: the default member applies.
    private sealed class DefaultMemberControl : IUpdateEveryFrame
    {
        public int Calls;

        public void Activity(double secondDifference) => Calls++;
    }

    private sealed class ChainSprite : Sprite
    {
        public ChainSprite() : base(null) { }
    }

    private static InteractiveGue CreateVisual(object? control)
    {
        InteractiveGue visual = new(new InvisibleRenderable());
        visual.FormsControlAsObject = control;
        return visual;
    }

    private static void RunFrames(GraphicalUiElement root, int frames, double secondDifference = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            root.AnimateSelf(secondDifference);
        }
    }

    private static ChainSprite CreateChainSprite()
    {
        ChainSprite sprite = new();
        AnimationChain chain = new() { Name = "Chain", Loop = true };
        chain.Add(new AnimationFrame { FrameLength = 10f });
        AnimationChainList chains = new();
        chains.Add(chain);
        sprite.AnimationLogic.AnimationChains = chains;
        return sprite;
    }

    #endregion

    #region Default member

    [Fact]
    public void Activity_ControlWithoutIsActivityNeededOverride_TicksEveryFrame()
    {
        ContainerRuntime root = new();
        DefaultMemberControl control = new();
        root.AddChild(CreateVisual(control));

        RunFrames(root, 6);

        control.Calls.ShouldBe(6);
    }

    [Fact]
    public void Activity_ControlWithoutIsActivityNeededOverride_KeepsAncestorsFromGoingIdle()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        parent.AddChild(CreateVisual(new DefaultMemberControl()));

        RunFrames(root, 5);

        parent.IsIdle.ShouldBeFalse();
        root.IsIdle.ShouldBeFalse();
    }

    #endregion

    #region IsActivityNeeded gating

    [Fact]
    public void Activity_NotNeeded_LetsAncestorsGoIdle()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        parent.AddChild(CreateVisual(new NeedsToggleControl { Needed = false }));

        RunFrames(root, 3);

        parent.IsIdle.ShouldBeTrue();
        root.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void Activity_NotNeeded_StopsBeingCalledOnceTheSubtreeIsIdle()
    {
        ContainerRuntime root = new();
        NeedsToggleControl control = new() { Needed = false };
        root.AddChild(CreateVisual(control));
        RunFrames(root, 3);
        int callsWhenIdle = control.Calls;

        RunFrames(root, 5);

        control.Calls.ShouldBe(callsWhenIdle);
    }

    [Fact]
    public void Activity_NeededTurnsTrueAfterIdle_TicksAgainOnceTheSubtreeIsInvalidated()
    {
        ContainerRuntime root = new();
        NeedsToggleControl control = new() { Needed = false };
        InteractiveGue visual = CreateVisual(control);
        root.AddChild(visual);
        RunFrames(root, 3);
        int callsWhenIdle = control.Calls;

        control.Needed = true;
        visual.InvalidateSubtreeCaches();
        RunFrames(root, 4);

        control.Calls.ShouldBe(callsWhenIdle + 4);
        root.IsIdle.ShouldBeFalse();
    }

    [Fact]
    public void Activity_NeededTurnsFalseWhileTicking_LetsTheSubtreeGoIdle()
    {
        ContainerRuntime root = new();
        NeedsToggleControl control = new() { Needed = true };
        root.AddChild(CreateVisual(control));
        RunFrames(root, 3);
        root.IsIdle.ShouldBeFalse();

        control.Needed = false;
        RunFrames(root, 3);

        root.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void Activity_SiblingBranches_OnlyTheBranchWithANeededControlStaysAwake()
    {
        ContainerRuntime root = new();
        ContainerRuntime quiet = new();
        ContainerRuntime busy = new();
        root.AddChild(quiet);
        root.AddChild(busy);
        quiet.AddChild(CreateVisual(new NeedsToggleControl { Needed = false }));
        NeedsToggleControl busyControl = new() { Needed = true };
        busy.AddChild(CreateVisual(busyControl));

        RunFrames(root, 4);

        quiet.IsIdle.ShouldBeTrue();
        busy.IsIdle.ShouldBeFalse();
        busyControl.Calls.ShouldBe(4);
    }

    [Fact]
    public void Activity_OneOfTwoControlsStopsNeeding_TheOtherKeepsTicking()
    {
        ContainerRuntime root = new();
        NeedsToggleControl first = new() { Needed = true };
        NeedsToggleControl second = new() { Needed = true };
        root.AddChild(CreateVisual(first));
        root.AddChild(CreateVisual(second));
        RunFrames(root, 2);

        first.Needed = false;
        int secondBefore = second.Calls;
        RunFrames(root, 3);

        second.Calls.ShouldBe(secondBefore + 3);
        root.IsIdle.ShouldBeFalse();
    }

    [Fact]
    public void Activity_ThatInvalidatesItself_IsCalledAgainNextFrameEvenWhenNotNeeded()
    {
        ContainerRuntime root = new();
        NeedsToggleControl control = new() { Needed = false };
        InteractiveGue visual = CreateVisual(control);
        control.OnActivity = () => visual.InvalidateSubtreeCaches();
        root.AddChild(visual);

        RunFrames(root, 4);

        control.Calls.ShouldBe(4);
        root.IsIdle.ShouldBeFalse();
    }

    #endregion

    #region Control assignment

    [Fact]
    public void FormsControlAsObject_SetToNull_LetsAncestorsGoIdleAndStopsTicks()
    {
        ContainerRuntime root = new();
        DefaultMemberControl control = new();
        InteractiveGue visual = CreateVisual(control);
        root.AddChild(visual);
        RunFrames(root, 2);

        visual.FormsControlAsObject = null;
        RunFrames(root, 3);

        control.Calls.ShouldBe(2);
        root.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void FormsControlAsObject_ReplacedWithNotNeededControl_LetsAncestorsGoIdle()
    {
        ContainerRuntime root = new();
        InteractiveGue visual = CreateVisual(new DefaultMemberControl());
        root.AddChild(visual);
        RunFrames(root, 2);
        root.IsIdle.ShouldBeFalse();

        visual.FormsControlAsObject = new NeedsToggleControl { Needed = false };
        RunFrames(root, 3);

        root.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void FormsControlAsObject_ReplacedWithDefaultControlAfterIdle_StartsTicking()
    {
        ContainerRuntime root = new();
        InteractiveGue visual = CreateVisual(new NeedsToggleControl { Needed = false });
        root.AddChild(visual);
        RunFrames(root, 3);

        DefaultMemberControl replacement = new();
        visual.FormsControlAsObject = replacement;
        RunFrames(root, 3);

        replacement.Calls.ShouldBe(3);
    }

    #endregion

    #region Visible / in-tree rules

    [Fact]
    public void Activity_HiddenVisual_IsNotCalledUntilShown()
    {
        ContainerRuntime root = new();
        DefaultMemberControl control = new();
        InteractiveGue visual = CreateVisual(control);
        root.AddChild(visual);
        visual.Visible = false;

        RunFrames(root, 3);
        control.Calls.ShouldBe(0);

        visual.Visible = true;
        RunFrames(root, 2);

        control.Calls.ShouldBe(2);
    }

    [Fact]
    public void Activity_HiddenAncestor_IsNotCalledUntilShown()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        DefaultMemberControl control = new();
        parent.AddChild(CreateVisual(control));
        RunFrames(root, 1);
        parent.Visible = false;

        RunFrames(root, 3);
        control.Calls.ShouldBe(1);

        parent.Visible = true;
        RunFrames(root, 2);

        control.Calls.ShouldBe(3);
    }

    [Fact]
    public void Activity_NeededTurnsTrueWhileAncestorIsHidden_TicksOnceShown()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        NeedsToggleControl control = new() { Needed = false };
        parent.AddChild(CreateVisual(control));
        RunFrames(root, 3);
        parent.Visible = false;
        RunFrames(root, 2);

        control.Needed = true;
        parent.Visible = true;
        int before = control.Calls;
        RunFrames(root, 3);

        control.Calls.ShouldBe(before + 3);
    }

    [Fact]
    public void Activity_VisualNotInTheTree_IsNotCalled()
    {
        ContainerRuntime root = new();
        DefaultMemberControl control = new();
        CreateVisual(control);

        RunFrames(root, 3);

        control.Calls.ShouldBe(0);
    }

    [Fact]
    public void Activity_VisualRemovedFromTheTree_StopsAndResumesWhenAddedBack()
    {
        ContainerRuntime root = new();
        DefaultMemberControl control = new();
        InteractiveGue visual = CreateVisual(control);
        root.AddChild(visual);
        RunFrames(root, 2);

        root.Children!.Remove(visual);
        RunFrames(root, 3);
        control.Calls.ShouldBe(2);

        root.AddChild(visual);
        RunFrames(root, 2);

        control.Calls.ShouldBe(4);
    }

    [Fact]
    public void Activity_NotNeededControlAddedToAnIdleTree_IsCalledAtLeastOnce()
    {
        ContainerRuntime root = new();
        root.AddChild(new ContainerRuntime());
        RunFrames(root, 3);
        root.IsIdle.ShouldBeTrue();

        NeedsToggleControl control = new() { Needed = false };
        root.AddChild(CreateVisual(control));
        RunFrames(root, 2);

        control.Calls.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Mutating the tree from Activity

    [Fact]
    public void Activity_ThatRemovesItsOwnVisual_DoesNotSkipTheNextSibling()
    {
        ContainerRuntime root = new();
        NeedsToggleControl remover = new();
        InteractiveGue removerVisual = CreateVisual(remover);
        remover.OnActivity = () => root.Children!.Remove(removerVisual);
        DefaultMemberControl next = new();
        root.AddChild(removerVisual);
        root.AddChild(CreateVisual(next));

        RunFrames(root, 3);

        remover.Calls.ShouldBe(1);
        next.Calls.ShouldBe(3);
    }

    [Fact]
    public void Activity_ThatAddsASibling_TicksTheNewSiblingOnTheFollowingFrames()
    {
        ContainerRuntime root = new();
        DefaultMemberControl added = new();
        NeedsToggleControl adder = new();
        bool didAdd = false;
        adder.OnActivity = () =>
        {
            if (!didAdd)
            {
                didAdd = true;
                root.AddChild(CreateVisual(added));
            }
        };
        root.AddChild(CreateVisual(adder));

        RunFrames(root, 3);

        added.Calls.ShouldBeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Interplay with animation

    [Fact]
    public void AnimateSelf_NotNeededControlSharesAParentWithAnAnimatingSprite_SpriteStillAdvances()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        parent.AddChild(CreateVisual(new NeedsToggleControl { Needed = false }));
        ChainSprite sprite = CreateChainSprite();
        parent.AddChild(new GraphicalUiElement(sprite));
        RunFrames(root, 3);
        parent.IsIdle.ShouldBeTrue();

        sprite.Animate = true;
        RunFrames(root, 2);

        sprite.AnimationLogic.TimeIntoAnimation.ShouldBe(0.2, 0.0001);
        parent.IsIdle.ShouldBeFalse();
    }

    [Fact]
    public void AnimateSelf_SpriteStopsAnimatingBesideNotNeededControl_ParentGoesIdleAgain()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        parent.AddChild(CreateVisual(new NeedsToggleControl { Needed = false }));
        ChainSprite sprite = CreateChainSprite();
        parent.AddChild(new GraphicalUiElement(sprite));
        sprite.Animate = true;
        RunFrames(root, 3);
        parent.IsIdle.ShouldBeFalse();

        sprite.Animate = false;
        RunFrames(root, 3);

        parent.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void AnimateSelf_StateAnimationOnAParentOfANotNeededControl_RunsAfterTheSubtreeWentIdle()
    {
        ContainerRuntime root = new();
        ContainerRuntime parent = new();
        root.AddChild(parent);
        parent.AddChild(CreateVisual(new NeedsToggleControl { Needed = false }));
        StateSaveCategory category = new() { Name = "Move" };
        StateSave from = new() { Name = "From" };
        from.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(from);
        StateSave to = new() { Name = "To" };
        to.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(to);
        parent.Categories["Move"] = category;
        AnimationRuntime animation = new() { Name = "Move" };
        animation.Keyframes.Add(new KeyframeRuntime { Time = 0, StateName = "Move/From", InterpolationType = InterpolationType.Linear });
        animation.Keyframes.Add(new KeyframeRuntime { Time = 1, StateName = "Move/To" });
        RunFrames(root, 3);

        parent.PlayAnimation(animation);
        RunFrames(root, 1, secondDifference: 0.5);

        parent.X.ShouldBe(50, 0.01f);
    }

    #endregion
}
