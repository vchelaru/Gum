using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Gum.Plugins;

namespace Gum.Services.Dialogs;

/// <summary>
/// View model backing the "Manage Plugins" dialog. Lists every loaded plugin and lets the user
/// enable/disable them via <see cref="IPluginManager"/>.
/// </summary>
public class PluginsDialogViewModel : DialogViewModel
{
    public string Title { get => Get<string>(); set => Set(value); }

    public ObservableCollection<PluginItemViewModel> Plugins { get; } = [];

    /// <summary>
    /// What the plugin-folder scan found, as copyable text. A plugin missing from the list above is
    /// otherwise indistinguishable from one that was never installed.
    /// </summary>
    public string Diagnostics { get; }

    /// <summary>
    /// Puts <see cref="Diagnostics"/> on the clipboard. The point of the scan is to end up in a bug
    /// report, and selecting several screens of text by hand is a poor way to get it there.
    /// </summary>
    public RelayCommand CopyDiagnosticsCommand { get; }

    public PluginsDialogViewModel(IDialogService dialogService, IPluginManager pluginManager,
        IClipboardService clipboardService)
    {
        Title = "Manage Plugins";
        AffirmativeText = "Close";
        NegativeText = null;

        // Sorted by name so a plugin can be found by scanning; MEF hands them back in load order,
        // which is effectively arbitrary.
        foreach (PluginSummary summary in pluginManager.GetAllPluginSummaries()
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            Plugins.Add(new PluginItemViewModel(summary, pluginManager, dialogService));
        }

        PluginScanReport? scanReport = pluginManager.GetPluginScanReport();

        // After the running plugins: these were found but never loaded, so they cannot be turned on.
        foreach (RefusedPlugin refused in (scanReport?.RefusedPlugins ?? [])
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            Plugins.Add(PluginItemViewModel.ForRefused(refused, pluginManager, dialogService));
        }

        Diagnostics = scanReport?.Describe()
            ?? "Plugins have not been loaded, so there is nothing to report.";

        CopyDiagnosticsCommand = new RelayCommand(() => clipboardService.SetText(Diagnostics));
    }
}

/// <summary>
/// A single row in the "Manage Plugins" dialog. Wraps a <see cref="PluginSummary"/> snapshot and
/// re-fetches it from <see cref="IPluginManager"/> whenever the user toggles the plugin.
/// </summary>
public class PluginItemViewModel : Mvvm.ViewModel
{
    private readonly IPluginManager pluginManager;
    private readonly IDialogService dialogService;
    private PluginSummary summary;
    private readonly RefusedPlugin? _refused;

    public string DisplayText => _refused != null ? $"{_refused.Name} (not loaded)" : summary.DisplayText;

    /// <summary>
    /// Whether the checkbox is offered. A plugin the tool needs cannot be turned off, but one that
    /// crashed can still be turned back on. One that was never loaded cannot be turned on.
    /// </summary>
    public bool CanToggle => _refused == null && (summary.CanBeDisabled || !summary.IsEnabled);

    /// <summary>Explains a checkbox that is not offered; null when it is.</summary>
    public string? ToolTip =>
        _refused != null ? "Not loaded: " + _refused.Reason
        : CanToggle ? null
        : "Gum needs this plugin, so it cannot be turned off.";

    public bool IsEnabled
    {
        get => summary.IsEnabled;
        set
        {
            if (value == summary.IsEnabled || _refused != null)
            {
                return;
            }

            if (!value)
            {
                summary = pluginManager.DisableUserPlugin(summary.PluginHandle);
                NotifySummaryChanged();
            }
            else
            {
                TryEnablePlugin();
            }
        }
    }

    public PluginItemViewModel(PluginSummary summary, IPluginManager pluginManager, IDialogService dialogService)
        : this(summary, null, pluginManager, dialogService)
    {
    }

    private PluginItemViewModel(PluginSummary summary, RefusedPlugin? refused, IPluginManager pluginManager,
        IDialogService dialogService)
    {
        this.summary = summary;
        _refused = refused;
        this.pluginManager = pluginManager;
        this.dialogService = dialogService;
    }

    /// <summary>A row for a plugin that was found but not loaded: unchecked, and never toggleable.</summary>
    public static PluginItemViewModel ForRefused(RefusedPlugin refused, IPluginManager pluginManager,
        IDialogService dialogService) =>
        new(new PluginSummary(refused.Name, refused.Name, IsEnabled: false, HasFailureDetails: false, PluginHandle: refused,
            CanBeDisabled: false), refused, pluginManager, dialogService);

    private void TryEnablePlugin()
    {
        bool shouldEnable = true;

        if (summary.HasFailureDetails)
        {
            shouldEnable = dialogService.ShowYesNoMessage(
                "The plugin " + summary.Name + " has crashed so" +
                " it was disabled.  Are you sure you want to re-enable it?",
                "Re-enable crashed plugin?");
        }

        if (shouldEnable)
        {
            summary = pluginManager.TryEnablePlugin(summary.PluginHandle);
        }

        NotifySummaryChanged();
    }

    private void NotifySummaryChanged()
    {
        NotifyPropertyChanged(nameof(IsEnabled));
        NotifyPropertyChanged(nameof(DisplayText));
        NotifyPropertyChanged(nameof(CanToggle));
        NotifyPropertyChanged(nameof(ToolTip));
    }
}
