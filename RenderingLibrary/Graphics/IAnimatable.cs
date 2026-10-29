using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface IAnimatable
    {
        bool AnimateSelf(double time);
    }

    /// <summary>
    /// An <see cref="IAnimatable"/> whose per-frame work is gated by an AnimationChain playback flag,
    /// so GraphicalUiElement.AnimateSelf can skip it while the flag is off. An IAnimatable that does
    /// not implement this is ticked every frame.
    /// </summary>
    internal interface IAnimationChainRenderable
    {
        /// <summary>
        /// Whether the AnimationChain playback flag (Animate) is on. While off, AnimateSelf does nothing.
        /// </summary>
        bool IsChainAnimationEnabled { get; }

        /// <summary>
        /// The AnimationChainList currently assigned, typed as object because GumCommon cannot name it.
        /// Hot reload compares it with the list it loaded from a file to tell chains that came from
        /// the file from chains assigned by code.
        /// </summary>
        object? AnimationChainList { get; }

        /// <summary>
        /// True when a chain name was set and the assigned list has no chain with that name, so nothing plays.
        /// </summary>
        bool IsNamedChainMissing { get; }

        /// <summary>
        /// Sets the callback raised when the playback flag turns on, so the owning element can stop
        /// skipping this renderable.
        /// </summary>
        void SetChainAnimationEnabledCallback(System.Action? callback);
    }
}
