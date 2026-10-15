using Gum.ProjectServices.CodeGeneration;
using Gum.DataTypes;
using Gum;
using System;
using System.Linq;
using ToolsUtilities;
using Gum.Managers;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace CodeOutputPlugin.Manager;

public class CodeGenerationService
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;
    private readonly CodeGenerationFileLocationsService _codeGenerationFileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly IRetryService _retryService;
    private readonly HeadlessCodeGenerationService _headlessCodeGenerationService;

    public CodeGenerationService(IGuiCommands guiCommands, CodeGenerator codeGenerator,
        IDialogService dialogService,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationNameVerifier nameVerifier,
        IProjectDirectoryProvider projectDirectoryProvider,
        IRetryService retryService)
    {
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService(_codeGenerator, nameVerifier, projectDirectoryProvider);
        _elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        _guiCommands = guiCommands;
        _dialogService = dialogService;
        _retryService = retryService;

        // The fallback file and the batch loop come from the same orchestrator gumcli uses; only the
        // file writes (retried here) and the per-element step (this class's own) differ.
        ICodeGenLogger logger = new GuiCommandsCodeGenLogger(guiCommands);
        _headlessCodeGenerationService = new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, _codeGenerationFileLocationsService, _elementSettingsManager,
            logger, new RetryingCodeFileWriter(retryService, logger));
    }

    private class GuiCommandsCodeGenLogger : ICodeGenLogger
    {
        private readonly IGuiCommands _guiCommands;

        public GuiCommandsCodeGenLogger(IGuiCommands guiCommands)
        {
            _guiCommands = guiCommands;
        }

        public void PrintOutput(string message) => _guiCommands.PrintOutput(message);

        public void PrintError(string message) => _guiCommands.PrintOutput(message);
    }


    /// <summary>
    /// Generates every Screen and Component through the headless orchestrator, the same one
    /// <c>gumcli codegen</c> uses, so elements set to never generate are skipped and the per-project
    /// Standard Elements fallback file (issue #3505) is written. Each element still goes through
    /// <see cref="GenerateCodeForElement"/> for the tool's output and error handling.
    /// </summary>
    /// <returns>The number of elements generated.</returns>
    public int GenerateCodeForAllElements(GumProjectSave project, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        CodeGenerationRunResult result = _headlessCodeGenerationService.GenerateCodeForElements(
            project, codeOutputProjectSettings, new CodeGenerationRunOptions
            {
                ElementGenerator = (element, elementSettings, checkForMissing) =>
                    GenerateCodeForElement(element, elementSettings, codeOutputProjectSettings,
                        showPopups: false, checkForMissing: checkForMissing)
            });

        return result.GeneratedCount;
    }

    /// <returns>True if the element's code was written.</returns>
    public bool GenerateCodeForElement(ElementSave selectedElement, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings codeOutputProjectSettings, bool showPopups,
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
            return false;
        }

        //////////////////////////////////////End Early Out//////////////////////////

        // Same check gumcli codegen runs (FormsCodegenBaseTypeErrorSource): without it the error
        // sentence is written into the file as the class's base type.
        string? baseTypeError = CodeGenerator.GetUnsupportedFormsBaseTypeError(selectedElement, codeOutputProjectSettings);
        if (baseTypeError != null)
        {
            if (showPopups)
            {
                _dialogService.ShowMessage(baseTypeError);
            }
            else
            {
                _guiCommands.PrintOutput(baseTypeError);
            }
            return false;
        }

        if(checkForMissing)
        {
            var elementReferences = ObjectFinder.Self.GetElementsReferencedByThis(selectedElement);
            var elementsWithMissingCodeGen = elementReferences
                .OfType<ElementSave>()
                .Where(item =>
                {
                    if(item is StandardElementSave)
                    {
                        return false;
                    }
                    else
                    {
                        var settings = _elementSettingsManager.LoadOrCreateSettingsFor(item);
                        var genFileName = _codeGenerationFileLocationsService.GetGeneratedFileName(item, settings, codeOutputProjectSettings, visualApi);
                        return genFileName?.Exists() == false;
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
                    GenerateCodeForElement(element, _elementSettingsManager.LoadOrCreateSettingsFor(element), codeOutputProjectSettings, showPopups: false, checkForMissing: false);
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
                _retryService.TryMultipleTimes(() =>
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
            _retryService.TryMultipleTimes(() => System.IO.File.WriteAllText(generatedFileName.FullPath, contents));

            message += $"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}";

            if (!string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
            {
                var fullPath = generatedFileName.FullPath;
                var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";

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

        return hasDirectory;
    }
}
