namespace Gum.Services;

/// <summary>
/// Sets the base folder used to resolve relative paths in file-picking controls (e.g. "reveal in
/// Explorer" on a file-selection property). Abstracts the WPF-typed
/// <c>WpfDataUi.Controls.FilePickingLogic.FolderRelativeTo</c> static so consumers can set it
/// without referencing <c>WpfDataUi</c> (ADR-0005). See
/// <see cref="Gum.Services.FilePickingFolderProvider"/> for the concrete implementation (tool project).
/// </summary>
public interface IFilePickingFolderProvider
{
    /// <summary>The base directory relative-path resolution should use.</summary>
    string FolderRelativeTo { set; }
}
