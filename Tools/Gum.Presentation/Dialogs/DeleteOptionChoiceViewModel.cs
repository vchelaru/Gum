using System.Collections.Generic;

namespace Gum.Services.Dialogs;

/// <summary>
/// A pick-one group of options in the delete confirmation (for example "Delete only parent(s)" versus
/// "Delete parent and children"). Each option is a <see cref="DeleteOptionCheckboxViewModel"/> whose
/// <see cref="DeleteOptionCheckboxViewModel.IsChecked"/> is its radio state, so a plugin reads the
/// user's pick the same way it reads a check box.
/// </summary>
public class DeleteOptionChoiceViewModel
{
    /// <summary>Creates a group with <paramref name="header"/> over <paramref name="options"/>.</summary>
    public DeleteOptionChoiceViewModel(string header, IReadOnlyList<DeleteOptionCheckboxViewModel> options)
    {
        Header = header;
        Options = options;
    }

    /// <summary>The group's caption.</summary>
    public string Header { get; }

    /// <summary>The options; exactly one should be checked.</summary>
    public IReadOnlyList<DeleteOptionCheckboxViewModel> Options { get; }
}
