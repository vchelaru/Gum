using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// Behavior pins for the ref-field style on <see cref="ViewModel"/> (spike #6397): <c>Set(ref _field, value)</c>
/// and <see cref="ViewModelField{T}"/> behave like the by-name Get/Set for events, dependents and
/// collections, and the pins record where the field style differs (initial values, Clone).
/// </summary>
public class ViewModelRefFieldTests : BaseTestClass
{
    private class RefViewModel : ViewModel
    {
        private static readonly ViewModelProperty<float> FloatKey = new(nameof(Float));

        private int _intValue;
        private string? _text;
        private float _float;
        private int _preset = 7;
        private ObservableCollection<int>? _items;

        public int IntValue { get => _intValue; set => Set(ref _intValue, value); }
        public string? Text { get => _text; set => Set(ref _text, value); }
        public float Float { get => _float; set => Set(ref _float, value, FloatKey); }
        public int Preset { get => _preset; set => Set(ref _preset, value); }
        public ObservableCollection<int>? Items { get => _items; set => Set(ref _items, value); }

        // A by-name property on the same view model.
        public int Named { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(IntValue))]
        public int Doubled => IntValue * 2;

        public bool SetQuietly(int value) => SetWithoutNotifying(ref _intValue, value, nameof(IntValue));
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

    private class ObjectViewModel : ViewModel
    {
        private readonly ViewModelField<int> _score;

        public ObjectViewModel() => _score = new ViewModelField<int>(this, nameof(Score));

        public int Score { get => _score.Value; set => _score.Value = value; }

        [DependsOn(nameof(Score))]
        public int Doubled => Score * 2;

        public ObjectViewModel CloneIt() => Clone<ObjectViewModel>();
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
    public void Clone_PropertyObjectsAreSharedWithTheOriginal()
    {
        // Documents the limitation: MemberwiseClone copies the reference to the ViewModelField, so the
        // clone reads and writes the storage of the original and raises its events.
        ObjectViewModel original = new ObjectViewModel();
        original.Score = 1;
        ObjectViewModel clone = original.CloneIt();
        List<string?> originalRaised = Record(original);

        clone.Score = 2;

        original.Score.ShouldBe(2);
        originalRaised.ShouldBe(new string?[] { "Score", "Doubled" });
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
    public void PropertyObject_NewValue_RaisesPropertyChangedThenDependents()
    {
        ObjectViewModel vm = new ObjectViewModel();
        List<string?> raised = Record(vm);

        vm.Score = 3;
        vm.Score = 3;

        vm.Score.ShouldBe(3);
        raised.ShouldBe(new string?[] { "Score", "Doubled" });
    }

    [Fact]
    public void Set_KeyOverload_RaisesTheKeyName()
    {
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        vm.Float = 2f;

        raised.ShouldBe(new string?[] { "Float" });
    }

    [Fact]
    public void Set_NewValue_RaisesPropertyChangedThenDependents()
    {
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        vm.IntValue = 3;

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
    public void SetWithoutNotifying_ChangesValueAndRaisesNothing()
    {
        RefViewModel vm = new RefViewModel();
        List<string?> raised = Record(vm);

        bool didSet = vm.SetQuietly(9);

        didSet.ShouldBeTrue();
        vm.IntValue.ShouldBe(9);
        raised.ShouldBeEmpty();
    }
}
