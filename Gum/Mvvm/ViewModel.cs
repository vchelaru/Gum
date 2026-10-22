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

        // Whether a value stored through Set<T> can be an INotifyCollectionChanged, decided once per T.
        // False only when that is impossible: T is sealed (or a value type) and does not implement the
        // interface, so no runtime type can add it. A non-sealed T (object, an interface, an unsealed
        // class) is always true because a derived runtime type may implement it. A Nullable<S> boxes to S,
        // so it is judged by S.
        private static class CanBeCollection<T>
        {
            public static readonly bool Value = Compute();

            private static bool Compute()
            {
                Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return typeof(INotifyCollectionChanged).IsAssignableFrom(type) || type.IsSealed == false;
            }
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to a backing field and raises <see cref="PropertyChanged"/> when it
        /// changed. The value lives in the caller's field, so reading it is a plain field read.
        /// </summary>
        /// <remarks>
        /// <c>float _score; public float Score { get => _score; set => Set(ref _score, value); }</c>
        /// Unlike <see cref="Set{T}(T, string)"/>, the field's current value is the starting point, so a field
        /// initializer counts as already set and assigning that same value raises nothing.
        /// A collection value is watched like it is for <see cref="Set{T}(T, string)"/>.
        /// </remarks>
        /// <returns>True when the value changed.</returns>
        protected bool Set<T>(ref T field, T value, [CallerMemberName]string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            return SetField(ref field, value, propertyName!, notify: true, trackCollections: true);
        }

        /// <summary>
        /// Assigns a backing field like <see cref="Set{T}(ref T, T, string)"/> but raises no events. A collection
        /// value is still watched.
        /// </summary>
        /// <returns>True when the value changed.</returns>
        protected bool SetWithoutNotifying<T>(ref T field, T value, [CallerMemberName]string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            return SetField(ref field, value, propertyName!, notify: false, trackCollections: true);
        }

        private bool SetField<T>(ref T field, T value, string propertyName, bool notify, bool trackCollections)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                // Like the by-name style, a collection that is already stored still gets its subscription.
                if (trackCollections && CanBeCollection<T>.Value && field is INotifyCollectionChanged stored)
                {
                    SubscribeToCollectionCore(stored, propertyName);
                }

                return false;
            }

            // The old value is still in the field here, so its subscription can be removed before assignment.
            if (trackCollections && collectionSubscriptions != null && !typeof(T).IsValueType)
            {
                UnsubscribeFromCollection(field, propertyName);
            }

            field = value;

            if (trackCollections && CanBeCollection<T>.Value && value is INotifyCollectionChanged collection)
            {
                SubscribeToCollectionCore(collection, propertyName);
            }

            if (notify)
            {
                NotifyPropertyChanged(propertyName);
            }

            return true;
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

            if (propertyName != null && CanBeCollection<T>.Value && propertyValue is INotifyCollectionChanged collection)
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

            SubscribeToCollectionCore(collection, propertyName);
        }

        private void SubscribeToCollectionCore(INotifyCollectionChanged collection, string propertyName)
        {
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
            // The same as Set(ref ...) except that a collection's contents are not watched.
            SetField(ref property, value, propertyName!, notify: true, trackCollections: false);
        }

        // PropertyChangedEventArgs is immutable, so one instance per property name is reused across
        // all notifications and all ViewModel instances instead of allocating one per notification.
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _eventArgsCache = new();

        // The same cache keyed on the string instance, so a hit hashes no characters. Property names normally
        // come from [CallerMemberName] or nameof, which are interned literals, so one instance per name hits
        // every time. Only interned instances are added, which bounds the table by the number of literals in
        // the program; a name built at run time takes the by-value cache above.
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _eventArgsByReference =
            new(ReferenceStringComparer.Instance);

        private sealed class ReferenceStringComparer : IEqualityComparer<string>
        {
            public static readonly ReferenceStringComparer Instance = new();

            public bool Equals(string? x, string? y) => ReferenceEquals(x, y);

            public int GetHashCode(string value) => RuntimeHelpers.GetHashCode(value);
        }

        private static PropertyChangedEventArgs GetEventArgs(string propertyName)
        {
            return _eventArgsByReference.TryGetValue(propertyName, out PropertyChangedEventArgs? args)
                ? args
                : GetEventArgsByValue(propertyName);
        }

        private static PropertyChangedEventArgs GetEventArgsByValue(string propertyName)
        {
            PropertyChangedEventArgs args =
                _eventArgsCache.GetOrAdd(propertyName, static name => new PropertyChangedEventArgs(name));
            if (ReferenceEquals(string.IsInterned(propertyName), propertyName))
            {
                _eventArgsByReference.TryAdd(propertyName, args);
            }

            return args;
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            System.Diagnostics.Debug.Assert(propertyName != null, "propertyName should not be null");
            if (PropertyChanged != null)
            {
                PropertyChanged(this, GetEventArgs(propertyName));
            }

            // Most view models declare no [DependsOn]; an empty dictionary skips the lookup call. The
            // dictionary is filled only by the constructor, so this is exact for derived and generic types.
            if (notifyRelationships.Count != 0 && notifyRelationships.TryGetValue(propertyName, out var childPropertyNames))
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
