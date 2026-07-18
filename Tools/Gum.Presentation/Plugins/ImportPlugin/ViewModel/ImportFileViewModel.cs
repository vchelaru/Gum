using CommunityToolkit.Mvvm.Input;
using Gum.Services.Dialogs;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public abstract partial class ImportBaseDialogViewModel : DialogViewModel
{
    private readonly IDialogService _dialogService;
    private readonly ObservableCollection<string> _filteredFiles = [];

    public abstract string Title { get; }
    public abstract string BrowseFileFilter { get; }

    public string? SearchText
    {
        get => Get<string?>();
        set
        {
            if (Set(value))
            {
                RefreshFilteredFiles();
            }
        }
    }

    public ObservableCollection<string> UnfilteredFiles { get; } = [];

    /// <summary>
    /// A live, search-filtered view of <see cref="UnfilteredFiles"/>, rebuilt whenever
    /// <see cref="SearchText"/> or <see cref="UnfilteredFiles"/> changes. Backed by a plain
    /// ObservableCollection rather than WPF's ICollectionView/CollectionViewSource so this VM can
    /// live in the headless Gum.Presentation assembly, which never references WPF.
    /// </summary>
    public ReadOnlyObservableCollection<string> FilteredFiles { get; }

    public ObservableCollection<string> SelectedFiles { get; } = [];

    public override bool CanExecuteAffirmative() => SelectedFiles.Any();

    protected ImportBaseDialogViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        AffirmativeText = "Import";
        NegativeText = "Cancel";
        FilteredFiles = new ReadOnlyObservableCollection<string>(_filteredFiles);
        UnfilteredFiles.CollectionChanged += (_, _) => RefreshFilteredFiles();
        SelectedFiles.CollectionChanged += (_, _) => AffirmativeCommand.NotifyCanExecuteChanged();
    }

    private void RefreshFilteredFiles()
    {
        _filteredFiles.Clear();
        _filteredFiles.AddRange(UnfilteredFiles.Where(Matches));
    }

    private bool Matches(string value) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        value.ToLowerInvariant().Contains(SearchText!.ToLowerInvariant());

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