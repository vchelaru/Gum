using System;
using System.Collections.Generic;
using System.Linq;

namespace RenderingLibrary.Content;

public class LoaderManager
{
    public enum ExistingContentBehavior
    {
        ThrowException,
        Replace
    }

    #region Fields

    static LoaderManager mSelf;


    #endregion

    #region Properties

    public static LoaderManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new LoaderManager();
            }
            return mSelf;
        }
    }

    public IContentLoader ContentLoader
    {
        get;
        set;
    }

    // February 6, 2024
    // Why is this false?
    // Caching should be turned
    // on by default...
    //bool mCacheTextures = false;
    bool mCacheTextures = true;
    Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);
    public bool CacheTextures
    {
        get { return mCacheTextures; }
        set
        {
            mCacheTextures = value;

            if (!mCacheTextures)
            {
                foreach (KeyValuePair<string, IDisposable> kvp in mCachedDisposables)
                {
                    kvp.Value.Dispose();
                }

                mCachedDisposables.Clear();

            }
        }
    }

    #endregion


    public T LoadContent<T>(string contentName)
    {
#if DEBUG
        if (this.ContentLoader == null)
        {
            throw new Exception("The content loader is null - you must set it prior to calling LoadContent.");
        }
#endif

        return ContentLoader.LoadContent<T>(contentName);
    }

    public T TryLoadContent<T>(string contentName)
    {

#if DEBUG
        if (this.ContentLoader == null)
        {
            throw new Exception("The content loader is null - you must set it prior to calling LoadContent.");
        }
#endif
        return ContentLoader.TryLoadContent<T>(contentName);
    }

    public IDisposable? GetDisposable(string name)
    {
        if (mCachedDisposables.ContainsKey(name))
        {
            return mCachedDisposables[name];
        }
        else
        {
            return null;
        }
    }

    public void AddDisposable(string name, IDisposable disposable, ExistingContentBehavior existingContentBehavior = ExistingContentBehavior.ThrowException)
    {
#if DEBUG
        if(mCachedDisposables.ContainsKey(name) && existingContentBehavior == ExistingContentBehavior.ThrowException)
        {
            throw new ArgumentException(
                $"The cached disposable already contains an entry for {name}:{mCachedDisposables[name]}");
        }
#endif
        if(existingContentBehavior == ExistingContentBehavior.ThrowException)
        {
            mCachedDisposables.Add(name, disposable);
        }
        else
        {
            mCachedDisposables[name] = disposable;
        }
    }

    public void DisposeAndClear()
    {
        foreach (var item in mCachedDisposables.Values)
        {
            item.Dispose();
        }

        mCachedDisposables.Clear();
    }

    public void Dispose(string name)
    {
        if (mCachedDisposables.ContainsKey(name))
        {
            mCachedDisposables[name].Dispose();
            mCachedDisposables.Remove(name);
        }
    }

    public void RemoveWithoutDisposing(IDisposable disposable)
    {
        var kvp = mCachedDisposables.FirstOrDefault(item => item.Value == disposable);

        if(kvp.Value == disposable)
        {
            mCachedDisposables.Remove(kvp.Key);
        }
    }
}
