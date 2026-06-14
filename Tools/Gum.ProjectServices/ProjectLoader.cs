using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;

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

        // A merge/rebase can leave git conflict markers in the .gumx, which makes it invalid XML.
        // Detect that here, before deserialization, so the user gets an actionable message instead
        // of the parser's cryptic line/column error. A conflicted .gumx can't load, so this is fatal.
        if (ContainsGitConflictMarkers(File.ReadAllText(filePath)))
        {
            result.ErrorMessage = ConflictMarkerMessage(Path.GetFileName(filePath));
            return result;
        }

        StandardElementsManager.Self.Initialize();
        // INTERIM: bridges shapes (Arc/ColoredCircle/RoundedRectangle/Line) and Skia
        // (Canvas/Svg/LottieAnimation) into headless loading. Remove once these are
        // promoted to first-class standard types in StandardElementsManager.RefreshDefaults().
        StandardElementsManager.Self.RegisterExtendedDefaultStates();

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
                project.Initialize();

                var elementLoadErrors = ParseElementLoadErrors(gumLoadResult.ErrorMessage);
                var conflictErrors = DetectConflictMarkers(project, projectDirectory);

                // A conflicted element file is invalid XML, so it ALSO failed to deserialize and
                // produced a cryptic "Malformed XML" entry via ParseElementLoadErrors. Drop those
                // duplicates so the clear conflict-marker message is the only error for that element.
                var conflictElementNames = new HashSet<string>(
                    conflictErrors.Select(e => e.ElementName));
                elementLoadErrors.RemoveAll(e => conflictElementNames.Contains(e.ElementName));

                result.LoadErrors.AddRange(elementLoadErrors);
                result.LoadErrors.AddRange(conflictErrors);
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
    /// Scans each referenced element/behavior file for unresolved git conflict markers and reports
    /// an <see cref="ErrorSeverity.Error"/> per conflicted file. Iterates the project's references
    /// (not the loaded element lists) because a conflicted file is invalid XML and never makes it
    /// into those lists. Mirrors the per-element error model used by <see cref="DetectSilentlyDroppedContent"/>.
    /// </summary>
    private static List<ErrorResult> DetectConflictMarkers(
        GumProjectSave project, string projectDirectory)
    {
        var errors = new List<ErrorResult>();

        void Check(string name, string subfolder, string extension)
        {
            string path = Path.Combine(projectDirectory, subfolder, name + "." + extension);
            if (File.Exists(path) && ContainsGitConflictMarkers(File.ReadAllText(path)))
            {
                errors.Add(new ErrorResult
                {
                    ElementName = name,
                    Message = ConflictMarkerMessage(name + "." + extension),
                    Severity = ErrorSeverity.Error
                });
            }
        }

        foreach (var reference in project.ScreenReferences)
        {
            Check(reference.Name, ElementReference.ScreenSubfolder, GumProjectSave.ScreenExtension);
        }
        foreach (var reference in project.ComponentReferences)
        {
            Check(reference.Name, ElementReference.ComponentSubfolder, GumProjectSave.ComponentExtension);
        }
        foreach (var reference in project.StandardElementReferences)
        {
            Check(reference.Name, ElementReference.StandardSubfolder, GumProjectSave.StandardExtension);
        }
        foreach (var reference in project.BehaviorReferences)
        {
            Check(reference.Name, BehaviorReference.Subfolder, BehaviorReference.Extension);
        }

        return errors;
    }

    /// <summary>
    /// Returns true if the raw file text contains an unresolved git conflict marker. Git always
    /// writes the markers (<c>&lt;&lt;&lt;&lt;&lt;&lt;&lt;</c>, <c>=======</c>, <c>&gt;&gt;&gt;&gt;&gt;&gt;&gt;</c>)
    /// at the start of a line; valid Gum XML never does (angle brackets in content are escaped),
    /// so a start-of-line match is an unambiguous signal with effectively no false positives.
    /// </summary>
    private static bool ContainsGitConflictMarkers(string content)
    {
        foreach (string line in content.Split('\n'))
        {
            if (line.StartsWith("<<<<<<<") ||
                line.StartsWith("=======") ||
                line.StartsWith(">>>>>>>"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the shared, actionable conflict-marker error message for the given file.</summary>
    private static string ConflictMarkerMessage(string fileName) =>
        $"{fileName} contains unresolved git conflict markers (<<<<<<< / ======= / >>>>>>>). " +
        "Resolve the conflict in your editor (e.g. VS Code) and reload.";

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
