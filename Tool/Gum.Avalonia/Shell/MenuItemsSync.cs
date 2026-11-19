using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Keeps a menu's item list in step with a model collection by applying each change where it
/// happened: an add or remove creates or drops only the controls of the items involved, and the
/// rest keep theirs. Plugins add entries one at a time while the tool starts, so rebuilding the
/// whole list on each change made the menu one of the slower parts of startup. A model item can
/// map to several controls (the menu bar shows a stepper as three items). Dropped controls are
/// detached from their models so they stop following them.
/// </summary>
internal sealed class MenuItemsSync : IDisposable
{
    private readonly IList _target;
    private readonly ObservableCollection<MenuItemModel> _source;
    private readonly Func<MenuItemModel, BuiltMenuItem> _create;
    private readonly List<BuiltMenuItem> _built = new List<BuiltMenuItem>();

    /// <summary>Fills <paramref name="target"/> from <paramref name="source"/> and follows its changes until disposed.</summary>
    public MenuItemsSync(IList target, ObservableCollection<MenuItemModel> source, Func<MenuItemModel, BuiltMenuItem> create)
    {
        _target = target;
        _source = source;
        _create = create;
        Rebuild();
        _source.CollectionChanged += HandleCollectionChanged;
    }

    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Insert(e.NewStartingIndex, e.NewItems!);
                break;
            case NotifyCollectionChangedAction.Remove:
                Remove(e.OldStartingIndex, e.OldItems!.Count);
                break;
            case NotifyCollectionChangedAction.Replace:
                Remove(e.OldStartingIndex, e.OldItems!.Count);
                Insert(e.NewStartingIndex, e.NewItems!);
                break;
            case NotifyCollectionChangedAction.Move:
                Move(e.OldStartingIndex, e.NewStartingIndex, e.OldItems!.Count);
                break;
            default:
                Rebuild();
                break;
        }
    }

    private void Rebuild()
    {
        DetachAll();
        _target.Clear();
        Insert(0, _source);
    }

    private void Insert(int index, IList models)
    {
        int targetIndex = TargetIndexOf(index);
        foreach (MenuItemModel model in models)
        {
            BuiltMenuItem built = _create(model);
            _built.Insert(index++, built);
            foreach (object control in built.Controls)
            {
                _target.Insert(targetIndex++, control);
            }
        }
    }

    private void Remove(int index, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Take(index).Detach?.Invoke();
        }
    }

    private void Move(int oldIndex, int newIndex, int count)
    {
        List<BuiltMenuItem> moved = new List<BuiltMenuItem>();
        for (int i = 0; i < count; i++)
        {
            moved.Add(Take(oldIndex));
        }
        int targetIndex = TargetIndexOf(newIndex);
        foreach (BuiltMenuItem built in moved)
        {
            _built.Insert(newIndex++, built);
            foreach (object control in built.Controls)
            {
                _target.Insert(targetIndex++, control);
            }
        }
    }

    /// <summary>Takes the item at <paramref name="index"/> out of the list, with its controls, without detaching it.</summary>
    private BuiltMenuItem Take(int index)
    {
        BuiltMenuItem built = _built[index];
        int targetIndex = TargetIndexOf(index);
        for (int i = 0; i < built.Controls.Count; i++)
        {
            _target.RemoveAt(targetIndex);
        }
        _built.RemoveAt(index);
        return built;
    }

    private int TargetIndexOf(int index)
    {
        int targetIndex = 0;
        for (int i = 0; i < index; i++)
        {
            targetIndex += _built[i].Controls.Count;
        }
        return targetIndex;
    }

    private void DetachAll()
    {
        foreach (BuiltMenuItem built in _built)
        {
            built.Detach?.Invoke();
        }
        _built.Clear();
    }

    /// <summary>Stops following the model and detaches every control built so far.</summary>
    public void Dispose()
    {
        _source.CollectionChanged -= HandleCollectionChanged;
        DetachAll();
    }
}

/// <summary>The controls built for one model item, and how to detach them from the model.</summary>
internal readonly record struct BuiltMenuItem(IReadOnlyList<object> Controls, Action? Detach);
