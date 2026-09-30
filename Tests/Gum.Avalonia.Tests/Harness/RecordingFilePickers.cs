using Avalonia.Controls;
using Gum.Avalonia.Dialogs;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A stand-in for the native file pickers: records which window each picker was opened over and
/// what it was asked to show, and answers with <see cref="Answer"/> (empty is the user cancelling).
/// </summary>
internal sealed class RecordingFilePickers : IFilePickers
{
    public RecordingFilePickers()
    {
        Answer = new List<string>();
        Owners = new List<TopLevel>();
    }

    public List<string> Answer { get; set; }

    /// <summary>The window each picker was opened over, in order.</summary>
    public List<TopLevel> Owners { get; }

    public OpenFilePickerRequest? LastOpenFile { get; private set; }

    public SaveFilePickerRequest? LastSaveFile { get; private set; }

    public OpenFolderPickerRequest? LastOpenFolder { get; private set; }

    public Task<IReadOnlyList<string>> OpenFileAsync(TopLevel owner, OpenFilePickerRequest request)
    {
        Owners.Add(owner);
        LastOpenFile = request;
        return Task.FromResult<IReadOnlyList<string>>(Answer.ToList());
    }

    public Task<string?> SaveFileAsync(TopLevel owner, SaveFilePickerRequest request)
    {
        Owners.Add(owner);
        LastSaveFile = request;
        return Task.FromResult(Answer.FirstOrDefault());
    }

    public Task<string?> OpenFolderAsync(TopLevel owner, OpenFolderPickerRequest request)
    {
        Owners.Add(owner);
        LastOpenFolder = request;
        return Task.FromResult(Answer.FirstOrDefault());
    }
}
