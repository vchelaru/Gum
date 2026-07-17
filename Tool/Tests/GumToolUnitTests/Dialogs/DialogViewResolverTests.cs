using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Plugins.VariableGrid;
using Gum.Services.Dialogs;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using Xunit;

namespace GumToolUnitTests.Dialogs;

/// <summary>
/// Covers the cross-assembly resolution gap noted in Direction/ui-decoupling-plan.md's "known
/// gotchas" list: a relocated DialogViewModel (ADR-0005, headless Gum.Presentation assembly) whose
/// View stays in the WPF tool assembly is matched via an explicit [Dialog(typeof(VM))] attribute
/// rather than the same-assembly name-convention scan.
/// </summary>
public class DialogViewResolverTests
{
    [Fact]
    public void GetDialogViewType_ResolvesView_WhenViewModelLivesInDifferentAssemblyThanView()
    {
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance);

        // AddVariableViewModel lives in Gum.Presentation.dll; AddVariableWindow (its [Dialog]-
        // attributed View) lives in Gum.exe — two different assemblies.
        Type? viewType = resolver.GetDialogViewType(typeof(AddVariableViewModel));

        viewType.ShouldBe(typeof(AddVariableWindow));
    }
}
