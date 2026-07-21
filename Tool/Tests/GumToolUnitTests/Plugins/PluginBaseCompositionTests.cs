using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Moq.AutoMock;
using Shouldly;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace GumToolUnitTests.Plugins;

// Pins the MEF wiring that replaced PluginBase's Locator constructor: the shared services
// are bridged into the plugin container (PluginManager.LoadPlugins) and supplied to plugins
// via inherited [Import] properties on PluginBase, with [ImportingConstructor] still able to
// inject the same services for plugins that need them at construction time.
public class PluginBaseCompositionTests : BaseTestClass
{
    [Fact]
    public void Compose_SatisfiesInheritedServiceImports_OnPluginWithoutImportingConstructor()
    {
        var services = new BridgedServices();

        var plugin = ComposeSinglePlugin<PropertyInjectedTestPlugin>(services);

        plugin.GuiCommands.ShouldBeSameAs(services.GuiCommands);
        plugin.FileCommands.ShouldBeSameAs(services.FileCommands);
        plugin.TabManager.ShouldBeSameAs(services.TabManager);
        // MenuStripManager lives on WpfPluginBase (not PluginBase itself) - PropertyInjectedTestPlugin
        // reaches it transitively via PriorityPlugin : WpfPluginBase.
        ((WpfPluginBase)plugin).MenuStripManager.ShouldBeSameAs(services.MenuStripManager);
        plugin.DialogService.ShouldBeSameAs(services.DialogService);
    }

    [Fact]
    public void Compose_SatisfiesImportingConstructorAndInheritedImports_OnPluginWithImportingConstructor()
    {
        var services = new BridgedServices();

        var plugin = (CtorInjectedTestPlugin)ComposeSinglePlugin<CtorInjectedTestPlugin>(services);

        // The constructor-injected dependency resolves (the path used by plugins that need a
        // service at construction time, e.g. MainStatePlugin / DeleteObjectPlugin)...
        plugin.ConstructorInjectedGuiCommands.ShouldBeSameAs(services.GuiCommands);
        // ...and the inherited PluginBase property imports are still satisfied alongside it.
        plugin.DialogService.ShouldBeSameAs(services.DialogService);
        plugin.TabManager.ShouldBeSameAs(services.TabManager);
    }

    private static PluginBase ComposeSinglePlugin<T>(BridgedServices services) where T : PluginBase
    {
        var catalog = new TypeCatalog(typeof(T));
        var container = new CompositionContainer(catalog);

        var batch = new CompositionBatch();
        batch.AddExportedValue(services.GuiCommands);
        batch.AddExportedValue(services.FileCommands);
        batch.AddExportedValue(services.TabManager);
        batch.AddExportedValue(services.MenuStripManager);
        batch.AddExportedValue(services.DialogService);
        container.Compose(batch);

        return container.GetExportedValue<PluginBase>()!;
    }

    // Mirrors the five values PluginManager.LoadPlugins bridges into the plugin container.
    private sealed class BridgedServices
    {
        private readonly AutoMocker _mocker = new();

        public IGuiCommands GuiCommands => _mocker.GetMock<IGuiCommands>().Object;
        public IFileCommands FileCommands => _mocker.GetMock<IFileCommands>().Object;
        public ITabManager TabManager => _mocker.GetMock<ITabManager>().Object;
        public IDialogService DialogService => _mocker.GetMock<IDialogService>().Object;
        public MenuStripManager MenuStripManager { get; }

        public BridgedServices()
        {
            MenuStripManager = _mocker.CreateInstance<MenuStripManager>();
        }
    }

    [Export(typeof(PluginBase))]
    public class PropertyInjectedTestPlugin : PriorityPlugin
    {
        public override void StartUp() { }
    }

    [Export(typeof(PluginBase))]
    public class CtorInjectedTestPlugin : PriorityPlugin
    {
        public IGuiCommands ConstructorInjectedGuiCommands { get; }

        [ImportingConstructor]
        public CtorInjectedTestPlugin(IGuiCommands guiCommands)
        {
            ConstructorInjectedGuiCommands = guiCommands;
        }

        public override void StartUp() { }
    }
}
