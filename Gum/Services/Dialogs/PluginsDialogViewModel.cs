using System;
using System.Collections.ObjectModel;
using Gum.Plugins;

namespace Gum.Services.Dialogs;

public class PluginsDialogViewModel : DialogViewModel
{
    public string Title { get => Get<string>(); set => Set(value); }

    public ObservableCollection<PluginItemViewModel> Plugins { get; } = [];

    private readonly IDialogService dialogService;

    public PluginsDialogViewModel(IDialogService dialogService)
    {
        this.dialogService = dialogService;

        Title = "Manage Plugins";
        AffirmativeText = "Close";
        NegativeText = null;

        foreach (var container in PluginManager.AllPluginContainers)
        {
            Plugins.Add(new PluginItemViewModel(container, dialogService));
        }
    }
}

public class PluginItemViewModel : Mvvm.ViewModel
{
    private readonly PluginContainer container;
    private readonly IDialogService dialogService;

    public string DisplayText => container.ToString();

    public bool IsEnabled
    {
        get => container.IsEnabled;
        set
        {
            if (value == container.IsEnabled)
            {
                return;
            }

            if (!value)
            {
                PluginManager.ShutDownPlugin(container.Plugin, PluginShutDownReason.UserDisabled);
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayText));
            }
            else
            {
                TryEnablePlugin();
            }
        }
    }

    public PluginItemViewModel(PluginContainer container, IDialogService dialogService)
    {
        this.container = container;
        this.dialogService = dialogService;
    }

    private void TryEnablePlugin()
    {
        bool shouldEnable = true;

        if (!string.IsNullOrEmpty(container.FailureDetails))
        {
            shouldEnable = dialogService.ShowYesNoMessage(
                "The plugin " + container.Name + " has crashed so" +
                " it was disabled.  Are you sure you want to re-enable it?",
                "Re-enable crashed plugin?");
        }

        if (shouldEnable)
        {
            container.IsEnabled = true;
            try
            {
                container.Plugin.StartUp();
                PluginManager.ReenablePlugin(container.Plugin);
            }
            catch (Exception exception)
            {
                container.Fail(exception, "Failed in StartUp");
            }
        }

        NotifyPropertyChanged(nameof(IsEnabled));
        NotifyPropertyChanged(nameof(DisplayText));
    }
}
