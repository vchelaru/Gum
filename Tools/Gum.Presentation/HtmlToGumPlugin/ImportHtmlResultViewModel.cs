using CommunityToolkit.Mvvm.Input;
using Gum.Services.Dialogs;

namespace HtmlToGumPlugin;

/// <summary>
/// The dialog shown after an HTML import: a one-line summary, and the converter log behind a
/// Show details toggle. Either head supplies the view.
/// </summary>
public class ImportHtmlResultViewModel : DialogViewModel
{
    /// <summary>Creates the dialog over <paramref name="summary"/> and the optional <paramref name="details"/>.</summary>
    public ImportHtmlResultViewModel(string summary, string details)
    {
        Summary = summary;
        Details = details;
        AffirmativeText = "OK";
        NegativeText = null;
        ToggleDetailsCommand = new RelayCommand(() => IsDetailsVisible = !IsDetailsVisible);
    }

    /// <summary>The window title.</summary>
    public string Title => "Import HTML";

    /// <summary>What happened, in a line.</summary>
    public string Summary { get; }

    /// <summary>The converter log, shown on request.</summary>
    public string Details { get; }

    /// <summary>Whether there is a log to show at all.</summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    /// <summary>Whether the log is expanded.</summary>
    public bool IsDetailsVisible
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                NotifyPropertyChanged(nameof(DetailsButtonText));
            }
        }
    }

    /// <summary>The toggle's caption for the current state.</summary>
    public string DetailsButtonText => IsDetailsVisible ? "Hide details ▴" : "Show details ▾";

    /// <summary>Expands or collapses the log.</summary>
    public RelayCommand ToggleDetailsCommand { get; }
}
