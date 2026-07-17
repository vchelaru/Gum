using System.Collections.Generic;
using Gum.Plugins.InternalPlugins.Undos;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for UndoItemViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with no injected
/// interfaces.
/// </summary>
public class UndoItemViewModelTests
{
    [Fact]
    public void Display_Set_RaisesPropertyChanged()
    {
        UndoItemViewModel viewModel = new();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.Display = "Move Instance";

        viewModel.Display.ShouldBe("Move Instance");
        changedProperties.ShouldContain(nameof(UndoItemViewModel.Display));
    }

    [Fact]
    public void ToString_ReturnsDisplay()
    {
        UndoItemViewModel viewModel = new() { Display = "Delete Instance" };

        viewModel.ToString().ShouldBe("Delete Instance");
    }

    [Fact]
    public void UndoOrRedo_Set_RaisesPropertyChanged()
    {
        UndoItemViewModel viewModel = new();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.UndoOrRedo = UndoOrRedo.Redo;

        viewModel.UndoOrRedo.ShouldBe(UndoOrRedo.Redo);
        changedProperties.ShouldContain(nameof(UndoItemViewModel.UndoOrRedo));
    }
}
