using System.Collections.ObjectModel;

namespace Gum.Services.Dialogs;

/// <summary>
/// The delete confirmation as a framework-neutral dialog: a message plus the options plugins add
/// before it is shown (through <see cref="Plugins.BaseClasses.PluginBase.DeleteOptionsShow"/>). After
/// the user confirms, plugins read their options back in
/// <see cref="Plugins.BaseClasses.PluginBase.DeleteOptionsConfirmed"/>. The Avalonia head shows this
/// through <see cref="IDialogService"/>; the WPF head renders the same options into its
/// <c>DeleteOptionsWindow</c>.
/// </summary>
public class DeleteOptionsDialogViewModel : DialogViewModel
{
    /// <summary>Creates an empty Yes/No confirmation.</summary>
    public DeleteOptionsDialogViewModel()
    {
        AffirmativeText = "Yes";
        NegativeText = "No";
        Title = "Delete?";
        Message = string.Empty;
        CheckBoxes = new ObservableCollection<DeleteOptionCheckboxViewModel>();
        Choices = new ObservableCollection<DeleteOptionChoiceViewModel>();
    }

    /// <summary>The window title.</summary>
    public string Title { get => Get<string>(); set => Set(value); }

    /// <summary>What is about to be deleted.</summary>
    public string Message { get => Get<string>(); set => Set(value); }

    /// <summary>Check-box options, such as "Delete XML file".</summary>
    public ObservableCollection<DeleteOptionCheckboxViewModel> CheckBoxes { get; }

    /// <summary>Pick-one option groups, such as whether to delete children.</summary>
    public ObservableCollection<DeleteOptionChoiceViewModel> Choices { get; }
}
