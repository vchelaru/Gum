using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A collection that changes again while its CollectionChanged is still being raised leaves an indexed binding on
/// the item finally at the index, following that item's later PropertyChanged.
/// </summary>
public class IndexedPathReentrantCollectionTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    // No reentrancy block (unlike ObservableCollection), so a handler can change it during its own raise.
    private class ReentrantCollection : IList, INotifyCollectionChanged
    {
        private readonly List<object?> _inner = new();

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void InsertFront(object? item)
        {
            _inner.Insert(0, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public object? this[int index] { get => _inner[index]; set => _inner[index] = value; }
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public int Count => _inner.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        public int Add(object? value) { _inner.Add(value); return _inner.Count - 1; }
        public void Clear() => _inner.Clear();
        public bool Contains(object? value) => _inner.Contains(value);
        public void CopyTo(Array array, int index) => ((IList)_inner).CopyTo(array, index);
        public IEnumerator GetEnumerator() => _inner.GetEnumerator();
        public int IndexOf(object? value) => _inner.IndexOf(value);
        public void Insert(int index, object? value) => _inner.Insert(index, value);
        public void Remove(object? value) => _inner.Remove(value);
        public void RemoveAt(int index) => _inner.RemoveAt(index);
    }

    private class ListViewModel : ViewModel
    {
        public ReentrantCollection Items { get; } = new();
    }

    [Fact]
    public void ChangeRaisedBeforeBindingHandler_EndsOnFinalItem()
    {
        ListViewModel vm = new();
        vm.Items.Add(new ItemViewModel { Name = "a" });
        bool mutated = false;
        vm.Items.CollectionChanged += (_, _) =>
        {
            if (!mutated)
            {
                mutated = true;
                vm.Items.InsertFront(new ItemViewModel { Name = "nested" });
            }
        };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Items[0].Name");

        Should.NotThrow(() => vm.Items.InsertFront(new ItemViewModel { Name = "outer" }));
        label.Text.ShouldBe("nested");

        ((ItemViewModel)vm.Items[0]!).Name = "nested2";
        label.Text.ShouldBe("nested2");

        ((ItemViewModel)vm.Items[1]!).Name = "outer-should-not-show";
        label.Text.ShouldBe("nested2");
    }

    [Fact]
    public void ChangeRaisedAfterBindingHandler_EndsOnFinalItem()
    {
        ListViewModel vm = new();
        vm.Items.Add(new ItemViewModel { Name = "a" });
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Items[0].Name");
        int raises = 0;
        vm.Items.CollectionChanged += (_, _) =>
        {
            raises++;
            if (raises == 1)
            {
                vm.Items.InsertFront(new ItemViewModel { Name = "second" });
            }
        };

        Should.NotThrow(() => vm.Items.InsertFront(new ItemViewModel { Name = "first" }));
        label.Text.ShouldBe("second");

        ((ItemViewModel)vm.Items[0]!).Name = "second2";
        label.Text.ShouldBe("second2");
    }
}
