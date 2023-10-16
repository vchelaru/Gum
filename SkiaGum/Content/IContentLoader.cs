using System;

namespace RenderingLibrary.Content
{
    public interface IContentLoader
    {

        T TryGetCachedDisposable<T>(string contentName);

        void AddDisposable(string contentName, IDisposable disposable);


        T LoadContent<T>(string contentName);

        T TryLoadContent<T>(string contentName);

    }
}
