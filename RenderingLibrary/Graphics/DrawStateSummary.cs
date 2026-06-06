using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// A per-frame breakdown of why <c>SpriteBatch.Begin</c> was called, derived from
    /// <see cref="SpriteRenderer.LastFrameDrawStates"/>. Use it to diagnose what is driving a high
    /// begin count: clipping, non-clip render-state changes (blend/color/wrap), or texture switches
    /// within batches. <see cref="ShapeBatchBeginCount"/> surfaces the separate Apos.Shapes begin
    /// count — the only category <see cref="BatchKeyGroupedOrderer"/> can reduce, and one that
    /// <see cref="SpriteRenderer.LastFrameDrawStates"/> does not see.
    /// <para>
    /// Build one with <see cref="FromDrawStates"/>, or call <see cref="Renderer.GetDrawStateSummary"/>
    /// for the just-completed frame.
    /// </para>
    /// </summary>
    public class DrawStateSummary
    {
        /// <summary>Total <c>SpriteBatch.Begin</c> calls recorded for the frame — the sum of the category counts below.</summary>
        public int BeginCount { get; private set; }

        /// <summary>Begins for the first batch of a layer or render cycle. An unavoidable baseline cost.</summary>
        public int InitialBeginCount { get; private set; }

        /// <summary>
        /// Begins caused by entering or leaving a <c>ClipsChildren</c> region. Frequently the dominant cost in
        /// Forms-heavy UI, where each clipping container (ListBox item, ScrollViewer, etc.) forces a begin.
        /// </summary>
        public int ClipChangeBeginCount { get; private set; }

        /// <summary>Begins caused by a non-clip render-state change: blend state, color operation, or texture wrap.</summary>
        public int StateChangeBeginCount { get; private set; }

        /// <summary>
        /// Texture/font sets recorded within batches. These are not <c>SpriteBatch.Begin</c> calls, but each is a
        /// GPU draw-call break inside its batch. Reduce them by atlasing sprites, fonts, and the single-pixel
        /// texture onto shared PNGs.
        /// </summary>
        public int TextureSetCount { get; private set; }

        /// <summary>
        /// Apos.Shapes <c>ShapeBatch.Begin</c> calls for the frame, sourced from
        /// <see cref="RenderStateChangeStatistics.ShapeBatchBeginCount"/>. This is the cross-batcher cost that
        /// <see cref="BatchKeyGroupedOrderer"/> targets; when it is ~0 the grouped orderer cannot help.
        /// </summary>
        public int ShapeBatchBeginCount { get; private set; }

        private DrawStateSummary()
        {
        }

        /// <summary>
        /// Buckets the supplied draw states by the cause of each begin. Pass
        /// <see cref="SpriteRenderer.LastFrameDrawStates"/> and, optionally, the frame's
        /// <see cref="RenderStateChangeStatistics.ShapeBatchBeginCount"/>.
        /// </summary>
        /// <remarks>
        /// Cause is inferred from each begin's recorded <c>ObjectChangingState</c>: null is the initial begin,
        /// a string is a clip-exit, a clipping <see cref="IRenderableIpso"/> is a clip-enter, and any other
        /// renderable is a blend/color/wrap change. The clip-vs-state split is heuristic — a renderable that both
        /// clips and changes a non-clip state in the same begin is attributed to clipping.
        /// </remarks>
        public static DrawStateSummary FromDrawStates(IEnumerable<BeginParameters> drawStates, int shapeBatchBeginCount = 0)
        {
            DrawStateSummary summary = new DrawStateSummary();
            summary.ShapeBatchBeginCount = shapeBatchBeginCount;

            foreach (BeginParameters state in drawStates)
            {
                summary.BeginCount++;

                if (state.ChangeRecord != null)
                {
                    summary.TextureSetCount += state.ChangeRecord.Count;
                }

                object? cause = state.ObjectChangingState;
                if (cause == null)
                {
                    summary.InitialBeginCount++;
                }
                else if (cause is string)
                {
                    // The only begin call site that passes a string is the clip-exit ("Un-set ... Clip").
                    summary.ClipChangeBeginCount++;
                }
                else if (cause is IRenderableIpso ipso && ipso.ClipsChildren)
                {
                    summary.ClipChangeBeginCount++;
                }
                else
                {
                    summary.StateChangeBeginCount++;
                }
            }

            return summary;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string newLine = Environment.NewLine;
            return $"Draw State Summary: {BeginCount} SpriteBatch.Begin(s)"
                + $"{newLine}  Initial:       {InitialBeginCount}"
                + $"{newLine}  Clip changes:  {ClipChangeBeginCount}"
                + $"{newLine}  State changes: {StateChangeBeginCount}"
                + $"{newLine}  Texture sets within batches:     {TextureSetCount}"
                + $"{newLine}  Apos.Shapes ShapeBatch.Begin(s): {ShapeBatchBeginCount}";
        }
    }
}
