using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class AnimationControllerTests : BaseTestClass
{
    #region Play Tests

    [Fact]
    public void Play_ShouldSetCurrentAnimation()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);

        controller.CurrentAnimation.ShouldBe(animation);
    }

    [Fact]
    public void Play_ShouldResetCurrentTimeToZero()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);

        controller.CurrentTime.ShouldBe(0.5);

        controller.Play(animation);

        controller.CurrentTime.ShouldBe(0.0);
    }

    [Fact]
    public void Play_ShouldSetStateToPlaying()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);

        controller.IsPlaying.ShouldBeTrue();
        controller.IsPaused.ShouldBeFalse();
        controller.IsStopped.ShouldBeFalse();
    }

    [Fact]
    public void Play_ShouldRaiseOnStartedEvent()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        bool eventRaised = false;

        controller.OnStarted += () => eventRaised = true;
        controller.Play(animation);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Play_ShouldThrow_IfAnimationIsNull()
    {
        var controller = new AnimationController();

        Should.Throw<ArgumentNullException>(() => controller.Play(null!));
    }

    #endregion

    #region Stop Tests

    [Fact]
    public void Stop_ShouldClearCurrentAnimation()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);
        controller.Stop();

        controller.CurrentAnimation.ShouldBeNull();
    }

    [Fact]
    public void Stop_ShouldResetCurrentTimeToZero()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);
        controller.Stop();

        controller.CurrentTime.ShouldBe(0.0);
    }

    [Fact]
    public void Stop_ShouldSetStateToStopped()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);
        controller.Stop();

        controller.IsStopped.ShouldBeTrue();
        controller.IsPlaying.ShouldBeFalse();
        controller.IsPaused.ShouldBeFalse();
    }

    [Fact]
    public void Stop_ShouldRaiseOnStoppedEvent()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        bool eventRaised = false;

        controller.OnStopped += () => eventRaised = true;
        controller.Play(animation);
        controller.Stop();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Stop_ShouldNotRaiseEvent_WhenAlreadyStopped()
    {
        var controller = new AnimationController();
        int eventCount = 0;

        controller.OnStopped += () => eventCount++;
        controller.Stop();

        eventCount.ShouldBe(0);
    }

    #endregion

    #region Pause Tests

    [Fact]
    public void Pause_ShouldSetStateToPaused_WhenPlaying()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);
        controller.Pause();

        controller.IsPaused.ShouldBeTrue();
        controller.IsPlaying.ShouldBeFalse();
        controller.IsStopped.ShouldBeFalse();
    }

    [Fact]
    public void Pause_ShouldRaiseOnPausedEvent()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        bool eventRaised = false;

        controller.OnPaused += () => eventRaised = true;
        controller.Play(animation);
        controller.Pause();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Pause_ShouldNotRaiseEvent_WhenNotPlaying()
    {
        var controller = new AnimationController();
        int eventCount = 0;

        controller.OnPaused += () => eventCount++;
        controller.Pause();

        eventCount.ShouldBe(0);
    }

    [Fact]
    public void Pause_ShouldPreserveCurrentTime()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);
        controller.Pause();

        controller.CurrentTime.ShouldBe(0.5);
    }

    #endregion

    #region Resume Tests

    [Fact]
    public void Resume_ShouldSetStateToPlaying_WhenPaused()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);
        controller.Pause();
        controller.Resume();

        controller.IsPlaying.ShouldBeTrue();
        controller.IsPaused.ShouldBeFalse();
        controller.IsStopped.ShouldBeFalse();
    }

    [Fact]
    public void Resume_ShouldRaiseOnResumedEvent()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        bool eventRaised = false;

        controller.OnResumed += () => eventRaised = true;
        controller.Play(animation);
        controller.Pause();
        controller.Resume();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Resume_ShouldNotRaiseEvent_WhenNotPaused()
    {
        var controller = new AnimationController();
        int eventCount = 0;

        controller.OnResumed += () => eventCount++;
        controller.Resume();

        eventCount.ShouldBe(0);
    }

    [Fact]
    public void Resume_ShouldPreserveCurrentTime()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);
        controller.Pause();
        controller.Resume();

        controller.CurrentTime.ShouldBe(0.5);
    }

    #endregion

    #region Restart Tests

    [Fact]
    public void Restart_ShouldResetCurrentTimeToZero()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);
        controller.Restart();

        controller.CurrentTime.ShouldBe(0.0);
    }

    [Fact]
    public void Restart_ShouldSetStateToPlaying_WhenPaused()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.Play(animation);
        controller.Pause();
        controller.Restart();

        controller.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public void Restart_ShouldRaiseOnStartedEvent_WhenNotPlaying()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        bool eventRaised = false;

        controller.Play(animation);
        controller.Pause();
        controller.OnStarted += () => eventRaised = true;
        controller.Restart();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Restart_ShouldDoNothing_WhenNoAnimationLoaded()
    {
        var controller = new AnimationController();

        // Should not throw
        controller.Restart();

        controller.CurrentTime.ShouldBe(0.0);
        controller.IsStopped.ShouldBeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldAdvanceCurrentTime_WhenPlaying()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.3, gue);

        controller.CurrentTime.ShouldBe(0.3);

        controller.Update(0.2, gue);

        controller.CurrentTime.ShouldBe(0.5);
    }

    [Fact]
    public void Update_ShouldNotAdvanceCurrentTime_WhenPaused()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.3, gue);
        controller.Pause();
        controller.Update(0.2, gue);

        controller.CurrentTime.ShouldBe(0.3);
    }

    [Fact]
    public void Update_ShouldNotAdvanceCurrentTime_WhenStopped()
    {
        var controller = new AnimationController();
        var gue = CreateTestGraphicalUiElement();

        controller.Update(0.5, gue);

        controller.CurrentTime.ShouldBe(0.0);
    }

    [Fact]
    public void Update_ShouldApplyAnimationToTarget()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        gue.X.ShouldBe(0f);

        controller.Play(animation);
        controller.Update(0.5, gue);

        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void Update_ShouldCompleteNonLoopingAnimation_WhenTimeExceedsLength()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        animation.Loops = false;
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(1.5, gue); // Animation length is 1.0

        controller.IsStopped.ShouldBeTrue();
        controller.CurrentAnimation.ShouldBeNull();
        controller.CurrentTime.ShouldBe(0.0);
    }

    [Fact]
    public void Update_ShouldRaiseOnCompletedEvent_WhenNonLoopingAnimationCompletes()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        animation.Loops = false;
        var gue = CreateTestGraphicalUiElement();
        bool eventRaised = false;

        controller.OnCompleted += () => eventRaised = true;
        controller.Play(animation);
        controller.Update(1.5, gue);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Update_ShouldNotCompleteLoopingAnimation()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        animation.Loops = true;
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(1.5, gue); // Exceeds length

        controller.IsPlaying.ShouldBeTrue();
        controller.CurrentAnimation.ShouldNotBeNull();
    }

    [Fact]
    public void Update_ShouldContinueLoopingAnimation_BeyondLength()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        animation.Loops = true;
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(2.5, gue); // 2.5 seconds into a 1-second animation

        controller.CurrentTime.ShouldBe(2.5);
        controller.IsPlaying.ShouldBeTrue();
    }

    #endregion

    #region State Property Tests

    [Fact]
    public void IsPlaying_ShouldBeTrue_OnlyWhenStatePlaying()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.IsPlaying.ShouldBeFalse();

        controller.Play(animation);
        controller.IsPlaying.ShouldBeTrue();

        controller.Pause();
        controller.IsPlaying.ShouldBeFalse();

        controller.Resume();
        controller.IsPlaying.ShouldBeTrue();

        controller.Stop();
        controller.IsPlaying.ShouldBeFalse();
    }

    [Fact]
    public void IsPaused_ShouldBeTrue_OnlyWhenStatePaused()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.IsPaused.ShouldBeFalse();

        controller.Play(animation);
        controller.IsPaused.ShouldBeFalse();

        controller.Pause();
        controller.IsPaused.ShouldBeTrue();

        controller.Resume();
        controller.IsPaused.ShouldBeFalse();

        controller.Pause();
        controller.IsPaused.ShouldBeTrue();

        controller.Stop();
        controller.IsPaused.ShouldBeFalse();
    }

    [Fact]
    public void IsStopped_ShouldBeTrue_OnlyWhenStateStopped()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();

        controller.IsStopped.ShouldBeTrue();

        controller.Play(animation);
        controller.IsStopped.ShouldBeFalse();

        controller.Pause();
        controller.IsStopped.ShouldBeFalse();

        controller.Stop();
        controller.IsStopped.ShouldBeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PauseResumeWorkflow_ShouldWorkCorrectly()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        // Play for 0.3 seconds
        controller.Play(animation);
        controller.Update(0.3, gue);
        gue.X.ShouldBeInRange(29.99f, 30.01f);

        // Pause and verify time doesn't advance
        controller.Pause();
        controller.Update(0.5, gue);
        controller.CurrentTime.ShouldBe(0.3);
        gue.X.ShouldBeInRange(29.99f, 30.01f);

        // Resume and continue
        controller.Resume();
        controller.Update(0.2, gue);
        controller.CurrentTime.ShouldBe(0.5);
        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void StopAndReplay_ShouldResetAnimation()
    {
        var controller = new AnimationController();
        var animation = CreateTestAnimation();
        var gue = CreateTestGraphicalUiElement();

        controller.Play(animation);
        controller.Update(0.5, gue);
        controller.Stop();

        controller.CurrentTime.ShouldBe(0.0);
        controller.CurrentAnimation.ShouldBeNull();

        controller.Play(animation);
        controller.Update(0.3, gue);

        controller.CurrentTime.ShouldBe(0.3);
        gue.X.ShouldBeInRange(29.99f, 30.01f);
    }

    #endregion

    #region Helper Methods

    private static AnimationRuntime CreateTestAnimation()
    {
        var category = new StateSaveCategory { Name = "TestCategory" };

        var state1 = new StateSave { Name = "State1" };
        state1.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(state1);

        var state2 = new StateSave { Name = "State2" };
        state2.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(state2);

        var keyframe1 = new KeyframeRuntime
        {
            InterpolationType = InterpolationType.Linear,
            Time = 0,
            StateName = "TestCategory/State1"
        };

        var keyframe2 = new KeyframeRuntime
        {
            Time = 1,
            StateName = "TestCategory/State2"
        };

        var animation = new AnimationRuntime { Name = "TestAnimation" };
        animation.Keyframes.Add(keyframe1);
        animation.Keyframes.Add(keyframe2);

        return animation;
    }

    private static GraphicalUiElement CreateTestGraphicalUiElement()
    {
        var category = new StateSaveCategory { Name = "TestCategory" };

        var state1 = new StateSave { Name = "State1" };
        state1.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(state1);

        var state2 = new StateSave { Name = "State2" };
        state2.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(state2);

        var gue = new GraphicalUiElement(new InvisibleRenderable());
        gue.Categories["TestCategory"] = category;

        return gue;
    }

    #endregion
}
