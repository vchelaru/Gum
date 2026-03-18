using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("MonoGameGum.Tests")]
[assembly: InternalsVisibleTo("MonoGameGum.Tests.V2")]

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

/// <summary>
/// Watches a dotted-path (e.g. "Foo.Bar.Baz") on any INotifyPropertyChanged root
/// and raises ValueChanged whenever the final value changes (or any segment in between changes).
/// </summary>
internal class PropertyPathObserver : IDisposable
{
    private readonly PathSegment[] _segments;
    private readonly List<WeakListener> _listeners = new();
    private readonly List<WeakCollectionListener> _collectionListeners = new();
    private object? _currentRoot;
    public object? CurrentRoot => _currentRoot;
    public Type? LeafType { get; private set; }
    public bool HasResolution => LeafType is not null;

    /// <summary> Raised whenever the value at the end of the path has changed. </summary>
    public event Action? ValueChanged;

    public PropertyPathObserver(string path)
    {
        _segments = BinderHelpers.ParseSegments(path);
    }

    public void Detach()
    {
        DetachAll();
        _currentRoot = null;
        LeafType = null;
    }

    /// <summary>
    /// Start observing on a new root object (detaches from the old).
    /// </summary>
    public void Attach(object newRoot)
    {
        _currentRoot = newRoot;
        LeafType = newRoot.GetType();

        // walk each segment, hooking listeners on each INotifyPropertyChanged along the way
        object? cursor = newRoot;
        for (int i = 0; i < _segments.Length; i++)
        {
            if (cursor is INotifyPropertyChanged inpc)
            {
                var wl = new WeakListener(this, inpc, i);
                _listeners.Add(wl);
            }
            // move next
            PropertyInfo? pi = cursor?.GetType().GetProperty(_segments[i].Name);

            if (i == _segments.Length - 1)
            {
                LeafType = pi?.PropertyType;
                if (_segments[i].Index.HasValue && LeafType != null)
                {
                    LeafType = GetElementTypeForLeaf(LeafType);
                }
            }

            cursor = pi?.GetValue(cursor);

            // For indexed segments, subscribe to collection changes before indexing into it.
            // PERF: Currently reacts to ALL collection changes, not just those affecting the
            // bound index. This is intentionally broad to avoid subtle bugs from incorrect
            // index-relevance logic. A future optimization could inspect
            // NotifyCollectionChangedEventArgs to skip changes that don't affect the bound
            // index (e.g. inserts/removes after the bound index position).
            if (_segments[i].Index.HasValue && cursor != null)
            {
                if (cursor is INotifyCollectionChanged incc)
                {
                    var cl = new WeakCollectionListener(this, incc);
                    _collectionListeners.Add(cl);
                }

                cursor = GetIndexedValue(cursor, _segments[i].Index.Value);
            }
        }
    }

    private void OnSegmentChanged(int level, string propName)
    {
        // only react if the changed property matches the segment
        if (_segments[level].Name != propName)
        {
            return;
        }

        // detach all listeners below this level
        for (int i = _listeners.Count - 1; i > level; i--)
        {
            _listeners[i].Detach();
        }

        _listeners.RemoveAll(x => x.Detached);

        // detach all collection listeners and re-subscribe during the re-walk
        DetachAllCollectionListeners();

        // re-walk from here forward
        object? cursor = WalkTo(level);
        for (int next = level + 1; next < _segments.Length; next++)
        {
            if (cursor is INotifyPropertyChanged inpc)
            {
                var wl = new WeakListener(this, inpc, next);
                _listeners.Add(wl);
            }
            PropertyInfo? pi = cursor?.GetType().GetProperty(_segments[next].Name);

            if (next == _segments.Length - 1)
            {
                LeafType = pi?.PropertyType;
                if (_segments[next].Index.HasValue && LeafType != null)
                {
                    LeafType = GetElementTypeForLeaf(LeafType);
                }
            }

            cursor = pi?.GetValue(cursor);

            if (_segments[next].Index.HasValue && cursor != null)
            {
                if (cursor is INotifyCollectionChanged incc)
                {
                    var cl = new WeakCollectionListener(this, incc);
                    _collectionListeners.Add(cl);
                }

                cursor = GetIndexedValue(cursor, _segments[next].Index.Value);
            }
        }

        OnValueChanged();
    }

    private object? WalkTo(int level)
    {
        object? cursor = _currentRoot;
        for (int i = 0; i <= level && cursor != null; i++)
        {
            cursor = cursor.GetType()
                           .GetProperty(_segments[i].Name)
                           ?.GetValue(cursor);

            if (_segments[i].Index.HasValue && cursor != null)
            {
                cursor = GetIndexedValue(cursor, _segments[i].Index.Value);
            }
        }
        return cursor;
    }

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
        foreach (var wl in _listeners)
        {
            wl.Detach();
        }
        _listeners.Clear();

        DetachAllCollectionListeners();
    }

    private void DetachAllCollectionListeners()
    {
        foreach (var cl in _collectionListeners)
        {
            cl.Detach();
        }
        _collectionListeners.Clear();
    }

    public void Dispose()
    {
        DetachAll();
    }

    // ──── Inner WeakListener (INotifyPropertyChanged) ────
    private class WeakListener
    {
        private readonly WeakReference<PropertyPathObserver> _weakObs;
        private readonly INotifyPropertyChanged _source;
        private readonly int _level;
        public bool Detached { get; private set; }

        public WeakListener(PropertyPathObserver obs, INotifyPropertyChanged src, int level)
        {
            _weakObs = new WeakReference<PropertyPathObserver>(obs);
            _source = src;
            _level = level;
            _source.PropertyChanged += OnChanged;
        }

        public void Detach()
        {
            _source.PropertyChanged -= OnChanged;
            Detached = true;
        }

        private void OnChanged(object? _, PropertyChangedEventArgs e)
        {
            if (_weakObs.TryGetTarget(out var obs))
            {
                obs.OnSegmentChanged(_level, e.PropertyName!);
            }
            else
            {
                _source.PropertyChanged -= OnChanged;
            }
        }
    }

    /// <summary>
    /// Listens to <see cref="INotifyCollectionChanged"/> on a collection that an indexed
    /// binding path passes through. On any collection change, fires
    /// <see cref="PropertyPathObserver.ValueChanged"/> so the binding re-evaluates.
    /// <para>
    /// <b>PERF note:</b> This currently reacts to ALL collection changes regardless of
    /// whether the specific bound index is affected. This is intentionally broad to avoid
    /// bugs from incorrect index-relevance logic. A future optimization could inspect
    /// <see cref="NotifyCollectionChangedEventArgs"/> to skip changes that cannot affect
    /// the bound index (e.g. inserts/removes at positions after the bound index).
    /// </para>
    /// </summary>
    private class WeakCollectionListener
    {
        private readonly WeakReference<PropertyPathObserver> _weakObs;
        private readonly INotifyCollectionChanged _source;

        public WeakCollectionListener(PropertyPathObserver obs, INotifyCollectionChanged src)
        {
            _weakObs = new WeakReference<PropertyPathObserver>(obs);
            _source = src;
            _source.CollectionChanged += OnCollectionChanged;
        }

        public void Detach()
        {
            _source.CollectionChanged -= OnCollectionChanged;
        }

        private void OnCollectionChanged(object? _, NotifyCollectionChangedEventArgs e)
        {
            if (_weakObs.TryGetTarget(out var obs))
            {
                obs.OnValueChanged();
            }
            else
            {
                _source.CollectionChanged -= OnCollectionChanged;
            }
        }
    }
}