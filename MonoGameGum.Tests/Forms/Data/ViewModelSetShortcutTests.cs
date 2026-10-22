using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// Pins for the by-name Set shortcuts (issue #6396): the empty <see cref="DependsOnAttribute"/> lookup and
/// the collection interface test are skipped when they cannot apply, and nothing observable changes.
/// </summary>
public class ViewModelSetShortcutTests : BaseTestClass
{
    private class NoDependsOnViewModel : ViewModel
    {
        public int Value { get => Get<int>(); set => Set(value); }
    }

    private class BaseViewModel : ViewModel
    {
        public int Source { get => Get<int>(); set => Set(value); }
    }

    private class DerivedAddsDependsOnViewModel : BaseViewModel
    {
        [DependsOn(nameof(Source))]
        public int Doubled => Source * 2;
    }

    private class BaseWithDependentViewModel : ViewModel
    {
        public int Source { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(Source))]
        public int Doubled => Source * 2;
    }

    private class DerivedFromDependentViewModel : BaseWithDependentViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }

        [DependsOn(nameof(Name))]
        public string? Shout => Name?.ToUpperInvariant();
    }

    private class GenericViewModel<T> : ViewModel
    {
        public T? Value { get => Get<T?>(); set => Set(value); }

        [DependsOn(nameof(Value))]
        public string Label => $"{Value}";
    }

    private class GenericNoDependsOnViewModel<T> : ViewModel
    {
        public T? Value { get => Get<T?>(); set => Set(value); }
    }

    private class CollectionBase
    {
    }

    private class CollectionDerived : CollectionBase, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Raise() =>
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private sealed class SealedCollection : INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Raise() =>
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private sealed class SealedPlain
    {
    }

    private class TypedCollectionsViewModel : ViewModel
    {
        public object? AsObject { get => Get<object?>(); set => Set(value); }
        public CollectionBase? AsUnsealedBase { get => Get<CollectionBase?>(); set => Set(value); }
        public INotifyCollectionChanged? AsInterface { get => Get<INotifyCollectionChanged?>(); set => Set(value); }
        public SealedCollection? AsSealed { get => Get<SealedCollection?>(); set => Set(value); }
        public SealedPlain? Plain { get => Get<SealedPlain?>(); set => Set(value); }
        public string? Text { get => Get<string?>(); set => Set(value); }
    }

    private static List<string?> Record(ViewModel vm)
    {
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);
        return raised;
    }

    [Fact]
    public void Set_ClassWithNoDependsOn_RaisesOnlyOwnProperty()
    {
        NoDependsOnViewModel vm = new NoDependsOnViewModel();
        List<string?> raised = Record(vm);

        vm.Value = 3;
        vm.Value = 3;
        vm.Value = 4;

        raised.ShouldBe(new[] { "Value", "Value" });
    }

    [Fact]
    public void Set_DerivedAddingDependsOnOverBaseWithNone_RaisesDependent()
    {
        // The base type is used first so a cache keyed on the declaring type would remember "none".
        BaseViewModel baseVm = new BaseViewModel();
        List<string?> baseRaised = Record(baseVm);
        baseVm.Source = 1;

        DerivedAddsDependsOnViewModel derived = new DerivedAddsDependsOnViewModel();
        List<string?> derivedRaised = Record(derived);
        derived.Source = 1;

        baseRaised.ShouldBe(new[] { "Source" });
        derivedRaised.ShouldBe(new[] { "Source", "Doubled" });
    }

    [Fact]
    public void Set_DependentSourceDeclaredOnBaseClass_RaisesDependentInOrder()
    {
        DerivedFromDependentViewModel vm = new DerivedFromDependentViewModel();
        List<string?> raised = Record(vm);

        vm.Source = 2;
        vm.Name = "a";

        raised.ShouldBe(new[] { "Source", "Doubled", "Name", "Shout" });
    }

    [Fact]
    public void Set_GenericViewModelWithDependsOn_RaisesDependentForEachInstantiation()
    {
        GenericViewModel<int> intVm = new GenericViewModel<int>();
        GenericViewModel<string> stringVm = new GenericViewModel<string>();
        List<string?> intRaised = Record(intVm);
        List<string?> stringRaised = Record(stringVm);

        intVm.Value = 5;
        stringVm.Value = "x";

        intRaised.ShouldBe(new[] { "Value", "Label" });
        stringRaised.ShouldBe(new[] { "Value", "Label" });
    }

    [Fact]
    public void Set_GenericViewModelWithoutDependsOn_RaisesOnlyOwnProperty()
    {
        GenericNoDependsOnViewModel<int> intVm = new GenericNoDependsOnViewModel<int>();
        GenericViewModel<int> dependentVm = new GenericViewModel<int>();
        List<string?> raised = Record(intVm);

        dependentVm.Value = 1;
        intVm.Value = 5;

        raised.ShouldBe(new[] { "Value" });
    }

    [Fact]
    public void Set_CollectionHeldByObjectProperty_Subscribes()
    {
        TypedCollectionsViewModel vm = new TypedCollectionsViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        vm.AsObject = items;
        List<string?> raised = Record(vm);

        items.Add(1);

        raised.ShouldBe(new[] { "AsObject" });
    }

    [Fact]
    public void Set_CollectionHeldByInterfaceProperty_Subscribes()
    {
        TypedCollectionsViewModel vm = new TypedCollectionsViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        vm.AsInterface = items;
        List<string?> raised = Record(vm);

        items.Add(1);

        raised.ShouldBe(new[] { "AsInterface" });
    }

    [Fact]
    public void Set_DerivedCollectionHeldByUnsealedBaseProperty_Subscribes()
    {
        TypedCollectionsViewModel vm = new TypedCollectionsViewModel();
        CollectionDerived collection = new CollectionDerived();
        vm.AsUnsealedBase = collection;
        List<string?> raised = Record(vm);

        collection.Raise();

        raised.ShouldBe(new[] { "AsUnsealedBase" });
    }

    [Fact]
    public void Set_SealedCollectionProperty_Subscribes()
    {
        TypedCollectionsViewModel vm = new TypedCollectionsViewModel();
        SealedCollection collection = new SealedCollection();
        vm.AsSealed = collection;
        List<string?> raised = Record(vm);

        collection.Raise();

        raised.ShouldBe(new[] { "AsSealed" });
    }

    [Fact]
    public void Set_SealedNonCollectionTypes_StoreAndNotify()
    {
        TypedCollectionsViewModel vm = new TypedCollectionsViewModel();
        List<string?> raised = Record(vm);
        SealedPlain plain = new SealedPlain();

        vm.Plain = plain;
        vm.Plain = plain;
        vm.Text = "a";
        vm.Text = "a";
        vm.Text = null;

        vm.Plain.ShouldBeSameAs(plain);
        vm.Text.ShouldBeNull();
        raised.ShouldBe(new[] { "Plain", "Text", "Text" });
    }
}
