using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Gum.Commands;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins;

/// <summary>
/// A plugin that cannot be created, such as a third-party plugin built against the WPF tool, is
/// reported and skipped, and every other plugin still loads.
/// </summary>
public class PluginInstantiatorTests
{
    [Fact]
    public void CreatePlugins_SkipsAndReportsPluginsThatCannotBeCreated()
    {
        TypeCatalog catalog = new TypeCatalog(typeof(WorkingPlugin), typeof(ThrowingPlugin), typeof(MissingServicePlugin));
        using CompositionContainer container = new CompositionContainer(catalog);
        CompositionBatch batch = new CompositionBatch();
        batch.AddExportedValue(new Mock<IGuiCommands>().Object);
        batch.AddExportedValue(new Mock<IFileCommands>().Object);
        batch.AddExportedValue(new Mock<ITabManager>().Object);
        batch.AddExportedValue(new Mock<IDialogService>().Object);
        container.Compose(batch);
        List<string> errors = new List<string>();
        Mock<IOutputManager> output = new Mock<IOutputManager>();
        output.Setup(x => x.AddError(It.IsAny<string>())).Callback<string>(errors.Add);

        List<PluginBase> plugins = new PluginInstantiator(output.Object).CreatePlugins(container, catalog);

        plugins.ShouldHaveSingleItem().ShouldBeOfType<WorkingPlugin>();
        errors.ShouldContain(error => error.Contains(nameof(ThrowingPlugin)) && error.Contains("failed while it was being created"));
        errors.ShouldContain(error => error.Contains(nameof(MissingServicePlugin)) && error.Contains($"needs {typeof(IWpfOnlyService).FullName}"));
    }

    [Export(typeof(PluginBase))]
    public class WorkingPlugin : TestPlugin
    {
    }

    [Export(typeof(PluginBase))]
    public class ThrowingPlugin : TestPlugin
    {
        // What a plugin built against the WPF tool hits when its constructor touches a type the
        // Avalonia head does not have.
        public ThrowingPlugin() => throw new TypeLoadException("Could not load type 'Gum.ToolStates.SelectedState'.");
    }

    [Export(typeof(PluginBase))]
    public class MissingServicePlugin : TestPlugin
    {
        [ImportingConstructor]
        public MissingServicePlugin(IWpfOnlyService service)
        {
        }
    }

    public interface IWpfOnlyService
    {
    }

    public abstract class TestPlugin : PluginBase
    {
        public override string FriendlyName => GetType().Name;

        public override void StartUp()
        {
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
