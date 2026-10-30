using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Mvvm;
using Gum.ProjectServices;

namespace Gum.Dialogs;

/// <summary>
/// One folder or file in the New Project dialog's Files tree. A folder shows how many files are under it.
/// </summary>
public class NewProjectPlanNode : ViewModel
{
    /// <summary>The folder or file name, without a trailing slash.</summary>
    public string Name { get; }

    /// <summary>Whether this node is a folder.</summary>
    public bool IsFolder { get; }

    /// <summary>Whether this is the Gum project file, which the tree highlights.</summary>
    public bool IsMainFile { get; init; }

    /// <summary>The number of files under a folder, at any depth; 0 for a file.</summary>
    public int FileCount { get; private set; }

    /// <summary>The children, folders first, each group sorted by name.</summary>
    public IReadOnlyList<NewProjectPlanNode> Children { get; }

    /// <summary>Whether a folder is open in the tree.</summary>
    public bool IsExpanded
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>The name with a trailing slash for a folder.</summary>
    public string DisplayName => IsFolder ? Name + "/" : Name;

    /// <summary>The count shown after a folder's name, empty for a file.</summary>
    public string CountText => IsFolder ? FileCount.ToString() : string.Empty;

    private NewProjectPlanNode(string name, bool isFolder, IReadOnlyList<NewProjectPlanNode> children)
    {
        Name = name;
        IsFolder = isFolder;
        Children = children;
    }

    /// <summary>
    /// Builds the tree for a plan. <paramref name="openDepth"/> folders deep start open; the rest start collapsed.
    /// </summary>
    /// <param name="plan">What creation will write.</param>
    /// <param name="rootName">The name shown for the root folder.</param>
    /// <param name="mainFile">The plan-relative path of the Gum project file, highlighted; may be null.</param>
    /// <param name="openDepth">How many levels of folders start open.</param>
    public static NewProjectPlanNode Build(NewProjectPlan plan, string rootName, string? mainFile, int openDepth)
    {
        FolderBuilder root = new FolderBuilder(rootName);
        foreach (string directory in plan.Directories)
        {
            root.GetFolder(directory.Split('/'));
        }
        foreach (string file in plan.Files)
        {
            string[] parts = file.Split('/');
            root.GetFolder(parts.Take(parts.Length - 1)).Files.Add(parts[^1]);
        }
        return root.ToNode(mainFile, string.Empty, openDepth, 0);
    }

    /// <summary>Opens or closes this folder and every folder under it.</summary>
    public void SetExpandedRecursive(bool expanded)
    {
        if (IsFolder)
        {
            IsExpanded = expanded;
        }
        foreach (NewProjectPlanNode child in Children)
        {
            child.SetExpandedRecursive(expanded);
        }
    }

    private class FolderBuilder
    {
        private readonly string _name;
        private readonly SortedDictionary<string, FolderBuilder> _folders = new SortedDictionary<string, FolderBuilder>(StringComparer.Ordinal);

        public FolderBuilder(string name) => _name = name;

        public List<string> Files { get; } = new List<string>();

        public FolderBuilder GetFolder(IEnumerable<string> parts)
        {
            FolderBuilder current = this;
            foreach (string part in parts)
            {
                if (!current._folders.TryGetValue(part, out FolderBuilder? next))
                {
                    next = new FolderBuilder(part);
                    current._folders[part] = next;
                }
                current = next;
            }
            return current;
        }

        public NewProjectPlanNode ToNode(string? mainFile, string path, int openDepth, int depth)
        {
            List<NewProjectPlanNode> children = new List<NewProjectPlanNode>();
            foreach (FolderBuilder folder in _folders.Values)
            {
                string childPath = path.Length == 0 ? folder._name : path + "/" + folder._name;
                children.Add(folder.ToNode(mainFile, childPath, openDepth, depth + 1));
            }
            foreach (string file in Files.OrderBy(name => name, StringComparer.Ordinal))
            {
                string filePath = path.Length == 0 ? file : path + "/" + file;
                children.Add(new NewProjectPlanNode(file, false, Array.Empty<NewProjectPlanNode>())
                {
                    IsMainFile = filePath == mainFile
                });
            }

            NewProjectPlanNode node = new NewProjectPlanNode(_name, true, children)
            {
                FileCount = children.Sum(child => child.IsFolder ? child.FileCount : 1)
            };
            node.IsExpanded = depth < openDepth;
            return node;
        }
    }
}
