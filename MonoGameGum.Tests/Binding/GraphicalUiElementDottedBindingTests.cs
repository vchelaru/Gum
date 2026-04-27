using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.DottedBinding;

public class GraphicalUiElementDottedBindingTests
{
    [Fact]
    public void PushValueToViewModel_DottedPath_NullIntermediate_DoesNotThrow()
    {
        // Arrange
        BindableGueDerived sut = new();
        ParentViewModel viewModel = new();
        sut.BindingContext = viewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), "TestViewModel.IntPropertyOnVm");

        viewModel.TestViewModel = null;

        // Act / Assert: pushing the value should not throw when intermediate is null
        Should.NotThrow(() => sut.IntPropertyOnGue = 7);

        // After re-attaching a fresh non-null intermediate, the leaf must remain at its
        // default value — the failed push must not have leaked into a hidden cache.
        TestViewModel replacement = new();
        viewModel.TestViewModel = replacement;
        replacement.IntPropertyOnVm.ShouldBe(0);
    }

    [Fact]
    public void PushValueToViewModel_DottedPath_IndexedLeaf_DoesNotMutate()
    {
        // Arrange: bind a UI property to a path whose leaf segment is itself indexed
        // ("Items[0]"). Write-back through an indexed leaf is intentionally a non-goal —
        // pushing the UI value must silently no-op, never throw, and never corrupt the
        // underlying collection.
        TextRuntime sut = new();
        IndexedLeafViewModel viewModel = new();
        viewModel.Names.Add("first");
        viewModel.Names.Add("second");
        sut.BindingContext = viewModel;
        sut.SetBinding(nameof(sut.Text), "Names[0]");
        sut.Text.ShouldBe("first");

        // Act / Assert: pushing to the UI property must not mutate the list slot.
        Should.NotThrow(() => sut.Text = "rewritten");
        viewModel.Names[0].ShouldBe("first");
        viewModel.Names[1].ShouldBe("second");
    }

    [Fact]
    public void SetBinding_DottedPath_Overwrite_DoesNotLeakPriorObserver()
    {
        // Arrange: bind Text to a dotted path, then overwrite that binding with another.
        // Mutating the originally-bound value must NOT update the UI through the dead binding.
        TextRuntime sut = new();
        ChildAndOtherViewModel viewModel = new();
        viewModel.Child!.Name = "original-child";
        viewModel.Other!.Name = "other-name";
        sut.BindingContext = viewModel;

        sut.SetBinding(nameof(sut.Text), "Child.Name");
        sut.Text.ShouldBe("original-child");

        // Act: overwrite with a new dotted binding for the same UI property
        sut.SetBinding(nameof(sut.Text), "Other.Name");
        sut.Text.ShouldBe("other-name");

        // Now mutate the originally-bound child — UI must NOT update.
        viewModel.Child.Name = "mutated";

        // Assert: text reflects the second binding's source, not the orphaned one.
        sut.Text.ShouldBe("other-name");
    }

    [Fact]
    public void SetBinding_DottedPath_OnEvent_NoBindingContext_ThrowsWhenContextAssigned()
    {
        // Arrange: set up a dotted event binding before BindingContext is assigned.
        // Because we have no context to walk, SetBinding can't determine the leaf is
        // an event yet. The throw is deferred until BindingContext arrives.
        EventBindableGueDerived sut = new();
        sut.SetBinding(nameof(sut.OnSomeEvent), "Child.SomeEvent");

        EventParentViewModel viewModel = new();

        // Act / Assert
        Should.Throw<NotSupportedException>(() => sut.BindingContext = viewModel);
    }

    [Fact]
    public void PushValueToViewModel_DottedPath_WritesLeaf()
    {
        // Arrange
        BindableGueDerived sut = new();
        ParentViewModel viewModel = new();
        sut.BindingContext = viewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), "TestViewModel.IntPropertyOnVm");

        // Act
        sut.IntPropertyOnGue = 99;

        // Assert
        viewModel.TestViewModel!.IntPropertyOnVm.ShouldBe(99);
    }

    [Fact]
    public void SetBinding_DottedPath_OnEvent_Throws()
    {
        // Arrange
        EventBindableGueDerived sut = new();
        EventParentViewModel viewModel = new();
        sut.BindingContext = viewModel;

        // Act / Assert
        Should.Throw<NotSupportedException>(() =>
            sut.SetBinding(nameof(sut.OnSomeEvent), "Child.SomeEvent"));
    }

    [Fact]
    public void SetBinding_DottedPath_ReadsLeafValue()
    {
        // Arrange
        TextRuntime sut = new();
        ParentViewModel viewModel = new();
        viewModel.TestViewModel!.StringValue = "hello";
        sut.SetBinding(nameof(sut.Text), "TestViewModel.StringValue");

        // Act
        sut.BindingContext = viewModel;

        // Assert
        sut.Text.ShouldBe("hello");
    }

    [Fact]
    public void SetBinding_DottedPath_RebindsOnBindingContextChange()
    {
        // Arrange
        TextRuntime sut = new();
        ParentViewModel firstViewModel = new();
        firstViewModel.TestViewModel!.StringValue = "first";
        ParentViewModel secondViewModel = new();
        secondViewModel.TestViewModel!.StringValue = "second";

        sut.SetBinding(nameof(sut.Text), "TestViewModel.StringValue");
        sut.BindingContext = firstViewModel;
        sut.Text.ShouldBe("first");

        // Act
        sut.BindingContext = secondViewModel;

        // Assert
        sut.Text.ShouldBe("second");
    }

    [Fact]
    public void SetBinding_DottedPath_UpdatesWhenIntermediateReassigned()
    {
        // Arrange
        TextRuntime sut = new();
        ParentViewModel viewModel = new();
        viewModel.TestViewModel!.StringValue = "original";
        sut.SetBinding(nameof(sut.Text), "TestViewModel.StringValue");
        sut.BindingContext = viewModel;
        sut.Text.ShouldBe("original");

        // Act
        TestViewModel replacement = new();
        replacement.StringValue = "replaced";
        viewModel.TestViewModel = replacement;

        // Assert
        sut.Text.ShouldBe("replaced");
    }

    [Fact]
    public void SetBinding_DottedPath_UpdatesWhenLeafChanges()
    {
        // Arrange
        TextRuntime sut = new();
        ParentViewModel viewModel = new();
        viewModel.TestViewModel!.StringValue = "before";
        sut.SetBinding(nameof(sut.Text), "TestViewModel.StringValue");
        sut.BindingContext = viewModel;
        sut.Text.ShouldBe("before");

        // Act
        viewModel.TestViewModel.StringValue = "after";

        // Assert
        sut.Text.ShouldBe("after");
    }

    [Fact]
    public void SetBinding_FlatPath_StillWorks()
    {
        // Arrange
        BindableGueDerived sut = new();
        TestViewModel viewModel = new();
        sut.BindingContext = viewModel;
        sut.SetBinding(nameof(sut.IntPropertyOnGue), nameof(TestViewModel.IntPropertyOnVm));

        // Act
        viewModel.IntPropertyOnVm = 42;

        // Assert
        sut.IntPropertyOnGue.ShouldBe(42);
    }

    #region View models and helpers

    private class TestViewModel : ViewModel
    {
        public int IntPropertyOnVm
        {
            get => Get<int>();
            set => Set(value);
        }

        public string StringValue
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    private class ParentViewModel : ViewModel
    {
        public TestViewModel? TestViewModel
        {
            get => Get<TestViewModel?>();
            set => Set(value);
        }

        public ParentViewModel()
        {
            TestViewModel = new TestViewModel();
        }
    }

    private class BindableGueDerived : GraphicalUiElement
    {
        private int _intProperty;

        public int IntPropertyOnGue
        {
            get => _intProperty;
            set
            {
                _intProperty = value;
                PushValueToViewModel();
            }
        }
    }

    private class IndexedLeafViewModel : ViewModel
    {
        public System.Collections.ObjectModel.ObservableCollection<string> Names { get; }
            = new System.Collections.ObjectModel.ObservableCollection<string>();
    }

    private class NamedChild : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    private class ChildAndOtherViewModel : ViewModel
    {
        public NamedChild? Child
        {
            get => Get<NamedChild?>();
            set => Set(value);
        }

        public NamedChild? Other
        {
            get => Get<NamedChild?>();
            set => Set(value);
        }

        public ChildAndOtherViewModel()
        {
            Child = new NamedChild();
            Other = new NamedChild();
        }
    }

    private class EventBindableGueDerived : GraphicalUiElement
    {
        public void OnSomeEvent(object? sender, EventArgs e) { }
    }

    private class EventParentViewModel : ViewModel
    {
        public EventChild Child { get; set; } = new();
    }

    private class EventChild
    {
#pragma warning disable CS0067 // Event is never used; only present to satisfy reflection-based binding
        public event EventHandler? SomeEvent;
#pragma warning restore CS0067
    }

    #endregion
}
