using Gum.DataTypes;
using Gum.Managers;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Resolves output file paths for generated and custom code files.
/// </summary>
public class CodeGenerationFileLocationsService
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationNameVerifier _nameVerifier;
    private readonly string? _projectDirectory;

    public CodeGenerationFileLocationsService(CodeGenerator codeGenerator, CodeGenerationNameVerifier nameVerifier, string? projectDirectory)
    {
        _codeGenerator = codeGenerator;
        _nameVerifier = nameVerifier;
        _projectDirectory = projectDirectory;
    }

    /// <summary>
    /// Gets the generated (.Generated.cs) file path for an element.
    /// </summary>
    public FilePath? GetGeneratedFileName(ElementSave selectedElement, CodeOutputElementSettings elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi, string? forcedElementName = null)
    {
        ///////////////////Early Out///////////////////
        if (codeOutputProjectSettings.CodeProjectRoot == null)
        {
            return null;
        }
        /////////////////End Early Out/////////////////
        string generatedFileName = elementSettings.GeneratedFileName;

        if (!string.IsNullOrEmpty(forcedElementName))
        {
            var foundElement = ObjectFinder.Self.GetElementSave(forcedElementName!);
            selectedElement = foundElement ?? selectedElement;
        }
        if (selectedElement != null)
        {
            var elementName = forcedElementName ?? selectedElement.Name;

            var effectiveVisualApi = visualApi;

            if (string.IsNullOrEmpty(generatedFileName) && !string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
            {
                string prefix = selectedElement is ScreenSave ? "Screens"
                    : selectedElement is ComponentSave ? "Components"
                    : "Standards";
                var splitName = (prefix + "/" + elementName).Split('/');

                var context = new CodeGenerationContext(_nameVerifier, selectedElement);
                context.CodeOutputProjectSettings = codeOutputProjectSettings;

                string? fileName = _codeGenerator.GetClassNameForType(elementName, selectedElement.GetType(), effectiveVisualApi, context, out bool isPrefixed);
                if (isPrefixed)
                {
                    fileName = fileName?.Substring(1);
                }

                var nameWithNamespaceArray = splitName.Take(splitName.Length - 1).Append(fileName);

                var folder = codeOutputProjectSettings.CodeProjectRoot;
                if (FileManager.IsRelative(folder))
                {
                    folder = _projectDirectory + folder;
                }

                generatedFileName = folder + string.Join("\\", nameWithNamespaceArray) + ".Generated.cs";
            }
        }

        if (!string.IsNullOrEmpty(generatedFileName) && FileManager.IsRelative(generatedFileName))
        {
            generatedFileName = _projectDirectory + generatedFileName;
        }

        // If it's empty, return null so it doesn't get used in code generation externally
        if (string.IsNullOrEmpty(generatedFileName))
        {
            return null;
        }
        else
        {
            return generatedFileName;
        }
    }

    /// <summary>
    /// Gets the custom code (.cs) file path for an element.
    /// </summary>
    public FilePath? GetCustomCodeFileName(ElementSave selectedElement, CodeOutputElementSettings elementSettings,
        CodeOutputProjectSettings codeOutputProjectSettings, VisualApi visualApi, string? forcedElementName = null)
    {
        var generatedFileName = GetGeneratedFileName(selectedElement, elementSettings, codeOutputProjectSettings, visualApi, forcedElementName);
        if (generatedFileName == null)
        {
            return null;
        }
        else
        {
            var fullPath = generatedFileName.FullPath;
            var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";
            return customCodeFileName;
        }
    }
}
