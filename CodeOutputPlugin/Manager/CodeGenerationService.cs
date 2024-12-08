using CodeOutputPlugin.Models;
using Gum.DataTypes;
using Gum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Gum.Managers;

namespace CodeOutputPlugin.Manager;

internal class CodeGenerationService
{
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;

    public CodeGenerationService()
    {
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService();
    }


    public void GenerateCodeForElement(ElementSave selectedElement, Models.CodeOutputElementSettings elementSettings, CodeOutputProjectSettings codeOutputProjectSettings, bool showPopups,
        bool checkForMissing = true)
    {
        var generatedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(selectedElement, elementSettings, codeOutputProjectSettings);

        ////////////////////////////////////////Early Out/////////////////////////////
        if (generatedFileName == null)
        {
            if (showPopups)
            {
                GumCommands.Self.GuiCommands.ShowMessage("Generated file name must be set first");
            }
            return;
        }
        //////////////////////////////////////End Early Out//////////////////////////

        // We used to use the view model code, but the viewmodel may have
        // an instance within the element selected. Instead, we want to output
        // the code for the whole selected element.
        //var contents = ViewModel.Code;

        if(checkForMissing)
        {
            var elementReferences = ObjectFinder.Self.GetElementsReferencedByThis(selectedElement);
            var elementsWithMissingCodeGen = elementReferences
                .Where(item =>
                {
                    if(item is StandardElementSave)
                    {
                        return false;
                    }
                    else
                    {
                        var settings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(item);
                        return _codeGenerationFileLocationsService.GetGeneratedFileName(item, settings, codeOutputProjectSettings).Exists() == false;
                    }
                })
                .ToList();

            var shouldGenerateMissingFiles = false;

            if (elementsWithMissingCodeGen.Count > 0)
            {
                var missingFiles = elementsWithMissingCodeGen.Select(item => item.Name);
                var missingFileMessage = $"The following elements are missing code generation files:\n{string.Join("\n", missingFiles)}";
                if (showPopups)
                {
                    shouldGenerateMissingFiles = GumCommands.Self.GuiCommands.ShowYesNoMessageBox(missingFileMessage, "Generate missing files?") == System.Windows.MessageBoxResult.Yes;
                }
                else
                {
                    GumCommands.Self.GuiCommands.PrintOutput(missingFileMessage);
                }

            }

            if(shouldGenerateMissingFiles)
            {
                foreach(var element in elementsWithMissingCodeGen)
                {
                    GenerateCodeForElement(element, CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(element), codeOutputProjectSettings, false, false);
                }
            }
        }



        string contents = CodeGenerator.GetGeneratedCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
        contents = $"//Code for {selectedElement.ToString()}\r\n{contents}";

        string message = string.Empty;

        var codeDirectory = generatedFileName.GetDirectoryContainingThis();

        var hasDirectory = true;

        if (!System.IO.Directory.Exists(codeDirectory.FullPath))
        {
            hasDirectory = false;
            try
            {
                GumCommands.Self.TryMultipleTimes(() =>
                    System.IO.Directory.CreateDirectory(codeDirectory.FullPath));
                hasDirectory = true;
            }
            catch (Exception e)
            {
                GumCommands.Self.GuiCommands.PrintOutput($"Error creating directory {codeDirectory}:\n{e.Message}");
            }
        }

        if (hasDirectory)
        {
            GumCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(generatedFileName.FullPath, contents));

            // show a message somewhere?
            message += $"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}";

            if (!string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
            {

                // nope! This strips out periods in folders. We don't want to do that:
                //var splitFileWithoutGenerated = generatedFileName.Split('.').ToArray();
                //var customCodeFileName = string.Join("\\", splitFileWithoutGenerated.Take(splitFileWithoutGenerated.Length - 2)) + ".cs";
                // Instead, just strip it off the end:
                var fullPath = generatedFileName.FullPath;
                var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";

                // todo - only save this if it doesn't already exist
                if (!System.IO.File.Exists(customCodeFileName))
                {
                    var directory = FileManager.GetDirectory(customCodeFileName);
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    var customCodeContents = CustomCodeGenerator.GetCustomCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
                    System.IO.File.WriteAllText(customCodeFileName, customCodeContents);
                }
            }


            if (showPopups)
            {
                GumCommands.Self.GuiCommands.ShowMessage(message);
            }
            else
            {
                GumCommands.Self.GuiCommands.PrintOutput(message);
            }
        }


    }
}
