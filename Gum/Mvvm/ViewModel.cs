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

        // A value-typed property is stored in a mutable typed cell, so a changed value overwrites the
        // cell instead of allocating a new box. Cells are private to one instance (Clone copies
        // them) and never leave this class: every read that is not an exact ValueCell<T> match
        // unwraps to the boxed value, which is what the dictionary held before cells existed.
        // Like the rest of the property storage this is not thread safe.
        private abstract class ValueCell
        {
            public abstract object? Boxed { get; }
            public abstract ValueCell Copy();
        }

        private sealed class ValueCell<T> : ValueCell
        {
            public T Value = default!;
            public override object? Boxed => Value;
            public override ValueCell Copy() => new ValueCell<T> { Value = Value };
        }

        protected T Get<T>([CallerMemberName]string? propertyName = null)
        {
            T toReturn = default(T)!;

            if (propertyName != null && propertyDictionary.TryGetValue(propertyName, out object? storedValue))
            {
                if (typeof(T).IsValueType && storedValue is ValueCell<T> cell)
                {
                    return cell.Value;
                }

                if (storedValue is ValueCell other)
                {
                    storedValue = other.Boxed;
                }

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

        // One subscription per property, created the first time the property holds a collection and
        // reused afterwards, so the exact handler that was added is the one removed. A fresh
        // closure per Set never equals the one added earlier, so -= would silently do nothing.
        // Subscribed remembers the instance the handler is currently attached to, so setting the
        // same collection again does no event add or remove at all.
        // Null until a collection is set, so view models without collection properties pay nothing.
        private sealed class CollectionSubscription
        {
            public CollectionSubscription(NotifyCollectionChangedEventHandler handler)
            {
                Handler = handler;
            }

            public NotifyCollectionChangedEventHandler Handler { get; }

            /// <summary>The collection <see cref="Handler"/> is attached to, or null when none.</summary>
            public INotifyCollectionChanged? Subscribed { get; set; }
        }

        private Dictionary<string, CollectionSubscription>? collectionSubscriptions;

        private void SubscribeToCollection(INotifyCollectionChanged collection, string propertyName)
        {
            // Listen only to the instance the property actually holds. When Equals says an incoming
            // collection equals the stored one, the stored one is kept and the incoming one is not.
            if (ReferenceEquals(propertyDictionary[propertyName], collection) == false)
            {
                return;
            }

            collectionSubscriptions ??= new Dictionary<string, CollectionSubscription>();
            if (collectionSubscriptions.TryGetValue(propertyName, out CollectionSubscription? subscription) == false)
            {
                subscription = new CollectionSubscription(CreateCollectionHandler(propertyName));
                collectionSubscriptions[propertyName] = subscription;
            }

            // Already attached to this instance (it is only ever attached to the stored value).
            if (ReferenceEquals(subscription.Subscribed, collection))
            {
                return;
            }

            collection.CollectionChanged += subscription.Handler;
            subscription.Subscribed = collection;
        }

        private void UnsubscribeFromCollection(object? oldValue, string propertyName)
        {
            if (oldValue is INotifyCollectionChanged oldCollection &&
                collectionSubscriptions!.TryGetValue(propertyName, out CollectionSubscription? subscription))
            {
                oldCollection.CollectionChanged -= subscription.Handler;
                subscription.Subscribed = null;
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

            if (typeof(T).IsValueType)
            {
                return SetValueTyped(propertyValue, propertyName!);
            }

            if (propertyDictionary.TryGetValue(propertyName, out object? storedValue))
            {
                // A name that held a value type under another T (Set<int>, then Set<object>).
                if (storedValue is ValueCell cell)
                {
                    storedValue = cell.Boxed;
                }

                var storage = (T?)storedValue;
                if (EqualityComparer<T>.Default.Equals(storage, propertyValue) == false)
                {
                    didSet = true;
                    if (collectionSubscriptions != null)
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

        private bool SetValueTyped<T>(T propertyValue, string propertyName)
        {
            if (propertyDictionary.TryGetValue(propertyName, out object? storedValue))
            {
                if (storedValue is ValueCell<T> cell)
                {
                    if (EqualityComparer<T>.Default.Equals(cell.Value, propertyValue))
                    {
                        return false;
                    }

                    cell.Value = propertyValue;
                    return true;
                }

                // Stored by Set<object>, or under another cell type: same comparison and cast as before.
                if (storedValue is ValueCell other)
                {
                    storedValue = other.Boxed;
                }

                var storage = (T?)storedValue;
                if (EqualityComparer<T>.Default.Equals(storage, propertyValue))
                {
                    return false;
                }

                if (collectionSubscriptions != null)
                {
                    UnsubscribeFromCollection(storedValue, propertyName);
                }

                propertyDictionary[propertyName] = new ValueCell<T> { Value = propertyValue };
                return true;
            }

            propertyDictionary[propertyName] = new ValueCell<T> { Value = propertyValue };

            // Even though the user is setting a new value, we want to make sure it's
            // not the same:
            return EqualityComparer<T>.Default.Equals(default(T), propertyValue) == false;
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
            // Each cell is copied, otherwise a Set on the clone would overwrite the original's value.
            var copiedProperties = new Dictionary<string, object?>(this.propertyDictionary.Count);
            foreach (var pair in this.propertyDictionary)
            {
                copiedProperties[pair.Key] = pair.Value is ValueCell cell ? cell.Copy() : pair.Value;
            }
            asViewModel.propertyDictionary = copiedProperties;
            // MemberwiseClone copied the reference; the subscriptions belong to this instance, and a
            // clone replacing a collection must not unsubscribe the original.
            asViewModel.collectionSubscriptions = null;
            var asT = (T)asObject;

            return asT;
        }

    }
}
