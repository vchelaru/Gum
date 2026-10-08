using Gum.DataTypes;
using Gum.Managers;
using System.Collections.Generic;
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
    private readonly IProjectDirectoryProvider _projectDirectoryProvider;

    public CodeGenerationFileLocationsService(CodeGenerator codeGenerator, CodeGenerationNameVerifier nameVerifier, IProjectDirectoryProvider projectDirectoryProvider)
    {
        _codeGenerator = codeGenerator;
        _nameVerifier = nameVerifier;
        _projectDirectoryProvider = projectDirectoryProvider;
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
        var projectDirectory = _projectDirectoryProvider.ProjectDirectory;
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
                string prefix = GetElementCodeSubfolder(selectedElement, codeOutputProjectSettings);
                var splitName = (prefix + "/" + elementName).Split('/');

                var context = new CodeGenerationContext(_nameVerifier, selectedElement);
                context.CodeOutputProjectSettings = codeOutputProjectSettings;

                string? fileName = _codeGenerator.GetClassNameForType(elementName, selectedElement.GetType(), effectiveVisualApi, context, out bool isPrefixed);
                if (isPrefixed)
                {
                    fileName = fileName?.Substring(1);
                }

                var nameWithNamespaceArray = splitName.Take(splitName.Length - 1).Append(fileName);

                string folder = GetCodeOutputFolder(codeOutputProjectSettings)!;

                generatedFileName = folder + string.Join("\\", nameWithNamespaceArray) + ".Generated.cs";
            }
        }

        if (!string.IsNullOrEmpty(generatedFileName) && FileManager.IsRelative(generatedFileName))
        {
            generatedFileName = projectDirectory + generatedFileName;
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
    /// The folder generated code is written under: <see cref="CodeOutputProjectSettings.CodeProjectRoot"/>
    /// plus the optional <see cref="CodeOutputProjectSettings.GeneratedCodeFolder"/>, resolved against the
    /// Gum project directory when relative. Returns null when
    /// <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> isn't configured.
    /// </summary>
    public string? GetCodeOutputFolder(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        string folder = codeOutputProjectSettings.CodeProjectRoot;
        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        if (FileManager.IsRelative(folder))
        {
            folder = _projectDirectoryProvider.ProjectDirectory + folder;
        }

        string generatedCodeFolder = codeOutputProjectSettings.GeneratedCodeFolder;
        if (!string.IsNullOrEmpty(generatedCodeFolder))
        {
            folder = FileManager.IsRelative(generatedCodeFolder)
                ? WithTrailingSeparator(folder) + generatedCodeFolder
                : generatedCodeFolder;
            folder = WithTrailingSeparator(folder);
        }

        return folder;
    }

    /// <summary>
    /// The folder, relative to <see cref="GetCodeOutputFolder"/>, that an element's code files go in:
    /// <c>Screens</c> or <c>Components</c> with <see cref="CodeOutputProjectSettings.GeneratedCodeFolderPrefix"/>
    /// in front, using <c>/</c> separators. This is only the code location; the element's name inside the
    /// Gum project and its namespace still use the plain <see cref="ElementReference.GetSubfolder"/> name.
    /// </summary>
    public static string GetElementCodeSubfolder(ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        string[] segments = codeOutputProjectSettings.GeneratedCodeFolderPrefix.Trim().Replace('\\', '/').Split('/');
        List<string> kept = new List<string>();
        for (int i = 0; i < segments.Length; i++)
        {
            // "." and ".." would let the files leave the output folder, which the orphan scan walks;
            // an empty segment is a leading or doubled slash. Only the last one (a trailing slash) is kept.
            bool isLast = i == segments.Length - 1;
            if (segments[i] == "." || segments[i] == ".." || (segments[i].Length == 0 && !isLast))
            {
                continue;
            }
            kept.Add(segments[i]);
        }
        return string.Join("/", kept) + ElementReference.GetSubfolder(element);
    }

    private static string WithTrailingSeparator(string folder) =>
        folder.EndsWith("/") || folder.EndsWith("\\") ? folder : folder + "/";

    /// <summary>
    /// Gets the file path for the per-project Standard Elements fallback-registration file
    /// (<c>StandardElements.Generated.cs</c>). Written directly under <see cref="GetCodeOutputFolder"/>
    /// with no Screens/Components subfolder, since it isn't owned by any single element. Returns null
    /// when <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> isn't configured.
    /// </summary>
    public FilePath? GetStandardElementsFallbackFileName(CodeOutputProjectSettings codeOutputProjectSettings)
    {
        string? folder = GetCodeOutputFolder(codeOutputProjectSettings);
        return folder == null ? null : folder + "StandardElements.Generated.cs";
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
