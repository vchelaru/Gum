namespace Gum.StateAnimation.Runtime;

/// <summary>
/// Carries information about a named event raised during animation playback.
/// Raised via <see cref="AnimationController.NamedEventOccurred"/> when playback
/// crosses the time of a named event authored on an animation timeline.
/// </summary>
public class NamedAnimationEventArgs
{
    /// <summary>
    /// The name of the event, as authored in the animation. Typically switched on
    /// by game code to decide which action to take (play a sound, enable a hitbox, etc.).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The time, in seconds from the start of the animation, at which the event is authored.
    /// </summary>
    public float Time { get; }

    public NamedAnimationEventArgs(string name, float time)
    {
        Name = name;
        Time = time;
    }
}
