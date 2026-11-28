using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Mvvm;
#endif

/// <summary>
/// Watches a dotted-path (e.g. "Foo.Bar.Baz") on any INotifyPropertyChanged root
/// and raises <see cref="ValueChanged"/> whenever the final value changes (or any
/// segment in between changes).
/// </summary>
public class PropertyPathObserver : IDisposable
{
    private readonly PathSegment[] _segments;
    private readonly List<WeakListener> _listeners = new();
    private readonly List<WeakCollectionListener> _collectionListeners = new();
    private object? _currentRoot;

    /// <summary>
    /// The root object the observer is currently attached to, if any.
    /// </summary>
    public object? CurrentRoot => _currentRoot;

    /// <summary>
    /// The resolved type of the value at the leaf of the path, if known.
    /// </summary>
    public Type? LeafType { get; private set; }

    /// <summary>
    /// True when <see cref="LeafType"/> has been resolved (i.e. the path could be walked).
    /// </summary>
    public bool HasResolution => LeafType is not null;

    /// <summary>
    /// True when the leaf resolved to a public field rather than a property. A field is read, never written.
    /// </summary>
    public bool IsLeafField { get; private set; }

    /// <summary>
    /// The parsed path segments. Exposed for callers that need to walk the path
    /// themselves (e.g. for two-way write-back).
    /// </summary>
    public PathSegment[] Segments => _segments;

    /// <summary> Raised whenever the value at the end of the path has changed. </summary>
    public event Action? ValueChanged;

    /// <summary>
    /// Creates an observer for the given dotted path. Call <see cref="Attach"/>
    /// with a root object to start observing.
    /// </summary>
    public PropertyPathObserver(string path)
    {
        _segments = PathSegmentParser.ParseSegments(path);
    }

    /// <summary>
    /// Detaches all listeners and clears the current root.
    /// </summary>
    public void Detach()
    {
        DetachAll();
        _currentRoot = null;
        LeafType = null;
        IsLeafField = false;
    }

    /// <summary>
    /// Start observing on a new root object (detaches from the old).
    /// </summary>
    [RequiresUnreferencedCode(
        "Walks the path on newRoot's own type, resolving each segment by name (GetProperty). Those " +
        "members may be removed under PublishTrimmed if nothing else in the app references them.")]
    public void Attach(object newRoot)
    {
        if (newRoot is null)
        {
            throw new ArgumentNullException(nameof(newRoot));
        }

        _currentRoot = newRoot;
        LeafType = newRoot.GetType();

        HookFrom(0, newRoot, ownerHooked: false);
    }

    /// <summary>
    /// Walks the path on the current root and returns the leaf value, or null if any
    /// intermediate is null. Returns null if no root is attached.
    /// </summary>
    [RequiresUnreferencedCode("Calls " + nameof(WalkSegments) + ".")]
    public object? GetCurrentValue()
    {
        if (_currentRoot is null)
        {
            return null;
        }
        return WalkSegments(_currentRoot, _segments, _segments.Length);
    }

    /// <summary>
    /// Walks the first <paramref name="count"/> segments starting from <paramref name="root"/>
    /// and returns the resulting object, or null if any intermediate is null.
    /// </summary>
    [RequiresUnreferencedCode(
        "Walks the path on root's own type, resolving each segment by name (GetProperty). Those " +
        "members may be removed under PublishTrimmed if nothing else in the app references them.")]
    public static object? WalkSegments(object? root, PathSegment[] segments, int count)
    {
        object? cursor = root;
        for (int i = 0; i < count && cursor != null; i++)
        {
            PropertyInfo? pi = cursor.GetType().GetProperty(segments[i].Name);
            cursor = GetSegmentValue(cursor, pi, segments[i]);

            if (segments[i].Index is int index && cursor != null)
            {
                cursor = GetIndexedValue(cursor, index);
            }
        }
        return cursor;
    }

    [RequiresUnreferencedCode(
        "Re-walks the path from the changed segment, resolving each by name (GetProperty) on the " +
        "runtime type. Those members may be removed under PublishTrimmed if nothing else in the app " +
        "references them.")]
    private void OnSegmentChanged(int level, string? propName)
    {
        // React when the changed property matches the segment. A null or empty name means every
        // property changed (the .NET convention), so the path is re-read from this level down.
        if (!string.IsNullOrEmpty(propName) && _segments[level].Name != propName)
        {
            return;
        }

        // The owner of this segment stays; everything it leads to, including the collection an indexed
        // segment holds, may have been replaced.
        DetachDeeperThan(level, keepCollectionAtLevel: false);
        HookFrom(level, WalkTo(level - 1), ownerHooked: true);
        OnValueChanged();
    }

    [RequiresUnreferencedCode(
        "Re-walks the path below the changed collection, resolving each segment by name (GetProperty) on " +
        "the runtime type. Those members may be removed under PublishTrimmed if nothing else in the app " +
        "references them.")]
    private void OnCollectionChanged(int level)
    {
        // The collection instance stays; the item under the index, and everything below it, may differ.
        DetachDeeperThan(level, keepCollectionAtLevel: true);
        HookFrom(level + 1, WalkTo(level), ownerHooked: false);
        OnValueChanged();
    }

    // Detaches listeners on objects reached through segment `level`, matched by the level each listener
    // records. List positions are not levels: a level whose object is not INotifyPropertyChanged adds no
    // listener. Plain loops, not RemoveAll with a capturing lambda, because this runs on every
    // PropertyChanged of a bound leaf and must not allocate.
    private void DetachDeeperThan(int level, bool keepCollectionAtLevel)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (_listeners[i].Level > level)
            {
                _listeners[i].Detach();
                _listeners.RemoveAt(i);
            }
        }

        int firstCollectionLevelToDetach = keepCollectionAtLevel ? level + 1 : level;
        for (int i = _collectionListeners.Count - 1; i >= 0; i--)
        {
            if (_collectionListeners[i].Level >= firstCollectionLevelToDetach)
            {
                _collectionListeners[i].Detach();
                _collectionListeners.RemoveAt(i);
            }
        }
    }

    // Walks from segment `start` to the leaf beginning on `owner` (the object holding segment `start`),
    // subscribing to each INotifyPropertyChanged on the way and to the collection of each indexed segment.
    // `ownerHooked` skips the listener on `owner` when it is already attached.
    [RequiresUnreferencedCode(
        "Walks the path on owner's runtime type, resolving each segment by name (GetProperty). Those " +
        "members may be removed under PublishTrimmed if nothing else in the app references them.")]
    private void HookFrom(int start, object? owner, bool ownerHooked)
    {
        object? cursor = owner;
        for (int i = start; i < _segments.Length; i++)
        {
            if (cursor is INotifyPropertyChanged inpc && !(ownerHooked && i == start))
            {
                _listeners.Add(new WeakListener(this, inpc, i));
            }

            PropertyInfo? pi = cursor?.GetType().GetProperty(_segments[i].Name);

            if (i == _segments.Length - 1)
            {
                LeafType = pi?.PropertyType ?? GetLeafFieldType(cursor, _segments[i]);
                IsLeafField = pi == null && LeafType != null;
                if (_segments[i].Index.HasValue && LeafType != null)
                {
                    LeafType = GetElementTypeForLeaf(LeafType);
                }
            }

            cursor = GetSegmentValue(cursor, pi, _segments[i]);

            // PERF: reacts to every change of the indexed collection, not only those that move an item
            // under the bound index. Filtering by NotifyCollectionChangedEventArgs is left out on purpose
            // to avoid index-relevance bugs.
            if (_segments[i].Index is int index && cursor != null)
            {
                if (cursor is INotifyCollectionChanged incc)
                {
                    _collectionListeners.Add(new WeakCollectionListener(this, incc, i));
                }

                cursor = GetIndexedValue(cursor, index);
            }
        }
    }

    [RequiresUnreferencedCode(
        "Walks the path from the root, resolving each segment by name (GetProperty) on the runtime " +
        "type. Those members may be removed under PublishTrimmed if nothing else in the app references " +
        "them.")]
    private object? WalkTo(int level)
    {
        object? cursor = _currentRoot;
        for (int i = 0; i <= level && cursor != null; i++)
        {
            cursor = GetSegmentValue(cursor, cursor.GetType().GetProperty(_segments[i].Name), _segments[i]);

            if (_segments[i].Index is int index && cursor != null)
            {
                cursor = GetIndexedValue(cursor, index);
            }
        }
        return cursor;
    }

    // A segment that is not a property may name a public instance field, which is read (a view model field holding
    // a struct, for "Position.X") and never written.
    [RequiresUnreferencedCode(
        "Looks up a field on cursor's own type by name (GetField). That member may be removed under " +
        "PublishTrimmed if nothing else in the app references it.")]
    private static object? GetSegmentValue(object? cursor, PropertyInfo? property, PathSegment segment) =>
        property != null
            ? property.GetValue(cursor)
            : cursor?.GetType().GetField(segment.Name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(cursor);

    // A leaf may name a public instance field, which is read once and never raises change notification.
    [RequiresUnreferencedCode(
        "Looks up a field on owner's own type by name (GetField). That member may be removed under " +
        "PublishTrimmed if nothing else in the app references it.")]
    private static Type? GetLeafFieldType(object? owner, PathSegment segment)
    {
        if (owner == null || segment.Index.HasValue)
        {
            return null;
        }
        return owner.GetType().GetField(segment.Name, BindingFlags.Public | BindingFlags.Instance)?.FieldType;
    }

    [RequiresUnreferencedCode(
        "Searches collectionType's own properties by reflection for an int indexer. That indexer may " +
        "be removed under PublishTrimmed if nothing else in the app references it.")]
    private static Type GetElementTypeForLeaf(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        foreach (PropertyInfo prop in collectionType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] indexParams = prop.GetIndexParameters();
            if (indexParams.Length == 1 && indexParams[0].ParameterType == typeof(int))
            {
                return prop.PropertyType;
            }
        }

        return typeof(object);
    }

    /// <summary>
    /// Returns the element at <paramref name="index"/> in <paramref name="collection"/>,
    /// or null if the index is out of bounds. Out-of-bounds is expected when a binding
    /// path references an index that doesn't yet exist (e.g. empty collection) or has
    /// been removed. The binding will re-evaluate when the collection changes.
    /// </summary>
    [RequiresUnreferencedCode(
        "Falls back to searching collection's own properties by reflection for an int indexer when " +
        "it isn't an array or IList. That indexer may be removed under PublishTrimmed if nothing else " +
        "in the app references it.")]
    private static object? GetIndexedValue(object collection, int index)
    {
        Type type = collection.GetType();
        if (type.IsArray)
        {
            Array arr = (Array)collection;
            if (index < 0 || index >= arr.Length)
            {
                return null;
            }
            return arr.GetValue(index);
        }

        if (collection is IList list)
        {
            if (index < 0 || index >= list.Count)
            {
                return null;
            }
            return list[index];
        }

        // Fallback: reflection-based indexer lookup — no bounds check possible here,
        // so let it throw if the indexer doesn't handle out-of-range gracefully.
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] indexParams = prop.GetIndexParameters();
            if (indexParams.Length == 1 && indexParams[0].ParameterType == typeof(int))
            {
                return prop.GetValue(collection, new object[] { index });
            }
        }

        throw new InvalidOperationException(
            $"Type '{type.Name}' does not have an integer indexer.");
    }

    private void OnValueChanged()
    {
        ValueChanged?.Invoke();
    }

    private void DetachAll()
    {
        foreach (WeakListener wl in _listeners)
        {
            wl.Detach();
        }
        _listeners.Clear();

        DetachAllCollectionListeners();
    }

    private void DetachAllCollectionListeners()
    {
        foreach (WeakCollectionListener cl in _collectionListeners)
        {
            cl.Detach();
        }
        _collectionListeners.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DetachAll();
    }

    // ──── Inner WeakListener (INotifyPropertyChanged) ────
    private class WeakListener
    {
        private readonly WeakReference<PropertyPathObserver> _weakObs;
        private readonly INotifyPropertyChanged _source;
        public int Level { get; }

        public WeakListener(PropertyPathObserver obs, INotifyPropertyChanged src, int level)
        {
            _weakObs = new WeakReference<PropertyPathObserver>(obs);
            _source = src;
            Level = level;
            _source.PropertyChanged += OnChanged;
        }

        public void Detach()
        {
            _source.PropertyChanged -= OnChanged;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Automatic relay for an already-attached path observer's INotifyPropertyChanged " +
                "subscription. The trim risk is already surfaced wherever Attach was actually called.")]
        private void OnChanged(object? _, PropertyChangedEventArgs e)
        {
            if (_weakObs.TryGetTarget(out PropertyPathObserver? obs))
            {
                obs.OnSegmentChanged(Level, e.PropertyName);
            }
            else
            {
                _source.PropertyChanged -= OnChanged;
            }
        }
    }

    /// <summary>
    /// Listens to <see cref="INotifyCollectionChanged"/> on a collection that an indexed
    /// binding path passes through. On any collection change, re-walks the path below the indexed
    /// segment so the binding follows the item now at the index, then re-evaluates.
    /// </summary>
    private class WeakCollectionListener
    {
        private readonly WeakReference<PropertyPathObserver> _weakObs;
        private readonly INotifyCollectionChanged _source;
        public int Level { get; }

        public WeakCollectionListener(PropertyPathObserver obs, INotifyCollectionChanged src, int level)
        {
            _weakObs = new WeakReference<PropertyPathObserver>(obs);
            _source = src;
            Level = level;
            _source.CollectionChanged += OnCollectionChanged;
        }

        public void Detach()
        {
            _source.CollectionChanged -= OnCollectionChanged;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Automatic relay for an already-attached path observer's INotifyCollectionChanged " +
                "subscription. The trim risk is already surfaced wherever Attach was actually called.")]
        private void OnCollectionChanged(object? _, NotifyCollectionChangedEventArgs e)
        {
            if (_weakObs.TryGetTarget(out PropertyPathObserver? obs))
            {
                obs.OnCollectionChanged(Level);
            }
            else
            {
                _source.CollectionChanged -= OnCollectionChanged;
            }
        }
    }
}
