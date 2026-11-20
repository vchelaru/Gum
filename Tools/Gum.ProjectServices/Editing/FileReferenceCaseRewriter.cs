using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Where a project stores a file path, found by <see cref="FileReferenceCaseRewriter.Find"/>.
/// </summary>
public class FileReferenceLocations
{
    /// <summary>The file variables (Sprite SourceFile, Text CustomFontFile, ...) holding the path, with the element each lives in.</summary>
    public List<(ElementSave Element, StateSave State, VariableSave Variable)> Variables { get; } = new();

    /// <summary>True when the project's single pixel texture setting holds the path.</summary>
    public bool IsSinglePixelTexture { get; set; }

    /// <summary>Positions in <see cref="GumProjectSave.LocalizationFiles"/> holding the path.</summary>
    public List<int> LocalizationFileIndexes { get; } = new();

    /// <summary>True when the project stores the path anywhere this class can rewrite.</summary>
    public bool Any => Variables.Count > 0 || IsSinglePixelTexture || LocalizationFileIndexes.Count > 0;
}

/// <summary>
/// Rewrites a file reference that differs from the file on disk only by case (GUM0008) to the on-disk
/// spelling. Handles the references the project file and element files store; a reference inside another
/// file (an <c>.achx</c> frame, a <c>.fnt</c> page) or an element's own file name is not rewritten.
/// </summary>
public static class FileReferenceCaseRewriter
{
    /// <summary>Finds every place <paramref name="project"/> stores <paramref name="referencedPath"/>.</summary>
    public static FileReferenceLocations Find(GumProjectSave project, string referencedPath)
    {
        FileReferenceLocations locations = new FileReferenceLocations();
        string wanted = Normalize(referencedPath);

        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Value is string value
                        && string.Equals(Normalize(value), wanted, StringComparison.Ordinal)
                        && GumProjectDependencyWalker.IsFileVariable(variable, element))
                    {
                        locations.Variables.Add((element, state, variable));
                    }
                }
            }
        }

        locations.IsSinglePixelTexture = !string.IsNullOrEmpty(project.SinglePixelTextureFile)
            && string.Equals(Normalize(project.SinglePixelTextureFile), wanted, StringComparison.Ordinal);

        for (int i = 0; i < project.LocalizationFiles.Count; i++)
        {
            if (string.Equals(Normalize(project.LocalizationFiles[i]), wanted, StringComparison.Ordinal))
            {
                locations.LocalizationFileIndexes.Add(i);
            }
        }

        return locations;
    }

    /// <summary>Stores <paramref name="newPath"/> in the variables of <paramref name="element"/> that <see cref="Find"/> listed.</summary>
    public static void ApplyToElement(FileReferenceLocations locations, ElementSave element, string newPath)
    {
        foreach ((ElementSave owner, _, VariableSave variable) in locations.Variables)
        {
            if (owner == element)
            {
                variable.Value = newPath;
            }
        }
    }

    /// <summary>Stores <paramref name="newPath"/> in the project's own settings that <see cref="Find"/> listed.</summary>
    public static void ApplyToProject(GumProjectSave project, FileReferenceLocations locations, string newPath)
    {
        if (locations.IsSinglePixelTexture)
        {
            project.SinglePixelTextureFile = newPath;
        }
        foreach (int index in locations.LocalizationFileIndexes)
        {
            project.LocalizationFiles[index] = newPath;
        }
    }

    /// <summary>
    /// True when a folder or file along <paramref name="onDiskPath"/> has a sibling that differs from it only
    /// by case, so the on-disk spelling cannot be told from the other one and the reference must not be rewritten.
    /// </summary>
    public static bool HasCaseTwin(string projectDirectory, string onDiskPath)
    {
        string current = projectDirectory;
        foreach (string segment in onDiskPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }
            if (segment == "..")
            {
                current = Path.GetDirectoryName(current.TrimEnd('/', '\\')) ?? current;
                continue;
            }
            if (!Directory.Exists(current))
            {
                return false;
            }
            int sameIgnoringCase = Directory.EnumerateFileSystemEntries(current)
                .Count(entry => string.Equals(Path.GetFileName(entry), segment, StringComparison.OrdinalIgnoreCase));
            if (sameIgnoringCase > 1)
            {
                return true;
            }
            current = Path.Combine(current, segment);
        }
        return false;
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
