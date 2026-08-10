using Gum.ProjectServices.CodeGeneration;
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

    public void HandleRename(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi)
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
            RegenerateAndMoveCode(element, elementSettings, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage(e.ToString(), $"Error moving code for {element}");
        }
    }

    private void RegenerateAndMoveCode(ElementSave element,
        CodeOutputElementSettings? elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, FilePath? oldGeneratedFileName,
        FilePath? oldCustomFileName, FilePath? newCustomFileName)
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
                // Moving into a folder for the first time means the destination directory may not
                // exist yet - without this the move throws and the custom file orphans at its old path.
                var newDirectory = newCustomFileName!.GetDirectoryContainingThis();
                if (newDirectory != null && !System.IO.Directory.Exists(newDirectory.FullPath))
                {
                    System.IO.Directory.CreateDirectory(newDirectory.FullPath);
                }

                System.IO.File.Move(oldCustomFileName.FullPath, newCustomFileName.FullPath);
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
