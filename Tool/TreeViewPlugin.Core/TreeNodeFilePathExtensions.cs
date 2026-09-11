using System;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.DataTypes.Behaviors;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Resolves the file or folder a tree node stands for, by walking up to the top-level container
/// node and re-deriving the path from the project's own directory layout rather than storing it.
/// </summary>
/// <remarks>
/// Named <c>GetTreeNodeFullFilePath</c> rather than <c>GetFullFilePath</c> because
/// <see cref="ITreeNode"/> already declares an instance member of the latter name, which would
/// always win over an extension method - a node's own implementation calls straight into here.
/// </remarks>
public static class TreeNodeFilePathExtensions
{
    public static FilePath? GetTreeNodeFullFilePath(this ITreeNode treeNode)
    {
        if (treeNode.IsTopComponentContainerTreeNode() ||
            treeNode.IsTopStandardElementTreeNode() ||
            treeNode.IsTopScreenContainerTreeNode() ||
            treeNode.IsTopBehaviorTreeNode())
        {
            // Locator is retained here (not ctor-injected): this is a static extension method, so
            // there is no instance to hold injected IProjectManager/IDialogService fields. Both are
            // DI-registered; the only blocker to draining is the static context.
            IProjectManager projectManager = Locator.GetRequiredService<IProjectManager>();

            if (projectManager.GumProjectSave == null ||
                string.IsNullOrEmpty(projectManager.GumProjectSave.FullFileName))
            {
                Locator.GetRequiredService<IDialogService>()
                    .ShowMessage("Project isn't saved yet so the root of the project isn't known");
                return null;
            }

            string projectDirectory = FileManager.GetDirectory(projectManager.GumProjectSave.FullFileName);

            if (treeNode.IsTopComponentContainerTreeNode())
            {
                return projectDirectory + ElementReference.ComponentSubfolder + "\\";
            }
            if (treeNode.IsTopStandardElementTreeNode())
            {
                return projectDirectory + ElementReference.StandardSubfolder + "\\";
            }
            if (treeNode.IsTopScreenContainerTreeNode())
            {
                return projectDirectory + ElementReference.ScreenSubfolder + "\\";
            }
            if (treeNode.IsTopBehaviorTreeNode())
            {
                return projectDirectory + BehaviorReference.Subfolder + "\\";
            }

            throw new InvalidOperationException();
        }

        // Extensions follow the open project's own format so a .gumj project resolves
        // .gusj/.gucj/.gutj/.behj (issue #4595).
        bool isJsonFormat = GumProjectSave.IsJsonFormat(
            Locator.GetRequiredService<IProjectManager>().GumProjectSave?.FullFileName ?? "");

        if (treeNode.IsStandardElementTreeNode() ||
            treeNode.IsComponentTreeNode() ||
            treeNode.IsScreenTreeNode())
        {
            ElementSave element = (ElementSave)treeNode.Tag!;
            return treeNode.Parent!.GetTreeNodeFullFilePath() + treeNode.Text + "." + element.GetFileExtension(isJsonFormat);
        }

        if (treeNode.IsBehaviorTreeNode())
        {
            return treeNode.Parent!.GetTreeNodeFullFilePath() + treeNode.Text + "." +
                (isJsonFormat ? BehaviorReference.JsonExtension : BehaviorReference.Extension);
        }

        return treeNode.Parent!.GetTreeNodeFullFilePath() + treeNode.Text + "\\";
    }
}
