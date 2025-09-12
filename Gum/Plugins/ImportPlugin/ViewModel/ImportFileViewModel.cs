using CommunityToolkit.Mvvm.Input;
using Gum.Services.Dialogs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public abstract partial class ImportBaseDialogViewModel : DialogViewModel
{
    private readonly IDialogService _dialogService;

    public abstract string Title { get; }
    public abstract string BrowseFileFilter { get; }

    public string? SearchText 
    { 
        get => Get<string?>(); 
        set
        {
            if (Set(value))
            {
                FilteredFiles.Refresh();
            }
        }
    }

    public ObservableCollection<string> UnfilteredFiles { get; } = [];
    public ICollectionView FilteredFiles { get; }
    public ObservableCollection<string> SelectedFiles { get; } = [];

    public override bool CanExecuteAffirmative() => SelectedFiles.Any();

    protected ImportBaseDialogViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        AffirmativeText = "Import";
        NegativeText = "Cancel";
        FilteredFiles = CollectionViewSource.GetDefaultView(UnfilteredFiles);
        FilteredFiles.Filter = Filter;
        SelectedFiles.CollectionChanged += (_, _) => AffirmativeCommand.NotifyCanExecuteChanged();
    }

    private bool Filter(object item) =>
        item is string val &&
        (string.IsNullOrWhiteSpace(SearchText) ||
        val.ToLowerInvariant().Contains(SearchText!.ToLowerInvariant()));

    [RelayCommand]
    private void Browse()
    {
        OpenFileDialogOptions options = new()
        {
            // false for now. Components support it, but screens don't
            //Multiselect = true,
            Filter = BrowseFileFilter
        };

        if (_dialogService.OpenFile(options) is { Count: > 0 } files)
        {
            SelectedFiles.Clear();
            SelectedFiles.AddRange(files);
            AffirmativeCommand.Execute(null);
        }
    }
}