using RenderingLibrary.Content;
using SkiaSharp;
using SKSvg = Svg.Skia.SKSvg;

namespace SkiaGum.Content
{
    public class EmbeddedResourceContentLoader : IContentLoader
    {
        public T LoadContent<T>(string contentName)
        {
            if (typeof(T) == typeof(SKTypeface))
            {
                return (T)(object)LoadSKTypeface(contentName);
            }
            else if (typeof(T) == typeof(SKSvg))
            {
                return (T)(object)LoadSKSvg(contentName);
            }
            else if (typeof(T) == typeof(SKBitmap))
            {
                return (T)(object)LoadSKBitmap(contentName);
            }
            else

            {
                return default(T);
            }
        }

        private SKSvg LoadSKSvg(string contentName)
        {
            var modifiedContentName = SkiaResourceManager.AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetSvg(modifiedContentName);
        }

        private SKBitmap LoadSKBitmap(string contentName)
        {
            var modifiedContentName = SkiaResourceManager.AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetSKBitmap(modifiedContentName);
        }

        private SKTypeface LoadSKTypeface(string contentName)
        {
            var modifiedContentName = SkiaResourceManager.AdjustContentName?.Invoke(contentName) ?? contentName;

            return SkiaResourceManager.GetTypeface(modifiedContentName);
        }

        /// <inheritdoc/>
        public T TryLoadContent<T>(string contentName)
        {
            try
            {
                return LoadContent<T>(contentName);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
