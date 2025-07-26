using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("MonoGameGum.Tests")]

#if FRB
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
namespace Gum.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

/// <summary>
/// Watches a dotted-path (e.g. "Foo.Bar.Baz") on any INotifyPropertyChanged root
/// and raises ValueChanged whenever the final value changes (or any segment in between changes).
/// </summary>
internal class PropertyPathObserver : IDisposable
{
    private readonly string[] _segments;
    private readonly List<WeakListener> _listeners = new();
    private object? _currentRoot;
    public object? CurrentRoot => _currentRoot;
    public Type? LeafType { get; private set; }
    public bool HasResolution => LeafType is not null;

    /// <summary> Raised whenever the value at the end of the path has changed. </summary>
    public event Action? ValueChanged;

    public PropertyPathObserver(string path)
    {
        _segments = path.Split(["."], StringSplitOptions.RemoveEmptyEntries);
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
            var pi = cursor?.GetType().GetProperty(_segments[i]);

            if (i == _segments.Length - 1)
            {
                LeafType = pi?.PropertyType;
            }
            cursor = pi?.GetValue(cursor);
        }
    }

    private void OnSegmentChanged(int level, string propName)
    {
        // only react if the changed property matches the segment
        if (_segments[level] != propName) return;

        // detach all listeners below this level
        for (int i = _listeners.Count - 1; i > level; i--)
            _listeners[i].Detach();

        _listeners.RemoveAll(x => x.Detached);

        // re-walk from here forward
        object? cursor = WalkTo(level);
        for (int next = level+1; next < _segments.Length; next++)
        {
            if (cursor is INotifyPropertyChanged inpc)
            {
                var wl = new WeakListener(this, inpc, next);
                _listeners.Add(wl);
            }
            PropertyInfo? pi = cursor?.GetType().GetProperty(_segments[next]);

            if (next == _segments.Length - 1)
            {
                LeafType = pi?.PropertyType;
            }

            cursor = pi?.GetValue(cursor);
        }

        OnValueChanged();
    }

    private object? WalkTo(int level)
    {
        object? cursor = _currentRoot;
        for (int i = 0; i <= level && cursor != null; i++)
            cursor = cursor.GetType()
                           .GetProperty(_segments[i])
                           ?.GetValue(cursor);
        return cursor;
    }

    private void OnValueChanged()
    {
        ValueChanged?.Invoke();
    }

    private void DetachAll()
    {
        foreach (var wl in _listeners) wl.Detach();
        _listeners.Clear();
    }

    public void Dispose()
    {
        DetachAll();
    }

    // ──── Inner WeakListener ────
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
                obs.OnSegmentChanged(_level, e.PropertyName!);
            else
                _source.PropertyChanged -= OnChanged;
        }
    }
}