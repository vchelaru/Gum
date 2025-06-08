using System;

namespace Gum.Managers
{
    public class Singleton<T> where T : new()
    {
        static T mSelf;

        public static T Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new T();
                }
                return mSelf;
            }
        }
    }
    
    public static class Singleton
    {
        public static T Guard<T>(T? current, T self) where T : class
            => current is null
                ? self
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is a singleton and cannot be instantiated more than once.");
    }
}
