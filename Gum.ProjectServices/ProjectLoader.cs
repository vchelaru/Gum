using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ProjectLoader : IProjectLoader
{
    /// <inheritdoc/>
    public ProjectLoadResult Load(string filePath)
    {
        var result = new ProjectLoadResult();

        if (!File.Exists(filePath))
        {
            result.ErrorMessage = $"Project file not found: {filePath}";
            return result;
        }

        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "";

        var gumLoadResult = new GumLoadResult();
        try
        {
            var project = GumProjectSave.Load(filePath, out gumLoadResult);
            result.Project = project;

            if (project == null)
            {
                result.ErrorMessage = gumLoadResult.ErrorMessage ?? "Failed to load project";
            }
            else
            {
                result.LoadErrors.AddRange(
                    ParseElementLoadErrors(gumLoadResult.ErrorMessage));
                result.LoadErrors.AddRange(
                    ConvertMissingFiles(gumLoadResult.MissingFiles, projectDirectory));
                result.LoadErrors.AddRange(
                    DetectSilentlyDroppedContent(project, projectDirectory));
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Failed to load project: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Parses the concatenated error string from <see cref="GumLoadResult.ErrorMessage"/>
    /// into individual <see cref="ErrorResult"/> entries.
    /// The format from GumProjectSave is: "\nError loading Name:\nmessage"
    /// and name mismatches as: "\nThe project references an element named ..."
    /// </summary>
    private static List<ErrorResult> ParseElementLoadErrors(string? errorMessage)
    {
        var errors = new List<ErrorResult>();

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return errors;
        }

        string[] lines = errorMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("Error loading ") && line.EndsWith(":"))
            {
                string elementName = line.Substring(
                    "Error loading ".Length,
                    line.Length - "Error loading ".Length - 1);
                string message = (i + 1 < lines.Length)
                    ? lines[i + 1].Trim()
                    : "Unknown error";
                errors.Add(new ErrorResult
                {
                    ElementName = elementName,
                    Message = $"Malformed XML: {message}",
                    Severity = ErrorSeverity.Error
                });
                i += 2;
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                errors.Add(new ErrorResult
                {
                    ElementName = "",
                    Message = line,
                    Severity = ErrorSeverity.Warning
                });
                i++;
            }
            else
            {
                i++;
            }
        }

        return errors;
    }

    // XmlSerializer silently ignores unknown elements. The patterns below are wrong XML element
    // names or wrapper elements that an AI (or developer) might write instead of the correct ones.
    // Each entry is (bad pattern to detect, message describing the fix).

    /// <summary>Wrong patterns valid for all element file types (components, screens, standards, behaviors).</summary>
    private static readonly (string Pattern, string Message)[] CommonWrongPatterns =
    [
        ("<States>",           "States must be <State> directly under the root, not wrapped in <States>."),
        ("<StateSave>",        "States must be <State>, not <StateSave>."),
        ("<Categories>",       "Categories must be <Category> directly under the root, not wrapped in <Categories>."),
        ("<StateSaveCategory>","Categories must be <Category>, not <StateSaveCategory>."),
        ("<Variables>",        "Variables must be <Variable> directly under <State>, not wrapped in <Variables>."),
        ("<VariableSave>",     "Variables must be <Variable>, not <VariableSave>."),
        ("<VariableLists>",    "Variable lists must be <VariableList> directly under <State>, not wrapped in <VariableLists>."),
        ("<VariableListSave>", "Variable lists must be <VariableList>, not <VariableListSave>."),
    ];

    /// <summary>Wrong instance patterns for component and screen files (where InstanceSave is never correct).</summary>
    private static readonly (string Pattern, string Message)[] ElementInstanceWrongPatterns =
    [
        ("<Instances>",   "Instances must be <Instance> directly under the root, not wrapped in <Instances>."),
        ("<InstanceSave>","Instances must be <Instance>, not <InstanceSave>."),
    ];

    /// <summary>Wrong instance patterns for behavior files (where InstanceSave IS correct inside RequiredInstances).</summary>
    private static readonly (string Pattern, string Message)[] BehaviorInstanceWrongPatterns =
    [
        ("<BehaviorInstanceSave>",
            "Required instances in behaviors must be <InstanceSave> inside <RequiredInstances>, not <BehaviorInstanceSave>."),
    ];

    /// <summary>
    /// Checks element files for XML content that XmlSerializer silently drops because element
    /// names or structure don't match the expected schema. Without this check, AI-generated files
    /// with structurally wrong XML would load as empty elements with no error reported.
    /// </summary>
    private static List<ErrorResult> DetectSilentlyDroppedContent(
        GumProjectSave project, string projectDirectory)
    {
        var errors = new List<ErrorResult>();

        foreach (var component in project.Components)
        {
            if (component.IsSourceFileMissing) continue;
            string path = Path.Combine(projectDirectory,
                ElementReference.ComponentSubfolder,
                component.Name + "." + GumProjectSave.ComponentExtension);
            errors.AddRange(CheckFileForWrongPatterns(path, component.Name,
                CommonWrongPatterns, ElementInstanceWrongPatterns));
        }

        foreach (var screen in project.Screens)
        {
            if (screen.IsSourceFileMissing) continue;
            string path = Path.Combine(projectDirectory,
                ElementReference.ScreenSubfolder,
                screen.Name + "." + GumProjectSave.ScreenExtension);
            errors.AddRange(CheckFileForWrongPatterns(path, screen.Name,
                CommonWrongPatterns, ElementInstanceWrongPatterns));
        }

        foreach (var standard in project.StandardElements)
        {
            if (standard.IsSourceFileMissing) continue;
            string path = Path.Combine(projectDirectory,
                ElementReference.StandardSubfolder,
                standard.Name + "." + GumProjectSave.StandardExtension);
            errors.AddRange(CheckFileForWrongPatterns(path, standard.Name,
                CommonWrongPatterns));
        }

        foreach (var behavior in project.Behaviors)
        {
            if (behavior.IsSourceFileMissing) continue;
            string path = Path.Combine(projectDirectory,
                BehaviorReference.Subfolder,
                behavior.Name + "." + BehaviorReference.Extension);
            errors.AddRange(CheckFileForWrongPatterns(path, behavior.Name,
                CommonWrongPatterns, BehaviorInstanceWrongPatterns));
        }

        return errors;
    }

    private static List<ErrorResult> CheckFileForWrongPatterns(
        string filePath, string elementName,
        params (string Pattern, string Message)[][] patternSets)
    {
        var errors = new List<ErrorResult>();

        if (!File.Exists(filePath))
        {
            return errors;
        }

        string content = File.ReadAllText(filePath);

        foreach (var patternSet in patternSets)
        {
            foreach (var (pattern, message) in patternSet)
            {
                if (content.Contains(pattern))
                {
                    errors.Add(new ErrorResult
                    {
                        ElementName = elementName,
                        Message = message,
                        Severity = ErrorSeverity.Error
                    });
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Converts missing file paths into <see cref="ErrorResult"/> entries with Warning severity.
    /// </summary>
    private static List<ErrorResult> ConvertMissingFiles(
        List<string> missingFiles, string projectDirectory)
    {
        var errors = new List<ErrorResult>();

        foreach (string filePath in missingFiles)
        {
            string elementName = Path.GetFileNameWithoutExtension(filePath);
            string relativePath = filePath;
            if (filePath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = filePath.Substring(projectDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            errors.Add(new ErrorResult
            {
                ElementName = elementName,
                Message = $"Referenced file not found: {relativePath}",
                Severity = ErrorSeverity.Warning
            });
        }

        return errors;
    }
}
