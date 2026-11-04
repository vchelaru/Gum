using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gum.Plugins;
using Gum.Services.Dialogs;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The rows of the Application > Plugins page of the Settings window over <see cref="PluginsDialogViewModel"/>,
/// built with <see cref="SettingsRows"/>: one row per plugin (its name, a one-line hint saying where it comes
/// from or what is wrong, a chip for its state and the enable toggle), then the plugins folder and what the
/// scan found in it. Each toggle reads and writes the same <see cref="PluginItemViewModel.IsEnabled"/> the
/// Manage Plugins dialog does, so enabling, disabling and the crashed-plugin prompt behave as before.
/// </summary>
public sealed class PluginsRows : IRowsSettingsPage
{
    private readonly PluginsDialogViewModel _viewModel;
    private readonly Dictionary<PluginItemViewModel, InstanceMember> _rowsByPlugin = new Dictionary<PluginItemViewModel, InstanceMember>();
    private Action? _reloaded;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public PluginsRows(PluginsDialogViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>
    /// Raised when the view should rebuild its rows. The page never needs a rebuild (a toggle only refreshes its
    /// own row), but the view's subscription tells the page it is on screen, so it follows the plugins only for
    /// that long.
    /// </summary>
    public event Action? Reloaded
    {
        add
        {
            if (_reloaded == null)
            {
                foreach (PluginItemViewModel plugin in _viewModel.Plugins)
                {
                    plugin.PropertyChanged += HandlePluginChanged;
                }
            }
            _reloaded += value;
        }
        remove
        {
            _reloaded -= value;
            if (_reloaded == null)
            {
                foreach (PluginItemViewModel plugin in _viewModel.Plugins)
                {
                    plugin.PropertyChanged -= HandlePluginChanged;
                }
            }
        }
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        _rowsByPlugin.Clear();
        List<InstanceMember> pluginRows = new List<InstanceMember>();
        List<InstanceMember> requiredRows = new List<InstanceMember>();
        foreach (PluginItemViewModel plugin in _viewModel.Plugins)
        {
            PluginItemViewModel item = plugin;
            InstanceMember row = SettingsRows.Toggle("Plugin:" + item.Name, item.Name, HintFor(item),
                () => item.IsEnabled, value => item.IsEnabled = value, chip: ChipFor(item));
            row.IsReadOnly = !item.CanToggle;
            row.ToolTipText = item.ToolTip;
            row.WarningText = item.NotLoadedReason;
            _rowsByPlugin[item] = row;
            (item.IsRequired ? requiredRows : pluginRows).Add(row);
        }

        // One compact line per plugin. The plugins Gum cannot run without are folded away, so the list a
        // user can act on is short; a search that matches one opens the section.
        List<MemberCategory> sections = new List<MemberCategory>();
        if (pluginRows.Count > 0)
        {
            sections.Add(SettingsRows.Section("Installed plugins", pluginRows));
        }
        if (requiredRows.Count > 0)
        {
            sections.Add(SettingsRows.Section("Gum internals", requiredRows, isExpanded: false));
        }
        sections.Add(SettingsRows.Section("Folder scan", BuildScanRows()));
        SettingsRows.Finish(sections);
        return sections;
    }

    /// <summary>The state chip shown beside a plugin's toggle, or null for an ordinary running or turned-off plugin.</summary>
    public static string? ChipFor(PluginItemViewModel plugin) =>
        plugin.HasFailed ? "Failed"
        : plugin.IsNotLoaded ? "Not loaded"
        : plugin.IsRequired ? "Required"
        : plugin.IsBuiltIn ? "Built-in"
        : null;

    /// <summary>The hint under a plugin's toggle: what is wrong, or null for a healthy plugin so its row stays one line.</summary>
    public static string? HintFor(PluginItemViewModel plugin)
    {
        if (plugin.IsNotLoaded)
        {
            return "Found in the Plugins folder, but this version of Gum cannot run it.";
        }
        if (plugin.HasFailed)
        {
            return $"Crashed ({plugin.FailureDetails}) and was turned off. Turning it on asks first.";
        }
        return null;
    }

    private List<InstanceMember> BuildScanRows()
    {
        PluginScanReport? report = _viewModel.ScanReport;
        InstanceMember folder = SettingsRows.ReadOnly("PluginsFolder", "Plugins folder",
            "Where Gum looks for plugins. Copy scan puts the result on the clipboard for a bug report.",
            () => report?.FolderPath ?? "Plugins have not been loaded, so there is nothing to report.",
            chip: report == null ? null : report.FolderExists ? "Found" : "Not found");
        folder.DetailAction = new DetailTextLink("Copy scan", () => _viewModel.CopyDiagnosticsCommand.Execute(null));

        List<InstanceMember> rows = new List<InstanceMember> { folder };
        if (report != null)
        {
            foreach (PluginFileScan file in report.Files
                         .Where(x => x.Outcome == PluginFileOutcome.LoadFailed || (x.Outcome == PluginFileOutcome.Loaded && x.CouldContainPlugins))
                         .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase))
            {
                bool failed = file.Outcome == PluginFileOutcome.LoadFailed;
                rows.Add(SettingsRows.ReadOnly("ScanFile:" + file.FileName, file.FileName,
                    failed ? (file.Detail ?? "Could not be loaded.") : null,
                    () => failed ? "Failed to load" : "Loaded"));
            }
        }
        return rows;
    }

    // A toggle can change a plugin's state (a crash, a declined prompt), so its row reads the plugin again.
    private void HandlePluginChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is PluginItemViewModel plugin && _rowsByPlugin.TryGetValue(plugin, out InstanceMember? row))
        {
            row.ChipText = ChipFor(plugin);
            row.DetailText = HintFor(plugin);
            row.SimulateValueChanged();
        }
    }
}
