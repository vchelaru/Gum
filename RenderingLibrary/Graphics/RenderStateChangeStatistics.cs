namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// Per-frame counters for render-state changes that the existing
    /// <see cref="SpriteRenderer.LastFrameDrawStates"/> does not capture — specifically the
    /// Apos.Shapes <c>ShapeBatch</c> begins, which live on a separate GPU command stream from
    /// the SpriteBatch. Used to measure how much shape rendering adds to a frame (e.g. comparing
    /// SpriteBatch-backed visuals against Apos.Shapes-backed ones).
    /// <para>
    /// Owned by <see cref="Renderer"/> and reset at the start of each <see cref="Renderer.Draw(SystemManagers)"/>,
    /// so after a frame the counts describe just-completed frame, mirroring
    /// <see cref="SpriteRenderer.LastFrameDrawStates"/>.
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
        /// Records one ShapeBatch begin. Called from the Apos.Shapes runtime whenever it opens
        /// (or re-opens) its ShapeBatch.
        /// </summary>
        public void RecordShapeBatchBegin()
        {
            ShapeBatchBeginCount++;
        }

        /// <summary>
        /// Clears all counters. Called once per frame at the start of <see cref="Renderer.Draw(SystemManagers)"/>.
        /// </summary>
        public void Reset()
        {
            ShapeBatchBeginCount = 0;
        }
    }
}
