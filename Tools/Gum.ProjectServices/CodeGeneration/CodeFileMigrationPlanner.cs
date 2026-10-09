using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Builds a <see cref="CodeFileMigrationPlan"/> from the orphan scan's results: code files that sit
/// at an old path because a code settings change (output library, namespace, folder layout, a Gum
/// upgrade) moved where their element's files belong. Read-only.
/// </summary>
public interface ICodeFileMigrationPlanner
{
    /// <summary>
    /// Plans every generated and custom code orphan in <paramref name="orphans"/>; element settings
    /// orphans are not part of a code settings migration and are ignored.
    /// </summary>
    CodeFileMigrationPlan CreatePlan(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        IReadOnlyList<OrphanCodeFile> orphans);
}

/// <inheritdoc cref="ICodeFileMigrationPlanner"/>
/// <remarks>
/// Orphans are matched to elements only through the generated file's <c>//Code for</c> header
/// (<see cref="OrphanCodeFile.ElementName"/>), never by guessing from class names. Custom code is
/// judged "untouched" by <see cref="ICustomCodeStubDetector"/>, which reads anything it can't
/// account for as real code, so a doubtful file is moved or skipped, never removed.
/// </remarks>
public class CodeFileMigrationPlanner : ICodeFileMigrationPlanner
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationFileLocationsService _fileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly ICustomCodeStubDetector _stubDetector;

    public CodeFileMigrationPlanner(
        CodeGenerator codeGenerator,
        CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager,
        ICustomCodeStubDetector stubDetector)
    {
        _codeGenerator = codeGenerator;
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _stubDetector = stubDetector;
    }

    /// <inheritdoc/>
    public CodeFileMigrationPlan CreatePlan(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        IReadOnlyList<OrphanCodeFile> orphans)
    {
        List<CodeFileMigrationStep> steps = new List<CodeFileMigrationStep>();
        // Two old files can map to one element (the project switched settings twice); only the
        // first may move into the current path, the rest are conflicts.
        HashSet<FilePath> claimedDestinations = new HashSet<FilePath>();

        foreach (OrphanCodeFile orphan in orphans)
        {
            if (orphan.Kind == OrphanCodeFileKind.ElementSettings)
            {
                continue;
            }

            string elementName = orphan.ElementName ?? string.Empty;
            ElementSave? element = FindCodeGeneratedElement(project, elementName);

            if (element == null)
            {
                steps.Add(new CodeFileMigrationStep(elementName, CodeFileMigrationAction.SkipNoElement, orphan.FilePath));
            }
            else if (orphan.Kind == OrphanCodeFileKind.Generated)
            {
                steps.Add(new CodeFileMigrationStep(elementName, CodeFileMigrationAction.RemoveGenerated, orphan.FilePath));
            }
            else
            {
                steps.Add(PlanCustomCode(element, elementName, orphan.FilePath, projectSettings, claimedDestinations));
            }
        }

        return new CodeFileMigrationPlan(steps);
    }

    private CodeFileMigrationStep PlanCustomCode(ElementSave element, string elementName, FilePath source,
        CodeOutputProjectSettings projectSettings, HashSet<FilePath> claimedDestinations)
    {
        if (IsUntouchedStub(source))
        {
            return new CodeFileMigrationStep(elementName, CodeFileMigrationAction.RemoveUntouchedStub, source);
        }

        CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
        FilePath? destination = _fileLocationsService.GetCustomCodeFileName(
            element, elementSettings, projectSettings, _codeGenerator.GetVisualApiForElement(element));

        if (destination == null)
        {
            // No code output configured; nowhere to move it.
            return new CodeFileMigrationStep(elementName, CodeFileMigrationAction.SkipConflict, source);
        }

        destination = Path.GetFullPath(destination.FullPath);
        bool destinationHasRealCode = File.Exists(destination.FullPath) && !IsUntouchedStub(destination);

        if (destinationHasRealCode || !claimedDestinations.Add(destination))
        {
            return new CodeFileMigrationStep(elementName, CodeFileMigrationAction.SkipConflict, source, destination);
        }

        return new CodeFileMigrationStep(elementName, CodeFileMigrationAction.MoveCustomCode, source, destination);
    }

    // An unreadable file counts as real code: the plan must fail toward keeping it.
    private bool IsUntouchedStub(FilePath file)
    {
        try
        {
            return _stubDetector.IsUntouchedStub(File.ReadAllText(file.FullPath));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Standards never get code generated, so only screens and components can own an orphan.
    private static ElementSave? FindCodeGeneratedElement(GumProjectSave project, string elementName) =>
        project.Screens.Cast<ElementSave>()
            .Concat(project.Components)
            .FirstOrDefault(element => string.Equals(element.Name, elementName, StringComparison.OrdinalIgnoreCase));
}
