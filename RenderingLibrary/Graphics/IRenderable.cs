using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics;

public interface IRenderable
{
    BlendState BlendState { get; }

    bool Wrap { get; }

    void Render(ISystemManagers managers);

    /// <summary>
    /// Perform logic which needs to occur before a SpriteBatch has been started
    /// </summary>
    void PreRender();

#if NET8_0_OR_GREATER
    public string BatchKey => string.Empty;

    /// <summary>
    /// Finer-grained grouping hint read only by <see cref="BatchKeyGroupedOrderer"/> (e.g. a
    /// Texture2D reference) — unlike <see cref="BatchKey"/>, <see cref="BatchOrchestrator"/> never
    /// reads this, so it carries no flush cost. Null means "no finer grouping than BatchKey."
    /// </summary>
    public object? BatchSortKey => null;

    public void StartBatch(ISystemManagers systemManagers) { }
    public void EndBatch(ISystemManagers systemManagers) { }
#else
    string BatchKey { get; }

    object? BatchSortKey { get; }

    void StartBatch(ISystemManagers systemManagers);
    void EndBatch(ISystemManagers systemManagers);
#endif
}
