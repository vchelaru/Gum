namespace Gum.Services.Dialogs;

public sealed record OpenFolderDialogOptions
{
    public string? Title { get; init; }
    public string? InitialDirectory { get; init; }
}
