using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// Behavior pins for the keyed property style on <see cref="ViewModel"/> (spike #6390): a
/// <see cref="ViewModelProperty{T}"/> passed to GetValue/SetValue behaves like the by-name
/// Get/Set for events, dependents, cloning and collections, and the two styles coexist.
/// </summary>
public class ViewModelKeyedPropertyTests : BaseTestClass
{
    private class KeyedViewModel : ViewModel
    {
        private static readonly ViewModelProperty<int> IntKey = new(nameof(IntValue));
        private static readonly ViewModelProperty<string?> TextKey = new(nameof(Text));
        private static readonly ViewModelProperty<ObservableCollection<int>?> ItemsKey = new(nameof(Items));

        public int IntValue { get => GetValue(IntKey); set => SetValue(IntKey, value); }
        public string? Text { get => GetValue(TextKey); set => SetValue(TextKey, value); }
        public ObservableCollection<int>? Items { get => GetValue(ItemsKey); set => SetValue(ItemsKey, value); }

        // A by-name property on the same view model.
        public int Named { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(IntValue))]
        public int Doubled => IntValue * 2;

        public bool SetQuietly(int value) => SetValueWithoutNotifying(IntKey, value);
        public KeyedViewModel CloneIt() => Clone<KeyedViewModel>();
    }

    private class DerivedKeyedViewModel : KeyedViewModel
    {
        private static readonly ViewModelProperty<float> FloatKey = new(nameof(FloatValue));
        public float FloatValue { get => GetValue(FloatKey); set => SetValue(FloatKey, value); }
    }

    private class OverridingViewModel : KeyedViewModel
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
    public void GetValue_Unset_ReturnsDefault()
    {
        KeyedViewModel vm = new KeyedViewModel();

        vm.IntValue.ShouldBe(0);
        vm.Text.ShouldBeNull();
    }

    [Fact]
    public void SetValue_ValueAndReferenceTypes_RoundTrip()
    {
        KeyedViewModel vm = new KeyedViewModel();

        vm.IntValue = 5;
        vm.Text = "hello";
        vm.IntValue = 6;

        vm.IntValue.ShouldBe(6);
        vm.Text.ShouldBe("hello");
    }

    [Fact]
    public void SetValue_NewValue_RaisesPropertyChangedThenDependents()
    {
        KeyedViewModel vm = new KeyedViewModel();
        List<string?> raised = Record(vm);

        vm.IntValue = 3;

        raised.ShouldBe(new string?[] { "IntValue", "Doubled" });
    }

    [Fact]
    public void SetValue_SameValueOrDefaultOnFirstSet_RaisesNothing()
    {
        KeyedViewModel vm = new KeyedViewModel();
        vm.IntValue = 3;
        List<string?> raised = Record(vm);

        vm.IntValue = 3;
        vm.Text = null;

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void SetValueWithoutNotifying_ChangesValueAndRaisesNothing()
    {
        KeyedViewModel vm = new KeyedViewModel();
        List<string?> raised = Record(vm);

        bool didSet = vm.SetQuietly(9);

        didSet.ShouldBeTrue();
        vm.IntValue.ShouldBe(9);
        raised.ShouldBeEmpty();
    }

    [Fact]
    public void OverriddenNotifyPropertyChanged_IsStillCalledForKeyedSets()
    {
        OverridingViewModel vm = new OverridingViewModel();
        List<string?> raised = Record(vm);

        vm.IntValue = 1;
        vm.Text = "a";

        vm.Seen.ShouldBe(new string?[] { "IntValue", "Doubled", "Text" });
        raised.ShouldBe(new string?[] { "IntValue", "Doubled", "Text" });
    }

    [Fact]
    public void KeyedAndByNameProperties_OnSameViewModel_DoNotInterfere()
    {
        KeyedViewModel vm = new KeyedViewModel();

        vm.IntValue = 1;
        vm.Named = 2;

        vm.IntValue.ShouldBe(1);
        vm.Named.ShouldBe(2);
    }

    [Fact]
    public void DerivedViewModel_KeysFromBaseAndDerived_AreIndependent()
    {
        DerivedKeyedViewModel vm = new DerivedKeyedViewModel();

        vm.IntValue = 4;
        vm.FloatValue = 2.5f;
        vm.Text = "x";

        vm.IntValue.ShouldBe(4);
        vm.FloatValue.ShouldBe(2.5f);
        vm.Text.ShouldBe("x");
    }

    [Fact]
    public void TwoInstances_DoNotShareKeyedValues()
    {
        KeyedViewModel first = new KeyedViewModel();
        KeyedViewModel second = new KeyedViewModel();

        first.IntValue = 1;
        second.IntValue = 2;

        first.IntValue.ShouldBe(1);
        second.IntValue.ShouldBe(2);
    }

    [Fact]
    public void Clone_KeyedValuesAreCopiedAndNotShared()
    {
        KeyedViewModel original = new KeyedViewModel();
        original.IntValue = 1;
        original.Text = "a";

        KeyedViewModel clone = original.CloneIt();
        clone.IntValue = 2;

        clone.Text.ShouldBe("a");
        clone.IntValue.ShouldBe(2);
        original.IntValue.ShouldBe(1);
    }

    [Fact]
    public void Collection_ChangesRaisePropertyChanged_AndReplacedCollectionIsUnsubscribed()
    {
        KeyedViewModel vm = new KeyedViewModel();
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
}
