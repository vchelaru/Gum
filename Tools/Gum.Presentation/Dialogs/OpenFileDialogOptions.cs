using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Services.Dialogs;

public sealed record OpenFileDialogOptions
{
    public string? Title { get; init; }
    public string? InitialDirectory { get; init; }
    public string Filter { get; init; } = "All Files (*.*)|*.*";
    public bool Multiselect { get; init; }
}