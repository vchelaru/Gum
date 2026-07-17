using System;
using System.Collections.Generic;
using System.Reflection;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Plugins.VariableGrid;
using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;
using ImportFromGumxPlugin.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Dialogs;

/// <summary>
/// <see cref="DialogViewResolver"/> pairs a <see cref="DialogViewModel"/> with its WPF View. Most
/// of these view models were relocated into the headless Gum.Presentation assembly (ADR-0005,
/// #3754); their Views necessarily stayed behind in a WPF-capable assembly (the Gum tool itself,
/// or a dynamically-loaded plugin like ImportFromGumxPlugin). These tests pin that cross-assembly
/// resolution alongside the original same-assembly naming-convention path.
/// </summary>
public class DialogViewResolverTests
{
    [Fact]
    public void GetDialogViewType_ResolvesFromOwnAssembly_WhenViewModelAndViewAreCoLocated()
    {
        // PluginsDialogViewModel/PluginsDialogView still live together in Gum.dll and pair via the
        // original naming-convention path (no [Dialog] attribute, no fallback assembly needed).
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance, new StubAssemblyProvider());

        Type? viewType = resolver.GetDialogViewType(typeof(PluginsDialogViewModel));

        viewType.ShouldBe(typeof(PluginsDialogView));
    }

    [Fact]
    public void GetDialogViewType_FallsBackToCandidateAssemblies_WhenViewLivesInTheToolAssembly()
    {
        // MessageDialogViewModel lives in the headless Gum.Presentation assembly (no WPF types);
        // its [Dialog]-attributed View stays behind in the Gum tool assembly.
        StubAssemblyProvider assemblyProvider = new(typeof(MessageDialogView).Assembly);
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance, assemblyProvider);

        Type? viewType = resolver.GetDialogViewType(typeof(MessageDialogViewModel));

        viewType.ShouldBe(typeof(MessageDialogView));
    }

    [Fact]
    public void GetDialogViewType_ResolvesView_WhenViewModelLivesInDifferentAssemblyThanView()
    {
        // AddVariableViewModel lives in Gum.Presentation.dll; AddVariableWindow (its [Dialog]-
        // attributed View) lives in Gum.exe — two different assemblies.
        StubAssemblyProvider assemblyProvider = new(typeof(AddVariableWindow).Assembly);
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance, assemblyProvider);

        Type? viewType = resolver.GetDialogViewType(typeof(AddVariableViewModel));

        viewType.ShouldBe(typeof(AddVariableWindow));
    }

    [Fact]
    public void GetDialogViewType_FallsBackToCandidateAssemblies_WhenViewLivesInAThirdPluginAssembly()
    {
        // StandardDiffDetailsViewModel also lives in Gum.Presentation, but its View lives in the
        // dynamically-loaded ImportFromGumxPlugin assembly - a third assembly distinct from both
        // the view model's own assembly and the main Gum tool assembly.
        StubAssemblyProvider assemblyProvider = new(typeof(StandardDiffDetailsView).Assembly);
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance, assemblyProvider);

        Type? viewType = resolver.GetDialogViewType(typeof(StandardDiffDetailsViewModel));

        viewType.ShouldBe(typeof(StandardDiffDetailsView));
    }

    [Fact]
    public void GetDialogViewType_ReturnsNull_WhenNoCandidateAssemblyHasAMatch()
    {
        DialogViewResolver resolver = new(NullLogger<DialogViewResolver>.Instance, new StubAssemblyProvider());

        Type? viewType = resolver.GetDialogViewType(typeof(MessageDialogViewModel));

        viewType.ShouldBeNull();
    }

    private class StubAssemblyProvider : IDialogViewAssemblyProvider
    {
        private readonly Assembly[] _assemblies;

        public StubAssemblyProvider(params Assembly[] assemblies)
        {
            _assemblies = assemblies;
        }

        public IEnumerable<Assembly> GetCandidateAssemblies() => _assemblies;
    }
}

/// <summary>
/// Pin for the default <see cref="IDialogViewAssemblyProvider"/> wired up in Builder.cs.
/// </summary>
public class AppDomainDialogViewAssemblyProviderTests
{
    [Fact]
    public void GetCandidateAssemblies_IncludesAssembliesAlreadyLoadedInTheProcess()
    {
        AppDomainDialogViewAssemblyProvider provider = new();

        IEnumerable<Assembly> assemblies = provider.GetCandidateAssemblies();

        assemblies.ShouldContain(typeof(AppDomainDialogViewAssemblyProvider).Assembly);
        assemblies.ShouldContain(typeof(DialogViewResolverTests).Assembly);
    }
}
