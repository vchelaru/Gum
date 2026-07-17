using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ErrorViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). OwnerPlugin was retyped from the
/// tool-assembly PluginBase to a plain object — it was only ever used for reference-equality
/// filtering, never a PluginBase member.
/// </summary>
public class ErrorViewModelTests
{
    [Fact]
    public void HasCode_IsFalse_WhenCodeIsNull()
    {
        ErrorViewModel viewModel = new();

        viewModel.HasCode.ShouldBeFalse();
    }

    [Fact]
    public void HasCode_IsTrue_WhenCodeIsSet()
    {
        ErrorViewModel viewModel = new() { Code = "GUM0001" };

        viewModel.HasCode.ShouldBeTrue();
    }

    [Fact]
    public void HasCodeWithoutHelpUrl_IsFalse_WhenHelpUrlIsSet()
    {
        ErrorViewModel viewModel = new() { Code = "GUM0001", HelpUrl = "https://example.com/gum0001" };

        viewModel.HasCodeWithoutHelpUrl.ShouldBeFalse();
    }

    [Fact]
    public void HasCodeWithoutHelpUrl_IsTrue_WhenCodeSetAndHelpUrlIsNot()
    {
        ErrorViewModel viewModel = new() { Code = "GUM0001" };

        viewModel.HasCodeWithoutHelpUrl.ShouldBeTrue();
    }

    [Fact]
    public void HasHelpUrl_IsTrue_WhenHelpUrlIsSet()
    {
        ErrorViewModel viewModel = new() { HelpUrl = "https://example.com/gum0001" };

        viewModel.HasHelpUrl.ShouldBeTrue();
    }

    [Fact]
    public void OwnerPlugin_ComparesByReference()
    {
        object owner = new();
        ErrorViewModel viewModel = new() { OwnerPlugin = owner };

        (viewModel.OwnerPlugin == owner).ShouldBeTrue();
        (viewModel.OwnerPlugin == new object()).ShouldBeFalse();
    }
}
