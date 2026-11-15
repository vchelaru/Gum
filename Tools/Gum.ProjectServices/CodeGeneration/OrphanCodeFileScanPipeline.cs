using Gum.Localization;
using Gum.Managers;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// The services the orphaned code file scan is built from, assembled in one place so the tool's plugin and
/// <see cref="HeadlessOrphanCodeFileProvider"/> (<c>gumcli check</c>) cannot drift apart. Callers differ only in
/// what they pass in: the name verifier, the localization service and the variable restrictions.
/// </summary>
public class OrphanCodeFileScanPipeline
{
    public CodeOutputElementSettingsManager ElementSettingsManager { get; }
    public CodeGenerator CodeGenerator { get; }
    public CodeGenerationFileLocationsService FileLocationsService { get; }
    public OrphanCodeFileScanService ScanService { get; }

    private OrphanCodeFileScanPipeline(
        CodeOutputElementSettingsManager elementSettingsManager, CodeGenerator codeGenerator,
        CodeGenerationFileLocationsService fileLocationsService, OrphanCodeFileScanService scanService)
    {
        ElementSettingsManager = elementSettingsManager;
        CodeGenerator = codeGenerator;
        FileLocationsService = fileLocationsService;
        ScanService = scanService;
    }

    public static OrphanCodeFileScanPipeline Create(
        IProjectDirectoryProvider projectDirectoryProvider, CodeGenerationNameVerifier nameVerifier,
        LocalizationService localizationService, IVariableRestrictions? variableRestrictions)
    {
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        CodeGenerator codeGenerator = new CodeGenerator(
            nameVerifier, localizationService, elementSettingsManager, projectDirectoryProvider,
            variableRestrictions: variableRestrictions);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, nameVerifier, projectDirectoryProvider);
        OrphanCodeFileScanService scanService = new OrphanCodeFileScanService(
            codeGenerator, fileLocationsService, elementSettingsManager, projectDirectoryProvider);
        return new OrphanCodeFileScanPipeline(elementSettingsManager, codeGenerator, fileLocationsService, scanService);
    }
}
