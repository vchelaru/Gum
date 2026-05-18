using System;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// A page within the immediate-mode sample. Each screen owns its own Gum objects and
    /// draws them via a shared <see cref="GumBatch"/> (and optionally <see cref="SpriteBatch"/>).
    /// </summary>
    public interface IImmediateModeScreen : IDisposable
    {
        void Initialize(GraphicsDevice graphicsDevice);

        void Draw(GumBatch gumBatch, SpriteBatch spriteBatch);
    }
}
