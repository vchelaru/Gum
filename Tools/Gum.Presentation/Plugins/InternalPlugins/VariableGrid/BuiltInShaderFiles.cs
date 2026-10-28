using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Managers;
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
    /// its project-relative path. A copy already there that differs from the shipped file (line endings
    /// aside) is replaced only if the user picks "Replace with built-in"; otherwise, and when it is
    /// the same, it is kept as is. Closing the prompt keeps the copy.
    /// Null when the project has not been saved yet or the shader is not shipped.
    /// </summary>
    string? CopyIntoProject(string name);
}

/// <inheritdoc/>
public class BuiltInShaderFiles : IBuiltInShaderFiles
{
    private const string ProjectFolder = "Shaders";
    private const string ReplaceKey = "replace";
    private const string KeepKey = "keep";
    private static readonly string[] ShaderExtensions = { ".slang", ".fx" };

    private readonly IProjectState _projectState;
    private readonly IDialogService _dialogService;
    private readonly IOutputManager _outputManager;
    private readonly string _builtInFolder;

    /// <param name="builtInFolder">The folder the shipped shaders are staged in.</param>
    public BuiltInShaderFiles(IProjectState projectState, IDialogService dialogService, IOutputManager outputManager, string builtInFolder)
    {
        _projectState = projectState;
        _dialogService = dialogService;
        _outputManager = outputManager;
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
        else
        {
            ReplaceCopyIfUserAgrees(name, copyPath, shippedPath);
        }

        return ProjectFolder + "/" + name;
    }

    // A copy that cannot be read or written is kept and reported; the shader is still assigned.
    private void ReplaceCopyIfUserAgrees(string name, string copyPath, string shippedPath)
    {
        string relativePath = ProjectFolder + "/" + name;
        try
        {
            byte[] copyBytes = File.ReadAllBytes(copyPath);
            byte[] shippedBytes = File.ReadAllBytes(shippedPath);
            if (WithoutCarriageReturnLineFeeds(copyBytes).AsSpan().SequenceEqual(WithoutCarriageReturnLineFeeds(shippedBytes)))
            {
                return;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _outputManager.AddError($"Could not compare {relativePath} with the built-in, so it was left as is: {e.Message}");
            return;
        }

        if (!UserWantsBuiltIn(name, relativePath))
        {
            return;
        }

        try
        {
            File.Copy(shippedPath, copyPath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _outputManager.AddError($"Could not replace {relativePath} with the built-in, so it was left as is: {e.Message}");
        }
    }

    // "Keep my copy" is listed first so it is the choice Enter confirms; closing the dialog is also a keep.
    private bool UserWantsBuiltIn(string name, string relativePath)
    {
        DialogChoices<string> choices = new()
        {
            [KeepKey] = "Keep my copy",
            [ReplaceKey] = "Replace with built-in",
        };
        string message =
            $"Your project's {relativePath} differs from the built-in {name}.\n\n" +
            "Replace with built-in: overwrites your file with the built-in one.\n" +
            "Keep my copy: uses your file as it is.\n\n" +
            "The shader is assigned either way.";

        return _dialogService.ShowChoices(message, choices, title: "Built-in shader differs", canCancel: true) == ReplaceKey;
    }

    // Compares equal for files that differ only in CRLF against LF. Other bytes, including a lone CR, are kept.
    private static byte[] WithoutCarriageReturnLineFeeds(byte[] bytes)
    {
        List<byte> result = new List<byte>(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\r' && i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n')
            {
                continue;
            }
            result.Add(bytes[i]);
        }
        return result.ToArray();
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
