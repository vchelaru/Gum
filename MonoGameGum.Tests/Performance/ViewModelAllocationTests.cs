using Gum.Mvvm;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Allocation guards and behavior pins for the <see cref="ViewModel"/> property set/get path
/// (issue #6356). Each measurement runs a few thousand iterations after a warmup and compares
/// against fixed byte bounds.
/// </summary>
public class ViewModelAllocationTests : BaseTestClass
{
    private const int WarmupIterations = 200;
    private const int MeasuredIterations = 2000;
    private const int Attempts = 3;

    // A boxed float/int on a 64-bit runtime: 16-byte header plus the value, rounded up.
    private const int BoxedValueBytes = 24;

    private class AllocationViewModel : ViewModel
    {
        public float FloatProperty
        {
            get => Get<float>();
            set => Set(value);
        }

        public string? StringProperty
        {
            get => Get<string?>();
            set => Set(value);
        }

        [DependsOn(nameof(FloatProperty))]
        public float DoubleFloat => FloatProperty * 2;

        [DependsOn(nameof(StringProperty))]
        public int StringLength => StringProperty?.Length ?? 0;

        public ObservableCollection<int>? Items
        {
            get => Get<ObservableCollection<int>?>();
            set => Set(value);
        }

        public int IntProperty
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool BoolProperty
        {
            get => Get<bool>();
            set => Set(value);
        }

        public AllocationEnum EnumProperty
        {
            get => Get<AllocationEnum>();
            set => Set(value);
        }

        public BigStruct BigProperty
        {
            get => Get<BigStruct>();
            set => Set(value);
        }

        public void SetFloatWithoutNotifying(float value) =>
            SetWithoutNotifying(value, nameof(FloatProperty));
    }

    private enum AllocationEnum { A, B }

    private readonly record struct BigStruct(long A, long B, long C, long D) : System.IEquatable<BigStruct>;

    private static AllocationResult Measure(System.Action action)
    {
        return AllocationMeasurer.MeasureMinimum(
            action,
            attempts: Attempts,
            warmupIterations: WarmupIterations,
            measuredIterations: MeasuredIterations);
    }

    [Fact]
    public void Set_SameFloatValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 3 };

        AllocationResult result = Measure(() => vm.FloatProperty = 3);

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_SameStringValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { StringProperty = "abc" };

        AllocationResult result = Measure(() => vm.StringProperty = "abc");

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_SameValue_RaisesNothingAndLeavesDependentsAlone()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 3 };
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        vm.FloatProperty = 3;
        vm.FloatProperty = 3;

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Set_NewValue_RaisesPropertyAndThenItsDependents()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 3 };
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        vm.FloatProperty = 4;

        raised.ShouldBe(new string?[] { nameof(AllocationViewModel.FloatProperty), nameof(AllocationViewModel.DoubleFloat) });
        vm.DoubleFloat.ShouldBe(8);
    }

    [Fact]
    public void Set_CollectionContents_RaisesPropertyForTheCollectionProperty()
    {
        ObservableCollection<int> items = new ObservableCollection<int>();
        AllocationViewModel vm = new AllocationViewModel { Items = items };
        List<string?> raised = new List<string?>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        items.Add(1);

        raised.ShouldBe(new string?[] { nameof(AllocationViewModel.Items) });
    }

    [Fact]
    public void Set_NewFloatValue_AllocatesOnlyTheBox()
    {
        AllocationViewModel vm = new AllocationViewModel();
        float value = 0;

        AllocationResult result = Measure(() =>
        {
            value = value == 1 ? 2 : 1;
            vm.FloatProperty = value;
        });

        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(BoxedValueBytes);
    }

    [Fact]
    public void Set_NewStringValue_WithSubscriberAndDependent_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel();
        int raisedCount = 0;
        vm.PropertyChanged += (_, _) => raisedCount++;
        string a = "a";
        string b = "bb";
        bool useA = false;

        AllocationResult result = Measure(() =>
        {
            useA = !useA;
            vm.StringProperty = useA ? a : b;
        });

        raisedCount.ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewStringValue_OnViewModelWithSubscribedCollection_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { Items = new ObservableCollection<int>() };
        string a = "a";
        string b = "bb";
        bool useA = false;

        AllocationResult result = Measure(() =>
        {
            useA = !useA;
            vm.StringProperty = useA ? a : b;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_SameObservableCollection_AllocatesNothing()
    {
        ObservableCollection<int> items = new ObservableCollection<int>();
        // A second listener, like an ItemsControl bound to the collection: removing and re-adding
        // one handler from a multi-handler event allocates a new delegate list each time.
        items.CollectionChanged += (_, _) => { };
        AllocationViewModel vm = new AllocationViewModel { Items = items };

        AllocationResult result = Measure(() => vm.Items = items);

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewObservableCollection_StaysWithinSubscribeAndUnsubscribeCost()
    {
        AllocationViewModel vm = new AllocationViewModel();
        ObservableCollection<int> a = new ObservableCollection<int>();
        ObservableCollection<int> b = new ObservableCollection<int>();
        a.CollectionChanged += (_, _) => { };
        b.CollectionChanged += (_, _) => { };
        bool useA = false;

        AllocationResult result = Measure(() =>
        {
            useA = !useA;
            vm.Items = useA ? a : b;
        });

        // Main measured 112 B here (the benchmark's ObservableCollection row); the bound leaves
        // room for runtime differences but fails if a Set starts building a subscription per call again.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(128);
    }

    [Fact]
    public void Get_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 3, StringProperty = "abc" };
        float sink = 0;

        AllocationResult result = Measure(() =>
        {
            sink += vm.FloatProperty;
            sink += vm.StringProperty!.Length;
        });

        sink.ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewFloatValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 1 };
        float value = 1;

        AllocationResult result = Measure(() =>
        {
            value = value == 1 ? 2 : 1;
            vm.FloatProperty = value;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewIntValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { IntProperty = 1 };
        int value = 1;

        AllocationResult result = Measure(() =>
        {
            value = value == 1 ? 2 : 1;
            vm.IntProperty = value;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewBoolValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { BoolProperty = true };
        bool value = true;

        AllocationResult result = Measure(() =>
        {
            value = !value;
            vm.BoolProperty = value;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewEnumValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { EnumProperty = AllocationEnum.B };
        bool useB = true;

        AllocationResult result = Measure(() =>
        {
            useB = !useB;
            vm.EnumProperty = useB ? AllocationEnum.B : AllocationEnum.A;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewLargeStructValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { BigProperty = new BigStruct(1, 2, 3, 4) };
        BigStruct a = new BigStruct(1, 2, 3, 4);
        BigStruct b = new BigStruct(5, 6, 7, 8);
        bool useA = false;

        AllocationResult result = Measure(() =>
        {
            useA = !useA;
            vm.BigProperty = useA ? a : b;
        });

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Set_NewFloatValue_WithSubscriberAndDependent_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 1 };
        int raisedCount = 0;
        vm.PropertyChanged += (_, _) => raisedCount++;
        float value = 1;

        AllocationResult result = Measure(() =>
        {
            value = value == 1 ? 2 : 1;
            vm.FloatProperty = value;
        });

        raisedCount.ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void SetWithoutNotifying_NewFloatValue_AllocatesNothing()
    {
        AllocationViewModel vm = new AllocationViewModel { FloatProperty = 1 };
        float value = 1;

        AllocationResult result = Measure(() =>
        {
            value = value == 1 ? 2 : 1;
            vm.SetFloatWithoutNotifying(value);
        });

        result.TotalBytes.ShouldBe(0);
    }
}
