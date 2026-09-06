using System;
using System.Collections.Generic;
using Gum.Managers;
using Gum.Plugins.Errors;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for AllErrorsViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). Moves for free once ErrorViewModel does
/// — it has no dependency of its own beyond ErrorViewModel.
/// </summary>
public class AllErrorsViewModelTests
{
    private readonly Mock<IClipboardService> _clipboardService = new();

    private AllErrorsViewModel CreateViewModel() => new(_clipboardService.Object);

    [Fact]
    public void CountDescription_PluralizesForMultipleErrors()
    {
        AllErrorsViewModel viewModel = CreateViewModel();

        viewModel.Errors.Add(new ErrorViewModel { Message = "first" });
        viewModel.Errors.Add(new ErrorViewModel { Message = "second" });

        viewModel.CountDescription.ShouldBe("2 Errors");
    }

    [Fact]
    public void CountDescription_ReflectsSingleError()
    {
        AllErrorsViewModel viewModel = CreateViewModel();

        viewModel.Errors.Add(new ErrorViewModel { Message = "only" });

        viewModel.CountDescription.ShouldBe("1 Error");
    }

    [Fact]
    public void CountDescription_ReflectsZeroErrors_WhenConstructed()
    {
        AllErrorsViewModel viewModel = CreateViewModel();

        viewModel.CountDescription.ShouldBe("0 Errors");
    }

    [Fact]
    public void Errors_Add_RaisesCountDescriptionPropertyChanged()
    {
        AllErrorsViewModel viewModel = CreateViewModel();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.Errors.Add(new ErrorViewModel { Message = "boom" });

        changedProperties.ShouldContain(nameof(AllErrorsViewModel.CountDescription));
    }

    [Fact]
    public void SelectedItem_Set_RaisesPropertyChanged()
    {
        AllErrorsViewModel viewModel = CreateViewModel();
        ErrorViewModel item = new() { Message = "boom" };
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.SelectedItem = item;

        viewModel.SelectedItem.ShouldBe(item);
        changedProperties.ShouldContain(nameof(AllErrorsViewModel.SelectedItem));
    }

    [Fact]
    public void CopySelectedError_CopiesCodeAndMessage()
    {
        AllErrorsViewModel viewModel = CreateViewModel();
        ErrorViewModel error = new()
        {
            Code = "GUM0003",
            Message = "Category state references itself"
        };
        viewModel.Errors.Add(error);
        viewModel.SelectedItem = error;

        viewModel.CopySelectedErrorCommand.Execute(null);

        _clipboardService.Verify(x => x.SetText("GUM0003: Category state references itself"), Times.Once);
    }

    [Fact]
    public void CopySelectedError_CopiesMessageOnly_WhenErrorHasNoCode()
    {
        AllErrorsViewModel viewModel = CreateViewModel();
        ErrorViewModel error = new() { Message = "Missing base type" };
        viewModel.Errors.Add(error);
        viewModel.SelectedItem = error;

        viewModel.CopySelectedErrorCommand.Execute(null);

        _clipboardService.Verify(x => x.SetText("Missing base type"), Times.Once);
    }

    [Fact]
    public void CopyAllErrors_CopiesEveryErrorOnItsOwnLine()
    {
        AllErrorsViewModel viewModel = CreateViewModel();
        viewModel.Errors.Add(new ErrorViewModel { Code = "GUM0001", Message = "First problem" });
        viewModel.Errors.Add(new ErrorViewModel { Message = "Second problem" });

        viewModel.CopyAllErrorsCommand.Execute(null);

        _clipboardService.Verify(
            x => x.SetText("GUM0001: First problem" + Environment.NewLine + "Second problem"),
            Times.Once);
    }

    [Fact]
    public void CopyCommands_CannotExecute_WhenThereIsNothingToCopy()
    {
        AllErrorsViewModel viewModel = CreateViewModel();

        viewModel.CopySelectedErrorCommand.CanExecute(null).ShouldBeFalse();
        viewModel.CopyAllErrorsCommand.CanExecute(null).ShouldBeFalse();

        ErrorViewModel error = new() { Message = "Missing base type" };
        viewModel.Errors.Add(error);
        viewModel.SelectedItem = error;

        viewModel.CopySelectedErrorCommand.CanExecute(null).ShouldBeTrue();
        viewModel.CopyAllErrorsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public void CopyCommands_CannotExecute_WhenEveryErrorIsBlank()
    {
        // Clipboard.SetText throws on empty text, so a blank error must not reach it.
        AllErrorsViewModel viewModel = CreateViewModel();
        ErrorViewModel blank = new();
        viewModel.Errors.Add(blank);
        viewModel.SelectedItem = blank;

        viewModel.CopySelectedErrorCommand.CanExecute(null).ShouldBeFalse();
        viewModel.CopyAllErrorsCommand.CanExecute(null).ShouldBeFalse();
    }
}
