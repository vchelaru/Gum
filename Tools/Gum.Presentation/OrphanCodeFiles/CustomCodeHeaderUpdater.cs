using CodeOutputPlugin.Manager;
using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace OrphanCodeFilePlugin;

/// <summary>Rewrites custom code headers after a namespace or inheritance settings change; see <see cref="CustomCodeHeaderUpdater"/>.</summary>
public interface ICustomCodeHeaderUpdater
{
    /// <summary>
    /// Lists every custom code file whose namespace or class header no longer matches
    /// <paramref name="projectSettings"/>, and rewrites them if the user confirms.
    /// </summary>
    void Update(GumProjectSave project, FilePath projectFile, CodeOutputProjectSettings projectSettings, string changeDescription);
}

/// <summary>
/// After a Code tab edit changes the root namespace, folder namespaces or inheritance location,
/// rewrites each element's custom code file in place so it stays the same partial class as its
/// regenerated <c>.Generated.cs</c> (#5855). Files are backed up first, the same way a code file
/// migration is, so <b>Restore Last Code File Migration</b> undoes it. Shows nothing when every
/// file already matches.
/// </summary>
public class CustomCodeHeaderUpdater : ICustomCodeHeaderUpdater
{
    private const string Title = "Update Custom Code";
    private const string RestoreMenuPath = "Content > Restore Last Code File Migration";

    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationFileLocationsService _fileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly CustomCodeHeaderRewriter _headerRewriter;
    private readonly CodeFileBackupService _backupService;
    private readonly IElementCodeRegenerator _regenerator;
    private readonly IDialogService _dialogService;

    public CustomCodeHeaderUpdater(CodeGenerator codeGenerator, CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager, CustomCodeHeaderRewriter headerRewriter,
        CodeFileBackupService backupService, IElementCodeRegenerator regenerator, IDialogService dialogService)
    {
        _codeGenerator = codeGenerator;
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _headerRewriter = headerRewriter;
        _backupService = backupService;
        _regenerator = regenerator;
        _dialogService = dialogService;
    }

    /// <inheritdoc/>
    public void Update(GumProjectSave project, FilePath projectFile, CodeOutputProjectSettings projectSettings, string changeDescription)
    {
        List<Rewrite> rewrites = PlanRewrites(project, projectSettings);

        ///////////////////Early Out///////////////////
        if (rewrites.Count == 0)
        {
            return;
        }

        string codeRoot = _fileLocationsService.GetCodeOutputFolder(projectSettings) ?? string.Empty;
        StringBuilder question = new StringBuilder(
            $"You changed {changeDescription}. These custom code files still declare the old namespace or class header, " +
            "so they would no longer be part of their element's class:\n\n");
        foreach (Rewrite rewrite in rewrites)
        {
            question.Append("  ").Append(Path.GetRelativePath(codeRoot, rewrite.File.FullPath).Replace('\\', '/')).Append('\n');
        }
        question.Append("\nUpdate them and regenerate their elements' code? A copy of each file is kept first.");

        MessageDialogStyle confirm = new MessageDialogStyle { AffirmativeText = "Update", NegativeText = "Cancel" };
        if (_dialogService.ShowMessage(question.ToString(), Title, confirm) != MessageDialogResult.Affirmative)
        {
            return;
        }
        /////////////////End Early Out/////////////////

        string? error = Apply(projectFile, rewrites, out CodeFileBackup? backup);
        if (error != null)
        {
            _dialogService.ShowMessage(error, Title);
            return;
        }

        foreach (Rewrite rewrite in rewrites)
        {
            _regenerator.Regenerate(rewrite.ElementName, projectSettings);
        }

        _dialogService.ShowMessage(
            $"Updated {rewrites.Count} file(s) and regenerated their elements' code.\n\n" +
            $"A copy of every file it changed is in:\n{backup?.Folder}\n\n" +
            $"To undo it, use {RestoreMenuPath}.",
            Title);
    }

    // Elements that never generate keep their custom code as it is: their generated half would not follow.
    private List<Rewrite> PlanRewrites(GumProjectSave project, CodeOutputProjectSettings projectSettings)
    {
        List<Rewrite> rewrites = new List<Rewrite>();
        foreach (ElementSave element in project.Screens.Cast<ElementSave>().Concat(project.Components))
        {
            CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
            if (elementSettings.GenerationBehavior == GenerationBehavior.NeverGenerate)
            {
                continue;
            }

            FilePath? file = _fileLocationsService.GetCustomCodeFileName(
                element, elementSettings, projectSettings, _codeGenerator.GetVisualApiForElement(element));
            if (file == null || !File.Exists(file.FullPath))
            {
                continue;
            }

            string contents;
            try
            {
                contents = File.ReadAllText(file.FullPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                continue;
            }

            string updated = _headerRewriter.Rewrite(contents, element, elementSettings, projectSettings);
            if (updated != contents)
            {
                rewrites.Add(new Rewrite(element.Name, new FilePath(Path.GetFullPath(file.FullPath)), updated));
            }
        }
        return rewrites;
    }

    // Each rewritten file is recorded as created, so Restore removes it and copies the original back.
    private string? Apply(FilePath projectFile, List<Rewrite> rewrites, out CodeFileBackup? backup)
    {
        backup = null;
        try
        {
            backup = _backupService.Create(projectFile, rewrites.Select(rewrite => rewrite.File).ToList());
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            return "Could not back up the files, so nothing was changed: " + exception.Message;
        }

        try
        {
            foreach (Rewrite rewrite in rewrites)
            {
                File.WriteAllText(rewrite.File.FullPath, rewrite.Contents);
                _backupService.AddCreatedFile(backup, rewrite.File);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _backupService.Restore(backup);
            return "The update stopped: " + exception.Message + " Every file was put back as it was.";
        }
        return null;
    }

    private sealed record Rewrite(string ElementName, FilePath File, string Contents);
}
