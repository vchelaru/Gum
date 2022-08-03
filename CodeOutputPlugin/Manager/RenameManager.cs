using CodeOutputPlugin.Models;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager
{
    internal static class RenameManager
    {
        internal static void HandleRename(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings)
        {
            var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

            var oldGeneratedFileName = CodeGenerator.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings, oldName);
            var oldCustomFileName = CodeGenerator.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings, oldName);
            var newCustomFileName = CodeGenerator.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings);
            RegenerateAndMoveCode(element, oldName, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName);

        }

        private static void RegenerateAndMoveCode(ElementSave element, string oldName, CodeOutputProjectSettings codeOutputProjectSettings, FilePath oldGeneratedFileName, FilePath oldCustomFileName, FilePath newCustomFileName,
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

                var visualApi = CodeGenerator.GetVisualApiForElement(element);

                var oldClassName = CodeGenerator.GetClassNameForType(oldName, oldVisualApi ?? visualApi);
                var newClassName = CodeGenerator.GetClassNameForType(element.Name, visualApi);

                RenameClassInCode(
                    oldClassName,
                    newClassName,
                    ref fileContents);

                FileManager.SaveText(fileContents, newCustomFileName.FullPath);
            }

            // 4. Regenerate everything referencing this?
            var referencingElements = ObjectFinder.Self.GetElementsReferencingRecursively(element);

            foreach (var referencingElement in referencingElements)
            {
                var elementOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(referencingElement);
                CodeGenerator.GenerateCodeForElement(referencingElement, elementOutputSettings, codeOutputProjectSettings, false);
            }

            var thisElementOutputSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);

            // 5. Regenerate this
            CodeGenerator.GenerateCodeForElement(element, thisElementOutputSettings, codeOutputProjectSettings, false);
        }

        internal static void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
        {
            /////////////////////////Early Out////////////////////
            if(variableName != "Base Type" || instance != null)
            {
                return;
            }
            /////////////////////End Early Out////////////////////


            // we changed the base type, so let's see if this changed the names
            // The easeist way to do this is to set the value back and compare with the new:
            var elementSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element);
            // Vic - tomorrow keep testing this swapping back adn forth
            var newValue = element.BaseType;
            var newCustomFileName = CodeGenerator.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings);

            if(oldValue is StandardElementTypes standardElementTypes)
            {
                element.BaseType = standardElementTypes.ToString();
            }
            else
            {
                element.BaseType = (string)oldValue; 
            }

            var oldGeneratedFileName = CodeGenerator.GetGeneratedFileName(element, elementSettings, codeOutputProjectSettings);
            var oldCustomFileName = CodeGenerator.GetCustomCodeFileName(element, elementSettings, codeOutputProjectSettings);
            var oldVisualApi = CodeGenerator.GetVisualApiForElement(element);


            element.BaseType = newValue;

            if (newCustomFileName != oldCustomFileName)
            {
                RegenerateAndMoveCode(element, element.Name, codeOutputProjectSettings, oldGeneratedFileName, oldCustomFileName, newCustomFileName, oldVisualApi);
            }

        }

        static void RenameClassInCode(string oldClassName, string newClassName, ref string contents)
        {
            contents = contents.Replace("partial class " + oldClassName,
                "partial class " + newClassName);
        }
    }
}
