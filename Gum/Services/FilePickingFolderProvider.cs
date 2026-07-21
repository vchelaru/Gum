using WpfDataUi.Controls;

namespace Gum.Services;

/// <inheritdoc cref="IFilePickingFolderProvider"/>
public class FilePickingFolderProvider : IFilePickingFolderProvider
{
    public string FolderRelativeTo
    {
        set => FilePickingLogic.FolderRelativeTo = value;
    }
}
