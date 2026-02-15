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
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;

namespace CodeOutputPlugin.Manager;

internal class CodeGenerationService
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;

    public CodeGenerationService(IGuiCommands guiCommands, CodeGenerator codeGenerator, 
        IDialogService dialogService,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationNameVerifier nameVerifier)
    {
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(_codeGenerator, nameVerifier);
        _guiCommands = guiCommands;
        _dialogService = dialogService;
    }


    public void GenerateCodeForElement(ElementSave selectedElement, Models.CodeOutputElementSettings elementSettings, CodeOutputProjectSettings codeOutputProjectSettings, bool showPopups,
        bool checkForMissing = true)
    {
        var visualApi = _codeGenerator.GetVisualApiForElement(selectedElement);
        var generatedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(selectedElement, elementSettings,  codeOutputProjectSettings, visualApi);

        ////////////////////////////////////////Early Out/////////////////////////////
        string errorMessage = string.Empty;
        if (generatedFileName == null)
        {
            errorMessage = "Generated file name must be set first";
        }
        if(string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
        {
            errorMessage = "You must first specify a Code Project Root before generating code";
        }

        if(errorMessage != string.Empty)
        {
            if(showPopups)
            {
                _dialogService.ShowMessage(errorMessage);
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
                    if(item is StandardElementSave || item == null)
                    {
                        return false;
                    }
                    else
                    {
                        var settings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(item);
                        var generatedFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(item, settings, codeOutputProjectSettings, visualApi);
                        return generatedFileName?.Exists() == false;
                    }
                })
                .ToList();

            var shouldGenerateMissingFiles = false;

            if (elementsWithMissingCodeGen.Count > 0)
            {
                var missingFiles = elementsWithMissingCodeGen.Select(item => item.Name).ToArray();
                var missingFileMessage = $"The following elements are missing code generation files:\n{string.Join("\n", missingFiles)}";
                if (showPopups)
                {
                    if(missingFiles.Length == 1)
                    {
                        missingFileMessage += "\n\nGenerate this file too?";
                    }
                    else
                    {
                        missingFileMessage += "\n\nGenerate these files too?";
                    }

                    missingFileMessage += "\n\nYou should click \"Yes\" unless you intend to create the code for these components by hand, or if you are using " +
                        "an existing UI framework and would like to \"bait-and-switch\" these components.";


                    shouldGenerateMissingFiles =
                        _dialogService.ShowYesNoMessage(missingFileMessage, "Generate missing files?");
                }
                else
                {
                    shouldGenerateMissingFiles = true;
                    _guiCommands.PrintOutput(missingFileMessage);
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



        string contents = _codeGenerator.GetGeneratedCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
        contents = $"//Code for {selectedElement.ToString()}\r\n{contents}";

        string message = string.Empty;

        var codeDirectory = generatedFileName!.GetDirectoryContainingThis();

        var hasDirectory = true;

        if (codeDirectory != null && !System.IO.Directory.Exists(codeDirectory.FullPath))
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
                _guiCommands.PrintOutput($"Error creating directory {codeDirectory}:\n{e.Message}");
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
                    var customCodeContents = _customCodeGenerator.GetCustomCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
                    System.IO.File.WriteAllText(customCodeFileName, customCodeContents);
                }
            }


            if (showPopups)
            {
                _dialogService.ShowMessage(message);
            }
            else
            {
                _guiCommands.PrintOutput(message);
            }
        }


    }
}
