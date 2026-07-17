using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace StateAnimationPlugin.Managers
{
    public class BitmapLoader : IBitmapLoader
    {
        // Caches each resource's raw bytes by resource name. This preserves the "read once,
        // share the bytes" behavior the icons used to get from AnimatedKeyframeViewModel's
        // static fields, now that every view model loads its icons via this instance.
        private readonly Dictionary<string, byte[]> _cache = new();

        public byte[] LoadImage(string resourceName)
        {
            if (_cache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            Assembly thisassembly = Assembly.GetExecutingAssembly();

            string fullName = "StateAnimationPlugin.Resources." + resourceName;
            using (var imageStream = thisassembly.GetManifestResourceStream(fullName))
            using (var memoryStream = new MemoryStream())
            {
                imageStream!.CopyTo(memoryStream);
                byte[] bytes = memoryStream.ToArray();
                _cache[resourceName] = bytes;
                return bytes;
            }
        }
    }
}
