using System;

#nullable enable

namespace RenderingLibrary.Graphics.Animation;

/// <summary>
/// Obsolete alias for <see cref="AnimationChainLogic"/>. The class was renamed
/// to reflect that the playback state is shared across multiple renderable types
/// (Sprite, NineSlice, ...), not only Sprite. Use <see cref="AnimationChainLogic"/>
/// in new code.
/// </summary>
[Obsolete("Renamed to AnimationChainLogic. This subclass is kept for back-compat and will be removed in a future release.")]
public class SpriteAnimationLogic : AnimationChainLogic
{
}
