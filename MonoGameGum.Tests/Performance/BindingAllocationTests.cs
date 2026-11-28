using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Per-notification allocation baseline and regression guard for the runtime data-binding path
/// (issue #1934): a <see cref="ViewModel"/> raising <c>INotifyPropertyChanged</c> that propagates
/// through <see cref="GraphicalUiElement.SetBinding(string, string, string)"/> to a bound visual
/// property. The guards use <see cref="AllocationMeasurer.Measure"/>.
/// </summary>
public class BindingAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public BindingAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private class BindingViewModel : ViewModel
    {
        public int IntProperty
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    [Fact]
    public void PropertyChange_PropagatingToBoundVisual_IsLowAllocation()
    {
        BindingViewModel vm = new();
        ContainerRuntime container = new();
        container.BindingContext = vm;
        container.SetBinding(nameof(container.X), nameof(vm.IntProperty));

        int value = 0;
        const int measuredIterations = 500;

        // Alternate the value every notification so each Set genuinely changes the VM and fires
        // PropertyChanged (a no-op Set would notify nothing and make the result meaningless).
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                value = value == 1 ? 2 : 1;
                vm.IntProperty = value;
            },
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        _output.WriteLine($"VM property change propagating to a bound visual property: " +
            $"{result.BytesPerIteration:N0} bytes/notification ({result.TotalBytes:N0} bytes over {result.Iterations} notifications)");

        // Liveness: prove the notification actually propagated all the way to the bound UI property.
        container.X.ShouldBe(value);

        // The residual per-notification cost is the value-type boxing inherent to the reflection-based
        // flat binding and the ViewModel's object-dictionary storage (VM int stored/read as object, and
        // int-to-float ConvertValue). Structural allocations (the descendant-walk iterator and a fresh
        // PropertyChangedEventArgs per notification) have been removed. This guards against a regression
        // reintroducing them (#1934).
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(104);
    }

    private class AttachViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public int Score { get => Get<int>(); set => Set(value); }
        public bool Shown { get => Get<bool>(); set => Set(value); }
    }

    [Fact]
    public void BindingContextSwap_OnVisualWithThreeBindings_DoesNotCompileGettersPerAttach()
    {
        AttachViewModel first = new() { Name = "a", Score = 1, Shown = true };
        AttachViewModel second = new() { Name = "b", Score = 2, Shown = false };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(AttachViewModel.Name));
        text.SetBinding(nameof(TextRuntime.Width), nameof(AttachViewModel.Score));
        text.SetBinding(nameof(TextRuntime.Visible), nameof(AttachViewModel.Shown));
        bool useFirst = false;

        // Every assignment re-attaches all three bindings to the new view model.
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                useFirst = !useFirst;
                text.BindingContext = useFirst ? first : second;
            },
            warmupIterations: 20,
            measuredIterations: 200);

        _output.WriteLine($"BindingContext swap with three bindings: {result.BytesPerIteration:N0} bytes/swap");
        text.Text.ShouldBe("b");

        // Compiling a getter allocates several KB, so a per-attach compile of three of them lands far above this.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(4096);
    }
}
