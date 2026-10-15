using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
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
    private readonly ICodeFileWriter _fileWriter;

    // fileWriter writes the generated files; null writes each once through DirectCodeFileWriter.
    public HeadlessCodeGenerationService(
        CodeGenerator codeGenerator,
        CustomCodeGenerator customCodeGenerator,
        CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager,
        ICodeGenLogger logger,
        ICodeFileWriter? fileWriter = null)
    {
        _codeGenerator = codeGenerator;
        _customCodeGenerator = customCodeGenerator;
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _logger = logger;
        _fileWriter = fileWriter ?? new DirectCodeFileWriter(logger);
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

        if (!_fileWriter.TryWrite(generatedFileName.FullPath, contents))
        {
            return false;
        }
        _logger.PrintOutput($"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}");

        // Write custom code file if it doesn't exist
        var fullPath = generatedFileName.FullPath;
        var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";
        if (!System.IO.File.Exists(customCodeFileName))
        {
            var customCodeContents = _customCodeGenerator.GetCustomCodeForElement(
                element, elementSettings, projectSettings);
            return _fileWriter.TryWrite(customCodeFileName, customCodeContents);
        }

        return true;
    }

    /// <inheritdoc/>
    public CodeGenerationRunResult GenerateCodeForElements(GumProjectSave project,
        CodeOutputProjectSettings projectSettings, CodeGenerationRunOptions? options = null)
    {
        IReadOnlyList<ElementSave>? subset = options?.Elements;
        IReadOnlyList<ElementSave> elements = subset ??
            project.Screens.Cast<ElementSave>().Concat(project.Components).ToList();

        // A subset run generates referenced elements that have no code yet; a whole-project run
        // reaches them anyway.
        bool checkForMissing = subset != null;
        ElementCodeGenerator generateElement = options?.ElementGenerator ??
            ((element, settings, check) => GenerateCodeForElement(element, settings, projectSettings, check));

        int generatedCount = 0;
        int blockedCount = 0;

        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (ElementSave element in elements)
            {
                CodeOutputElementSettings settings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
                if (settings.GenerationBehavior == GenerationBehavior.NeverGenerate)
                {
                    continue;
                }

                if (options?.Gate != null && !options.Gate(element))
                {
                    blockedCount++;
                    continue;
                }

                if (generateElement(element, settings, checkForMissing))
                {
                    generatedCount++;
                }
            }

            if (subset == null)
            {
                GenerateStandardElementsFallbackFile(project, projectSettings);
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        return new CodeGenerationRunResult { GeneratedCount = generatedCount, BlockedCount = blockedCount };
    }

    private bool GenerateStandardElementsFallbackFile(GumProjectSave project, CodeOutputProjectSettings projectSettings)
    {
        string? contents = _codeGenerator.GenerateStandardElementsFallbackCode(project, projectSettings);
        if (contents == null)
        {
            return false;
        }

        var generatedFileName = _fileLocationsService.GetStandardElementsFallbackFileName(projectSettings);
        if (generatedFileName == null)
        {
            return false;
        }

        if (!_fileWriter.TryWrite(generatedFileName.FullPath, contents))
        {
            return false;
        }
        _logger.PrintOutput($"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}");

        return true;
    }

    private void GenerateMissingReferencedElements(ElementSave element,
        CodeOutputProjectSettings projectSettings, VisualApi visualApi)
    {
        var elementReferences = ObjectFinder.Self.GetElementsReferencedByThis(element);
        var elementsWithMissingCodeGen = elementReferences
            .OfType<ElementSave>()
            .Where(item =>
            {
                if (item is StandardElementSave)
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
