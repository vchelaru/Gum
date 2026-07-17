using System.Collections.Generic;
using Gum.Managers;
using Gum.Plugins.Errors;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for AllErrorsViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). Moves for free once ErrorViewModel does
/// — it has no dependency of its own beyond ErrorViewModel.
/// </summary>
public class AllErrorsViewModelTests
{
    [Fact]
    public void CountDescription_PluralizesForMultipleErrors()
    {
        AllErrorsViewModel viewModel = new();

        viewModel.Errors.Add(new ErrorViewModel { Message = "first" });
        viewModel.Errors.Add(new ErrorViewModel { Message = "second" });

        viewModel.CountDescription.ShouldBe("2 Errors");
    }

    [Fact]
    public void CountDescription_ReflectsSingleError()
    {
        AllErrorsViewModel viewModel = new();

        viewModel.Errors.Add(new ErrorViewModel { Message = "only" });

        viewModel.CountDescription.ShouldBe("1 Error");
    }

    [Fact]
    public void CountDescription_ReflectsZeroErrors_WhenConstructed()
    {
        AllErrorsViewModel viewModel = new();

        viewModel.CountDescription.ShouldBe("0 Errors");
    }

    [Fact]
    public void Errors_Add_RaisesCountDescriptionPropertyChanged()
    {
        AllErrorsViewModel viewModel = new();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.Errors.Add(new ErrorViewModel { Message = "boom" });

        changedProperties.ShouldContain(nameof(AllErrorsViewModel.CountDescription));
    }

    [Fact]
    public void SelectedItem_Set_RaisesPropertyChanged()
    {
        AllErrorsViewModel viewModel = new();
        ErrorViewModel item = new() { Message = "boom" };
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.SelectedItem = item;

        viewModel.SelectedItem.ShouldBe(item);
        changedProperties.ShouldContain(nameof(AllErrorsViewModel.SelectedItem));
    }
}
