using ToolsUtilities;

namespace Gum.Managers;

public interface ITreeNode
{
    object? Tag { get; }
    FilePath GetFullFilePath();
    ITreeNode? Parent { get; }
    string Text { get; }
    string FullPath { get; }

    void Expand();
}
