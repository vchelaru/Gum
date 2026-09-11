using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.IO;

namespace Gum.Managers
{
    /// <summary>
    /// Manages fonts used by the app, as opposed to fonts used by the loaded project. Plugin-scoped:
    /// instantiated by the editor plugin (<c>MainEditorTabPlugin</c>), not registered in Builder.cs.
    /// </summary>
    public interface IToolFontService
    {
        /// <summary>
        /// The font used by editor overlay text. Null until <see cref="Initialize"/> has been called.
        /// </summary>
        BitmapFont ToolFont { get; }

        /// <summary>
        /// Loads the tool font from the application's content directory.
        /// </summary>
        void Initialize();
    }

    /// <inheritdoc cref="IToolFontService"/>
    public class ToolFontService : IToolFontService
    {
        BitmapFont _toolFont;
        public BitmapFont ToolFont
        {
            get => _toolFont;
        }
        public void Initialize()
        {
            // Assembly.GetEntryAssembly().Location returns empty string in single-file published apps.
            // AppContext.BaseDirectory is the correct way to locate the executable's directory.
            var directory = AppContext.BaseDirectory;

            var fntFilePath = Path.Combine(directory, "Content/Fonts/Font18Arial_o1.fnt");
            var font = new BitmapFont(fntFilePath);

            // Remove the loaded contnet from the loaderManager so it is never accidentally disposed
            // when we clear cache
            LoaderManager.Self.RemoveWithoutDisposing(font);

            foreach (var texture in font.Textures)
            {
                LoaderManager.Self.RemoveWithoutDisposing(texture);
            }
            _toolFont = font;
        }
    }
}
