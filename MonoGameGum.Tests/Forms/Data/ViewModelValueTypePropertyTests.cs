using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

/// <summary>
/// Behavior pins for value-type properties on <see cref="ViewModel"/> (issue #6379): values read
/// back correctly for every type, events and dependents are unchanged, and a clone never shares
/// state with the original.
/// </summary>
public class ViewModelValueTypePropertyTests : BaseTestClass
{
    private enum Mode { Off, On }

    private struct PlainPoint
    {
        public float X;
        public float Y;
    }

    private readonly record struct WideStruct(long A, long B, long C, long D);

    private class ValueViewModel : ViewModel
    {
        public float FloatValue { get => Get<float>(); set => Set(value); }
        public int IntValue { get => Get<int>(); set => Set(value); }
        public int? NullableInt { get => Get<int?>(); set => Set(value); }
        public Mode ModeValue { get => Get<Mode>(); set => Set(value); }
        public string? Text { get => Get<string?>(); set => Set(value); }
        public ObservableCollection<int>? Items { get => Get<ObservableCollection<int>?>(); set => Set(value); }
        public WideStruct Wide { get => Get<WideStruct>(); set => Set(value); }
        public PlainPoint Point { get => Get<PlainPoint>(); set => Set(value); }

        [DependsOn(nameof(IntValue))]
        public int Doubled => IntValue * 2;

        public bool SetQuietly(int value) => SetWithoutNotifying(value, nameof(IntValue));

        public bool SetTyped<T>(T value, string name) => Set(value, name);
        public T GetTyped<T>(string name) => Get<T>(name);

        public ValueViewModel CloneIt() => Clone<ValueViewModel>();
    }

    private static List<string?> Record(ViewModel vm)
    {
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);
        return raised;
    }

    [Fact]
    public void GetAndSet_ValueTypes_RoundTrip()
    {
        ValueViewModel vm = new ValueViewModel();

        vm.FloatValue = 1.5f;
        vm.IntValue = 7;
        vm.ModeValue = Mode.On;
        vm.Wide = new WideStruct(1, 2, 3, 4);
        vm.Point = new PlainPoint { X = 1, Y = 2 };
        vm.FloatValue = 2.5f;
        vm.Wide = new WideStruct(5, 6, 7, 8);

        vm.FloatValue.ShouldBe(2.5f);
        vm.IntValue.ShouldBe(7);
        vm.ModeValue.ShouldBe(Mode.On);
        vm.Wide.ShouldBe(new WideStruct(5, 6, 7, 8));
        vm.Point.X.ShouldBe(1);
    }

    [Fact]
    public void Get_Unset_ReturnsDefault()
    {
        ValueViewModel vm = new ValueViewModel();

        vm.FloatValue.ShouldBe(0);
        vm.NullableInt.ShouldBeNull();
        vm.ModeValue.ShouldBe(Mode.Off);
        vm.Wide.ShouldBe(default);
    }

    [Fact]
    public void Set_FirstValue_ReportsChangeOnlyWhenNotDefault()
    {
        ValueViewModel vm = new ValueViewModel();
        List<string?> raised = Record(vm);

        vm.IntValue = 0;
        vm.FloatValue = 1;

        raised.ShouldBe(new string?[] { nameof(ValueViewModel.FloatValue) });
    }

    [Fact]
    public void Set_ReturnValue_ReflectsWhetherValueChanged()
    {
        ValueViewModel vm = new ValueViewModel();

        vm.SetTyped(5, nameof(ValueViewModel.IntValue)).ShouldBeTrue();
        vm.SetTyped(5, nameof(ValueViewModel.IntValue)).ShouldBeFalse();
        vm.SetTyped(6, nameof(ValueViewModel.IntValue)).ShouldBeTrue();
    }

    [Fact]
    public void Set_NullableValueType_RoundTripsIncludingNull()
    {
        ValueViewModel vm = new ValueViewModel();
        List<string?> raised = Record(vm);

        vm.NullableInt = 5;
        vm.NullableInt.ShouldBe(5);
        vm.NullableInt = null;
        vm.NullableInt.ShouldBeNull();
        vm.NullableInt = null;
        vm.NullableInt = 3;
        vm.NullableInt = 3;

        vm.NullableInt.ShouldBe(3);
        raised.Count.ShouldBe(3);
    }

    [Fact]
    public void Set_EnumValue_RaisesOnlyOnChange()
    {
        ValueViewModel vm = new ValueViewModel();
        List<string?> raised = Record(vm);

        vm.ModeValue = Mode.On;
        vm.ModeValue = Mode.On;
        vm.ModeValue = Mode.Off;

        vm.ModeValue.ShouldBe(Mode.Off);
        raised.Count.ShouldBe(2);
    }

    [Fact]
    public void Set_StructWithoutIEquatable_BehavesAsBefore()
    {
        ValueViewModel vm = new ValueViewModel();
        List<string?> raised = Record(vm);

        vm.Point = new PlainPoint { X = 1, Y = 2 };
        vm.Point = new PlainPoint { X = 1, Y = 2 };
        vm.Point = new PlainPoint { X = 3, Y = 2 };

        vm.Point.X.ShouldBe(3);
        raised.Count.ShouldBe(2);
    }

    [Fact]
    public void Set_MixedReferenceAndValueProperties_DoNotInterfere()
    {
        ObservableCollection<int> items = new ObservableCollection<int>();
        ValueViewModel vm = new ValueViewModel { Text = "a", IntValue = 1, Items = items };
        vm.FloatValue = 2;
        vm.Text = "b";
        vm.IntValue = 3;

        vm.Text.ShouldBe("b");
        vm.IntValue.ShouldBe(3);
        vm.FloatValue.ShouldBe(2);
        vm.Items.ShouldBeSameAs(items);
    }

    [Fact]
    public void Set_DependentIsRecomputedAfterValueTypeChange()
    {
        ValueViewModel vm = new ValueViewModel { IntValue = 2 };
        List<string?> raised = Record(vm);

        vm.IntValue = 5;
        vm.IntValue = 5;

        vm.Doubled.ShouldBe(10);
        raised.ShouldBe(new string?[] { nameof(ValueViewModel.IntValue), nameof(ValueViewModel.Doubled) });
    }

    [Fact]
    public void SetWithoutNotifying_ChangesValueAndRaisesNothing()
    {
        ValueViewModel vm = new ValueViewModel { IntValue = 2 };
        List<string?> raised = Record(vm);

        vm.SetQuietly(3).ShouldBeTrue();
        vm.SetQuietly(3).ShouldBeFalse();

        vm.IntValue.ShouldBe(3);
        vm.Doubled.ShouldBe(6);
        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Clone_ValueTypeChangesOnOneSide_DoNotAffectTheOther()
    {
        ValueViewModel original = new ValueViewModel { IntValue = 1, FloatValue = 1, Wide = new WideStruct(1, 1, 1, 1) };
        ValueViewModel clone = original.CloneIt();

        clone.IntValue = 2;
        original.FloatValue = 9;
        original.Wide = new WideStruct(2, 2, 2, 2);

        original.IntValue.ShouldBe(1);
        clone.IntValue.ShouldBe(2);
        clone.FloatValue.ShouldBe(1);
        original.FloatValue.ShouldBe(9);
        clone.Wide.ShouldBe(new WideStruct(1, 1, 1, 1));
        original.Wide.ShouldBe(new WideStruct(2, 2, 2, 2));
    }

    [Fact]
    public void Clone_SetOnOriginalAfterClone_DoesNotRaiseOnClone()
    {
        ValueViewModel original = new ValueViewModel { IntValue = 1 };
        ValueViewModel clone = original.CloneIt();
        List<string?> raisedByClone = Record(clone);

        original.IntValue = 4;
        clone.IntValue = 1;

        clone.IntValue.ShouldBe(1);
        raisedByClone.ShouldBeEmpty();
    }

    [Fact]
    public void GetAndSet_TypeChangesBetweenCallsOnSameName_KeepsWorking()
    {
        ValueViewModel vm = new ValueViewModel();

        vm.SetTyped(5, "Shared");
        vm.GetTyped<int>("Shared").ShouldBe(5);
        vm.GetTyped<int?>("Shared").ShouldBe(5);
        vm.GetTyped<object>("Shared").ShouldBe(5);

        vm.SetTyped<int?>(6, "Shared");
        vm.GetTyped<int>("Shared").ShouldBe(6);
        vm.GetTyped<object>("Shared").ShouldBe(6);

        vm.SetTyped<int?>(null, "Shared");
        vm.GetTyped<int?>("Shared").ShouldBeNull();
        vm.GetTyped<object>("Shared").ShouldBeNull();
    }

    [Fact]
    public void Set_ObjectTypedOnNameHoldingValueType_ComparesByValue()
    {
        ValueViewModel vm = new ValueViewModel();
        vm.SetTyped(5, "Shared");
        List<string?> raised = Record(vm);

        vm.SetTyped<object>(5, "Shared").ShouldBeFalse();
        vm.SetTyped<object>(6, "Shared").ShouldBeTrue();

        vm.GetTyped<int>("Shared").ShouldBe(6);
        vm.GetTyped<object>("Shared").ShouldBe(6);
        raised.ShouldBe(new string?[] { "Shared" });
    }

    [Fact]
    public void Set_ValueTypeOnNameHoldingObject_ComparesByValue()
    {
        ValueViewModel vm = new ValueViewModel();
        vm.SetTyped<object>(5, "Shared");
        List<string?> raised = Record(vm);

        vm.SetTyped(5, "Shared").ShouldBeFalse();
        vm.SetTyped(6, "Shared").ShouldBeTrue();

        vm.GetTyped<int>("Shared").ShouldBe(6);
        raised.ShouldBe(new string?[] { "Shared" });
    }

    [Fact]
    public void Get_WrongValueType_ThrowsInvalidCast()
    {
        ValueViewModel vm = new ValueViewModel();
        vm.SetTyped(5, "Shared");

        Should.Throw<InvalidCastException>(() => vm.GetTyped<float>("Shared"));
        Should.Throw<InvalidCastException>(() => vm.GetTyped<string>("Shared"));
        Should.Throw<InvalidCastException>(() => vm.SetTyped(1f, "Shared"));
    }
}
