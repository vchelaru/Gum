using Gum.Wireframe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gum.StateAnimation.Runtime;

/// <summary>
/// Controls the playback of animations on a GraphicalUiElement, managing state, timing, and events.
/// </summary>
public class AnimationController
{
    private AnimationState _state = AnimationState.Stopped;
    private TaskCompletionSource<bool>? _asyncCompletionSource;

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
    /// Raised when playback crosses the time of a named event authored on the animation timeline.
    /// Games typically switch on <see cref="NamedAnimationEventArgs.Name"/> to trigger sound effects,
    /// hitboxes, and similar. For looping animations the event fires once per loop.
    /// </summary>
    public event Action<NamedAnimationEventArgs>? NamedEventOccurred;

    /// <summary>
    /// Starts playing the specified animation from the beginning.
    /// </summary>
    /// <param name="animation">The AnimationRuntime to play.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public void Play(AnimationRuntime animation)
    {
        if (animation == null)
            throw new ArgumentNullException(nameof(animation));

        // Interrupting a pending PlayAnimationAsync task cancels it rather than
        // leaving it unresolved, since only one animation can play at a time.
        _asyncCompletionSource?.TrySetCanceled();

        CurrentAnimation = animation;
        CurrentTime = 0;
        _state = AnimationState.Playing;
        OnStarted?.Invoke();
    }

    /// <summary>
    /// Starts playing the specified animation from the beginning and returns a <see cref="Task"/>
    /// that completes when the animation finishes.
    /// </summary>
    /// <param name="animation">The AnimationRuntime to play.</param>
    /// <param name="cancellationToken">
    /// A token used to stop the animation and cancel the returned task before it finishes.
    /// </param>
    /// <returns>
    /// A task that completes successfully when the animation reaches its end. If the animation
    /// is stopped, replaced by another <see cref="Play(AnimationRuntime)"/>/<see cref="PlayAnimationAsync"/>
    /// call, or the <paramref name="cancellationToken"/> is triggered before it finishes, the task
    /// is cancelled (<see cref="TaskCanceledException"/>) instead of completing.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public Task PlayAnimationAsync(AnimationRuntime animation, CancellationToken cancellationToken = default)
    {
        Play(animation);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _asyncCompletionSource = tcs;

        void OnCompletedHandler() => tcs.TrySetResult(true);
        void OnStoppedHandler() => tcs.TrySetCanceled();

        OnCompleted += OnCompletedHandler;
        OnStopped += OnStoppedHandler;

        CancellationTokenRegistration registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled(cancellationToken))
                {
                    Stop();
                }
            })
            : default;

        tcs.Task.ContinueWith(_ =>
        {
            OnCompleted -= OnCompletedHandler;
            OnStopped -= OnStoppedHandler;
            registration.Dispose();

            if (ReferenceEquals(_asyncCompletionSource, tcs))
            {
                _asyncCompletionSource = null;
            }
        }, TaskScheduler.Default);

        return tcs.Task;
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
            AnimationRuntime animation = CurrentAnimation;
            double previousTime = CurrentTime;
            CurrentTime += secondDifference;
            animation.ApplyAtTimeTo(CurrentTime, target);

            RaiseNamedEvents(animation, previousTime, CurrentTime);

            // Check if animation has completed
            if (CurrentAnimation != null && !CurrentAnimation.Loops && CurrentTime >= CurrentAnimation.Length)
            {
                _state = AnimationState.Stopped;
                CurrentAnimation = null;
                CurrentTime = 0;
                OnCompleted?.Invoke();
            }
        }
    }

    // Fires NamedEventOccurred for every named event whose time falls within (previousTime, currentTime].
    // For looping animations each authored event recurs every Length seconds, so all occurrences in the
    // interval are raised. Skipped entirely when nothing is subscribed to avoid per-frame work.
    private void RaiseNamedEvents(AnimationRuntime animation, double previousTime, double currentTime)
    {
        if (NamedEventOccurred == null)
        {
            return;
        }

        double length = animation.Length;
        bool loops = animation.Loops && length > 0;

        // Plain loop rather than LINQ: this runs per frame for every playing animation.
        for (int i = 0; i < animation.Keyframes.Count; i++)
        {
            KeyframeRuntime keyframe = animation.Keyframes[i];
            if (string.IsNullOrEmpty(keyframe.EventName))
            {
                continue;
            }

            float eventTime = keyframe.Time;

            if (loops)
            {
                // Advance to the first occurrence strictly after previousTime, then fire each up to currentTime.
                double occurrence = eventTime;
                if (occurrence <= previousTime)
                {
                    double loopsToSkip = Math.Floor((previousTime - eventTime) / length) + 1;
                    occurrence = eventTime + loopsToSkip * length;
                }
                while (occurrence <= currentTime)
                {
                    NamedEventOccurred?.Invoke(new NamedAnimationEventArgs(keyframe.EventName!, eventTime));
                    occurrence += length;
                }
            }
            else if (eventTime > previousTime && eventTime <= currentTime)
            {
                NamedEventOccurred?.Invoke(new NamedAnimationEventArgs(keyframe.EventName!, eventTime));
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
