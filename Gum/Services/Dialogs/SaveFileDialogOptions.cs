using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Services.Dialogs;

public sealed record SaveFileDialogOptions
{
    public string? Title { get; init; }
    public string? InitialDirectory { get; init; }
    public string Filter { get; init; } = "All Files (*.*)|*.*";

    /// <summary>
    /// The file name initially shown in the dialog (the suggested name to save as).
    /// </summary>
    public string? FileName { get; init; }
}
