using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    public PluginsDialogViewModel(IDialogService dialogService, IPluginManager pluginManager)
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

        Diagnostics = pluginManager.GetPluginScanReport()?.Describe()
            ?? "Plugins have not been loaded, so there is nothing to report.";
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

    public string DisplayText => summary.DisplayText;

    public bool IsEnabled
    {
        get => summary.IsEnabled;
        set
        {
            if (value == summary.IsEnabled)
            {
                return;
            }

            if (!value)
            {
                summary = pluginManager.DisableUserPlugin(summary.PluginHandle);
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayText));
            }
            else
            {
                TryEnablePlugin();
            }
        }
    }

    public PluginItemViewModel(PluginSummary summary, IPluginManager pluginManager, IDialogService dialogService)
    {
        this.summary = summary;
        this.pluginManager = pluginManager;
        this.dialogService = dialogService;
    }

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

        NotifyPropertyChanged(nameof(IsEnabled));
        NotifyPropertyChanged(nameof(DisplayText));
    }
}
