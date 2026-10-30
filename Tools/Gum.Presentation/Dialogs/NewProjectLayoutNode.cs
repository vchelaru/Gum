using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Mvvm;
using Gum.ProjectServices;

namespace Gum.Dialogs;

/// <summary>
/// One folder in the New Project dialog's folder tree, or the Gum project file inside one.
/// </summary>
public class NewProjectLayoutNode : ViewModel
{
    /// <summary>The folder or file name, without a trailing slash.</summary>
    public string Name { get; }

    /// <summary>Whether this node is a folder.</summary>
    public bool IsFolder { get; }

    /// <summary>Whether this is the Gum project file, which the tree highlights.</summary>
    public bool IsMainFile { get; init; }

    /// <summary>The children, folders first, each group sorted by name.</summary>
    public IReadOnlyList<NewProjectLayoutNode> Children { get; }

    /// <summary>Whether a folder is open in the tree.</summary>
    public bool IsExpanded
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>Whether this is the top folder, whose name is the full path of the folder that will be created.</summary>
    public bool IsRoot { get; private set; }

    /// <summary>The name with a trailing separator for a folder; the root already ends with one.</summary>
    public string DisplayName => IsFolder && !IsRoot ? Name + System.IO.Path.DirectorySeparatorChar : Name;

    private static readonly char[] Separators = { '/', (char)92 };

    private NewProjectLayoutNode(string name, bool isFolder, IReadOnlyList<NewProjectLayoutNode> children)
    {
        Name = name;
        IsFolder = isFolder;
        Children = children;
    }

    /// <summary>
    /// Builds the tree for a layout. <paramref name="openDepth"/> folders deep start open; the rest start collapsed.
    /// </summary>
    /// <param name="layout">Where the creation puts its folders and the Gum project file.</param>
    /// <param name="rootName">The name shown for the root folder.</param>
    /// <param name="openDepth">How many levels of folders start open.</param>
    public static NewProjectLayoutNode Build(NewProjectLayout layout, string rootName, int openDepth)
    {
        FolderBuilder root = new FolderBuilder(rootName);
        foreach (string folder in layout.Folders)
        {
            root.GetFolder(folder.Split(Separators, StringSplitOptions.RemoveEmptyEntries));
        }

        string[] projectParts = layout.GumProjectFile.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        root.GetFolder(projectParts.Take(projectParts.Length - 1)).MainFile = projectParts[^1];
        return root.ToNode(openDepth, 0);
    }

    /// <summary>Opens or closes this folder and every folder under it.</summary>
    public void SetExpandedRecursive(bool expanded)
    {
        if (IsFolder)
        {
            IsExpanded = expanded;
        }
        foreach (NewProjectLayoutNode child in Children)
        {
            child.SetExpandedRecursive(expanded);
        }
    }

    private class FolderBuilder
    {
        private readonly string _name;
        private readonly SortedDictionary<string, FolderBuilder> _folders = new SortedDictionary<string, FolderBuilder>(StringComparer.Ordinal);

        public FolderBuilder(string name) => _name = name;

        public string? MainFile { get; set; }

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

        public NewProjectLayoutNode ToNode(int openDepth, int depth)
        {
            List<NewProjectLayoutNode> children = _folders.Values
                .Select(folder => folder.ToNode(openDepth, depth + 1))
                .ToList();
            if (MainFile != null)
            {
                children.Add(new NewProjectLayoutNode(MainFile, false, Array.Empty<NewProjectLayoutNode>()) { IsMainFile = true });
            }

            return new NewProjectLayoutNode(_name, true, children)
            {
                IsRoot = depth == 0,
                IsExpanded = depth < openDepth
            };
        }
    }
}
