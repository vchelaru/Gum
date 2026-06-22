using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.Managers
{
    public class BitmapLoader : IBitmapLoader
    {
        // Caches each decoded frame by resource name. This preserves the "decode once,
        // share the frame" behavior the icons used to get from AnimatedKeyframeViewModel's
        // static fields, now that every view model loads its icons via this instance.
        private readonly Dictionary<string, BitmapFrame> _cache = new();

        public BitmapFrame LoadImage(string resourceName)
        {
            if (_cache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            Assembly thisassembly = Assembly.GetExecutingAssembly();

            string fullName = "StateAnimationPlugin.Resources." + resourceName;
            using (var imageStream =
                thisassembly.GetManifestResourceStream(fullName))
            {
                var frame = BitmapFrame.Create(imageStream);
                _cache[resourceName] = frame;
                return frame;
            }
        }
    }
}
