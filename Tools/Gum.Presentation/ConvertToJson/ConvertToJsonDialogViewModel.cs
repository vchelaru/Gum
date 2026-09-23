using System;
using Gum.Services.Dialogs;

namespace ConvertToJsonPlugin;

/// <summary>
/// The "Convert to JSON" confirmation: says what conversion writes and where the game must load from
/// afterward, and offers (opt-in) to move the original XML files to the OS trash once the JSON
/// project is open (issue #4926).
/// </summary>
public class ConvertToJsonDialogViewModel : DialogViewModel
{
    /// <summary>What the OS calls its trash on this platform.</summary>
    public static string TrashName => OperatingSystem.IsWindows() ? "Recycle Bin" : "Trash";

    /// <summary>Creates the dialog for converting the project whose XML file is <paramref name="xmlProjectFileName"/>.</summary>
    public ConvertToJsonDialogViewModel(string xmlProjectFileName, string jsonProjectFileName)
    {
        AffirmativeText = "Convert";
        NegativeText = "Cancel";

        Message =
            $"Gum will write a JSON copy of every file in this project next to the existing XML files, " +
            $"then reopen the project from {jsonProjectFileName}.\n\n" +
            $"Your game must then load {jsonProjectFileName} instead of {xmlProjectFileName}.";
        RecycleCheckBoxText = $"Move the original XML files to the {TrashName} after converting";
        RecycleWarning =
            $"The .gumx, .gusx, .gucx, .gutx, .behx and .ganx files this project uses will be removed " +
            $"from the project folder. A game that still loads {xmlProjectFileName} will fail to load " +
            $"until it is changed to {jsonProjectFileName}.";
    }

    /// <summary>The window title.</summary>
    public string Title => "Convert to JSON";

    /// <summary>What conversion does and what the game must change afterward.</summary>
    public string Message { get; }

    /// <summary>The label of the opt-in recycle check box.</summary>
    public string RecycleCheckBoxText { get; }

    /// <summary>Shown while <see cref="ShouldRecycleXmlFiles"/> is checked: which files go away and what breaks.</summary>
    public string RecycleWarning { get; }

    /// <summary>Whether to move the converted XML files to the OS trash once the JSON project is open. Off by default.</summary>
    public bool ShouldRecycleXmlFiles { get => Get<bool>(); set => Set(value); }
}
