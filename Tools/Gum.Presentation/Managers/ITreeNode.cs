using ToolsUtilities;

namespace Gum.Managers;

public interface ITreeNode
{
    object? Tag { get; }
    FilePath GetFullFilePath();
    ITreeNode? Parent { get; }

    /// <summary>
    /// The node's display label. Settable so callers (e.g. a folder rename) can update the label
    /// in place without depending on the concrete, WinForms-coupled tree node implementation.
    /// </summary>
    string Text { get; set; }

    string FullPath { get; }

    void Expand();
}
