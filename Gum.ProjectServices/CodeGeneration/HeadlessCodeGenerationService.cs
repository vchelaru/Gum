using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Headless code generation service that writes generated .cs files to disk.
/// This is the CLI/headless equivalent of the tool's CodeGenerationService.
/// </summary>
public class HeadlessCodeGenerationService : IHeadlessCodeGenerationService
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CustomCodeGenerator _customCodeGenerator;
    private readonly CodeGenerationFileLocationsService _fileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly ICodeGenLogger _logger;

    public HeadlessCodeGenerationService(
        CodeGenerator codeGenerator,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager,
        ICodeGenLogger logger)
    {
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool GenerateCodeForElement(ElementSave element, CodeOutputElementSettings elementSettings,
        CodeOutputProjectSettings projectSettings, bool checkForMissing = true)
    {
        var visualApi = _codeGenerator.GetVisualApiForElement(element);
        var generatedFileName = _fileLocationsService.GetGeneratedFileName(
            element, elementSettings, projectSettings, visualApi);

        if (generatedFileName == null)
        {
            _logger.PrintError($"Generated file name could not be determined for {element.Name}");
            return false;
        }

        if (string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
        {
            _logger.PrintError("Code project root must be specified before generating code");
            return false;
        }

        // Check for missing referenced elements and generate them
        if (checkForMissing)
        {
            GenerateMissingReferencedElements(element, projectSettings, visualApi);
        }

        // Generate the main code
        string contents = _codeGenerator.GetGeneratedCodeForElement(element, elementSettings, projectSettings);
        contents = $"//Code for {element}\r\n{contents}";

        // Ensure directory exists
        var codeDirectory = generatedFileName.GetDirectoryContainingThis();
        if (codeDirectory != null && !System.IO.Directory.Exists(codeDirectory.FullPath))
        {
            try
            {
                System.IO.Directory.CreateDirectory(codeDirectory.FullPath);
            }
            catch (Exception e)
            {
                _logger.PrintError($"Error creating directory {codeDirectory}: {e.Message}");
                return false;
            }
        }

        // Write generated file
        System.IO.File.WriteAllText(generatedFileName.FullPath, contents);
        _logger.PrintOutput($"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}");

        // Write custom code file if it doesn't exist
        if (!string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
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
                var customCodeContents = _customCodeGenerator.GetCustomCodeForElement(
                    element, elementSettings, projectSettings);
                System.IO.File.WriteAllText(customCodeFileName, customCodeContents);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public int GenerateCodeForAllElements(GumProjectSave project, CodeOutputProjectSettings projectSettings)
    {
        int count = 0;

        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (var screen in project.Screens)
            {
                var settings = _elementSettingsManager.LoadOrCreateSettingsFor(screen);
                if (settings.GenerationBehavior != GenerationBehavior.NeverGenerate)
                {
                    if (GenerateCodeForElement(screen, settings, projectSettings, checkForMissing: false))
                    {
                        count++;
                    }
                }
            }

            foreach (var component in project.Components)
            {
                var settings = _elementSettingsManager.LoadOrCreateSettingsFor(component);
                if (settings.GenerationBehavior != GenerationBehavior.NeverGenerate)
                {
                    if (GenerateCodeForElement(component, settings, projectSettings, checkForMissing: false))
                    {
                        count++;
                    }
                }
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        return count;
    }

    private void GenerateMissingReferencedElements(ElementSave element,
        CodeOutputProjectSettings projectSettings, VisualApi visualApi)
    {
        var elementReferences = ObjectFinder.Self.GetElementsReferencedByThis(element);
        var elementsWithMissingCodeGen = elementReferences
            .Where(item =>
            {
                if (item is StandardElementSave || item == null)
                {
                    return false;
                }
                else
                {
                    var settings = _elementSettingsManager.LoadOrCreateSettingsFor(item);
                    var genFileName = _fileLocationsService.GetGeneratedFileName(
                        item, settings, projectSettings, visualApi);
                    return genFileName?.Exists() == false;
                }
            })
            .ToList();

        if (elementsWithMissingCodeGen.Count > 0)
        {
            var missingNames = elementsWithMissingCodeGen.Select(item => item.Name).ToList();
            _logger.PrintOutput($"Generating missing referenced elements: {string.Join(", ", missingNames)}");

            foreach (var missingElement in elementsWithMissingCodeGen)
            {
                var settings = _elementSettingsManager.LoadOrCreateSettingsFor(missingElement);
                GenerateCodeForElement(missingElement, settings, projectSettings, checkForMissing: false);
            }
        }
    }
}
