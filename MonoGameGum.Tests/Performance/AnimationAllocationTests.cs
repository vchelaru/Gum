using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Per-frame allocation baselines and regression guards for active AnimationChain (.achx) playback
/// driven through <see cref="GraphicalUiElement.AnimateSelf(double)"/>. Part of the runtime
/// allocation pass, issue #1934: animated content advances every frame, so any allocation here is
/// paid continuously. Each guard uses <see cref="AllocationMeasurer.MeasureMinimum"/> and a liveness
/// assertion proving the animation actually advanced during the measured window.
/// </summary>
public class AnimationAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public AnimationAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Builds a root container holding a Sprite whose animation chain has several frames, animating
    /// and looping. Returns the root (to drive <see cref="GraphicalUiElement.AnimateSelf(double)"/>)
    /// and the sprite (to read back playback state for the liveness assertion).
    /// </summary>
    private static (ContainerRuntime Root, SpriteRuntime Sprite) BuildAnimatedSpriteScene(int frameCount)
    {
        ContainerRuntime root = new();

        SpriteRuntime sprite = new();

        AnimationChain chain = new() { Name = "TestChain" };
        for (int i = 0; i < frameCount; i++)
        {
            chain.Add(new AnimationFrame { FrameLength = 0.1f });
        }

        AnimationChainList chainList = new();
        chainList.Add(chain);

        sprite.AnimationChains = chainList;
        sprite.Animate = true;

        root.AddChild(sprite);
        root.UpdateLayout();

        return (root, sprite);
    }

    [Fact]
    public void AnimateSelf_TextureChainPlayback_IsZeroAllocation()
    {
        const int frameCount = 4;
        const double frameLength = 0.1;

        (ContainerRuntime root, SpriteRuntime sprite) = BuildAnimatedSpriteScene(frameCount);

        int cycleCount = 0;
        sprite.AnimationChainCycled += () => cycleCount++;

        // Advance a partial frame each iteration so the frame index changes several times per
        // full loop (proving playback is live), rather than a single big jump.
        const double secondsPerFrame = frameLength / 2.0;
        const int measuredIterations = 500;
        const int attempts = 3;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => root.AnimateSelf(secondsPerFrame),
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        _output.WriteLine($"AnimateSelf on a {frameCount}-frame animated sprite: " +
            $"{result.BytesPerIteration:N0} bytes/frame ({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // Liveness: the chain must have looped many times across the measured window, proving the
        // frame-advance path actually ran (so a zero-allocation result isn't a no-op artifact).
        cycleCount.ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    /// <summary>
    /// Builds a looping state animation that interpolates X from 0 to 100 over one second and starts
    /// it playing on a root container. Returns the root (to drive AnimateSelf).
    /// </summary>
    private static ContainerRuntime BuildStateAnimationScene()
    {
        ContainerRuntime root = new();

        StateSaveCategory category = new() { Name = "Category1" };
        root.Categories[category.Name] = category;

        StateSave state1 = new() { Name = "State1" };
        state1.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(state1);

        StateSave state2 = new() { Name = "State2" };
        state2.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(state2);

        KeyframeRuntime keyframe1 = new()
        {
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
            Time = 0,
            StateName = "Category1/State1"
        };
        KeyframeRuntime keyframe2 = new()
        {
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
            Time = 1,
            StateName = "Category1/State2"
        };

        AnimationRuntime animation = new() { Name = "Animation1", Loops = true };
        animation.Keyframes.Add(keyframe1);
        animation.Keyframes.Add(keyframe2);
        animation.RefreshCumulativeStates(root);

        root.Animations = new List<AnimationRuntime> { animation };
        root.PlayAnimation("Animation1");

        return root;
    }

    [Fact]
    public void AnimateSelf_StateKeyframePlayback_MinimizesAllocation()
    {
        ContainerRuntime root = BuildStateAnimationScene();

        // Advance a fraction of the one-second animation each iteration so the interpolated X keeps
        // moving and the animation (which loops) never stops.
        const double secondsPerFrame = 0.05;
        const int measuredIterations = 500;
        const int attempts = 3;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => root.AnimateSelf(secondsPerFrame),
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        _output.WriteLine($"AnimateSelf on a looping state animation: " +
            $"{result.BytesPerIteration:N0} bytes/frame ({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // Liveness: the animation must still be playing and X must be interpolating within the range.
        root.AnimationController.IsPlaying.ShouldBeTrue();
        root.X.ShouldBeInRange(0f, 100f);
        root.X.ShouldBeGreaterThan(0f);

        // Upper-bound guard for the state/keyframe advancement path. Removing the LINQ
        // closures/iterators from the keyframe lookup took this from ~3,040 to ~2,336 B/frame. The
        // residual is the per-frame interpolated StateSave that gets applied (Clone + MergeIntoThis
        // + ApplyState), which is out of scope here; the bound locks in the win and guards the
        // avoidable-allocation portion from regressing.
        result.BytesPerIteration.ShouldBeLessThan(2500);
    }
}
