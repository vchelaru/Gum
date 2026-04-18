namespace SokolGum.Animation;

/// <summary>
/// Ordered sequence of <see cref="AnimationFrame"/>s under a single name
/// ("IdleLeft", "Walk", etc). Derives from <see cref="List{T}"/> to match
/// the shared-Gum API shape — indexers and enumeration Just Work.
/// </summary>
public sealed class AnimationChain : List<AnimationFrame>
{
    public string? Name { get; set; }

    /// <summary>Sum of <see cref="AnimationFrame.FrameLength"/> across all frames, in seconds.</summary>
    public float TotalLength
    {
        get
        {
            float sum = 0;
            for (int i = 0; i < Count; i++) sum += this[i].FrameLength;
            return sum;
        }
    }

    public AnimationChain() { }
    public AnimationChain(int capacity) : base(capacity) { }
}
