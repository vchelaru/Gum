using Gum.Services.Dialogs;
using System.Collections.Generic;

namespace ImportFromGumxPlugin.ViewModels;

/// <summary>
/// Read-only modal that lists the per-variable / per-category differences for one Standard
/// that the import dialog has flagged as differing from the destination (#2779).
/// </summary>
public class StandardDiffDetailsViewModel : DialogViewModel
{
    /// <summary>Name of the Standard whose diff is being shown (used as the dialog title).</summary>
    public string StandardName { get; }

    /// <summary>Flattened diff rows for display.</summary>
    public IReadOnlyList<StandardDiffRowViewModel> Rows { get; }

    public StandardDiffDetailsViewModel(string standardName, IReadOnlyList<StandardDiffRowViewModel> rows)
    {
        StandardName = standardName;
        Rows = rows;
        AffirmativeText = "Close";
        NegativeText = null;
    }
}
