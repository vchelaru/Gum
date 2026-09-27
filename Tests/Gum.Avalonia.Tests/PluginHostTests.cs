using Avalonia.Headless.XUnit;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Gum.Avalonia.Plugins;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using PluginHostFixture;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The shared plugin host composes under this head: its built-in plugins load through the real
/// <see cref="PluginManager"/>, and a plugin assembly that builds without WPF is accepted and
/// composes against the head's exports the same way the running tool loads it from its Plugins
/// folder.
/// </summary>
public class PluginHostTests
{
    private static readonly Assembly[] NeutralPluginAssemblies =
    {
        typeof(global::ConvertToJsonPlugin.MainConvertToJsonPlugin).Assembly,
        typeof(global::EventOutputPlugin.MainEventOutputPlugin).Assembly,
        typeof(global::PerformanceMeasurementPlugin.MainPlugin).Assembly,
        typeof(global::GumFormsPlugin.MainGumFormsPlugin).Assembly,
        typeof(global::ImportFromGumxPlugin.MainImportFromGumxPlugin).Assembly,
        typeof(global::SkiaPlugin.MainSkiaPlugin).Assembly,
    };

    [Fact]
    public void Head_HostsPluginAssembliesThatBuildWithoutWpf()
    {
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();

        foreach (Assembly assembly in NeutralPluginAssemblies)
        {
            host.CanHostExternalAssembly(assembly, out string? reason).ShouldBeTrue(reason);
        }
    }

    [AvaloniaFact]
    public void TestProcess_LoadsEachNeutralPluginFromItsPluginsFolder_AsOneAssembly()
    {
        ToolStartup.EnsureInitialized();
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();

        foreach (Assembly assembly in NeutralPluginAssemblies)
        {
            string name = assembly.GetName().Name!;
            File.Exists(Path.Combine(PluginManager.PluginFolder, name, name + ".dll")).ShouldBeTrue($"{name} is staged in the test output's Plugins folder");
            // The folder's copy and the one this test references must be one assembly, or the
            // plugin's types would not be the types the tests use.
            pluginManager.InitializedPlugins.ShouldContain(plugin => plugin.GetType().Assembly == assembly, $"{name} is loaded");
        }
    }

    [Fact]
    public void Head_ExposesCursorStateAsInputLibraryCursorSelf()
    {
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();

        // DragDropManager.OnNodeObjectDroppedInWireframe resolves the drop position through
        // IPluginHostConfiguration.CursorState; a null CursorState makes every tree-item drop
        // land at world (0,0) instead of the cursor position (#4704).
        host.CursorState.ShouldBeSameAs(InputLibrary.Cursor.Self);
    }

    [Fact]
    public void Head_RejectsAnAssemblyThatReferencesWpf()
    {
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();
        Assembly wpfDependent = new WpfReferencingAssembly();

        host.CanHostExternalAssembly(wpfDependent, out string? reason).ShouldBeFalse();
        reason.ShouldNotBeNull();
        reason.ShouldContain("PresentationFramework");
    }

    [Fact]
    public void Head_RejectsAPluginBuiltAgainstTheWpfToolsGumAssembly()
    {
        // The WPF head's assembly is also named Gum, so the reference binds to this head and only
        // the missing type gives it away (#5135).
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();
        string pluginPath = PluginAssemblyWriter.WriteDerivingFrom(NewScratchDirectory(), "WpfBuiltPlugin" + Guid.NewGuid().ToString("N"),
            "Gum", "Gum.Plugins.BaseClasses", "PriorityPlugin", "Gum.Presentation");
        Assembly plugin = Assembly.LoadFrom(pluginPath);

        host.CanHostExternalAssembly(plugin, out string? reason).ShouldBeFalse();
        reason.ShouldNotBeNull();
        reason.ShouldContain("Gum.Plugins.BaseClasses.PriorityPlugin");
        reason.ShouldContain("needs an Avalonia build");
    }

    [Fact]
    public void Head_HostsAPluginBuiltAgainstThisHeadsGumAssembly()
    {
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();
        string pluginPath = PluginAssemblyWriter.WriteDerivingFrom(NewScratchDirectory(), "AvaloniaBuiltPlugin" + Guid.NewGuid().ToString("N"),
            "Gum", "Gum.Avalonia.Plugins", nameof(ShellTitlePlugin), "Gum.Presentation");
        Assembly plugin = Assembly.LoadFrom(pluginPath);

        host.CanHostExternalAssembly(plugin, out string? reason).ShouldBeTrue(reason);
    }

    private static string NewScratchDirectory() =>
        Path.Combine(Path.GetTempPath(), "GumPluginHostTests", Guid.NewGuid().ToString("N"));

    // Plugin StartUp builds tab controls, so composition runs on the headless UI thread.
    [AvaloniaFact]
    public void NeutralPlugins_ComposeAgainstTheHeadExports()
    {
        IServiceProvider services = TestAppBuilder.Services;
        AggregateCatalog catalog = new AggregateCatalog();
        foreach (Assembly assembly in NeutralPluginAssemblies)
        {
            catalog.Catalogs.Add(new AssemblyCatalog(assembly));
        }
        using CompositionContainer container = new CompositionContainer(catalog);

        // The same exports PluginManager bridges: the core services through the container's
        // registrations, then the head's own contribution.
        CompositionBatch batch = new CompositionBatch();
        PluginManager pluginManager = services.GetRequiredService<PluginManager>();
        pluginManager.AddCoreExports(batch);
        services.GetRequiredService<IPluginHostConfiguration>().AddHeadExports(batch);
        container.Compose(batch);

        PluginBase[] plugins = container.GetExportedValues<PluginBase>().ToArray();

        plugins.Select(plugin => plugin.GetType().Name)
            .ShouldBe(new[] { "MainConvertToJsonPlugin", "MainEventOutputPlugin", "MainPlugin", "MainGumFormsPlugin", "MainImportFromGumxPlugin", "MainSkiaPlugin" }, ignoreOrder: true);
        plugins.ShouldAllBe(plugin => plugin.Menu != null);
    }

    [Fact]
    public void TabViewRegistry_HasAViewForEveryNeutralPluginsTab()
    {
        TabViewRegistry registry = TestAppBuilder.Services.GetRequiredService<TabViewRegistry>();

        registry.HasView(typeof(global::PerformanceMeasurementPlugin.ViewModels.PerformanceViewModel)).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void PluginManager_LoadsThisHeadsBuiltInPlugins()
    {
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();

        Type[] loaded = pluginManager.InitializedPlugins.Select(plugin => plugin.GetType()).ToArray();

        loaded.ShouldContain(typeof(ShellTitlePlugin));
        // Built-in plugins shared with the WPF head live in Gum.Presentation and are internal there.
        string[] names = loaded.Select(type => type.Name).ToArray();
        names.ShouldContain("MainOutputPlugin");
        names.ShouldContain("MainHotkeyPlugin");
        names.ShouldContain("MainFileWatchPlugin");
        names.ShouldContain("MainRecentFilesPlugin");
        names.ShouldContain("MainInheritancePlugin");
    }

    [AvaloniaFact]
    public void PluginManager_ReportsNoBuiltInPluginItCouldNotCreate()
    {
        // Plugins are created one at a time and a failure goes to the Output tab rather than
        // stopping the rest, so this is where a built-in plugin missing an export shows up.
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        pluginManager.IsInitialized.ShouldBeTrue();

        string output = TestAppBuilder.Services.GetRequiredService<MainOutputViewModel>().OutputText;

        output.ShouldNotContain("was not loaded");
    }

    [AvaloniaFact]
    public void EveryTabASharedPluginAdds_ResolvesToAnAvaloniaView()
    {
        AvaloniaTabManager tabs = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>();

        string[] unresolved = tabs.AllTabs
            .Where(tab => tab.Content is not global::Avalonia.Controls.Control)
            .Select(tab => $"{tab.Title} ({tab.Content.GetType().Name})")
            .ToArray();

        unresolved.ShouldBeEmpty("Register an Avalonia view in TabViewRegistry for: " + string.Join(", ", unresolved));
        tabs.AllTabs.Select(tab => tab.Title).ShouldContain("Output");
    }

    /// <summary>A stand-in assembly whose only referenced assembly is WPF's PresentationFramework.</summary>
    private sealed class WpfReferencingAssembly : Assembly
    {
        public override AssemblyName[] GetReferencedAssemblies() =>
            new[] { new AssemblyName("PresentationFramework, Version=10.0.0.0") };
    }
}
