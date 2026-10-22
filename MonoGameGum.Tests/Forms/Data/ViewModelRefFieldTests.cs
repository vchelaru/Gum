using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// Pins for the ref-field style on <see cref="ViewModel"/> (#6356): <c>Set(ref _field, value)</c> and
/// <c>SetWithoutNotifying(ref _field, value)</c> match the by-name Get/Set for events, dependents and
/// collections, and record where the field style differs (initial values, Clone).
/// </summary>
public class ViewModelRefFieldTests : BaseTestClass
{
    private class RefViewModel : ViewModel
    {
        private int _intValue;
        private string? _text;
        private int _preset = 7;
        private ObservableCollection<int>? _items;
        private ObservableCollection<int>? _legacyItems;

        public int IntValue { get => _intValue; set => Set(ref _intValue, value); }
        public string? Text { get => _text; set => Set(ref _text, value); }
        public int Preset { get => _preset; set => Set(ref _preset, value); }
        public ObservableCollection<int>? Items { get => _items; set => Set(ref _items, value); }

        // A by-name property on the same view model.
        public int Named { get => Get<int>(); set => Set(value); }

        // The existing ref API, for the pins that say how it differs.
        public ObservableCollection<int>? LegacyItems
        {
            get => _legacyItems;
            set => ChangeAndNotify(ref _legacyItems, value);
        }

        [DependsOn(nameof(IntValue))]
        public int Doubled => IntValue * 2;

        public bool SetReturn(int value) => Set(ref _intValue, value, nameof(IntValue));
        public bool SetNamed(int value, string name) => Set(ref _intValue, value, name);
        public bool SetQuietly(int value) => SetWithoutNotifying(ref _intValue, value);
        public bool SetItemsQuietly(ObservableCollection<int>? value) => SetWithoutNotifying(ref _items, value, nameof(Items));
        public RefViewModel CloneIt() => Clone<RefViewModel>();
    }

    private class OverridingViewModel : RefViewModel
    {
        public List<string?> Seen { get; } = new();

        protected override void NotifyPropertyChanged(string? propertyName = null)
        {
            Seen.Add(propertyName);
            base.NotifyPropertyChanged(propertyName);
        }
    }

    private static List<string?> Record(ViewModel vm)
    {
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);
        return raised;
    }

    [Fact]
    public void ByNameAndFieldProperties_OnSameViewModel_DoNotInterfere()
    {
        RefViewModel vm = new RefViewModel();

        vm.IntValue = 1;
        vm.Named = 2;

        vm.IntValue.ShouldBe(1);
        vm.Named.ShouldBe(2);
    }

    [Fact]
    public void ChangeAndNotify_NewValueRaisesOnce_AndSameValueRaisesNothing()
    {
        // Pins ChangeAndNotify before and after it shares the Set(ref) implementation.
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        List<string?> raised = Record(vm);

        vm.LegacyItems = items;
        vm.LegacyItems = items;

        raised.ShouldBe(new string?[] { "LegacyItems" });
    }

    [Fact]
    public void ChangeAndNotify_Collection_IsNotSubscribed()
    {
        // ChangeAndNotify has never watched collection contents; only Set(ref) does.
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        vm.LegacyItems = items;
        List<string?> raised = Record(vm);

        items.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Clone_FieldValuesAreCopiedByMemberwiseCloneAndNotShared()
    {
        RefViewModel original = new RefViewModel();
        original.IntValue = 1;
        original.Text = "a";

        RefViewModel clone = original.CloneIt();
        clone.IntValue = 2;

        clone.Text.ShouldBe("a");
        clone.IntValue.ShouldBe(2);
        original.IntValue.ShouldBe(1);
    }

    [Fact]
    public void Clone_ReplacingCollection_DoesNotUnsubscribeTheOriginal()
    {
        RefViewModel original = new RefViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        original.Items = items;
        RefViewModel clone = original.CloneIt();
        List<string?> originalRaised = Record(original);

        clone.Items = new ObservableCollection<int>();
        items.Add(1);

        originalRaised.ShouldBe(new string?[] { "Items" });
    }

    [Fact]
    public void Collection_ChangesRaisePropertyChanged_AndReplacedCollectionIsUnsubscribed()
    {
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> first = new ObservableCollection<int>();
        ObservableCollection<int> second = new ObservableCollection<int>();
        vm.Items = first;
        List<string?> raised = Record(vm);

        first.Add(1);
        vm.Items = second;
        raised.Clear();
        first.Add(2);
        second.Add(3);

        raised.ShouldBe(new string?[] { "Items" });
    }

    [Fact]
    public void Collection_StoredQuietlyAndThenSetAgain_IsSubscribed()
    {
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        vm.SetItemsQuietly(items);
        List<string?> raised = Record(vm);

        // Setting the stored instance again attaches the subscription, like the by-name style does.
        vm.Items = items;
        items.Add(1);

        raised.ShouldBe(new string?[] { "Items" });
    }

    [Fact]
    public void SetWithoutNotifying_Collection_IsNotSubscribed()
    {
        // Same as the by-name SetWithoutNotifying: it stores the collection and never listens to it.
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> items = new ObservableCollection<int>();
        vm.SetItemsQuietly(items);
        List<string?> raised = Record(vm);

        items.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void SetWithoutNotifying_ReplacingSubscribedCollection_UnsubscribesOldAndDoesNotSubscribeNew()
    {
        // Same as the by-name SetWithoutNotifying: the replaced collection stops being watched, the new one is not.
        RefViewModel vm = new RefViewModel();
        ObservableCollection<int> first = new ObservableCollection<int>();
        ObservableCollection<int> second = new ObservableCollection<int>();
        vm.Items = first;
        vm.SetItemsQuietly(second);
        List<string?> raised = Record(vm);

        first.Add(1);
        second.Add(1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void FieldWithInitialValue_SetToSameValue_RaisesNothing()
    {
        // Differs from the by-name style, where the first Set of any non-default value notifies.
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        vm.Preset = 7;

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void OverriddenNotifyPropertyChanged_IsStillCalledForFieldSets()
    {
        OverridingViewModel vm = new OverridingViewModel();
        List<string?> raised = Record(vm);

        vm.IntValue = 1;
        vm.Text = "a";

        vm.Seen.ShouldBe(new string?[] { "IntValue", "Doubled", "Text" });
        raised.ShouldBe(new string?[] { "IntValue", "Doubled", "Text" });
    }

    [Fact]
    public void Set_NewValue_RaisesPropertyChangedThenDependents()
    {
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        bool didSet = vm.SetReturn(3);

        didSet.ShouldBeTrue();
        raised.ShouldBe(new string?[] { "IntValue", "Doubled" });
    }

    [Fact]
    public void Set_SameValueOrDefaultOnFirstSet_RaisesNothing()
    {
        RefViewModel vm = new RefViewModel();
        vm.IntValue = 3;
        List<string?> raised = Record(vm);

        vm.IntValue = 3;
        vm.Text = null;

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_ValueAndReferenceTypes_RoundTrip()
    {
        RefViewModel vm = new RefViewModel();

        vm.IntValue = 5;
        vm.Text = "hello";
        vm.IntValue = 6;

        vm.IntValue.ShouldBe(6);
        vm.Text.ShouldBe("hello");
    }

    [Fact]
    public void Set_WithAnEqualNameThatIsADifferentStringInstance_RaisesTheName()
    {
        // The event args are found by name, so a name built at run time must still be reported correctly.
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        vm.SetNamed(1, new string("IntValue".ToCharArray()));
        vm.SetNamed(2, "IntValue");

        raised.ShouldBe(new string?[] { "IntValue", "Doubled", "IntValue", "Doubled" });
    }

    [Fact]
    public void Set_EventArgs_AreSharedAcrossInstances_AndKeepTheirNameWhenANameIsBuiltAtRunTime()
    {
        RefViewModel first = new RefViewModel();
        RefViewModel second = new RefViewModel();
        List<PropertyChangedEventArgs> firstArgs = new();
        List<PropertyChangedEventArgs> secondArgs = new();
        first.PropertyChanged += (_, args) => firstArgs.Add(args);
        second.PropertyChanged += (_, args) => secondArgs.Add(args);

        first.IntValue = 1;
        second.IntValue = 1;
        // Populated through the literal name above; a different instance of the same name must still resolve.
        second.SetNamed(2, new string("IntValue".ToCharArray()));

        ReferenceEquals(firstArgs[0], secondArgs[0]).ShouldBeTrue();
        secondArgs.Select(a => a.PropertyName).ShouldBe(new string?[] { "IntValue", "Doubled", "IntValue", "Doubled" });
    }

    [Fact]
    public void SetWithoutNotifying_ChangesValueAndRaisesNothing()
    {
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        bool didSet = vm.SetQuietly(9);

        didSet.ShouldBeTrue();
        vm.IntValue.ShouldBe(9);
        raised.ShouldBeEmpty();
    }

    [Fact]
    public void SetWithoutNotifying_SameValue_ReturnsFalse()
    {
        RefViewModel vm = new RefViewModel();

        vm.SetQuietly(9).ShouldBeTrue();
        vm.SetQuietly(9).ShouldBeFalse();
    }
}
