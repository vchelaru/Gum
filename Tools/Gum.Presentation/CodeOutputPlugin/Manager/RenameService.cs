using Gum.ProjectServices.CodeGeneration;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Text.RegularExpressions;
using Gum.Services.Dialogs;
using ToolsUtilities;
using Gum.ToolStates;

namespace CodeOutputPlugin.Manager;

public class RenameService
{
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;

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
        _customCodeGenerator = customCodeGenerator;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
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
        if (newSettingsFile.Exists() && !IsSameFileWithDifferentCase(oldSettingsFile, newSettingsFile))
        {
            return;
        }
        //////////////End Early Out///////////////

        MoveFile(oldSettingsFile, newSettingsFile);
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
            // case-insensitive filesystem, so there is nothing to overwrite - MoveFile corrects the
            // casing instead.
            bool isOverwritingAnotherFile = newCustomFileName.Exists() &&
                !IsSameFileWithDifferentCase(oldCustomFileName, newCustomFileName);

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
                MoveFile(oldCustomFileName, newCustomFileName);
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
    /// True when the two paths differ only by casing, which means they are the same physical file on
    /// a case-insensitive filesystem (Windows, macOS).
    /// </summary>
    private static bool IsSameFileWithDifferentCase(FilePath first, FilePath second) =>
        first.FullPath != second.FullPath &&
        string.Equals(first.FullPath, second.FullPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Moves a file, creating the destination directory if needed and translating file-access
    /// failures into a message the user can act on.
    /// </summary>
    private static void MoveFile(FilePath source, FilePath destination)
    {
        try
        {
            // Moving into a folder for the first time means the destination directory may not exist
            // yet - without this the move throws and the file orphans at its old path.
            var destinationDirectory = destination.GetDirectoryContainingThis();
            if (destinationDirectory != null && !System.IO.Directory.Exists(destinationDirectory.FullPath))
            {
                System.IO.Directory.CreateDirectory(destinationDirectory.FullPath);
            }

            if (IsSameFileWithDifferentCase(source, destination))
            {
                // Source and destination are the same physical file, so move through a temporary
                // name. Windows corrects the casing with a direct move, but File.Move pre-checks the
                // destination on Unix and throws "already exists" on a case-insensitive macOS
                // volume. The casing on disk does have to change: git is case-sensitive even where
                // the filesystem is not.
                var temporaryPath = destination.FullPath + ".gumrename";
                System.IO.File.Move(source.FullPath, temporaryPath);
                System.IO.File.Move(temporaryPath, destination.FullPath);
            }
            else
            {
                System.IO.File.Move(source.FullPath, destination.FullPath);
            }
        }
        catch (Exception e) when (FileOperationFailure.IsAccessFailure(e))
        {
            throw new FileOperationException(
                FileOperationFailure.BuildMessage(
                    $"Could not move this file:\n{source.FullPath}\nto:\n{destination.FullPath}", e),
                e);
        }
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

    public void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        /////////////////////////Early Out////////////////////
        if (variableName != "BaseType" || instance != null)
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
        CodeOutputElementSettings? elementSettings, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        RenameNamespaceInCode(element, elementSettings, codeOutputProjectSettings, ref contents);
        RenameClassInCode(element, codeOutputProjectSettings, ref contents);
        return contents;
    }

    private void RenameNamespaceInCode(ElementSave element, CodeOutputElementSettings? elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, ref string contents)
    {
        var newNamespace = _codeGenerator.GetElementNamespace(element, elementSettings, codeOutputProjectSettings);

        ////////////////Early Out/////////////////
        // Generation would emit no namespace at all, so there is nothing to rename the existing
        // (presumably hand-written) namespace to.
        if (string.IsNullOrEmpty(newNamespace))
        {
            return;
        }

        // Matches both block-scoped ("namespace Foo") and file-scoped ("namespace Foo;") declarations.
        var match = Regex.Match(contents,
            @"^[ \t]*namespace[ \t]+(?<name>[^\s;{]+)",
            RegexOptions.Multiline);

        if (!match.Success)
        {
            return;
        }
        //////////////End Early Out///////////////

        var nameGroup = match.Groups["name"];
        contents = contents.Remove(nameGroup.Index, nameGroup.Length);
        contents = contents.Insert(nameGroup.Index, newNamespace);
    }

    private void RenameClassInCode(ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings, ref string contents)
    {
        var startOfLine = contents.IndexOf("partial class ");
        ////////////////Early Out/////////////////
        if (startOfLine <= -1)
        {
            return;
        }
        //////////////End Early Out///////////////

        var endOfLine = contents.IndexOf("\n", startOfLine + 1);
        if (endOfLine > startOfLine && contents[endOfLine - 1] == '\r')
        {
            endOfLine--;
        }

        var oldClassHeader = contents.Substring(startOfLine, endOfLine - startOfLine);
        string suffix = string.Empty;

        if (oldClassHeader.Contains(":"))
        {
            var colonIndex = oldClassHeader.IndexOf(":");
            suffix = " " + oldClassHeader.Substring(colonIndex).Trim();
        }

        contents = contents.Remove(startOfLine, endOfLine - startOfLine);

        var newHeader = _customCodeGenerator.GetClassHeader(element, codeOutputProjectSettings);
        // When InheritanceLocation is InCustomCode the generated header already carries the base list,
        // so re-appending the old one would emit "X : New : Old".
        if (!newHeader.Contains(":"))
        {
            newHeader += suffix;
        }
        contents = contents.Insert(startOfLine, newHeader);
    }
}
