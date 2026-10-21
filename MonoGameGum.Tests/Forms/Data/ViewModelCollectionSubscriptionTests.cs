using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.ObjectModel;
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
}
