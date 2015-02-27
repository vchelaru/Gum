using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Content
{
    public interface IContentLoader
    {

        T TryGetCachedDisposable<T>(string contentName);

        void AddDisposable(string contentName, IDisposable disposable);


        T LoadContent<T>(string contentName);
    }
}
