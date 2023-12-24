using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content
{
    public interface IContentLoader
    {
        T LoadContent<T>(string contentName);

        T TryLoadContent<T>(string contentName);
    }
}
