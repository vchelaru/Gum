using System.Collections.Generic;

namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// Per-frame counters for render-state changes that the existing
    /// <c>SpriteRenderer.LastFrameDrawStates</c> does not capture — specifically the
    /// Apos.Shapes <c>ShapeBatch</c> begins, which live on a separate GPU command stream from
    /// the SpriteBatch. Used to measure how much shape rendering adds to a frame (e.g. comparing
    /// SpriteBatch-backed visuals against Apos.Shapes-backed ones).
    /// <para>
    /// Owned by <see cref="Renderer"/> and reset at the start of each <see cref="Renderer.Draw(SystemManagers)"/>,
    /// so after a frame the counts describe just-completed frame, mirroring
    /// <c>SpriteRenderer.LastFrameDrawStates</c>.
    /// </para>
    /// </summary>
    public class RenderStateChangeStatistics
    {
        /// <summary>
        /// The number of Apos.Shapes <c>ShapeBatch.Begin</c> calls recorded since the last
        /// <see cref="Reset"/>. Each begin is a GPU state change that flushes the previous batch.
        /// </summary>
        public int ShapeBatchBeginCount { get; private set; }

        /// <summary>
        /// The number of GPU draw calls recorded since the last <see cref="Reset"/>. Unlike
        /// <see cref="ShapeBatchBeginCount"/> (an XNA/Apos.Shapes-specific begin count), this is a
        /// backend-neutral draw-call total. The raylib renderer owns a <c>RenderBatch</c> and banks
        /// its authoritative draw counter at each batch flush; the MonoGame/KNI/FNA renderer
        /// (<see cref="Renderer.Draw(SystemManagers)"/>) sources it from a before/after delta of
        /// <c>GraphicsDevice.Metrics.DrawCount</c> for the pass (issue #2697). On every runtime it counts
        /// draw submissions: the Skia renderer records one per DrawAtlas flush of batched sprites and one
        /// per draw that does not batch (a shape, a Text paint, a NineSlice section, a composited layer
        /// or render target) through <see cref="RecordDrawCall"/> (issue #7826).
        /// </summary>
        public int DrawCallCount { get; private set; }

        private readonly List<int> _bankSegments = new();

        /// <summary>
        /// One entry per raylib-backed <see cref="AddDrawCalls"/> call recorded since the last
        /// <see cref="Reset"/> (i.e. one per <c>BatchDrawCallCounter.Bank()</c> invocation, in
        /// order), each holding that segment's real draw-call count — including zero-draw segments.
        /// An empty list means the pass never banked at all (the counter was inactive, e.g. the raylib
        /// window wasn't ready yet); a list of all zeros means it banked but every segment was empty.
        /// Diagnostic only — added for issue #4901 so a recurrence of that flake shows *why* the
        /// total didn't grow, instead of only the final <see cref="DrawCallCount"/>.
        /// </summary>
        public IReadOnlyList<int> BankSegments => _bankSegments;

        /// <summary>
        /// Records one ShapeBatch begin. Called from the Apos.Shapes runtime whenever it opens
        /// (or re-opens) its ShapeBatch.
        /// </summary>
        public void RecordShapeBatchBegin()
        {
            ShapeBatchBeginCount++;
        }

        /// <summary>
        /// Adds one draw call to <see cref="DrawCallCount"/> without recording a
        /// <see cref="BankSegments"/> entry. Called by the Skia renderer and its renderables for each
        /// SKCanvas draw call they issue.
        /// </summary>
        public void RecordDrawCall()
        {
            DrawCallCount++;
        }

        /// <summary>
        /// Adds <paramref name="count"/> draw calls to <see cref="DrawCallCount"/> and records the
        /// segment in <see cref="BankSegments"/>. Called by the raylib renderer as it banks the
        /// owned <c>RenderBatch</c>'s draw counter at each flush.
        /// </summary>
        public void AddDrawCalls(int count)
        {
            DrawCallCount += count;
            _bankSegments.Add(count);
        }

        /// <summary>
        /// Clears all counters. Called once per frame at the start of <see cref="Renderer.Draw(SystemManagers)"/>.
        /// </summary>
        public void Reset()
        {
            ShapeBatchBeginCount = 0;
            DrawCallCount = 0;
            _bankSegments.Clear();
        }
    }
}
