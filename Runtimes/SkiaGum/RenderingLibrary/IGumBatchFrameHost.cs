namespace RenderingLibrary.Graphics;

/// <summary>
/// A host that owns the Skia frame (the render target and its composite onto the screen) and lets
/// <see cref="GumBatch"/> open that frame on demand, so a game can call <c>GumBatch.Begin</c>/<c>End</c>
/// in its own Draw before or after the service's <c>Draw()</c> without touching a Skia type. Each
/// Begin/End pair is one pass composited in call order, like a <c>SpriteBatch</c> pass.
/// </summary>
internal interface IGumBatchFrameHost
{
    /// <summary>
    /// Opens a frame for a <see cref="GumBatch"/> cycle and points <see cref="SystemManagers.Canvas"/>
    /// at it. Returns false, opening nothing, when the host does not own the target or a frame is
    /// already open (the cycle then draws into the open frame).
    /// </summary>
    bool TryBeginFrame();

    /// <summary>
    /// Ends the frame <see cref="TryBeginFrame"/> opened: composites it when <paramref name="present"/>
    /// is true, otherwise discards it.
    /// </summary>
    void EndFrame(bool present);
}
