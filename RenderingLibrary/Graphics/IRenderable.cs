using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics
{
    public interface IRenderable
    {
        BlendState BlendState { get; }

        bool Wrap { get; }

        void Render(SpriteRenderer spriteRenderer, SystemManagers managers);

        /// <summary>
        /// Perform logic which needs to occur before a SpriteBatch has been started
        /// </summary>
        void PreRender();
    }
}
