using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content
{
    /// <summary>
    /// Provides a simple implementation of IContentLoader for applications
    /// using the LoaderManager and not specifying their own custom ContentLoader.
    /// This content loader uses the default SystemManagers internally.
    /// </summary>
    public class ContentLoader : IContentLoader
    {
        public SystemManagers SystemManagers { get; set; }

        Dictionary<string, IDisposable> disposables = new Dictionary<string,IDisposable>();

        List<Atlas> atlases = new List<Atlas>();

        public T LoadContent<T>(string contentName)
        {
            if(typeof(T) == typeof(Texture2D))
            {
                var texture = LoaderManager.Self.Load(contentName, SystemManagers);
                return (T)(object)texture;
            }
            else if(typeof(T) == (typeof(AtlasedTexture)))
            {
                foreach(var atlas in atlases)
                {
                    if(atlas.Contains(contentName))
                    {
                        var asObject = (object)atlas.Get(contentName);
                        return (T)(asObject);
                    }
                }

                return default(T);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public T TryLoadContent<T>( string contentName)
        {
            bool knownType = false;
            try
            {
                if (typeof(T) == typeof(Texture2D))
                {
                    knownType = true;
                    var texture = LoaderManager.Self.Load(contentName, SystemManagers);
                    return (T)(object)texture;
                }
                else if (typeof(T) == (typeof(AtlasedTexture)))
                {
                    knownType = true;

                    foreach (var atlas in atlases)
                    {
                        if (atlas.Contains(contentName))
                        {
                            var asObject = (object)atlas.Get(contentName);
                            return (T)(asObject);
                        }
                    }

                    return default(T);
                }
            }
            catch
            { 
                return default( T );
            }

            if(!knownType)
            {
                throw new NotImplementedException();
            }
            else
            {
                return default(T);
            }
        }

        public void DisposeAndClear()
        {
            foreach(var item in disposables.Values)
            {
                item.Dispose();
            }

            disposables.Clear();
        }

        public T TryGetCachedDisposable<T>(string contentName)
        {
            if(disposables.ContainsKey(contentName))
            {
                return (T)disposables[contentName];
            }
            else
            {
                return default(T);
            }
        }

        public void AddDisposable(string contentName, IDisposable disposable)
        {
            if (disposables.ContainsKey(contentName))
            {
                throw new Exception("This item has already been added");
            }
            else
            {
                disposables.Add(contentName, disposable);
            }
        }

        public void AddAtlas(Atlas atlas)
        {
            atlases.Add(atlas);
        }

        
    }
}
