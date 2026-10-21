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
    }

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
}
