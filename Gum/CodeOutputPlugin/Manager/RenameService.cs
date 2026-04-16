using Gum.ProjectServices.CodeGeneration;
using Gum.DataTypes;
using Gum.Managers;
using System;
using Gum.Services.Dialogs;
using ToolsUtilities;
using Gum.ToolStates;

namespace CodeOutputPlugin.Manager;

internal class RenameService
{
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly IDialogService _dialogService;

    public RenameService(CodeGenerationService codeGenerationService,
        CodeGenerator codeGenerator,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationNameVerifier nameVerifier,
        IDialogService dialogService,
        IProjectDirectoryProvider projectDirectoryProvider)
    {
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(codeGenerator, nameVerifier, projectDirectoryProvider);
        _elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        _codeGenerationService = codeGenerationService;
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
        _dialogService = dialogService;
    }

    internal void HandleRename(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi)
    {
        if (codeOutputProjectSettings.CodeProjectRoot == string.Empty)
        {
            return;
        }
        try
        {
            var elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);

            var oldGeneratedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
            var oldCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
            var newCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi);
            RegenerateAndMoveCode(element, oldName, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage(e.ToString(), $"Error moving code for {element}");
        }
    }

    private void RegenerateAndMoveCode(ElementSave element, string oldName,
        CodeOutputProjectSettings codeOutputProjectSettings, FilePath? oldGeneratedFileName,
        FilePath? oldCustomFileName, FilePath? newCustomFileName,
        VisualApi? oldVisualApi = null)
    {
        // 1. Delete the old generated file
        if (oldGeneratedFileName?.Exists() == true)
        {
            System.IO.File.Delete(oldGeneratedFileName.FullPath);
        }

        // 2. Rename the existing custom code file
        if (oldCustomFileName?.Exists() == true)
        {
            bool shouldMove = true;
            if (newCustomFileName?.Exists() == true)
            {
                var message = $"Would you like to rename the custom code file to:\n" +
                    $"{newCustomFileName.FullPath}\n" +
                    $"This would delete the existing file that is already there";
                shouldMove = _dialogService.ShowYesNoMessage(message, "Overwrite?");

                if (shouldMove)
                {
                    System.IO.File.Delete(newCustomFileName.FullPath);
                }
            }

            if (shouldMove)
            {
                System.IO.File.Move(oldCustomFileName.FullPath, newCustomFileName!.FullPath);
            }
        }

        // 3. Rename the class inside the custom code file
        if (newCustomFileName?.Exists() == true)
        {
            string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

            RenameClassInCode(element, codeOutputProjectSettings, ref fileContents);

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

    internal void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
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
            if (newCustomFileName != oldCustomFileName)
            {
                RegenerateAndMoveCode(element, element.Name, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName, oldVisualApi);
            }
            else
            {
                string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

                RenameClassInCode(element, codeOutputProjectSettings, ref fileContents);

                FileManager.SaveText(fileContents, newCustomFileName.FullPath);
            }
        }
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

        var oldClassHeader = contents.Substring(startOfLine, endOfLine - startOfLine);
        string suffix = string.Empty;

        if (oldClassHeader.Contains(":"))
        {
            var colonIndex = oldClassHeader.IndexOf(":");
            suffix = " " + oldClassHeader.Substring(colonIndex).Trim();
        }

        contents = contents.Remove(startOfLine, endOfLine - startOfLine);

        var newHeader = _customCodeGenerator.GetClassHeader(element, codeOutputProjectSettings) + suffix;
        contents = contents.Insert(startOfLine, newHeader);
    }
}
