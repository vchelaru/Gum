using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.VisualClearBinding;

public class GraphicalUiElementClearBindingTests
{
    public enum BindingKind
    {
        StringPath,
        Lambda,
        BindingInstance
    }

    public class ClearViewModel : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _propertyChanged;
        private string? _name;
        private float _size;
        private ClearViewModel? _child;

        public ClearViewModel? Child
        {
            get => _child;
            set { _child = value; Notify(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        public int PropertyChangedSubscriberCount => _propertyChanged?.GetInvocationList().Length ?? 0;

        public string? Name
        {
            get => _name;
            set { _name = value; Notify(); }
        }

        public float Size
        {
            get => _size;
            set { _size = value; Notify(); }
        }

        public event EventHandler? Raised;
        public void Raise() => Raised?.Invoke(this, EventArgs.Empty);
        public bool HasRaisedSubscribers => Raised != null;

        private void Notify([CallerMemberName] string? name = null) =>
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClearRuntime : ContainerRuntime
    {
        public string? Label { get; set; }
        public int Hits;
        public void OnRaised(object? sender, EventArgs e) => Hits++;
    }

    [Theory]
    [InlineData(BindingKind.StringPath)]
    [InlineData(BindingKind.Lambda)]
    [InlineData(BindingKind.BindingInstance)]
    public void ClearBinding_RestoresPreBindingValue_StopsFollowingViewModel(BindingKind kind)
    {
        ClearViewModel viewModel = new() { Name = "bound" };
        ClearRuntime sut = new() { Label = "before", BindingContext = viewModel };
        switch (kind)
        {
            case BindingKind.StringPath:
                sut.SetBinding(nameof(ClearRuntime.Label), nameof(ClearViewModel.Name));
                break;
            case BindingKind.Lambda:
                sut.SetBinding<ClearViewModel>(nameof(ClearRuntime.Label), vm => vm.Name);
                break;
            case BindingKind.BindingInstance:
                sut.SetBinding(nameof(ClearRuntime.Label), new Binding(nameof(ClearViewModel.Name)));
                break;
        }
        sut.Label.ShouldBe("bound");

        sut.ClearBinding(nameof(ClearRuntime.Label));
        viewModel.Name = "changed";

        sut.Label.ShouldBe("before");
        sut.IsDataBound(nameof(ClearRuntime.Label)).ShouldBeFalse();
        viewModel.PropertyChangedSubscriberCount.ShouldBe(0);
    }

    [Fact]
    public void ClearBinding_PropertyNotBound_DoesNothing()
    {
        ClearViewModel viewModel = new() { Size = 123 };
        ClearRuntime sut = new() { Width = 50, Label = "unbound", BindingContext = viewModel };
        sut.SetBinding(nameof(ClearRuntime.Width), nameof(ClearViewModel.Size));

        Should.NotThrow(() => sut.ClearBinding(nameof(ClearRuntime.Label)));
        Should.NotThrow(() => new ClearRuntime().ClearBinding(nameof(ClearRuntime.Label)));

        sut.Label.ShouldBe("unbound");
        sut.Width.ShouldBe(123);
    }

    [Fact]
    public void ClearBinding_OneOfTwoBindings_LeavesOtherFollowingViewModel()
    {
        ClearViewModel viewModel = new() { Name = "bound", Size = 123 };
        ClearRuntime sut = new() { Width = 50, Label = "before", BindingContext = viewModel };
        sut.SetBinding(nameof(ClearRuntime.Label), nameof(ClearViewModel.Name));
        sut.SetBinding(nameof(ClearRuntime.Width), nameof(ClearViewModel.Size));

        sut.ClearBinding(nameof(ClearRuntime.Label));
        viewModel.Name = "changed";
        viewModel.Size = 7;

        sut.Label.ShouldBe("before");
        sut.Width.ShouldBe(7);
    }

    [Fact]
    public void ClearBinding_EventToMethod_UnsubscribesFromViewModelEvent()
    {
        ClearViewModel viewModel = new();
        ClearRuntime sut = new() { BindingContext = viewModel };
        sut.SetBinding(nameof(ClearRuntime.OnRaised), nameof(ClearViewModel.Raised));
        viewModel.HasRaisedSubscribers.ShouldBeTrue();

        sut.ClearBinding(nameof(ClearRuntime.OnRaised));
        viewModel.Raise();

        viewModel.HasRaisedSubscribers.ShouldBeFalse();
        sut.Hits.ShouldBe(0);
    }

    [Fact]
    public void ClearBinding_BoundBindingContext_InheritsParentContextAgain()
    {
        ClearViewModel child = new();
        ClearViewModel viewModel = new() { Child = child };
        ContainerRuntime parent = new() { BindingContext = viewModel };
        ClearRuntime sut = new();
        parent.Children.Add(sut);
        sut.SetBinding(nameof(ClearRuntime.BindingContext), nameof(ClearViewModel.Child));
        sut.BindingContext.ShouldBe(child);

        sut.ClearBinding(nameof(ClearRuntime.BindingContext));
        sut.BindingContext.ShouldBe(viewModel);

        ClearViewModel second = new();
        parent.BindingContext = second;
        sut.BindingContext.ShouldBe(second);
    }

    public class PushingRuntime : ContainerRuntime
    {
        private string? _label;

        public string? Label
        {
            get => _label;
            set
            {
                _label = value;
                PushValueToViewModel();
            }
        }
    }

    [Fact]
    public void ClearBinding_PushingProperty_DoesNotWriteRestoredValueToViewModel()
    {
        ClearViewModel viewModel = new() { Name = "bound" };
        PushingRuntime sut = new() { Label = "before", BindingContext = viewModel };
        sut.SetBinding(nameof(PushingRuntime.Label), nameof(ClearViewModel.Name));
        sut.Label = "pushed";
        viewModel.Name.ShouldBe("pushed");

        sut.ClearBinding(nameof(PushingRuntime.Label));

        sut.Label.ShouldBe("before");
        viewModel.Name.ShouldBe("pushed");
    }
}
