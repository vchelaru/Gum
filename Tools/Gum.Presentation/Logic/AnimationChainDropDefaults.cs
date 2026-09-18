using System.IO;
using Gum.Content.AnimationChain;

namespace Gum.Logic;

/// <summary>
/// Decides which animation chain (if any) should be auto-selected when an .achx/.achj file is
/// dropped onto something that sets a SourceFile (issue #4824). Pure file-parsing logic, kept
/// separate from the editor-tab plugin's drag/drop handling so it's testable without its DI graph.
/// </summary>
public static class AnimationChainDropDefaults
{
    public static string? GetFirstChainNameOrNull(string absoluteFilePath)
    {
        if (!IsAnimationChainFile(absoluteFilePath) || !File.Exists(absoluteFilePath))
        {
            return null;
        }

        AnimationChainListSave animationChainListSave = AnimationChainListSave.FromFile(absoluteFilePath);

        return animationChainListSave.AnimationChains.Count > 0
            ? animationChainListSave.AnimationChains[0].Name
            : null;
    }

    private static bool IsAnimationChainFile(string fileName) =>
        fileName.EndsWith(".achx") || fileName.EndsWith(".achj");
}
