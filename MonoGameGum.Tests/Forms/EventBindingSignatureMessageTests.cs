using System;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Binding a view model event to a method whose signature cannot handle it throws a message naming the method
/// and the event, so the developer can find the mismatched pair.
/// </summary>
public class EventBindingSignatureMessageTests : BaseTestClass
{
    public class WaveViewModel : ViewModel
    {
        public event Action? WaveStarted;
        public event EventHandler<int>? Scored;

        public void RaiseWaveStarted() => WaveStarted?.Invoke();
        public void RaiseScored() => Scored?.Invoke(this, 1);
    }

    private class WaveWatcher : FrameworkElement
    {
        public int Count;

        public WaveWatcher() : base(new ContainerRuntime())
        {
        }

        void HandleWithWrongSignature(int value) => Count += value;
        void HandleOverloaded(int value) => Count += value;
        void HandleOverloaded(string first, string second) => Count++;
        void HandleScored() => Count++;
    }

    [Fact]
    public void OverloadedMethodMismatch_MessageListsEveryOverloadAndKeepsInnerException()
    {
        WaveWatcher watcher = new() { BindingContext = new WaveViewModel() };

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            watcher.SetBinding("HandleOverloaded", nameof(WaveViewModel.WaveStarted)));

        exception.Message.ShouldContain("void HandleOverloaded(int value) or void HandleOverloaded(string first, string second)");
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void GenericEventHandlerMismatch_MessageNamesEventTypeAndNeededSignature()
    {
        WaveWatcher watcher = new() { BindingContext = new WaveViewModel() };

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            watcher.SetBinding("HandleScored", nameof(WaveViewModel.Scored)));

        exception.Message.ShouldContain("EventHandler<int>");
        exception.Message.ShouldContain("void HandleScored(object sender, int e)");
    }

    [Fact]
    public void MethodSignatureMismatch_MessageNamesMethodAndEvent()
    {
        WaveWatcher watcher = new() { BindingContext = new WaveViewModel() };

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            watcher.SetBinding("HandleWithWrongSignature", nameof(WaveViewModel.WaveStarted)));

        exception.Message.ShouldContain("HandleWithWrongSignature");
        exception.Message.ShouldContain(nameof(WaveViewModel.WaveStarted));
    }

    [Fact]
    public void MethodSignatureMismatch_MessageNamesBothSignatures()
    {
        WaveWatcher watcher = new() { BindingContext = new WaveViewModel() };

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            watcher.SetBinding("HandleWithWrongSignature", nameof(WaveViewModel.WaveStarted)));

        exception.Message.ShouldContain("void HandleWithWrongSignature(int value)");
        exception.Message.ShouldContain("void HandleWithWrongSignature()");
    }
}
