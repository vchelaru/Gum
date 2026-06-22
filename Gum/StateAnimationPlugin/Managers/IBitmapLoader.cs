using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.Managers
{
    /// <summary>
    /// Loads the plugin's embedded icon resources as WPF <see cref="BitmapFrame"/>s.
    /// Drained from the former <c>Singleton&lt;BitmapLoader&gt;</c> so the animation view
    /// models can take it by constructor and be substituted in tests.
    /// </summary>
    public interface IBitmapLoader
    {
        BitmapFrame LoadImage(string resourceName);
    }
}
