using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// <see cref="ViewModel"/> must stop listening to a collection once it is no longer the
/// property's value (issue #6364).
/// </summary>
public class ViewModelCollectionSubscriptionTests : BaseTestClass
{
    private class CollectionViewModel : ViewModel
    {
        public ObservableCollection<int>? Items
        {
            get => Get<ObservableCollection<int>?>();
            set => Set(value);
        }

        public ObservableCollection<int>? Other
        {
            get => Get<ObservableCollection<int>?>();
            set => Set(value);
        }

        public ObservableCollection<int>? Quiet
        {
            get => Get<ObservableCollection<int>?>();
            set => SetWithoutNotifying(value);
        }

        public CollectionViewModel CloneIt() => Clone<CollectionViewModel>();
    }

    /// <summary>A collection whose Equals compares contents, like a record or immutable list would.</summary>
    private class ValueEqualCollection : INotifyCollectionChanged
    {
        private readonly List<int> _items = new List<int>();

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public ValueEqualCollection(params int[] items) => _items.AddRange(items);

        public void Add(int item)
        {
            _items.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public override bool Equals(object? obj) =>
            obj is ValueEqualCollection other && _items.SequenceEqual(other._items);

        public override int GetHashCode() => _items.Count;
    }

    private class ValueEqualViewModel : ViewModel
    {
        public ValueEqualCollection? Items
        {
            get => Get<ValueEqualCollection?>();
            set => Set(value);
        }
    }

    private static List<string?> Record(ViewModel vm)
    {
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);
        return raised;
    }

    [Fact]
    public void Set_ReplacedCollection_NoLongerRaises()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        ObservableCollection<int> b = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a };
        vm.Items = b;
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_ReplacedCollection_NewCollectionStillRaises()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        ObservableCollection<int> b = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a };
        vm.Items = b;
        List<string?> raised = Record(vm);

        b.Add(1);

        raised.ShouldBe(new string?[] { nameof(CollectionViewModel.Items) });
    }

    [Fact]
    public void Set_Null_UnsubscribesOldCollection()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a };
        vm.Items = null;
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_NullThenCollection_Subscribes()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a };
        vm.Items = null;
        vm.Items = a;
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.ShouldBe(new string?[] { nameof(CollectionViewModel.Items) });
    }

    [Fact]
    public void Set_SameCollectionAgain_DoesNotDoubleSubscribe()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a };
        vm.Items = a;
        vm.Items = a;
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.ShouldBe(new string?[] { nameof(CollectionViewModel.Items) });
    }

    [Fact]
    public void Set_SameCollectionOnTwoProperties_EachRaisesOnce()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a, Other = a };
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.Count.ShouldBe(2);
        raised.ShouldContain(nameof(CollectionViewModel.Items));
        raised.ShouldContain(nameof(CollectionViewModel.Other));
    }

    [Fact]
    public void Set_ReplacingOneProperty_LeavesOtherPropertySubscribedToSameCollection()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Items = a, Other = a };
        vm.Items = new ObservableCollection<int>();
        List<string?> raised = Record(vm);

        a.Add(1);

        raised.ShouldBe(new string?[] { nameof(CollectionViewModel.Other) });
    }

    [Fact]
    public void Clone_ReplacingCollectionOnClone_DoesNotUnsubscribeOriginal()
    {
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel original = new CollectionViewModel { Items = a };
        CollectionViewModel clone = original.CloneIt();
        clone.Items = new ObservableCollection<int>();
        List<string?> raised = Record(original);

        a.Add(1);

        raised.ShouldBe(new string?[] { nameof(CollectionViewModel.Items) });
    }

    [Fact]
    public void Clone_ReplacingCollectionOnOriginal_ClonesOwnSubscriptionIsUnaffected()
    {
        // Pins existing behavior: a clone shares the collection but notifies through the
        // original's subscription, so the clone itself is not notified of collection changes.
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel original = new CollectionViewModel { Items = a };
        CollectionViewModel clone = original.CloneIt();
        List<string?> raisedByClone = Record(clone);

        a.Add(1);

        raisedByClone.ShouldBeEmpty();
    }

    [Fact]
    public void SetWithoutNotifying_ReplacingCollection_LeavesSubscriptionUntouched()
    {
        // SetWithoutNotifying never subscribed and never unsubscribed; pin that.
        ObservableCollection<int> a = new ObservableCollection<int>();
        ObservableCollection<int> b = new ObservableCollection<int>();
        CollectionViewModel vm = new CollectionViewModel { Quiet = a };
        vm.Quiet = b;
        List<string?> raised = Record(vm);

        a.Add(1);
        b.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_EqualButDifferentCollection_DoesNotSubscribeIncoming()
    {
        // Equals says equal, so the stored instance is kept and the incoming one is never stored.
        ValueEqualCollection a = new ValueEqualCollection(1);
        ValueEqualCollection b = new ValueEqualCollection(1);
        ValueEqualViewModel vm = new ValueEqualViewModel { Items = a };
        vm.Items = b;
        vm.Items.ShouldBeSameAs(a);
        List<string?> raised = Record(vm);

        b.Add(2);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_EqualButDifferentCollection_StoredCollectionStillRaises()
    {
        ValueEqualCollection a = new ValueEqualCollection(1);
        ValueEqualCollection b = new ValueEqualCollection(1);
        ValueEqualViewModel vm = new ValueEqualViewModel { Items = a };
        vm.Items = b;
        List<string?> raised = Record(vm);

        a.Add(2);

        raised.ShouldBe(new string?[] { nameof(ValueEqualViewModel.Items) });
    }

    [Fact]
    public void Set_NotEqualCollection_ReplacesAndSubscribesIncoming()
    {
        ValueEqualCollection a = new ValueEqualCollection(1);
        ValueEqualCollection b = new ValueEqualCollection(2);
        ValueEqualViewModel vm = new ValueEqualViewModel { Items = a };
        vm.Items = b;
        List<string?> raised = Record(vm);

        a.Add(3);
        b.Add(3);

        raised.ShouldBe(new string?[] { nameof(ValueEqualViewModel.Items) });
    }

    [Fact]
    public void Set_SameCollectionOnClone_Subscribes()
    {
        // Pins existing behavior: the clone shares the stored instance but has not subscribed
        // to it, and setting that same instance subscribes the clone.
        ObservableCollection<int> a = new ObservableCollection<int>();
        CollectionViewModel original = new CollectionViewModel { Items = a };
        CollectionViewModel clone = original.CloneIt();
        clone.Items = a;
        List<string?> raisedByClone = Record(clone);

        a.Add(1);

        raisedByClone.ShouldBe(new string?[] { nameof(CollectionViewModel.Items) });
    }
}
