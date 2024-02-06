using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content
{
    public class LoaderManager
    {
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
        Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>();
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

        public IDisposable GetDisposable(string name)
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

        public void AddDisposable(string name, IDisposable disposable)
        {
            mCachedDisposables.Add(name, disposable);
        }

        public void Dispose(string name)
        {
            if (mCachedDisposables.ContainsKey(name))
            {
                mCachedDisposables[name].Dispose();
                mCachedDisposables.Remove(name);
            }
        }

    }
}
