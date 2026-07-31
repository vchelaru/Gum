using System;
using System.Collections.Generic;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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

        /// <summary>
        /// Gum roots owned by this screen that are not part of <c>GumService.Default.Root</c>
        /// (e.g. a container only ever drawn via <c>GumBatch</c>) but still need Forms
        /// interactivity (hover/click/drag). Game1 combines these with Root and runs them through
        /// a single <c>GumService.Default.Update(gameTime, roots)</c> call each frame.
        ///
        /// This must be combined into one call rather than calling Update a second time: Cursor's
        /// push/click detection is edge-triggered (compares this frame's raw mouse state against a
        /// snapshot taken by the previous Update call), so a second Update call in the same frame
        /// sees no edge - its "previous" snapshot is the state the first call just wrote - and
        /// push/click silently never fire, even though hover keeps working since it only checks the
        /// cursor's current position. Default: none.
        /// </summary>
        IEnumerable<GraphicalUiElement> AdditionalUpdateRoots => Array.Empty<GraphicalUiElement>();

        void Draw(GumBatch gumBatch, SpriteBatch spriteBatch);
    }
}
