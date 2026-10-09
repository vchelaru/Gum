using Gum.ProjectServices.CodeGeneration;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using System;
using Gum.Services.Dialogs;
using ToolsUtilities;
using Gum.ToolStates;

namespace CodeOutputPlugin.Manager;

public class RenameService
{
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly CustomCodeHeaderRewriter _headerRewriter;

    public RenameService(CodeGenerationService codeGenerationService,
        CodeGenerator codeGenerator,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationNameVerifier nameVerifier,
        IDialogService dialogService,
        IProjectDirectoryProvider projectDirectoryProvider,
        IFileCommands fileCommands)
    {
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(codeGenerator, nameVerifier, projectDirectoryProvider);
        _elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        _codeGenerationService = codeGenerationService;
        _codeGenerator = codeGenerator;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _headerRewriter = new CustomCodeHeaderRewriter(codeGenerator, customCodeGenerator);
    }

    public void HandleRename(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi)
    {
        try
        {
            // The .codsj sits next to the element's XML rather than in the code project, so it
            // follows the element even when no code output folder is configured.
            MoveElementSettingsFile(element, oldName);

            if (codeOutputProjectSettings.CodeProjectRoot == string.Empty)
            {
                return;
            }

            var elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);

            var oldGeneratedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
            var oldCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
            var newCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi);
            RegenerateAndMoveCode(element, elementSettings, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);
        }
        catch (FileOperationException e)
        {
            _dialogService.ShowMessage(e.Message, $"Error moving code for {element}");
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage(e.ToString(), $"Error moving code for {element}");
        }
    }

    /// <summary>
    /// Moves the element's .codsj settings file from its old name/folder to the current one. Without
    /// this a renamed or relocated element silently reverts to default per-element code settings.
    /// </summary>
    private void MoveElementSettingsFile(ElementSave element, string oldName)
    {
        FilePath? oldSettingsFile = _elementSettingsManager.GetCodeSettingsFilePath(element, oldName);
        FilePath? newSettingsFile = _elementSettingsManager.GetCodeSettingsFilePath(element);

        ////////////////Early Out/////////////////
        if (oldSettingsFile == null || newSettingsFile == null ||
            oldSettingsFile.FullPath == newSettingsFile.FullPath ||
            !oldSettingsFile.Exists())
        {
            return;
        }

        // A file already at the destination belongs to some other element, so leave both alone
        // rather than overwriting settings that cannot be recovered.
        if (newSettingsFile.Exists() && !SidecarFileMover.IsSameFileWithDifferentCase(oldSettingsFile, newSettingsFile))
        {
            return;
        }
        //////////////End Early Out///////////////

        SidecarFileMover.Move(oldSettingsFile, newSettingsFile);
    }

    private void RegenerateAndMoveCode(ElementSave element,
        CodeOutputElementSettings? elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, FilePath? oldGeneratedFileName,
        FilePath? oldCustomFileName, FilePath? newCustomFileName)
    {
        // 1. Delete the old generated file. Generated code is derived data - step 5 recreates it
        // byte-identical at the new name - so deleting it outright is lossless.
        if (oldGeneratedFileName?.Exists() == true)
        {
            try
            {
                System.IO.File.Delete(oldGeneratedFileName.FullPath);
            }
            catch (Exception e) when (FileOperationFailure.IsAccessFailure(e))
            {
                throw new FileOperationException(
                    FileOperationFailure.BuildMessage(
                        $"Could not delete this generated code file:\n{oldGeneratedFileName.FullPath}", e),
                    e);
            }
        }

        // 2. Rename the existing custom code file
        if (oldCustomFileName?.Exists() == true && newCustomFileName != null)
        {
            bool shouldMove = true;

            // A rename that only changes casing points both names at the same physical file on a
            // case-insensitive filesystem, so there is nothing to overwrite - the move corrects the
            // casing instead.
            bool isOverwritingAnotherFile = newCustomFileName.Exists() &&
                !SidecarFileMover.IsSameFileWithDifferentCase(oldCustomFileName, newCustomFileName);

            if (isOverwritingAnotherFile)
            {
                var message = $"Would you like to rename the custom code file to:\n" +
                    $"{newCustomFileName.FullPath}\n" +
                    $"The file already there will be moved to the recycle bin.";
                shouldMove = _dialogService.ShowYesNoMessage(message, "Overwrite?");

                if (shouldMove)
                {
                    // Custom code is user-authored and unrecoverable through Gum's undo, so it goes
                    // to the recycle bin rather than being deleted outright.
                    RecycleFile(newCustomFileName);
                }
            }

            if (shouldMove)
            {
                SidecarFileMover.Move(oldCustomFileName, newCustomFileName);
            }
        }

        // 3. Update the namespace and class name inside the custom code file
        if (newCustomFileName?.Exists() == true)
        {
            string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

            fileContents = UpdateHeadersInCustomCode(fileContents, element, elementSettings, codeOutputProjectSettings);

            FileManager.SaveText(fileContents, newCustomFileName.FullPath);
        }

        // 4. Regenerate everything referencing this
        var referencingElements = ObjectFinder.Self.GetElementsReferencingRecursively(element);

        foreach (var referencingElement in referencingElements)
        {
            var elementOutputSettings = _elementSettingsManager.LoadOrCreateSettingsFor(referencingElement);
            _codeGenerationService.GenerateCodeForElement(referencingElement, elementOutputSettings, codeOutputProjectSettings, showPopups: false);
        }

        var thisElementOutputSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);

        // 5. Regenerate this
        _codeGenerationService.GenerateCodeForElement(element, thisElementOutputSettings, codeOutputProjectSettings, showPopups: false);
    }

    /// <summary>
    /// Sends a file to the recycle bin, translating file-access failures into a message the user
    /// can act on.
    /// </summary>
    private void RecycleFile(FilePath filePath)
    {
        try
        {
            _fileCommands.MoveToRecycleBin(filePath);
        }
        catch (Exception e) when (FileOperationFailure.IsAccessFailure(e))
        {
            throw new FileOperationException(
                FileOperationFailure.BuildMessage(
                    $"Could not move this file to the recycle bin:\n{filePath.FullPath}", e),
                e);
        }
    }

    public void HandleVariableSet(ElementSave? element, InstanceSave? instance, string variableName, object? oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        /////////////////////////Early Out////////////////////
        if (variableName != "BaseType" || instance != null || element == null)
        {
            return;
        }

        var elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);

        if (elementSettings.GenerationBehavior == GenerationBehavior.NeverGenerate)
        {
            return;
        }
        /////////////////////End Early Out////////////////////

        FilePath? oldGeneratedFileName = null;
        FilePath? oldCustomFileName = null;

        var oldVisualApi = _codeGenerator.GetVisualApiForElement(element);

        var newValue = element.BaseType;
        var newCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);

        if (oldValue != null)
        {
            // Temporarily set the element back to the old type to get the old values
            if (oldValue is StandardElementTypes standardElementTypes)
            {
                element.BaseType = standardElementTypes.ToString();
            }
            else
            {
                element.BaseType = (string)oldValue;
            }

            oldGeneratedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);
            oldCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);
        }

        element.BaseType = newValue;

        if (newCustomFileName != null)
        {
            try
            {
                if (newCustomFileName != oldCustomFileName)
                {
                    RegenerateAndMoveCode(element, elementSettings, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);
                }
                else
                {
                    string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

                    fileContents = UpdateHeadersInCustomCode(fileContents, element, elementSettings, codeOutputProjectSettings);

                    FileManager.SaveText(fileContents, newCustomFileName.FullPath);
                }
            }
            catch (FileOperationException e)
            {
                _dialogService.ShowMessage(e.Message, $"Error moving code for {element}");
            }
        }
    }

    /// <summary>
    /// Rewrites the namespace and partial class declarations in an element's custom code file so they
    /// match the element's current identity (name, containing folder, and base type). Called by the
    /// tool whenever an element is renamed, moved to another folder, or has its BaseType changed.
    /// </summary>
    /// <returns>The updated file contents.</returns>
    public string UpdateHeadersInCustomCode(string contents, ElementSave element,
        CodeOutputElementSettings? elementSettings, CodeOutputProjectSettings codeOutputProjectSettings) =>
        _headerRewriter.Rewrite(contents, element, elementSettings, codeOutputProjectSettings);
}
