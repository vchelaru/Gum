using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Content
{
    public interface IContentLoader
    {
        T LoadContent<T>(string contentName);
    }
}
