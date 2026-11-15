using Gum.DataTypes;
using Gum.Managers;
using System.IO;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Scans the disk on the calling thread, for <c>gumcli check</c>. Read-only: a project without a
/// ProjectCodeSettings.codsj is scanned with default settings, and nothing is written.
/// </summary>
public class HeadlessOrphanCodeFileProvider : IOrphanCodeFileProvider
{
    /// <inheritdoc/>
    public OrphanCodeFileScanResult? GetOrphans(GumProjectSave project)
    {
        string? projectDirectory = Path.GetDirectoryName(project.FullFileName);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }
        projectDirectory += Path.DirectorySeparatorChar;

        IProjectDirectoryProvider projectDirectoryProvider = new FixedProjectDirectoryProvider(projectDirectory);
        ICodeGenLogger logger = new TextWriterCodeGenLogger(TextWriter.Null, TextWriter.Null);
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(logger, projectDirectoryProvider);
        CodeOutputProjectSettings projectSettings = settingsManager.TryLoadSettingsForProject()
            ?? CodeOutputProjectSettingsManager.CreateDefaultSettings();

        // The same pipeline gumcli codegen --prune scans with; a variable the engine profile hides decides
        // which elements are generated, and so which files are orphans.
        CodeGenerationNameVerifier nameVerifier = new CodeGenerationNameVerifier(new NameRules());
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        EngineProfileLoadResult profileResult = new EngineProfileLoader().Load(project.FullFileName, project.EngineProfile);
        CodeGenerator codeGenerator = new CodeGenerator(
            nameVerifier, new Gum.Localization.LocalizationService(), elementSettingsManager, projectDirectoryProvider,
            variableRestrictions: new EngineProfileVariableRestrictions(profileResult.Profile));
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, nameVerifier, projectDirectoryProvider);

        OrphanCodeFileScanService scanService = new OrphanCodeFileScanService(
            codeGenerator, fileLocationsService, elementSettingsManager, projectDirectoryProvider);
        return scanService.Scan(project, projectSettings);
    }
}
