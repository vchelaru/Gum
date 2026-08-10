using Gum.Commands;
using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Reconciles an element's code files on disk when the element is deleted (issue #4422).
/// <para>
/// Three files are involved and they are not equally precious. The <c>.Generated.cs</c> is derived
/// data - a pure function of the element - and the <c>.codsj</c> holds regenerable settings for an
/// element that no longer exists, so both go unconditionally with no prompt
/// (<see cref="ReconcileFilesForDeletedElement"/>). The custom <c>.cs</c> is user-authored and
/// unrecoverable through Gum, so it is a separate, explicit decision surfaced as a checkbox in the
/// existing DeleteOptionsWindow (<see cref="HandleDeleteOptionsWindowShow"/> /
/// <see cref="HandleConfirmDelete"/>). Because that checkbox is computed once for the whole batch,
/// deleting twenty elements still produces exactly one confirmation.
/// </para>
/// <para>
/// A custom file that is still an untouched stub carries no user code, so it is removed without
/// asking. Elements set to <see cref="GenerationBehavior.NeverGenerate"/> are left completely
/// alone - the user is hand-managing those files.
/// </para>
/// </summary>
public class CodeFileDeleteService
{
    private readonly CodeGenerationFileLocationsService _fileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly CodeGenerator _codeGenerator;
    private readonly ICustomCodeStubDetector _stubDetector;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;

    public CodeFileDeleteService(
        CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager,
        CodeGenerator codeGenerator,
        ICustomCodeStubDetector stubDetector,
        IFileCommands fileCommands,
        IDialogService dialogService)
    {
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _codeGenerator = codeGenerator;
        _stubDetector = stubDetector;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Returns a single checkbox view model covering every element in <paramref name="objectsToDelete"/>
    /// whose custom code file holds user-authored code, or null when none do (so nothing is asked
    /// for untouched stubs). Defaults to unchecked: losing user code needs a deliberate click.
    /// </summary>
    public DeleteOptionCheckboxViewModel? HandleDeleteOptionsWindowShow(
        Array objectsToDelete, CodeOutputProjectSettings projectSettings)
    {
        List<FilePath> editedFiles = GetEditedCustomCodeFiles(objectsToDelete, projectSettings);

        if (editedFiles.Count == 0)
        {
            return null;
        }

        string label = editedFiles.Count == 1
            ? "Delete custom code file (contains your code)"
            : $"Delete {editedFiles.Count} custom code files (contain your code)";

        return new DeleteOptionCheckboxViewModel
        {
            Label = label,
            IsChecked = false
        };
    }

    /// <summary>
    /// Removes the custom code files for the deleted elements: untouched stubs always, and files
    /// containing user-authored code only when <paramref name="deleteEditedCustomCode"/> is true
    /// (the checkbox from <see cref="HandleDeleteOptionsWindowShow"/> was ticked). Always uses the
    /// recycle bin, never a hard delete.
    /// </summary>
    public void HandleConfirmDelete(
        Array deletedObjects, bool deleteEditedCustomCode, CodeOutputProjectSettings projectSettings)
    {
        List<FilePath> failures = new List<FilePath>();

        foreach (ElementSave element in GetGeneratedElements(deletedObjects))
        {
            CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
            FilePath? customCodeFile = GetCustomCodeFile(element, elementSettings, projectSettings);

            if (customCodeFile?.Exists() != true)
            {
                continue;
            }

            bool isStub = IsUntouchedStub(customCodeFile);
            if (isStub || deleteEditedCustomCode)
            {
                TryRecycle(customCodeFile, failures);
            }
        }

        ReportFailures(failures);
    }

    /// <summary>
    /// Removes the derived files for a deleted element - the <c>.Generated.cs</c> and the
    /// per-element <c>.codsj</c> settings file - with no prompt. The custom code file is never
    /// touched here; that decision belongs to <see cref="HandleConfirmDelete"/>.
    /// </summary>
    public void ReconcileFilesForDeletedElement(ElementSave element, CodeOutputProjectSettings projectSettings)
    {
        if (!IsGeneratedElement(element))
        {
            return;
        }

        CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
        if (elementSettings.GenerationBehavior == GenerationBehavior.NeverGenerate)
        {
            return;
        }

        List<FilePath> failures = new List<FilePath>();

        VisualApi visualApi = _codeGenerator.GetVisualApiForElement(element);
        FilePath? generatedFile = _fileLocationsService.GetGeneratedFileName(
            element, elementSettings, projectSettings, visualApi);

        if (generatedFile?.Exists() == true)
        {
            TryRecycle(generatedFile, failures);
        }

        FilePath? settingsFile = _elementSettingsManager.GetCodeSettingsFilePath(element);
        if (settingsFile?.Exists() == true)
        {
            TryRecycle(settingsFile, failures);
        }

        ReportFailures(failures);
    }

    private List<FilePath> GetEditedCustomCodeFiles(Array objectsToDelete, CodeOutputProjectSettings projectSettings)
    {
        List<FilePath> toReturn = new List<FilePath>();

        foreach (ElementSave element in GetGeneratedElements(objectsToDelete))
        {
            CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
            FilePath? customCodeFile = GetCustomCodeFile(element, elementSettings, projectSettings);

            if (customCodeFile?.Exists() == true && !IsUntouchedStub(customCodeFile))
            {
                toReturn.Add(customCodeFile);
            }
        }

        return toReturn;
    }

    private FilePath? GetCustomCodeFile(
        ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
    {
        VisualApi visualApi = _codeGenerator.GetVisualApiForElement(element);
        return _fileLocationsService.GetCustomCodeFileName(element, elementSettings, projectSettings, visualApi);
    }

    /// <summary>
    /// Elements whose code files Gum owns: Screens and Components that aren't hand-managed. A file
    /// that can't be read is treated as edited, so the user is asked rather than losing it silently.
    /// </summary>
    private IEnumerable<ElementSave> GetGeneratedElements(Array objects)
    {
        return objects.OfType<ElementSave>()
            .Where(IsGeneratedElement)
            .Where(item => _elementSettingsManager.LoadOrCreateSettingsFor(item).GenerationBehavior
                != GenerationBehavior.NeverGenerate);
    }

    private static bool IsGeneratedElement(ElementSave element) => element is ComponentSave or ScreenSave;

    private bool IsUntouchedStub(FilePath customCodeFile)
    {
        try
        {
            return _stubDetector.IsUntouchedStub(System.IO.File.ReadAllText(customCodeFile.FullPath));
        }
        catch (System.IO.IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void TryRecycle(FilePath filePath, List<FilePath> failures)
    {
        try
        {
            _fileCommands.MoveToRecycleBin(filePath);
        }
        catch
        {
            failures.Add(filePath);
        }
    }

    /// <summary>
    /// Reports every file that could not be removed in one informational popup - never a prompt,
    /// and never one popup per file, so a batch delete can't produce a wall of dialogs.
    /// </summary>
    private void ReportFailures(List<FilePath> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        string message = "Could not delete the following code file(s):\n"
            + string.Join("\n", failures.Select(item => "  • " + item.FullPath));

        _dialogService.ShowMessage(message);
    }
}
