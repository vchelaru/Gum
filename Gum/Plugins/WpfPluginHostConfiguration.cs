using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Gum.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.HideShowTools;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ViewModels;

namespace Gum.Plugins;

/// <summary>
/// The WPF head's contribution to the plugin host: the built-in plugins (this tool assembly's and
/// the ones shared with the Avalonia head in Gum.Presentation), the
/// WPF-only services its plugins inject, every external assembly, and the KNI cursor.
/// </summary>
public class WpfPluginHostConfiguration : IPluginHostConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The tool assembly, the built-in plugins shared by both heads (Gum.Presentation), and
    /// the neutral element-tree assembly.</remarks>
    public IEnumerable<Assembly> InternalPluginAssemblies => new[]
    {
        typeof(WpfPluginHostConfiguration).Assembly,
        typeof(CorePriorityPlugin).Assembly,
        typeof(ElementTreeViewManager).Assembly,
    };

    /// <inheritdoc/>
    public void AddHeadExports(CompositionBatch batch)
    {
        batch.AddExportedValue<MenuModel>(Locator.GetRequiredService<MenuModel>());
        // Still WPF-side pending their owning phases (Direction/avalonia-migration/coverage-matrix.md).
        batch.AddExportedValue<MenuStripManager>(Locator.GetRequiredService<MenuStripManager>());
        batch.AddExportedValue<MainPanelViewModel>(Locator.GetRequiredService<MainPanelViewModel>());
        batch.AddExportedValue<IToolsVisibility>(Locator.GetRequiredService<MainPanelViewModel>());
        batch.AddExportedValue<PropertyGridManager>(Locator.GetRequiredService<PropertyGridManager>());
        batch.AddExportedValue<ElementTreeViewManager>(Locator.GetRequiredService<ElementTreeViewManager>());
        batch.AddExportedValue<MainWindowViewModel>(Locator.GetRequiredService<MainWindowViewModel>());
    }

    /// <inheritdoc/>
    public bool CanHostExternalAssembly(Assembly assembly, out string? reason)
    {
        reason = null;
        return true;
    }

    /// <inheritdoc/>
    public IGumCursorState? CursorState => InputLibrary.Cursor.Self;
}
