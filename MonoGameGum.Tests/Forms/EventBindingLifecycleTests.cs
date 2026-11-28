using System;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A view model event bound to a method follows the BindingContext: the outgoing view model's event no longer
/// calls the method, the new one does, and a method whose signature cannot handle the event fails at the call
/// that attached it. Works on a visual as on a Forms control.
/// </summary>
public class EventBindingLifecycleTests : BaseTestClass
{
    public class WaveViewModel : ViewModel
    {
        public event Action? WaveStarted;
        public event EventHandler? BossAppeared;

        public void RaiseWaveStarted() => WaveStarted?.Invoke();
        public void RaiseBossAppeared() => BossAppeared?.Invoke(this, EventArgs.Empty);
        public bool HasWaveStartedSubscribers => WaveStarted != null;
    }

    private class WaveWatcher : FrameworkElement
    {
        public int Count;
        public int BossCount;

        public WaveWatcher() : base(new ContainerRuntime())
        {
        }

        void HandleWave() => Count++;

        void HandleBoss(object? sender, EventArgs e) => BossCount++;

        void HandleWithWrongSignature(int value) => Count += value;
    }

    private class WaveVisual : ContainerRuntime
    {
        public int Count;

        public void HandleWave() => Count++;
    }

    [Fact]
    public void ContextReplaced_OldViewModelEvent_NoLongerCallsMethod()
    {
        WaveWatcher watcher = new();
        watcher.SetBinding("HandleWave", nameof(WaveViewModel.WaveStarted));
        WaveViewModel first = new();
        WaveViewModel second = new();
        watcher.BindingContext = first;

        watcher.BindingContext = second;
        first.RaiseWaveStarted();
        second.RaiseWaveStarted();

        watcher.Count.ShouldBe(1);
        first.HasWaveStartedSubscribers.ShouldBeFalse();
    }

    [Fact]
    public void ContextCleared_ViewModelEventUnsubscribed()
    {
        WaveWatcher watcher = new();
        watcher.SetBinding("HandleWave", nameof(WaveViewModel.WaveStarted));
        WaveViewModel vm = new();
        watcher.BindingContext = vm;

        watcher.BindingContext = null;

        vm.HasWaveStartedSubscribers.ShouldBeFalse();
    }

    [Fact]
    public void EventHandlerSignature_BindsToMatchingMethod()
    {
        WaveWatcher watcher = new();
        watcher.SetBinding("HandleBoss", nameof(WaveViewModel.BossAppeared));
        WaveViewModel vm = new();
        watcher.BindingContext = vm;

        vm.RaiseBossAppeared();

        watcher.BossCount.ShouldBe(1);
    }

    [Fact]
    public void MethodSignatureMismatch_ContextAlreadySet_ThrowsAtSetBinding()
    {
        WaveWatcher watcher = new() { BindingContext = new WaveViewModel() };

        Should.Throw<Exception>(() =>
            watcher.SetBinding("HandleWithWrongSignature", nameof(WaveViewModel.WaveStarted)));
    }

    [Fact]
    public void InheritedContext_EventBindingAttaches()
    {
        StackPanel panel = new();
        WaveWatcher watcher = new();
        watcher.SetBinding("HandleWave", nameof(WaveViewModel.WaveStarted));
        panel.AddChild(watcher);
        WaveViewModel vm = new();

        panel.BindingContext = vm;
        vm.RaiseWaveStarted();

        watcher.Count.ShouldBe(1);
    }

    [Fact]
    public void Visual_EventBoundToMethod_CallsMethod()
    {
        WaveVisual visual = new();
        visual.SetBinding(nameof(WaveVisual.HandleWave), nameof(WaveViewModel.WaveStarted));
        WaveViewModel vm = new();
        visual.BindingContext = vm;

        vm.RaiseWaveStarted();

        visual.Count.ShouldBe(1);
    }

    [Fact]
    public void SameMethodBoundTwice_EventCallsMethodOnce()
    {
        WaveWatcher watcher = new();
        watcher.SetBinding("HandleWave", nameof(WaveViewModel.WaveStarted));
        watcher.SetBinding("HandleWave", nameof(WaveViewModel.WaveStarted));
        WaveViewModel vm = new();
        watcher.BindingContext = vm;

        vm.RaiseWaveStarted();

        watcher.Count.ShouldBe(1);
    }
}
