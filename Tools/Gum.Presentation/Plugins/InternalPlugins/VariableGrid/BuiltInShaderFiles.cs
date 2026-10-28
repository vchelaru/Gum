using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The example shaders that ship with the tool, offered by a shader file variable's drop-down
/// alongside the files the project already has.
/// </summary>
public interface IBuiltInShaderFiles
{
    /// <summary>
    /// The file names of the shipped shaders to offer, alphabetically: all of them except those whose
    /// copy the project already holds. <paramref name="projectChoices"/> are the project-relative
    /// files the drop-down lists, in whatever spelling the project uses.
    /// </summary>
    IReadOnlyList<string> GetOffered(IReadOnlyList<string> projectChoices);

    /// <summary>
    /// Copies the shipped shader <paramref name="name"/> into the project's Shaders folder and returns
    /// its project-relative path. A copy already there that differs from the shipped file is replaced
    /// only if the user says yes to a prompt; otherwise, and when it is identical, it is kept as is.
    /// Null when the project has not been saved yet or the shader is not shipped.
    /// </summary>
    string? CopyIntoProject(string name);
}

/// <inheritdoc/>
public class BuiltInShaderFiles : IBuiltInShaderFiles
{
    private const string ProjectFolder = "Shaders";
    private static readonly string[] ShaderExtensions = { ".slang", ".fx" };

    private readonly IProjectState _projectState;
    private readonly IDialogService _dialogService;
    private readonly string _builtInFolder;

    /// <param name="builtInFolder">The folder the shipped shaders are staged in.</param>
    public BuiltInShaderFiles(IProjectState projectState, IDialogService dialogService, string builtInFolder)
    {
        _projectState = projectState;
        _dialogService = dialogService;
        _builtInFolder = builtInFolder;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetOffered(IReadOnlyList<string> projectChoices)
    {
        string? projectDirectory = _projectState.ProjectDirectory;
        HashSet<FilePath> held = new HashSet<FilePath>();
        if (projectDirectory != null)
        {
            foreach (string choice in projectChoices)
            {
                held.Add(new FilePath(Path.GetFullPath(Path.Combine(projectDirectory, choice.Replace('\\', '/')))));
            }
        }

        return GetShippedNames()
            .Where(name => projectDirectory == null || !held.Contains(new FilePath(GetCopyPath(projectDirectory, name))))
            .ToList();
    }

    /// <inheritdoc/>
    public string? CopyIntoProject(string name)
    {
        string? projectDirectory = _projectState.ProjectDirectory;
        if (projectDirectory == null || !GetShippedNames().Contains(name, StringComparer.Ordinal))
        {
            return null;
        }

        string copyPath = GetCopyPath(projectDirectory, name);
        string shippedPath = Path.Combine(_builtInFolder, name);
        if (!File.Exists(copyPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
            File.Copy(shippedPath, copyPath);
        }
        else if (!File.ReadAllBytes(copyPath).AsSpan().SequenceEqual(File.ReadAllBytes(shippedPath)) &&
            _dialogService.ShowYesNoMessage($"Replace your {ProjectFolder}/{name} with the built-in?", "Replace shader?"))
        {
            File.Copy(shippedPath, copyPath, overwrite: true);
        }

        return ProjectFolder + "/" + name;
    }

    private static string GetCopyPath(string projectDirectory, string name) =>
        Path.GetFullPath(Path.Combine(projectDirectory, ProjectFolder, name));

    private IEnumerable<string> GetShippedNames()
    {
        if (!Directory.Exists(_builtInFolder))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateFiles(_builtInFolder)
            .Select(file => Path.GetFileName(file))
            .Where(name => ShaderExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal);
    }
}
