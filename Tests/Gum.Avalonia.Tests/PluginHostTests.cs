using Avalonia.Headless.XUnit;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Gum.Avalonia.Plugins;
using Gum.Avalonia.Services;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
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
        typeof(global::GumFormsPlugin.MainGumFormsPlugin).Assembly,
        typeof(global::ImportFromGumxPlugin.MainImportFromGumxPlugin).Assembly,
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

    [Fact]
    public void Head_RejectsAnAssemblyThatReferencesWpf()
    {
        IPluginHostConfiguration host = TestAppBuilder.Services.GetRequiredService<IPluginHostConfiguration>();
        Assembly wpfDependent = new WpfReferencingAssembly();

        host.CanHostExternalAssembly(wpfDependent, out string? reason).ShouldBeFalse();
        reason.ShouldNotBeNull();
        reason.ShouldContain("PresentationFramework");
    }

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
            .ShouldBe(new[] { "MainConvertToJsonPlugin", "MainEventOutputPlugin", "MainGumFormsPlugin", "MainImportFromGumxPlugin" }, ignoreOrder: true);
        plugins.ShouldAllBe(plugin => plugin.Menu != null);
    }

    [AvaloniaFact]
    public void PluginManager_LoadsThisHeadsBuiltInPlugins()
    {
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }

        Type[] loaded = pluginManager.Plugins.Select(plugin => plugin.GetType()).ToArray();

        loaded.ShouldContain(typeof(ShellTitlePlugin));
        loaded.ShouldContain(typeof(OutputPlugin));
    }

    /// <summary>A stand-in assembly whose only referenced assembly is WPF's PresentationFramework.</summary>
    private sealed class WpfReferencingAssembly : Assembly
    {
        public override AssemblyName[] GetReferencedAssemblies() =>
            new[] { new AssemblyName("PresentationFramework, Version=10.0.0.0") };
    }
}
