using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Gum.Avalonia.Dialogs;

/// <summary>What an open-file picker is asked to show. <see cref="StartDirectory"/> exists or is null.</summary>
public record OpenFilePickerRequest(string? Title, bool AllowMultiple, IReadOnlyList<FilePickerFileType> Filter, string? StartDirectory);

/// <summary>What a save-file picker is asked to show. <see cref="StartDirectory"/> exists or is null.</summary>
public record SaveFilePickerRequest(string? Title, string? FileName, IReadOnlyList<FilePickerFileType> Filter, string? StartDirectory);

/// <summary>What a folder picker is asked to show. <see cref="StartDirectory"/> exists or is null.</summary>
public record OpenFolderPickerRequest(string? Title, string? StartDirectory);

/// <summary>
/// The platform's native file pickers, opened modal over <c>owner</c>, answering with local paths.
/// <see cref="AvaloniaDialogService"/> goes through this so tests can see which window each picker
/// is opened over; Avalonia's storage types cannot be implemented outside Avalonia.
/// </summary>
public interface IFilePickers
{
    /// <summary>The picked files; empty when the user cancels.</summary>
    Task<IReadOnlyList<string>> OpenFileAsync(TopLevel owner, OpenFilePickerRequest request);

    /// <summary>The chosen file, or null when the user cancels.</summary>
    Task<string?> SaveFileAsync(TopLevel owner, SaveFilePickerRequest request);

    /// <summary>The picked folder, or null when the user cancels.</summary>
    Task<string?> OpenFolderAsync(TopLevel owner, OpenFolderPickerRequest request);
}

/// <summary>The <see cref="IFilePickers"/> over the owner window's <see cref="TopLevel.StorageProvider"/>.</summary>
public class StorageProviderFilePickers : IFilePickers
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> OpenFileAsync(TopLevel owner, OpenFilePickerRequest request)
    {
        IStorageProvider storage = owner.StorageProvider;
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = request.Title,
            AllowMultiple = request.AllowMultiple,
            FileTypeFilter = request.Filter,
            SuggestedStartLocation = await StartLocation(storage, request.StartDirectory),
        });
        return files.Select(file => file.Path.LocalPath).Where(path => !string.IsNullOrEmpty(path)).ToList();
    }

    /// <inheritdoc/>
    public async Task<string?> SaveFileAsync(TopLevel owner, SaveFilePickerRequest request)
    {
        IStorageProvider storage = owner.StorageProvider;
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = request.Title,
            SuggestedFileName = request.FileName,
            FileTypeChoices = request.Filter,
            SuggestedStartLocation = await StartLocation(storage, request.StartDirectory),
        });
        return file?.Path.LocalPath;
    }

    /// <inheritdoc/>
    public async Task<string?> OpenFolderAsync(TopLevel owner, OpenFolderPickerRequest request)
    {
        IStorageProvider storage = owner.StorageProvider;
        IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = request.Title,
            SuggestedStartLocation = await StartLocation(storage, request.StartDirectory),
        });
        return folders.Select(folder => folder.Path.LocalPath).FirstOrDefault(path => !string.IsNullOrEmpty(path));
    }

    private static async Task<IStorageFolder?> StartLocation(IStorageProvider storage, string? directory) =>
        directory == null ? null : await storage.TryGetFolderFromPathAsync(directory);
}
