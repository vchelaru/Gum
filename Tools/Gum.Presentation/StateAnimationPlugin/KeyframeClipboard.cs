using StateAnimationPlugin.ViewModels;

namespace StateAnimationPlugin;

/// <summary>
/// The keyframe the user last copied in the Animations tab. One instance outlives the per-element
/// <see cref="ElementAnimationsViewModel"/>s, so a keyframe copied on one element pastes on another.
/// </summary>
public interface IKeyframeClipboard
{
    /// <summary>The copied keyframe, or null when nothing has been copied.</summary>
    AnimatedKeyframeViewModel? Copied { get; set; }
}

/// <inheritdoc/>
public class KeyframeClipboard : IKeyframeClipboard
{
    /// <inheritdoc/>
    public AnimatedKeyframeViewModel? Copied { get; set; }
}
