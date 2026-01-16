using RenderingLibrary.Content;
using SkiaSharp;
using System;
using SKSvg = Svg.Skia.SKSvg;

namespace SkiaGum.Content
{
    public class EmbeddedResourceContentLoader : IContentLoader
    {
        
        public void AddDisposable(string contentName, IDisposable disposable)
        {
            throw new NotImplementedException();
        }

        public T LoadContent<T>(string contentName)
        {
            var name = SkiaResourceManager.AdjustContentName?.Invoke(contentName) ?? contentName;
            return typeof(T) switch
            {
                Type t when t == typeof(SKTypeface) => (T)(object)SkiaResourceManager.GetTypeface(name),
                Type t when t == typeof(SKSvg) => (T)(object)SkiaResourceManager.GetSvg(name),
                Type t when t == typeof(SKBitmap) => (T)(object)SkiaResourceManager.GetSKBitmap(name),
                _ => throw new NotImplementedException($"Type {typeof(T).Name} is not implemented by LoadContent yet.")
            };
        }

        public T TryGetCachedDisposable<T>(string contentName)
        {
            throw new NotImplementedException();
        }

        public T TryLoadContent<T>(string contentName)
        {
            throw new NotImplementedException();
        }
    }
}
