using System.Windows.Media;

namespace Gum.Controls.DataUi
{
    /// <summary>
    /// Pure color logic for <see cref="ColorDisplay"/>'s read-only swatch overlay.
    /// </summary>
    public static class ColorDisplayLogic
    {
        /// <summary>
        /// Returns the color to fill the read-only swatch overlay with, always fully opaque.
        /// Alpha is discarded so a resolved color with zero or partial alpha still renders as a
        /// solid swatch instead of blending into (or disappearing into) the background.
        /// </summary>
        public static Color ToOpaqueSwatchColor(Color color) => Color.FromRgb(color.R, color.G, color.B);
    }
}
