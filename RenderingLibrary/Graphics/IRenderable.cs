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

    public void StartBatch(ISystemManagers systemManagers) { }
    public void EndBatch(ISystemManagers systemManagers) { }
#else
    string BatchKey { get; }

    void StartBatch(ISystemManagers systemManagers);
    void EndBatch(ISystemManagers systemManagers);
#endif
}
