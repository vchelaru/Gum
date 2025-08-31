using CodeOutputPlugin.Models;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager;

internal class RenameService
{
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private CodeGenerationService _codeGenerationService;
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;

    public RenameService(CodeGenerationService codeGenerationService,
        CodeGenerator codeGenerator,
        CustomCodeGenerator customCodeGenerator)
    {
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService();
        _codeGenerationService = codeGenerationService;
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
    }

    internal void HandleRename(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi)
    {
        var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

        var oldGeneratedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
        var oldCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi, oldName);
        var newCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, visualApi);
        RegenerateAndMoveCode(element, oldName, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);

    }

    private void RegenerateAndMoveCode(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, FilePath oldGeneratedFileName, FilePath oldCustomFileName, FilePath newCustomFileName,
        VisualApi? oldVisualApi = null)
    {

        // This should:
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
                var message = $"Would you like to rename the custom code file to:\n{newCustomFileName.FullPath}\nThis would delete the existing file that is already there";
                var result = System.Windows.Forms.MessageBox.Show(message,
                    "Overwrite?",
                    System.Windows.Forms.MessageBoxButtons.YesNo);
                shouldMove = result == System.Windows.Forms.DialogResult.Yes;

                if (shouldMove)
                {
                    System.IO.File.Delete(newCustomFileName.FullPath);
                }
            }

            if (shouldMove)
            {
                System.IO.File.Move(oldCustomFileName.FullPath, newCustomFileName.FullPath);
            }
        }

        // 3. Rename the class inside the custom code file
        // Change the class name in the non-generated .cs
        if (newCustomFileName?.Exists() == true)
        {
            string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

            RenameClassInCode(element, codeOutputProjectSettings, ref fileContents);

            FileManager.SaveText(fileContents, newCustomFileName.FullPath);
        }

        // 4. Regenerate everything referencing this?
        var referencingElements = ObjectFinder.Self.GetElementsReferencingRecursively(element);

        foreach (var referencingElement in referencingElements)
        {
            var elementOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(referencingElement);
            _codeGenerationService.GenerateCodeForElement(referencingElement, elementOutputSettings, codeOutputProjectSettings, false);
        }

        var thisElementOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

        // 5. Regenerate this
        _codeGenerationService.GenerateCodeForElement(element, thisElementOutputSettings, codeOutputProjectSettings, false);
    }

    internal void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        /////////////////////////Early Out////////////////////
        if(variableName != "BaseType" || instance != null)
        {
            return;
        }


        // we changed the base type, so let's see if this changed the names
        // The easeist way to do this is to set the value back and compare with the new:
        var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

        if(elementSettings.GenerationBehavior == GenerationBehavior.NeverGenerate)
        {
            return;
        }
        /////////////////////End Early Out////////////////////

        var oldVisualApi = _codeGenerator.GetVisualApiForElement(element);

        // Vic - tomorrow keep testing this swapping back adn forth
        var newValue = element.BaseType;
        var newCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);

        if(oldValue is StandardElementTypes standardElementTypes)
        {
            element.BaseType = standardElementTypes.ToString();
        }
        else
        {
            element.BaseType = (string)oldValue; 
        }

        var oldGeneratedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);
        var oldCustomFileName = _codeGenerationFileLocationsService.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, oldVisualApi);


        element.BaseType = newValue;

        if(newCustomFileName != null)
        {
            if (newCustomFileName != oldCustomFileName)
            {
                RegenerateAndMoveCode(element, element.Name, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName, oldVisualApi);
            }
            else
            {
                // same file, which means we only changed the types:
                string fileContents = FileManager.FromFileText(newCustomFileName.FullPath);

                RenameClassInCode(element, codeOutputProjectSettings, ref fileContents);

                FileManager.SaveText(fileContents, newCustomFileName.FullPath);
            }
        }

    }

    void RenameClassInCode(ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings, ref string contents)
    {
        var startOfLine = contents.IndexOf("partial class ");
        ////////////////Early Out/////////////////
        if(startOfLine <= -1)
        {
            return;
        }
        //////////////End Early Out///////////////
        ///
        var endOfLine = contents.IndexOf("\n", startOfLine + 1);

        contents = contents.Remove(startOfLine, endOfLine - startOfLine);

        var newHeader = _customCodeGenerator.GetClassHeader(element, codeOutputProjectSettings)
            // don't append \n - it's already there from what was removed earlier
            //+ "\n"
            ;
        contents = contents.Insert(startOfLine, newHeader);

    }

}
