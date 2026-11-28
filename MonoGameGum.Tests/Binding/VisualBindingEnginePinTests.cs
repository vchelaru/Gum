using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.VisualBindingPins;

/// <summary>
/// Pins how a visual's SetBinding (string and lambda overloads) behaves, for moving visuals onto the Forms
/// binding engine (#8530). A MustKeep pin must hold on both engines. A WillMatchForms pin records where the
/// visual engine differs from the Forms engine; its paired Forms test shows the behavior a visual adopts.
/// </summary>
public class VisualBindingEnginePinTests : BaseTestClass
{
    private const string MustKeep = "MustKeep";
    private const string WillMatchForms = "WillMatchForms";

    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports;

    public VisualBindingEnginePinTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        _reports = new List<string>();
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    #region Types

    public enum PinMode
    {
        First,
        Second,
        Third
    }

    public class PinViewModel : ViewModel
    {
        public string? Value { get => Get<string?>(); set => Set(value); }
        public int Number { get => Get<int>(); set => Set(value); }
        public float Amount { get => Get<float>(); set => Set(value); }
        public double Ratio { get => Get<double>(); set => Set(value); }
        public decimal Price { get => Get<decimal>(); set => Set(value); }
        public bool Flag { get => Get<bool>(); set => Set(value); }
        public PinMode Mode { get => Get<PinMode>(); set => Set(value); }
        public object? Thing { get => Get<object?>(); set => Set(value); }
        public PinViewModel? Child { get => Get<PinViewModel?>(); set => Set(value); }
        public List<string> Items { get; } = new List<string>();
        public int GetOnly => 42;
        public int PrivateSet { get => Get<int>(); private set => Set(value); }
        public int Field;

        public event EventHandler? Raised;
        public void Raise() => Raised?.Invoke(this, EventArgs.Empty);
        public bool HasRaisedSubscribers => Raised != null;
    }

    public class PlainViewModel
    {
        public string? Value { get; set; }
        public event EventHandler? Raised;
        public void Raise() => Raised?.Invoke(this, EventArgs.Empty);
        public bool HasRaisedSubscribers => Raised != null;
    }

    public class PinThing
    {
        public override string ToString() => "thing";
    }

    public class PinRuntimeBase : ContainerRuntime
    {
        public int InheritedHits;
        protected void OnRaisedProtected(object? sender, EventArgs e) => InheritedHits++;
        private void OnRaisedBasePrivate(object? sender, EventArgs e) => InheritedHits++;
    }

    public class PinRuntime : PinRuntimeBase
    {
        private int _pushedInt;
        private string? _pushedString;
        private int _notifyingInt;

        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public byte ByteValue { get; set; }
        public PinMode ModeValue { get; set; }
        public bool BoolValue { get; set; }
        public int PrivateSetValue { get; private set; }
        public int ReadOnlyValue => 0;

        public int PushedInt
        {
            get => _pushedInt;
            set
            {
                _pushedInt = value;
                PushValueToViewModel();
            }
        }

        public string? PushedString
        {
            get => _pushedString;
            set
            {
                _pushedString = value;
                PushValueToViewModel();
            }
        }

        public int NotifyingInt
        {
            get => _notifyingInt;
            set
            {
                _notifyingInt = value;
                NotifyPropertyChanged();
            }
        }

        public int Hits;
        public void OnRaised(object? sender, EventArgs e) => Hits++;
        private void OnRaisedPrivate(object? sender, EventArgs e) => Hits++;
    }

    /// <summary>A Forms control with the same target properties, for the paired Forms tests.</summary>
    public class PinControl : FrameworkElement
    {
        private int _notifyingInt;

        public PinControl() : base(new InteractiveGue())
        {
        }

        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public byte ByteValue { get; set; }
        public PinMode ModeValue { get; set; }
        public bool BoolValue { get; set; }

        public int NotifyingInt
        {
            get => _notifyingInt;
            set
            {
                _notifyingInt = value;
                PushValueToViewModel();
            }
        }
    }

    private static readonly Dictionary<string, Expression<Func<PinViewModel, object?>>> Lambdas = new()
    {
        ["Value"] = vm => vm.Value,
        ["Number"] = vm => vm.Number,
        ["Amount"] = vm => vm.Amount,
        ["Ratio"] = vm => vm.Ratio,
        ["Price"] = vm => vm.Price,
        ["Flag"] = vm => vm.Flag,
        ["Mode"] = vm => vm.Mode,
        ["Thing"] = vm => vm.Thing,
        ["Child"] = vm => vm.Child,
        ["Child.Value"] = vm => vm.Child!.Value,
        ["GetOnly"] = vm => vm.GetOnly,
        ["PrivateSet"] = vm => vm.PrivateSet,
        ["Field"] = vm => vm.Field,
    };

    private static void Bind(GraphicalUiElement target, string uiProperty, string path, bool useLambda,
        string? format = null)
    {
        if (useLambda)
        {
            target.SetBinding<PinViewModel>(uiProperty, Lambdas[path], format);
        }
        else
        {
            target.SetBinding(uiProperty, path, format);
        }
    }

    #endregion

    #region MustKeep: value flow

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ContextSetBeforeSetBinding_AppliesValue(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);

        sut.StringValue.ShouldBe("a");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ContextSetAfterSetBinding_AppliesValue(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new();

        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);
        sut.StringValue.ShouldBeNull();
        sut.BindingContext = viewModel;

        sut.StringValue.ShouldBe("a");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ViewModelChange_UpdatesTarget(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };
        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);

        viewModel.Value = "b";

        sut.StringValue.ShouldBe("b");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelWithoutNotification_AppliesOnlyTheValueAtAttach()
    {
        PlainViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PlainViewModel.Value));

        viewModel.Value = "b";

        sut.StringValue.ShouldBe("a");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void InheritedContext_AppliesAndFollows(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "a" };
        ContainerRuntime parent = new();
        PinRuntime sut = new();
        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);
        parent.AddChild(sut);

        parent.BindingContext = viewModel;
        sut.StringValue.ShouldBe("a");
        viewModel.Value = "b";

        sut.StringValue.ShouldBe("b");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ContextSwap_FollowsNewContextAndIgnoresOld(bool useLambda)
    {
        PinViewModel first = new() { Value = "first" };
        PinViewModel second = new() { Value = "second" };
        PinRuntime sut = new() { BindingContext = first };
        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);

        sut.BindingContext = second;
        sut.StringValue.ShouldBe("second");
        first.Value = "first changed";
        sut.StringValue.ShouldBe("second");
        second.Value = "second changed";

        sut.StringValue.ShouldBe("second changed");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ContextSetToNull_KeepsLastValueAndStopsFollowing(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };
        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);

        sut.BindingContext = null;
        viewModel.Value = "b";

        sut.StringValue.ShouldBe("a");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedPath_ReadsLeafAndFollowsIntermediateReplacement(bool useLambda)
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "a" } };
        PinRuntime sut = new() { BindingContext = viewModel };
        Bind(sut, nameof(PinRuntime.StringValue), "Child.Value", useLambda);
        sut.StringValue.ShouldBe("a");

        viewModel.Child.Value = "b";
        sut.StringValue.ShouldBe("b");
        viewModel.Child = new PinViewModel { Value = "c" };

        sut.StringValue.ShouldBe("c");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedPath_NullIntermediate_SetsTargetToNull(bool useLambda)
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "a" } };
        PinRuntime sut = new() { BindingContext = viewModel };
        Bind(sut, nameof(PinRuntime.StringValue), "Child.Value", useLambda);

        viewModel.Child = null;

        sut.StringValue.ShouldBeNull();
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void PrivateSetterTarget_ReceivesValue(bool useLambda)
    {
        PinViewModel viewModel = new() { Number = 4 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.PrivateSetValue), "Number", useLambda);

        sut.PrivateSetValue.ShouldBe(4);
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadOnlyTarget_ThrowsAtSetBinding(bool useLambda)
    {
        PinRuntime sut = new();

        Should.Throw<InvalidOperationException>(() =>
            Bind(sut, nameof(PinRuntime.ReadOnlyValue), "Number", useLambda));
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void NameThatIsNotAMember_ThrowsAtSetBindingNamingPropertyOrMethod(bool useLambda)
    {
        PinRuntime sut = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            Bind(sut, "NotAMember", "Number", useLambda));

        exception.Message.ShouldBe("PinRuntime has no public property and no method named 'NotAMember', so it " +
            "cannot be the target of a binding. Bind a public property, or a method for a view model event.");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingViewModelProperty_ReportsOnceAndDoesNotThrow(bool useLambda)
    {
        PinRuntime sut = new() { BindingContext = new PlainViewModel() };

        Bind(sut, nameof(PinRuntime.StringValue), "Number", useLambda);
        sut.BindingContext = new PlainViewModel();

        _reports.ShouldBe(new[]
        {
            "Binding path 'Number' for PinRuntime.StringValue does not resolve: PlainViewModel has no property 'Number' (BindingContext: PlainViewModel)."
        });
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void RebindingSameTargetProperty_ReplacesEarlierBinding(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "value", Child = new PinViewModel { Value = "child" } };
        PinRuntime sut = new() { BindingContext = viewModel };
        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda);

        Bind(sut, nameof(PinRuntime.StringValue), "Child.Value", useLambda);
        sut.StringValue.ShouldBe("child");
        viewModel.Value = "value changed";

        sut.StringValue.ShouldBe("child");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ViewModelField_IsRead(bool useLambda)
    {
        PinViewModel viewModel = new() { Field = 9 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.IntValue), "Field", useLambda);

        sut.IntValue.ShouldBe(9);
    }

    #endregion

    #region MustKeep: formats and conversions

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_IsAFormatSpecifierAppliedToTheValue(bool useLambda)
    {
        PinViewModel viewModel = new() { Number = 1234 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Number", useLambda, "D6");

        sut.StringValue.ShouldBe("001234");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_UsesCurrentCulture(bool useLambda)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            PinViewModel viewModel = new() { Ratio = 1234.5 };
            PinRuntime sut = new() { BindingContext = viewModel };

            Bind(sut, nameof(PinRuntime.StringValue), "Ratio", useLambda, "N1");

            sut.StringValue.ShouldBe("1.234,5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_NullValue_SetsNull(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = null };
        PinRuntime sut = new() { StringValue = "start", BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda, "D6");

        sut.StringValue.ShouldBeNull();
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_StringValue_IsUnchanged(bool useLambda)
    {
        PinViewModel viewModel = new() { Value = "abc" };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Value", useLambda, "D6");

        sut.StringValue.ShouldBe("abc");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_NonStringTarget_IsIgnored(bool useLambda)
    {
        PinViewModel viewModel = new() { Number = 12 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.IntValue), "Number", useLambda, "D6");

        sut.IntValue.ShouldBe(12);
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void IntToString_UsesToString(bool useLambda)
    {
        PinViewModel viewModel = new() { Number = 5 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Number", useLambda);

        sut.StringValue.ShouldBe("5");
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void IntToFloat_Converts(bool useLambda)
    {
        PinViewModel viewModel = new() { Number = 3 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.FloatValue), "Number", useLambda);

        sut.FloatValue.ShouldBe(3f);
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void DoubleToFloat_Converts(bool useLambda)
    {
        PinViewModel viewModel = new() { Ratio = 2.5 };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.FloatValue), "Ratio", useLambda);

        sut.FloatValue.ShouldBe(2.5f);
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false, "12.5", 12.5f)]
    [InlineData(true, "12.5", 12.5f)]
    [InlineData(false, "abc", 0f)]
    [InlineData(true, "abc", 0f)]
    public void StringToFloat_ParsesOrSetsZero(bool useLambda, string source, float expected)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            PinViewModel viewModel = new() { Value = source };
            PinRuntime sut = new() { FloatValue = 7, BindingContext = viewModel };

            Bind(sut, nameof(PinRuntime.FloatValue), "Value", useLambda);

            sut.FloatValue.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void NullToValueTypeTarget_SetsDefault(bool useLambda)
    {
        PinViewModel viewModel = new() { Thing = null };
        PinRuntime sut = new() { IntValue = 7, BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.IntValue), "Thing", useLambda);

        sut.IntValue.ShouldBe(0);
    }

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(false)]
    [InlineData(true)]
    public void EnumToEnum_Applies(bool useLambda)
    {
        PinViewModel viewModel = new() { Mode = PinMode.Third };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.ModeValue), "Mode", useLambda);

        sut.ModeValue.ShouldBe(PinMode.Third);
    }

    #endregion

    #region MustKeep: UI to view model

    [Fact]
    [Trait("Pin", MustKeep)]
    public void PushValueToViewModel_WritesFlatProperty()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.Number));

        sut.PushedInt = 5;

        viewModel.Number.ShouldBe(5);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void PushValueToViewModel_WritesNestedLeaf()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel() };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedString), "Child.Value");

        sut.PushedString = "pushed";

        viewModel.Child.Value.ShouldBe("pushed");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void PushValueToViewModel_WritesPrivateViewModelSetter()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.PrivateSet));

        sut.PushedInt = 6;

        viewModel.PrivateSet.ShouldBe(6);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void PushValueToViewModel_WithoutContext_DoesNothing()
    {
        PinRuntime sut = new();
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.Number));

        Should.NotThrow(() => sut.PushedInt = 5);
    }

    #endregion

    #region MustKeep: BindingContext binding

    // Paired Forms test: FrameworkElementBindingTests.SetBinding_BindingContext_SetThenAdd.
    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void BindingContextBinding_SetBeforeAdd_ResolvesAgainstParentContext()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "child" } };
        ContainerRuntime parent = new() { BindingContext = viewModel };
        PinRuntime sut = new();
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));

        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));
        parent.AddChild(sut);

        sut.BindingContext.ShouldBe(viewModel.Child);
        sut.StringValue.ShouldBe("child");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void BindingContextBinding_SetAfterAdd_ResolvesAgainstParentContext()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "child" } };
        ContainerRuntime parent = new() { BindingContext = viewModel };
        PinRuntime sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));

        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));

        sut.BindingContext.ShouldBe(viewModel.Child);
        sut.StringValue.ShouldBe("child");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void BindingContextBinding_ParentContextReplaced_ResolvesAgainstNewParentContext()
    {
        PinViewModel first = new() { Child = new PinViewModel { Value = "first" } };
        PinViewModel second = new() { Child = new PinViewModel { Value = "second" } };
        ContainerRuntime parent = new() { BindingContext = first };
        PinRuntime sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));

        parent.BindingContext = second;

        sut.BindingContext.ShouldBe(second.Child);
        sut.StringValue.ShouldBe("second");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void BindingContextBinding_ParentHasFlatBinding_FollowsOwnerPropertyChange()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "first" }, Value = "parent" };
        PinRuntime parent = new() { BindingContext = viewModel };
        parent.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));
        PinRuntime sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));

        viewModel.Child = new PinViewModel { Value = "replaced" };

        sut.BindingContext.ShouldBe(viewModel.Child);
        sut.StringValue.ShouldBe("replaced");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void BindingContextBinding_PropertyReportsBoundPath()
    {
        PinRuntime sut = new();

        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));

        sut.BindingContextBinding.ShouldBe(nameof(PinViewModel.Child));
    }

    #endregion

    #region MustKeep: view model events bound to methods

    [Theory]
    [Trait("Pin", MustKeep)]
    [InlineData(nameof(PinRuntime.OnRaised))]
    [InlineData("OnRaisedPrivate")]
    public void ViewModelEvent_BoundToOwnMethod_CallsIt(string methodName)
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(methodName, nameof(PinViewModel.Raised));

        viewModel.Raise();

        sut.Hits.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelEvent_BoundToInheritedProtectedMethod_CallsIt()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding("OnRaisedProtected", nameof(PinViewModel.Raised));

        viewModel.Raise();

        sut.InheritedHits.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelEvent_BoundToInheritedPrivateMethod_CallsIt()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new();
        sut.SetBinding("OnRaisedBasePrivate", nameof(PinViewModel.Raised));

        sut.BindingContext = viewModel;
        viewModel.Raise();

        sut.InheritedHits.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelEvent_ContextSetAfterSetBinding_CallsMethod()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new();
        sut.SetBinding(nameof(PinRuntime.OnRaised), nameof(PinViewModel.Raised));

        sut.BindingContext = viewModel;
        viewModel.Raise();

        sut.Hits.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelEvent_OldNotifyingContext_IsUnsubscribed()
    {
        PinViewModel first = new();
        PinRuntime sut = new() { BindingContext = first };
        sut.SetBinding(nameof(PinRuntime.OnRaised), nameof(PinViewModel.Raised));

        sut.BindingContext = new PinViewModel();
        first.Raise();

        sut.Hits.ShouldBe(0);
        first.HasRaisedSubscribers.ShouldBeFalse();
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelEvent_DottedPath_Throws()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel() };
        PinRuntime sut = new() { BindingContext = viewModel };

        Should.Throw<NotSupportedException>(() => sut.SetBinding(nameof(PinRuntime.OnRaised), "Child.Raised"));
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void MethodTarget_ViewModelMemberIsProperty_ThrowsWhenContextArrives()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new();
        sut.SetBinding(nameof(PinRuntime.OnRaised), nameof(PinViewModel.Number));

        Should.Throw<Exception>(() => sut.BindingContext = viewModel);
    }

    #endregion

    #region MustKeep: a visual that is a Forms control's Visual

    [Fact]
    [Trait("Pin", MustKeep)]
    public void FormsVisual_VisualAndControlBindingsOnDifferentProperties_BothFollow()
    {
        PinViewModel viewModel = new() { Amount = 10, Flag = true };
        Button button = new() { BindingContext = viewModel };
        button.Visual.SetBinding(nameof(GraphicalUiElement.Width), nameof(PinViewModel.Amount));
        button.SetBinding(nameof(Button.IsEnabled), nameof(PinViewModel.Flag));

        viewModel.Amount = 20;
        viewModel.Flag = false;

        button.Visual.Width.ShouldBe(20);
        button.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void FormsVisual_SamePropertyBoundOnVisualAndControl_LastSourceChangeWins()
    {
        PinViewModel viewModel = new() { Amount = 10, Ratio = 30 };
        Button button = new() { BindingContext = viewModel };
        button.Visual.SetBinding(nameof(GraphicalUiElement.Width), nameof(PinViewModel.Amount));
        button.SetBinding(nameof(Button.Width), new Binding(nameof(PinViewModel.Ratio)) { Mode = BindingMode.OneWay });
        button.Visual.Width.ShouldBe(30);

        viewModel.Amount = 15;
        button.Visual.Width.ShouldBe(15);
        viewModel.Ratio = 40;

        button.Visual.Width.ShouldBe(40);
    }

    #endregion

    #region WillMatchForms

    // A visual keeps its own default mode (VM to UI only): its property changes come from states, SetProperty and
    // localization, never user input, so they must not reach the view model.
    [Fact]
    [Trait("Pin", MustKeep)]
    public void DefaultMode_TargetRaisesPropertyChanged_DoesNotWriteViewModel()
    {
        PinViewModel viewModel = new() { Number = 1 };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.NotifyingInt), nameof(PinViewModel.Number));

        sut.NotifyingInt = 5;

        viewModel.Number.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ApplyState_BoundText_LeavesViewModel()
    {
        PinViewModel viewModel = new() { Value = "fromVm" };
        TextRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(TextRuntime.Text), nameof(PinViewModel.Value));
        Gum.DataTypes.Variables.StateSave state = new();
        state.Variables.Add(new Gum.DataTypes.Variables.VariableSave
        {
            Name = "Text", Value = "fromState", SetsValue = true, Type = "string"
        });

        sut.ApplyState(state);

        sut.Text.ShouldBe("fromState");
        viewModel.Value.ShouldBe("fromVm");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void SetProperty_BoundText_LeavesViewModel()
    {
        PinViewModel viewModel = new() { Value = "fromVm" };
        TextRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(TextRuntime.Text), nameof(PinViewModel.Value));

        sut.SetProperty("Text", "fromSetProperty");

        viewModel.Value.ShouldBe("fromVm");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void ColorAssignment_BoundRedChannel_LeavesViewModel()
    {
        PinViewModel viewModel = new() { Number = 100 };
        SpriteRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(SpriteRuntime.Red), nameof(PinViewModel.Number));

        sut.Color = new Microsoft.Xna.Framework.Color(7, 8, 9, 255);

        viewModel.Number.ShouldBe(100);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void LocalizationRefresh_BoundText_KeepsViewModelKey()
    {
        Gum.Localization.LocalizationService localization = new();
        localization.AddDatabase(new Dictionary<string, string[]> { { "T_Hi", new[] { "Hello", "Bonjour" } } },
            new List<string> { "English", "French" });
        localization.CurrentLanguage = 0;
        Gum.Localization.ILocalizationService? previousService = CustomSetPropertyOnRenderable.LocalizationService;
        Action<GraphicalUiElement>? previousRefresh = GraphicalUiElement.RefreshLocalizationOnElementAction;
        CustomSetPropertyOnRenderable.LocalizationService = localization;
        GraphicalUiElement.RefreshLocalizationOnElementAction = element =>
        {
            string? key = CustomSetPropertyOnRenderable.TryGetLocalizationKey(element);
            if (key != null)
            {
                element.SetProperty("Text", key);
            }
        };
        try
        {
            PinViewModel viewModel = new() { Value = "T_Hi" };
            TextRuntime sut = new() { BindingContext = viewModel };
            sut.SetBinding(nameof(TextRuntime.Text), nameof(PinViewModel.Value));

            localization.CurrentLanguage = 1;
            sut.RefreshLocalization();

            viewModel.Value.ShouldBe("T_Hi");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = previousService;
            GraphicalUiElement.RefreshLocalizationOnElementAction = previousRefresh;
        }
    }

    // An explicit TwoWay on a visual writes every change, whatever made it.
    [Fact]
    public void ExplicitTwoWay_ApplyState_WritesViewModel()
    {
        PinViewModel viewModel = new() { Value = "fromVm" };
        TextRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(TextRuntime.Text), new Binding(nameof(PinViewModel.Value)) { Mode = BindingMode.TwoWay });
        Gum.DataTypes.Variables.StateSave state = new();
        state.Variables.Add(new Gum.DataTypes.Variables.VariableSave
        {
            Name = "Text", Value = "fromState", SetsValue = true, Type = "string"
        });

        sut.ApplyState(state);

        viewModel.Value.ShouldBe("fromState");
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_DefaultMode_TargetRaisesPropertyChanged_WritesViewModel()
    {
        PinViewModel viewModel = new() { Number = 1 };
        PinControl sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinControl.NotifyingInt), nameof(PinViewModel.Number));

        sut.NotifyingInt = 5;

        viewModel.Number.ShouldBe(5);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void TwoTargetPropertiesBoundToOneViewModelProperty_BothFollow()
    {
        PinViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PinViewModel.Value));
        sut.SetBinding(nameof(PinRuntime.PushedString), nameof(PinViewModel.Value));

        viewModel.Value = "b";

        sut.StringValue.ShouldBe("b");
        sut.PushedString.ShouldBe("b");
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_TwoTargetPropertiesBoundToOneViewModelProperty_BothFollow()
    {
        PinViewModel viewModel = new() { Value = "a", Number = 1 };
        PinControl sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinControl.StringValue), nameof(PinViewModel.Number));
        sut.SetBinding(nameof(PinControl.IntValue), nameof(PinViewModel.Number));

        viewModel.Number = 2;

        sut.StringValue.ShouldBe("2");
        sut.IntValue.ShouldBe(2);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void MissingViewModelProperty_SetsTargetToDefault()
    {
        PinRuntime sut = new() { IntValue = 5, BindingContext = new PlainViewModel() };

        sut.SetBinding(nameof(PinRuntime.IntValue), "Missing");

        sut.IntValue.ShouldBe(0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_MissingViewModelProperty_SetsTargetToDefault()
    {
        PinControl sut = new() { IntValue = 5, BindingContext = new PlainViewModel() };

        sut.SetBinding(nameof(PinControl.IntValue), "Missing");

        sut.IntValue.ShouldBe(0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void ReadOnlyTarget_MessageNamesTheMode()
    {
        PinRuntime sut = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            sut.SetBinding(nameof(PinRuntime.ReadOnlyValue), nameof(PinViewModel.Number)));

        exception.Message.ShouldBe("PinRuntime.ReadOnlyValue has no setter, so it cannot be the target of a " +
            "OneWay binding, which writes the view model's value to it. Bind a property that has a public setter.");
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_ReadOnlyTarget_MessageNamesTheMode()
    {
        Button sut = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            sut.SetBinding(nameof(Button.ActualWidth), nameof(PinViewModel.Number)));

        exception.Message.ShouldBe("Button.ActualWidth has no setter, so it cannot be the target of a " +
            "TwoWay binding, which writes the view model's value to it. Bind a property that has a public setter.");
    }

    [Theory]
    [Trait("Pin", WillMatchForms)]
    [InlineData(false)]
    [InlineData(true)]
    public void ToStringFormat_ValueThatIsNotANumber_UsesItsText(bool useLambda)
    {
        PinViewModel viewModel = new() { Flag = true, Thing = new PinThing() };
        PinRuntime sut = new() { BindingContext = viewModel };

        Bind(sut, nameof(PinRuntime.StringValue), "Flag", useLambda, "D6");
        sut.StringValue.ShouldBe("True");
        Bind(sut, nameof(PinRuntime.StringValue), "Thing", useLambda, "D6");

        sut.StringValue.ShouldBe("thing");
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_StringFormat_ValueThatIsNotANumber_UsesItsText()
    {
        PinViewModel viewModel = new() { Flag = true, Thing = new PinThing() };
        PinControl sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.StringValue), new Binding(nameof(PinViewModel.Flag)) { StringFormat = "{0:D6}" });
        sut.StringValue.ShouldBe("True");
        sut.SetBinding(nameof(PinControl.StringValue), new Binding(nameof(PinViewModel.Thing)) { StringFormat = "{0:D6}" });

        sut.StringValue.ShouldBe("thing");
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void InvalidStringToInt_SetsDefault()
    {
        PinViewModel viewModel = new() { Value = "abc" };
        PinRuntime sut = new() { IntValue = 7, BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.IntValue), nameof(PinViewModel.Value));

        sut.IntValue.ShouldBe(0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_InvalidStringToInt_SetsDefault()
    {
        PinViewModel viewModel = new() { Value = "abc" };
        PinControl sut = new() { IntValue = 7, BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.IntValue), nameof(PinViewModel.Value));

        sut.IntValue.ShouldBe(0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void DecimalToInt_Rounds()
    {
        PinViewModel viewModel = new() { Price = 2.7m };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.IntValue), nameof(PinViewModel.Price));

        sut.IntValue.ShouldBe(3);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_DecimalToInt_Rounds()
    {
        PinViewModel viewModel = new() { Price = 2.7m };
        PinControl sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.IntValue), nameof(PinViewModel.Price));

        sut.IntValue.ShouldBe(3);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void IntAboveByteRange_SetsDefault()
    {
        PinViewModel viewModel = new() { Number = 300 };
        PinRuntime sut = new() { ByteValue = 7, BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.ByteValue), nameof(PinViewModel.Number));

        sut.ByteValue.ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_IntAboveByteRange_SetsDefault()
    {
        PinViewModel viewModel = new() { Number = 300 };
        PinControl sut = new() { ByteValue = 7, BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.ByteValue), nameof(PinViewModel.Number));

        sut.ByteValue.ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void StringToBool_SetsDefault()
    {
        PinViewModel viewModel = new() { Value = "yes" };
        PinRuntime sut = new() { BoolValue = true, BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.BoolValue), nameof(PinViewModel.Value));

        sut.BoolValue.ShouldBeFalse();
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_StringToBool_SetsDefault()
    {
        PinViewModel viewModel = new() { Value = "yes" };
        PinControl sut = new() { BoolValue = true, BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.BoolValue), nameof(PinViewModel.Value));

        sut.BoolValue.ShouldBeFalse();
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void PushValueToViewModel_GetOnlyViewModelProperty_IgnoresTargetChange()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.GetOnly));
        sut.PushedInt.ShouldBe(42);

        sut.PushedInt = 5;

        viewModel.GetOnly.ShouldBe(42);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_TwoWayToGetOnlyViewModelProperty_IgnoresTargetChange()
    {
        PinViewModel viewModel = new();
        PinControl sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinControl.NotifyingInt), nameof(PinViewModel.GetOnly));

        sut.NotifyingInt = 5;

        viewModel.GetOnly.ShouldBe(42);
    }

    // A view model field is read but never written, on a visual and on a Forms control.
    [Fact]
    [Trait("Pin", MustKeep)]
    public void PushValueToViewModel_ViewModelField_IsNotWritten()
    {
        PinViewModel viewModel = new() { Field = 1 };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.Field));

        sut.PushedInt = 5;

        viewModel.Field.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void Forms_TwoWayToViewModelField_DoesNotWriteIt()
    {
        PinViewModel viewModel = new() { Field = 1 };
        PinControl sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinControl.NotifyingInt), nameof(PinViewModel.Field));

        sut.NotifyingInt = 5;

        viewModel.Field.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void PushValueToViewModel_IndexedLeaf_WritesThatItem()
    {
        PinViewModel viewModel = new();
        viewModel.Items.Add("first");
        viewModel.Items.Add("second");
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedString), "Items[0]");

        sut.PushedString = "rewritten";

        viewModel.Items[0].ShouldBe("rewritten");
        viewModel.Items[1].ShouldBe("second");
    }

    // Reading a view model field was visual-only (ViewModelField_IsRead); the shared engine keeps it.
    [Fact]
    [Trait("Pin", "FormsGainsVisualCapability")]
    public void Forms_ViewModelField_IsRead()
    {
        PinViewModel viewModel = new() { Field = 9 };
        PinControl sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.IntValue), nameof(PinViewModel.Field));

        sut.IntValue.ShouldBe(9);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void BindingContextBinding_ParentWithoutFlatBinding_FollowsOwnerPropertyChange()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "first" } };
        ContainerRuntime parent = new() { BindingContext = viewModel };
        PinRuntime sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(PinViewModel.Child));

        viewModel.Child = new PinViewModel { Value = "replaced" };

        sut.BindingContext.ShouldBe(viewModel.Child);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_BindingContextBinding_FollowsOwnerPropertyChange()
    {
        PinViewModel viewModel = new() { Child = new PinViewModel { Value = "first" } };
        StackPanel parent = new() { BindingContext = viewModel };
        PinControl sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(FrameworkElement.BindingContext), nameof(PinViewModel.Child));

        viewModel.Child = new PinViewModel { Value = "replaced" };

        sut.BindingContext.ShouldBe(viewModel.Child);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void ViewModelEvent_AfterNotifyingContextSwap_IsBoundToNewContext()
    {
        PinViewModel first = new();
        PinViewModel second = new();
        PinRuntime sut = new() { BindingContext = first };
        sut.SetBinding(nameof(PinRuntime.OnRaised), nameof(PinViewModel.Raised));

        sut.BindingContext = second;
        second.Raise();

        sut.Hits.ShouldBe(1);
    }

    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void ViewModelEvent_OldPlainContext_IsUnsubscribed()
    {
        PlainViewModel first = new();
        PinRuntime sut = new() { BindingContext = first };
        sut.SetBinding(nameof(PinRuntime.OnRaised), nameof(PlainViewModel.Raised));

        sut.BindingContext = new PlainViewModel();
        first.Raise();

        sut.Hits.ShouldBe(0);
        first.HasRaisedSubscribers.ShouldBeFalse();
    }

    // A Forms control binds a view model event to a method through the same engine (setbinding.md).
    [Fact]
    [Trait("Pin", "FormsGainsVisualCapability")]
    public void Forms_ViewModelEvent_BoundToPrivateMethod_CallsItAndFollowsContextSwap()
    {
        PinViewModel first = new();
        PinViewModel second = new();
        EventControl sut = new() { BindingContext = first };
        sut.SetBinding("OnRaised", nameof(PinViewModel.Raised));

        first.Raise();
        sut.BindingContext = second;
        first.Raise();
        second.Raise();

        sut.Hits.ShouldBe(2);
        first.HasRaisedSubscribers.ShouldBeFalse();
    }

    public class EventControl : FrameworkElement
    {
        public EventControl() : base(new InteractiveGue())
        {
        }

        public int Hits;
        private void OnRaised(object? sender, EventArgs e) => Hits++;
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void FormsVisual_ControlBindingChangesSharedProperty_VisualBindingDoesNotWriteItsSource()
    {
        PinViewModel viewModel = new() { Value = "visual", Child = new PinViewModel { Value = "control" } };
        Label label = new() { BindingContext = viewModel };
        TextRuntime text = (TextRuntime)label.Visual;
        text.SetBinding(nameof(TextRuntime.Text), nameof(PinViewModel.Value));
        label.SetBinding(nameof(Label.Text), new Binding("Child.Value") { Mode = BindingMode.OneWay });
        text.Text.ShouldBe("control");

        viewModel.Child.Value = "control changed";

        text.Text.ShouldBe("control changed");
        viewModel.Value.ShouldBe("visual");
    }

    // A null or empty PropertyChanged name re-reads every binding on both engines (#8539).
    [Fact]
    [Trait("Pin", MustKeep)]
    public void ViewModelRaisesEmptyPropertyName_TargetIsRefreshed()
    {
        PlainNotifyingViewModel viewModel = new() { Value = "a" };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.StringValue), nameof(PlainNotifyingViewModel.Value));

        viewModel.SetSilently("b");
        viewModel.RaiseAll(string.Empty);

        sut.StringValue.ShouldBe("b");
    }

    [Fact]
    [Trait("Pin", MustKeep)]
    public void Forms_ViewModelRaisesEmptyPropertyName_TargetIsRefreshed()
    {
        PlainNotifyingViewModel viewModel = new() { Value = "a" };
        PinControl sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinControl.StringValue), nameof(PlainNotifyingViewModel.Value));

        viewModel.SetSilently("b");
        viewModel.RaiseAll(string.Empty);

        sut.StringValue.ShouldBe("b");
    }

    #region Diagnostics for what used to throw

    // The old visual engine threw here; the shared engine shows the default and reports a [Gum] warning once.
    [Fact]
    public void InvalidStringToInt_ReportsOncePerBinding()
    {
        PinViewModel viewModel = new() { Value = "abc" };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.IntValue), nameof(PinViewModel.Value));

        viewModel.Value = "def";

        _reports.ShouldBe(new[]
        {
            "Binding for PinRuntime.IntValue could not convert the value 'abc' (String) to Int32, so the property shows its default."
        });
    }

    [Fact]
    public void Forms_InvalidStringToInt_ReportsOncePerBinding()
    {
        PinViewModel viewModel = new() { Value = "abc" };
        PinControl sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinControl.IntValue), new Binding(nameof(PinViewModel.Value)) { FallbackValue = 4 });

        sut.IntValue.ShouldBe(4);
        _reports.ShouldBe(new[]
        {
            "Binding for PinControl.IntValue could not convert the value 'abc' (String) to Int32, so the property shows its FallbackValue."
        });
    }

    [Fact]
    public void StringToBoolAndIntAboveByteRange_Report()
    {
        PinViewModel viewModel = new() { Value = "yes", Number = 300 };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.BoolValue), nameof(PinViewModel.Value));
        sut.SetBinding(nameof(PinRuntime.ByteValue), nameof(PinViewModel.Number));

        _reports.ShouldBe(new[]
        {
            "Binding for PinRuntime.BoolValue could not convert the value 'yes' (String) to Boolean, so the property shows its default.",
            "Binding for PinRuntime.ByteValue could not convert the value '300' (Int32) to Byte, so the property shows its default."
        });
    }

    [Fact]
    public void NullToValueTypeTarget_DoesNotReport()
    {
        PinViewModel viewModel = new() { Thing = null };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.IntValue), nameof(PinViewModel.Thing));

        _reports.ShouldBeEmpty();
    }

    // The old visual engine threw here; the shared engine leaves the source alone and reports a [Gum] warning once.
    [Fact]
    public void PushValueToViewModel_GetOnlyViewModelProperty_ReportsOncePerBinding()
    {
        PinViewModel viewModel = new();
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.GetOnly));

        sut.PushedInt = 5;
        sut.PushedInt = 6;

        _reports.ShouldBe(new[]
        {
            "PinRuntime.PushedInt pushed its value, but PinViewModel.GetOnly has no setter, so the view model was not changed."
        });
    }

    [Fact]
    public void PushValueToViewModel_ViewModelField_DoesNotReport()
    {
        PinViewModel viewModel = new() { Field = 1 };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), nameof(PinViewModel.Field));

        sut.PushedInt = 5;

        _reports.ShouldBeEmpty();
    }

    #endregion

    #region SetBinding(string, Binding) on a visual

    public class NegateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) => !(bool)value!;
        public object? ConvertBack(object? value, Type targetType, object? parameter) => !(bool)value!;
    }

    [Fact]
    public void BindingObject_ExplicitTwoWay_WritesTargetChanges()
    {
        PinViewModel viewModel = new() { Number = 1 };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.NotifyingInt), new Binding(nameof(PinViewModel.Number)) { Mode = BindingMode.TwoWay });

        sut.NotifyingInt = 5;

        viewModel.Number.ShouldBe(5);
    }

    [Fact]
    public void BindingObject_ExplicitOneWay_IgnoresPushValueToViewModel()
    {
        PinViewModel viewModel = new() { Number = 1 };
        PinRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(PinRuntime.PushedInt), new Binding(nameof(PinViewModel.Number)) { Mode = BindingMode.OneWay });

        sut.PushedInt = 5;
        viewModel.Number.ShouldBe(1);
        viewModel.Number = 7;

        sut.PushedInt.ShouldBe(7);
    }

    [Fact]
    public void BindingObject_OneWayToSource_WritesTargetToSource()
    {
        PinViewModel viewModel = new() { Number = 1 };
        PinRuntime sut = new() { NotifyingInt = 3, BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.NotifyingInt),
            new Binding(nameof(PinViewModel.Number)) { Mode = BindingMode.OneWayToSource });
        viewModel.Number.ShouldBe(3);
        sut.NotifyingInt = 4;
        viewModel.Number.ShouldBe(4);
        viewModel.Number = 9;

        sut.NotifyingInt.ShouldBe(4);
    }

    [Fact]
    public void BindingObject_Converter_Applies()
    {
        PinViewModel viewModel = new() { Flag = true };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.BoolValue),
            new Binding(nameof(PinViewModel.Flag)) { Converter = new NegateConverter() });

        sut.BoolValue.ShouldBeFalse();
        viewModel.Flag = false;
        sut.BoolValue.ShouldBeTrue();
    }

    [Fact]
    public void BindingObject_StringFormat_FormatsValue()
    {
        PinViewModel viewModel = new() { Number = 5 };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.StringValue),
            new Binding(nameof(PinViewModel.Number)) { StringFormat = "Gold: {0}" });

        sut.StringValue.ShouldBe("Gold: 5");
    }

    [Fact]
    public void BindingObject_FallbackAndTargetNullValue_Apply()
    {
        PinViewModel viewModel = new() { Value = null };
        PinRuntime sut = new() { BindingContext = viewModel };

        sut.SetBinding(nameof(PinRuntime.StringValue),
            new Binding(nameof(PinViewModel.Value)) { TargetNullValue = "none" });
        sut.StringValue.ShouldBe("none");
        sut.SetBinding(nameof(PinRuntime.IntValue), new Binding("Missing") { FallbackValue = 12 });

        sut.IntValue.ShouldBe(12);
    }

    public class ButtonWidthViewModel : ViewModel
    {
        public float ButtonWidth { get => Get<float>(); set => Set(value); }
    }

    // The sample in binding-deep-dive.md, "Binding Visual Properties Directly".
    [Fact]
    public void DocsSample_VisualBindingObject_FollowsViewModel()
    {
        ButtonWidthViewModel viewModel = new() { ButtonWidth = 150 };
        Button MyButton = new() { BindingContext = viewModel };

        var buttonVisual = MyButton.Visual;
        buttonVisual.SetBinding(
            nameof(buttonVisual.Width),
            new Binding(nameof(ButtonWidthViewModel.ButtonWidth))
            {
                Mode = BindingMode.OneWay,
                FallbackValue = 100f
            });

        buttonVisual.Width.ShouldBe(150);
        viewModel.ButtonWidth = 175;
        buttonVisual.Width.ShouldBe(175);
    }

    #endregion

    // Pair for PushValueToViewModel_IndexedLeaf_WritesThatItem.
    [Fact]
    [Trait("Pin", WillMatchForms)]
    public void Forms_TwoWayIndexedLeaf_WritesOnlyThatSlot()
    {
        PinViewModel viewModel = new();
        viewModel.Items.Add("first");
        viewModel.Items.Add("second");
        Label sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(Label.Text), "Items[0]");

        sut.Text = "rewritten";

        viewModel.Items[0].ShouldBe("rewritten");
        viewModel.Items[1].ShouldBe("second");
    }

    public class PlainNotifyingViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string? Value { get; set; }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public void SetSilently(string value) => Value = value;
        public void RaiseAll(string? name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    #endregion
}
