using System;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the "Binding to a ViewModel Event" sample on docs/code/controls/frameworkelement/setbinding.md:
/// a FrameworkElement binds a view-model event to one of its own methods.
/// </summary>
public class SetBindingEventDocsSampleTests : BaseTestClass
{
    public class GameViewModel : ViewModel
    {
        // Regular ViewModel events can be used in SetBinding
        public event Action? EnemySpawned;

        // If the event needs to be raised externally (outside of the ViewModel)
        // then a method for raising the event is needed:
        public void RaiseEnemySpawned() => EnemySpawned?.Invoke();
    }

    class EnemyWatcher : FrameworkElement
    {
        public int SpawnCount;

        public EnemyWatcher() : base(new ContainerRuntime())
        {
            CustomInitialize();
        }

        void CustomInitialize()
        {
            SetBinding(
                nameof(HandleEnemySpawned),
                nameof(GameViewModel.EnemySpawned));
        }

        void HandleEnemySpawned()
        {
            SpawnCount++;
        }
    }

    [Fact]
    public void FrameworkElementEventBinding_EventRaised_CallsHandler()
    {
        EnemyWatcher watcher = new();
        GameViewModel viewModel = new();
        watcher.BindingContext = viewModel;

        viewModel.RaiseEnemySpawned();

        watcher.SpawnCount.ShouldBe(1);
    }
}
