using System.IO;
using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.Managers
{
    /// <summary>
    /// Decodes the raw encoded image bytes returned by <see cref="IBitmapLoader"/> into a WPF
    /// <see cref="BitmapFrame"/>. Kept separate from <see cref="IBitmapLoader"/> so that interface
    /// stays free of WPF types (issue #3225) while its WPF-based consumers still get a frame.
    /// </summary>
    internal static class BitmapFrameFactory
    {
        public static BitmapFrame? Create(byte[]? imageBytes) =>
            imageBytes is null || imageBytes.Length == 0 ? null : BitmapFrame.Create(new MemoryStream(imageBytes));
    }
}
