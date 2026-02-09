using Gum.Wireframe;
using System;

namespace Gum.StateAnimation.Runtime;

/// <summary>
/// Controls the playback of animations on a GraphicalUiElement, managing state, timing, and events.
/// </summary>
public class AnimationController
{
    private AnimationState _state = AnimationState.Stopped;

    /// <summary>
    /// Gets the currently playing or paused animation, or null if no animation is loaded.
    /// </summary>
    public AnimationRuntime? CurrentAnimation { get; private set; }

    /// <summary>
    /// Gets the current playback time of the animation in seconds.
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    /// Gets whether an animation is currently playing (not paused or stopped).
    /// </summary>
    public bool IsPlaying => _state == AnimationState.Playing;

    /// <summary>
    /// Gets whether the current animation is paused.
    /// </summary>
    public bool IsPaused => _state == AnimationState.Paused;

    /// <summary>
    /// Gets whether no animation is currently playing or paused.
    /// </summary>
    public bool IsStopped => _state == AnimationState.Stopped;

    /// <summary>
    /// Raised when an animation starts playing.
    /// </summary>
    public event Action? OnStarted;

    /// <summary>
    /// Raised when an animation completes (reaches its end and does not loop).
    /// </summary>
    public event Action? OnCompleted;

    /// <summary>
    /// Raised when an animation is stopped before completion.
    /// </summary>
    public event Action? OnStopped;

    /// <summary>
    /// Raised when a playing animation is paused.
    /// </summary>
    public event Action? OnPaused;

    /// <summary>
    /// Raised when a paused animation is resumed.
    /// </summary>
    public event Action? OnResumed;

    /// <summary>
    /// Starts playing the specified animation from the beginning.
    /// </summary>
    /// <param name="animation">The AnimationRuntime to play.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public void Play(AnimationRuntime animation)
    {
        if (animation == null)
            throw new ArgumentNullException(nameof(animation));

        CurrentAnimation = animation;
        CurrentTime = 0;
        _state = AnimationState.Playing;
        OnStarted?.Invoke();
    }

    /// <summary>
    /// Pauses the currently playing animation, preserving the current time.
    /// </summary>
    public void Pause()
    {
        if (_state == AnimationState.Playing)
        {
            _state = AnimationState.Paused;
            OnPaused?.Invoke();
        }
    }

    /// <summary>
    /// Resumes a paused animation from its current time.
    /// </summary>
    public void Resume()
    {
        if (_state == AnimationState.Paused)
        {
            _state = AnimationState.Playing;
            OnResumed?.Invoke();
        }
    }

    /// <summary>
    /// Stops the current animation and resets the time to zero.
    /// </summary>
    public void Stop()
    {
        if (_state != AnimationState.Stopped)
        {
            _state = AnimationState.Stopped;
            CurrentAnimation = null;
            CurrentTime = 0;
            OnStopped?.Invoke();
        }
    }

    /// <summary>
    /// Restarts the current animation from the beginning without changing the playing state.
    /// </summary>
    public void Restart()
    {
        if (CurrentAnimation != null)
        {
            CurrentTime = 0;
            if (_state != AnimationState.Playing)
            {
                _state = AnimationState.Playing;
                OnStarted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Updates the animation state and applies it to the target GraphicalUiElement.
    /// Called internally during the update loop.
    /// </summary>
    /// <param name="secondDifference">The time elapsed since the last update in seconds.</param>
    /// <param name="target">The GraphicalUiElement to apply the animation to.</param>
    public void Update(double secondDifference, GraphicalUiElement target)
    {
        if (_state == AnimationState.Playing && CurrentAnimation != null)
        {
            CurrentTime += secondDifference;
            CurrentAnimation.ApplyAtTimeTo(CurrentTime, target);

            // Check if animation has completed
            if (!CurrentAnimation.Loops && CurrentTime >= CurrentAnimation.Length)
            {
                _state = AnimationState.Stopped;
                CurrentAnimation = null;
                CurrentTime = 0;
                OnCompleted?.Invoke();
            }
        }
    }
}

/// <summary>
/// Represents the playback state of an animation.
/// </summary>
public enum AnimationState
{
    /// <summary>
    /// No animation is playing or the animation has completed.
    /// </summary>
    Stopped,

    /// <summary>
    /// The animation is actively playing.
    /// </summary>
    Playing,

    /// <summary>
    /// The animation is paused at its current time.
    /// </summary>
    Paused
}
