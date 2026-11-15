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
    private readonly TextWriter _error;

    /// <param name="error">Where notes about an incomplete scan go. Null discards them.</param>
    public HeadlessOrphanCodeFileProvider(TextWriter? error = null)
    {
        _error = error ?? TextWriter.Null;
    }

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
        CodeOutputProjectSettings? loaded = settingsManager.TryLoadSettingsForProject(out string? loadError);
        if (loadError != null)
        {
            _error.WriteLine("warning: could not read ProjectCodeSettings.codsj, so the orphaned code file check " +
                $"used default settings and may be incomplete: {loadError}");
        }
        CodeOutputProjectSettings projectSettings = loaded ?? CodeOutputProjectSettingsManager.CreateDefaultSettings();

        // A variable the engine profile hides decides which elements are generated, and so which files are orphans.
        EngineProfileLoadResult profileResult = new EngineProfileLoader().Load(project.FullFileName, project.EngineProfile);
        OrphanCodeFileScanPipeline pipeline = OrphanCodeFileScanPipeline.Create(
            projectDirectoryProvider, new CodeGenerationNameVerifier(new NameRules()),
            new Gum.Localization.LocalizationService(), new EngineProfileVariableRestrictions(profileResult.Profile));

        OrphanCodeFileScanResult result = pipeline.ScanService.Scan(project, projectSettings);
        if (result.IsTruncated)
        {
            _error.WriteLine("warning: " + OrphanCodeFileScanService.GetTruncatedMessage(result.CodeRoot));
        }
        return result;
    }
}
