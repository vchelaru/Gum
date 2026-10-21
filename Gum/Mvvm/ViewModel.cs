using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gum.Mvvm
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        public string ParentProperty { get; set; }

        public DependsOnAttribute(string parentPropertyName)
        {
            ParentProperty = parentPropertyName;
        }

    }

    /// <summary>
    /// Base class for view models, providing property storage, change notification, and
    /// <see cref="DependsOnAttribute"/> relationships.
    /// </summary>
    /// <remarks>
    /// The type annotation makes the trimmer keep every derived view model's properties. The
    /// constructor scans them for <see cref="DependsOnAttribute"/>, and Gum's string-path binding
    /// resolves bound members by name, so both would silently stop working if they were trimmed.
    /// </remarks>
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public class ViewModel : INotifyPropertyChanged
    {
        Dictionary<string, List<string>> notifyRelationships = new Dictionary<string, List<string>>();
        private Dictionary<string, object?> propertyDictionary = new Dictionary<string, object?>();

        public int GetPropertyChangeCount() => PropertyChanged?.GetInvocationList().Length ?? 0;

        protected T Get<T>([CallerMemberName]string? propertyName = null)
        {
            T toReturn = default(T)!;

            if (propertyName != null && propertyDictionary.TryGetValue(propertyName, out object? storedValue))
            {
                toReturn = (T)storedValue!;
            }

            return toReturn;
        }

        protected bool Set<T>(T propertyValue, [CallerMemberName]string? propertyName = null)
        {
            bool didSet = SetWithoutNotifying(propertyValue, propertyName);

            if (propertyName != null && propertyValue is INotifyCollectionChanged collection)
            {
                // Kept out of line so a non-collection Set does no extra work.
                SubscribeToCollection(collection, propertyName);
            }

            if (didSet)
            {
                NotifyPropertyChanged(propertyName);
            }

            return didSet;
        }

        // One handler per property, created the first time the property holds a collection and
        // reused afterwards, so the exact delegate that was added is the one removed. A fresh
        // closure per Set never equals the one added earlier, so -= would silently do nothing.
        // Null until a collection is set, so view models without collection properties pay nothing.
        private Dictionary<string, NotifyCollectionChangedEventHandler>? collectionHandlers;

        private void SubscribeToCollection(INotifyCollectionChanged collection, string propertyName)
        {
            collectionHandlers ??= new Dictionary<string, NotifyCollectionChangedEventHandler>();
            if (collectionHandlers.TryGetValue(propertyName, out NotifyCollectionChangedEventHandler? handler) == false)
            {
                handler = CreateCollectionHandler(propertyName);
                collectionHandlers[propertyName] = handler;
            }

            // Remove first so setting the same collection again does not subscribe twice.
            collection.CollectionChanged -= handler;
            collection.CollectionChanged += handler;
        }

        private void UnsubscribeFromCollection(object? oldValue, string propertyName)
        {
            if (oldValue is INotifyCollectionChanged oldCollection &&
                collectionHandlers!.TryGetValue(propertyName, out NotifyCollectionChangedEventHandler? handler))
            {
                oldCollection.CollectionChanged -= handler;
            }
        }

        private NotifyCollectionChangedEventHandler CreateCollectionHandler(string propertyName)
        {
            // Reset arrives with null OldItems/NewItems even from Clear(), so every action notifies.
            return (_, _) => NotifyPropertyChanged(propertyName);
        }

        protected bool SetWithoutNotifying<T>(T propertyValue, [CallerMemberName]string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            var didSet = false;

            if (propertyDictionary.TryGetValue(propertyName, out object? storedValue))
            {
                var storage = (T?)storedValue;
                if (EqualityComparer<T>.Default.Equals(storage, propertyValue) == false)
                {
                    didSet = true;
                    if (collectionHandlers != null)
                    {
                        UnsubscribeFromCollection(storedValue, propertyName);
                    }
                    propertyDictionary[propertyName] = propertyValue;
                }
            }
            else
            {
                propertyDictionary[propertyName] = propertyValue;

                // Even though the user is setting a new value, we want to make sure it's
                // not the same:
                var defaultValue = default(T);
                var isSettingDefault =
                    EqualityComparer<T>.Default.Equals(defaultValue, propertyValue);

                didSet = isSettingDefault == false;
            }

            return didSet;
        }


        public ViewModel()
        {
            var derivedType = this.GetType();

            var properties = derivedType.GetRuntimeProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                string child = property.Name;
                foreach (var uncastedAttribute in attributes)
                {
                    if (uncastedAttribute is DependsOnAttribute attribute)
                    {

                        string parent = attribute.ParentProperty;

                        List<string> childrenProps;
                        if (notifyRelationships.ContainsKey(parent) == false)
                        {
                            childrenProps = new List<string>();
                            notifyRelationships[parent] = childrenProps;
                        }
                        else
                        {
                            childrenProps = notifyRelationships[parent];
                        }

                        childrenProps.Add(child);
                    }
                }
            }

        }
        protected void ChangeAndNotify<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            if (EqualityComparer<T>.Default.Equals(property, value) == false)
            {
                property = value;
                NotifyPropertyChanged(propertyName);
            }
        }

        // PropertyChangedEventArgs is immutable, so one instance per property name is reused across
        // all notifications and all ViewModel instances instead of allocating one per notification.
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _eventArgsCache = new();

        protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            if (PropertyChanged != null)
            {
                PropertyChanged(this, _eventArgsCache.GetOrAdd(propertyName, static name => new PropertyChangedEventArgs(name)));
            }

            if (notifyRelationships.TryGetValue(propertyName, out var childPropertyNames))
            {
                foreach (var childPropertyName in childPropertyNames)
                {
                    // todo - worry about recursive notifications?
                    NotifyPropertyChanged(childPropertyName);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected T Clone<T>()
        {
            var asObject = this.MemberwiseClone();
            var asViewModel = (ViewModel)asObject;
            asViewModel.propertyDictionary = new Dictionary<string, object?>(this.propertyDictionary);
            // MemberwiseClone copied the reference; the handlers belong to this instance, and a
            // clone replacing a collection must not unsubscribe the original.
            asViewModel.collectionHandlers = null;
            var asT = (T)asObject;

            return asT;
        }

    }
}
