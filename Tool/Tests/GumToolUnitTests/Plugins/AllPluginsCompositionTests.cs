using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Plugins;

/// <summary>
/// Headless guard for the Phase 2 plugin-drain work: composes every Gum tool plugin through MEF the
/// same way <see cref="Gum.Plugins.PluginManager"/> does at startup, and fails if any plugin cannot be
/// constructed. This is the automated replacement for the manual "launch Gum and check the plugin tab"
/// check — a missing/typo'd bridge in <c>LoadPlugins</c> or a bad <c>[ImportingConstructor]</c> signature
/// surfaces here as a red <see cref="CompositionException"/> instead of a silent "Error loading plugins".
///
/// Scales up the single-plugin precedent in <see cref="PluginBaseCompositionTests"/> to the whole plugin
/// catalog. Two stub seams stand in for the real DI container:
///  - the MEF <see cref="CompositionBatch"/> is the explicit mirror of every service
///    <c>LoadPlugins</c> bridges (<see cref="PluginBridgedServiceTypes"/>). Keeping it explicit (rather than a
///    catch-all export provider) is what preserves the regression signal: a plugin that gains an
///    <c>[ImportingConstructor]</c> dependency not present in this list — i.e. one a future drain forgot
///    to bridge in <c>LoadPlugins</c> — turns the test red.
///  - a catch-all <see cref="StubServiceProvider"/> registered with the <see cref="Locator"/> satisfies the
///    direct <c>Locator.GetRequiredService&lt;T&gt;()</c> calls that not-yet-drained plugins (e.g.
///    MainStateAnimationPlugin, MainEditorTabPlugin) still make from their constructors.
/// </summary>
public class AllPluginsCompositionTests : BaseTestClass
{
    /// <summary>
    /// One anchor type per assembly that contributes <c>[Export(typeof(PluginBase))]</c> plugins. The Gum
    /// executable holds the internal plugins; the rest ship as external plugin DLLs that the running tool
    /// loads from its Plugins folder. Referencing a type forces the assembly to load so its
    /// <see cref="AssemblyCatalog"/> sees every plugin.
    /// </summary>
    // global:: qualifiers are required because this test's own GumToolUnitTests.Plugins.* sub-namespaces
    // (other plugin test folders) otherwise shadow the plugins' root namespaces. FormsFileService anchors
    // GumFormsPlugin because its MainGumFormsPlugin entry type is internal and the assembly does not expose
    // internals to this test project.
    private static readonly Assembly[] PluginAssemblies =
    {
        typeof(Gum.Plugins.PluginManager).Assembly,
        typeof(global::CodeOutputPlugin.MainCodeOutputPlugin).Assembly,
        typeof(global::EventOutputPlugin.MainEventOutputPlugin).Assembly,
        typeof(global::GumFormsPlugin.Services.FormsFileService).Assembly,
        typeof(global::ImportFromGumxPlugin.MainImportFromGumxPlugin).Assembly,
        typeof(global::PerformanceMeasurementPlugin.MainPlugin).Assembly,
        typeof(global::SkiaPlugin.MainSkiaPlugin).Assembly,
        typeof(global::StateAnimationPlugin.MainStateAnimationPlugin).Assembly,
        typeof(global::TextureCoordinateSelectionPlugin.MainTextureCoordinatePlugin).Assembly,
        typeof(Gum.Plugins.InternalPlugins.EditorTab.MainEditorTabPlugin).Assembly,
    };

    private static readonly MethodInfo AddExportedValueMethod = typeof(AttributedModelServices)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(AttributedModelServices.AddExportedValue)
            && m.IsGenericMethodDefinition
            && m.GetParameters().Length == 2);

    private readonly StubServiceProvider _locatorStubs = new();

    public AllPluginsCompositionTests()
    {
        // Not-yet-drained plugins still resolve services from the locator in their constructors.
        Locator.Register(_locatorStubs);
    }

    // Plugin constructors (e.g. MainHotkeyPlugin building its HotkeyView) touch WPF, which requires STA;
    // xUnit's runner is MTA.
    [StaFact]
    public void AllPlugins_ComposeWithoutError()
    {
        List<Type> expectedPluginTypes = DiscoverPluginExportTypes();
        // Sanity check that the catalog is actually populated — guards against a future refactor that
        // silently drops every plugin and leaves an all-green "0 == 0" assertion.
        expectedPluginTypes.Count.ShouldBeGreaterThan(20);

        using CompositionContainer container = BuildPluginContainer();

        // Forcing the values composes every plugin; an unsatisfiable import or a throwing constructor
        // raises a CompositionException here, which is exactly the failure this test exists to catch.
        PluginBase[] composedPlugins = container.GetExportedValues<PluginBase>().ToArray();

        HashSet<Type> composedTypes = composedPlugins.Select(plugin => plugin.GetType()).ToHashSet();
        foreach (Type expected in expectedPluginTypes)
        {
            composedTypes.ShouldContain(expected);
        }
        composedPlugins.Length.ShouldBe(expectedPluginTypes.Count);
    }

    private CompositionContainer BuildPluginContainer()
    {
        AggregateCatalog catalog = new AggregateCatalog();
        foreach (Assembly assembly in PluginAssemblies)
        {
            catalog.Catalogs.Add(new AssemblyCatalog(assembly));
        }

        CompositionContainer container = new CompositionContainer(catalog);

        CompositionBatch batch = new CompositionBatch();
        foreach (Type serviceType in PluginBridgedServiceTypes.All)
        {
            AddExportedValue(batch, serviceType, _locatorStubs.GetService(serviceType)!);
        }
        container.Compose(batch);

        return container;
    }

    private static void AddExportedValue(CompositionBatch batch, Type contractType, object value)
    {
        // Equivalent to batch.AddExportedValue<contractType>(value); done reflectively so the bridged list
        // can be data-driven. The generic argument sets the export's type identity so it matches the
        // corresponding typed [Import]/[ImportingConstructor].
        AddExportedValueMethod.MakeGenericMethod(contractType).Invoke(null, new[] { batch, value });
    }

    private static List<Type> DiscoverPluginExportTypes()
    {
        List<Type> result = new List<Type>();
        foreach (Assembly assembly in PluginAssemblies)
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                bool exportsPluginBase = type
                    .GetCustomAttributes<ExportAttribute>(inherit: false)
                    .Any(export => export.ContractType == typeof(PluginBase));
                if (exportsPluginBase)
                {
                    result.Add(type);
                }
            }
        }
        return result;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        // Mirrors PluginManager.CreateResilientCatalog: a single unloadable type must not abort discovery.
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    /// <summary>
    /// Hands every requested service a throwaway stand-in: a Moq proxy for interfaces/abstract classes, and
    /// an uninitialized instance for concrete classes. Constructors are never run, so even the heavy
    /// WinForms/WPF-coupled concretes (ElementTreeViewManager, PropertyGridManager, ...) are produced
    /// without touching a real graphics/UI stack. Composition only needs each dependency to exist as the
    /// right type; the plugins under test store them and use them later in StartUp, not during construction.
    /// </summary>
    private sealed class StubServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _cache = new();
        private readonly object _gate = new();

        public object? GetService(Type serviceType)
        {
            lock (_gate)
            {
                if (!_cache.TryGetValue(serviceType, out object? stub))
                {
                    stub = CreateStub(serviceType);
                    _cache[serviceType] = stub;
                }
                return stub;
            }
        }

        private static object CreateStub(Type serviceType)
        {
            if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                Mock mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(serviceType))!;
                return mock.Object;
            }

            return RuntimeHelpers.GetUninitializedObject(serviceType);
        }
    }

    public override void Dispose()
    {
        // Locator has no Unregister; remove our provider so the global static does not leak into other tests
        // (same teardown RenameManagerTests uses).
        PropertyInfo prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<IServiceProvider> providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_locatorStubs);

        base.Dispose();
    }
}
