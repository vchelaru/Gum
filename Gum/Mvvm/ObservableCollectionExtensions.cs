using System.Collections.Generic;
using System.Linq;

namespace System.Collections.ObjectModel
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            foreach(var item in itemsToAdd)
            {
                collection.Add(item);
            }
        }

        public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> itemsToReplaceWith)
        {
            // see if any in the ObservableCollection aren't in itemsToReplace
            for(int i = collection.Count-1; i > -1; i--)
            {
                if(itemsToReplaceWith.Contains(collection[i]) == false)
                {
                    collection.RemoveAt(i);
                }
            }

            foreach(var item in itemsToReplaceWith)
            {
                if(!collection.Contains(item))
                {
                    collection.Add(item);
                }
            }
        }

        public static void RemoveAll<T>(this ObservableCollection<T> collection, Func<T, bool> removalPredicate)
        {
            for(var i = collection.Count-1;i > -1;i--)
            {
                if (removalPredicate(collection[i]))
                {
                    collection.RemoveAt(i);
                }
            }
        }
    }
}
