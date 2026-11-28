using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Shouldly;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Paused bindings in a subtree that left the root, across re-adds, edits, visual swaps, binding changes, and
/// elements that were never in the root themselves. The main rules are in <see cref="BindingAfterScreenRemovalTests"/>.
/// </summary>
public class BindingPausedOutOfRootTests : BaseTestClass
{
    private class Vm : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }
        public string? Text { get => Get<string?>(); set => Set(value); }
        public string? Selected { get => Get<string?>(); set => Set(value); }
        public ObservableCollection<string> Names { get; } = new();
    }

    // Counts PropertyChanged subscribers, to show a paused binding is not subscribed.
    private class CountingVm : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _changed;
        public int SubscriberCount => _changed?.GetInvocationList().Length ?? 0;
        public event PropertyChangedEventHandler? PropertyChanged { add => _changed += value; remove => _changed -= value; }
        private int _gold;
        public int Gold { get => _gold; set { _gold = value; _changed?.Invoke(this, new PropertyChangedEventArgs(nameof(Gold))); } }
    }

    private static (StackPanel screen, Label label) CreateScreen()
    {
        StackPanel screen = new();
        Label label = CreateBoundLabel();
        screen.AddChild(label);
        return (screen, label);
    }

    private static Label CreateBoundLabel()
    {
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(Vm.Gold));
        return label;
    }

    [Fact]
    public void RemoveAndAddToRootInOneFrame_FollowsViewModelAndSubscribesOnce()
    {
        CountingVm vm = new() { Gold = 1 };
        (StackPanel screen, Label label) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = vm;
        int count = vm.SubscriberCount;

        screen.RemoveFromRoot();
        screen.AddToRoot();

        vm.SubscriberCount.ShouldBe(count);
        vm.Gold = 5;
        label.Text.ShouldBe("5");
    }

    [Fact]
    public void ChildDetachedFromPausedScreenAndAddedBack_ResumesWithScreen()
    {
        Vm vm = new() { Gold = 1 };
        (StackPanel screen, Label label) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = vm;
        screen.RemoveFromRoot();
        label.Visual.Parent = null;
        screen.AddChild(label);
        vm.Gold = 4;
        label.Text.ShouldBe("1");

        screen.AddToRoot();
        label.Text.ShouldBe("4");
        vm.Gold = 6;
        label.Text.ShouldBe("6");
    }

    [Fact]
    public void TwoWayLostFocusEditPending_ScreenRemoved_ResumeShowsViewModelAndDoesNotOverwriteIt()
    {
        Vm vm = new() { Text = "a" };
        StackPanel screen = new();
        TextBox textBox = new();
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(Vm.Text)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        screen.AddChild(textBox);
        screen.AddToRoot();
        screen.BindingContext = vm;
        textBox.IsFocused = true;
        textBox.Text = "typed";

        screen.RemoveFromRoot();
        vm.Text = "elsewhere";
        screen.AddToRoot();

        textBox.Text.ShouldBe("elsewhere");
        vm.Text.ShouldBe("elsewhere");
        textBox.IsFocused = false;
        vm.Text.ShouldBe("elsewhere");
    }

    [Fact]
    public void ComboBoxDropDownOpen_ScreenRemoved_ResumeShowsViewModelSelection()
    {
        Vm vm = new();
        vm.Names.Add("first");
        vm.Names.Add("second");
        vm.Selected = "first";
        StackPanel screen = new();
        ComboBox comboBox = new();
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(Vm.Names));
        comboBox.SetBinding(nameof(ComboBox.SelectedObject),
            new Binding(nameof(Vm.Selected)) { Mode = BindingMode.TwoWay });
        screen.AddChild(comboBox);
        screen.AddToRoot();
        screen.BindingContext = vm;
        comboBox.IsDropDownOpen = true;

        screen.RemoveFromRoot();
        vm.Selected = "second";
        comboBox.SelectedObject.ShouldBe("first");
        comboBox.IsDropDownOpen = false;
        vm.Selected.ShouldBe("second");

        screen.AddToRoot();
        comboBox.SelectedObject.ShouldBe("second");
        vm.Selected.ShouldBe("second");
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(2);
    }

    [Fact]
    public void VisualSwappedWhilePaused_NewVisualAddedToPausedScreen_ResumesWithScreen()
    {
        Vm vm = new() { Gold = 1 };
        (StackPanel screen, Label label) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = vm;
        screen.RemoveFromRoot();

        label.Visual = new LabelVisual(tryCreateFormsObject: false);
        screen.AddChild(label);
        screen.AddToRoot();
        vm.Gold = 7;

        label.Text.ShouldBe("7");
    }

    [Fact]
    public void VisualSwappedWhileLive_ControlAddedToRoot_FollowsViewModel()
    {
        Vm labelVm = new() { Gold = 2 };
        (StackPanel screen, Label label) = CreateScreen();
        screen.AddToRoot();
        label.BindingContext = labelVm;

        label.Visual = new LabelVisual(tryCreateFormsObject: false);
        label.AddToRoot();
        labelVm.Gold = 7;

        label.Text.ShouldBe("7");
    }

    [Fact]
    public void ClearBindingWhilePaused_StaysClearedAfterResume()
    {
        CountingVm vm = new() { Gold = 1 };
        (StackPanel screen, Label label) = CreateScreen();
        Label other = CreateBoundLabel();
        screen.AddChild(other);
        screen.AddToRoot();
        screen.BindingContext = vm;
        int count = vm.SubscriberCount;
        screen.RemoveFromRoot();

        label.ClearBinding(nameof(Label.Text));
        screen.AddToRoot();
        vm.Gold = 5;

        label.Text.ShouldNotBe("5");
        other.Text.ShouldBe("5");
        vm.SubscriberCount.ShouldBe(count - 1);
    }

    [Fact]
    public void NeverRootedLabelAddedToPausedScreen_IsPausedThenResumesWithScreen()
    {
        Vm vm = new() { Gold = 1 };
        StackPanel screen = new();
        screen.AddToRoot();
        screen.BindingContext = vm;
        screen.RemoveFromRoot();

        Label label = CreateBoundLabel();
        screen.AddChild(label);
        label.Text.ShouldNotBe("1");
        vm.Gold = 7;
        label.Text.ShouldNotBe("7");

        screen.AddToRoot();
        label.Text.ShouldBe("7");
        vm.Gold = 8;
        label.Text.ShouldBe("8");
    }

    [Fact]
    public void NeverRootedLabelWithOwnContext_AddedToPausedScreen_IsPaused()
    {
        Vm screenVm = new() { Gold = 1 };
        StackPanel screen = new();
        screen.AddToRoot();
        screen.BindingContext = screenVm;
        screen.RemoveFromRoot();

        Vm labelVm = new() { Gold = 2 };
        Label label = CreateBoundLabel();
        label.BindingContext = labelVm;
        label.Text.ShouldBe("2");
        screen.AddChild(label);
        labelVm.Gold = 5;
        label.Text.ShouldBe("2");

        screen.AddToRoot();
        label.Text.ShouldBe("5");
    }

    [Fact]
    public void LabelMovedFromPausedScreenToNeverRootedPanel_StaysPausedUntilPanelAddedToRoot()
    {
        Vm screenVm = new() { Gold = 1 };
        StackPanel screen = new();
        screen.AddToRoot();
        screen.BindingContext = screenVm;
        screen.RemoveFromRoot();

        Vm labelVm = new() { Gold = 2 };
        Label label = CreateBoundLabel();
        label.BindingContext = labelVm;
        screen.AddChild(label);

        StackPanel freshPanel = new();
        label.Visual.Parent = null;
        freshPanel.AddChild(label);
        labelVm.Gold = 3;
        label.Text.ShouldBe("2");

        freshPanel.AddToRoot();
        label.Text.ShouldBe("3");
    }
}
