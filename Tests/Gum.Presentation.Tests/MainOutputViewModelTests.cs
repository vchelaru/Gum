using System.Collections.Generic;
using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for MainOutputViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with no injected
/// interfaces.
/// </summary>
public class MainOutputViewModelTests
{
    [Fact]
    public void AddError_PrefixesValueWithError()
    {
        MainOutputViewModel viewModel = new();

        viewModel.AddError("bad thing");

        viewModel.OutputText.ShouldContain("ERROR:  bad thing");
    }

    [Fact]
    public void AddOutput_AppendsValue_AndRaisesPropertyChanged()
    {
        MainOutputViewModel viewModel = new();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.AddOutput("hello");

        viewModel.OutputText.ShouldContain("hello");
        changedProperties.ShouldContain(nameof(MainOutputViewModel.OutputText));
    }

    [Fact]
    public void AddOutput_TruncatesWhenExceedingMaxLength()
    {
        MainOutputViewModel viewModel = new();
        string longValue = new string('x', 60_000);

        viewModel.AddOutput(longValue);

        viewModel.OutputText.Length.ShouldBeLessThan(60_000);
    }

    [Fact]
    public void ClearOutputCommand_ResetsOutputTextToEmpty()
    {
        MainOutputViewModel viewModel = new();
        viewModel.AddOutput("hello");

        viewModel.ClearOutputCommand.Execute(null);

        viewModel.OutputText.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_InitializesOutputTextToEmpty()
    {
        MainOutputViewModel viewModel = new();

        viewModel.OutputText.ShouldBe(string.Empty);
    }
}
