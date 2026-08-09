using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace GumFormsPlugin.Services;

/// <inheritdoc/>
public class FormsThemeImporter : IFormsThemeImporter
{
    private readonly IFormsFileService _formsFileService;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly ISkiaShapeStandardsLogic _skiaShapeStandards;

    public FormsThemeImporter(
        IFormsFileService formsFileService,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IImportLogic importLogic,
        IProjectState projectState,
        IFileWatchManager fileWatchManager,
        ISkiaShapeStandardsLogic skiaShapeStandards)
    {
        _formsFileService = formsFileService;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _importLogic = importLogic;
        _projectState = projectState;
        _fileWatchManager = fileWatchManager;
        _skiaShapeStandards = skiaShapeStandards;
    }

    /// <inheritdoc/>
    public bool ImportTheme(string themeName, bool isIncludeDemoScreenGum)
    {
        // Prerequisites have already been surfaced to the user (inline in the Add Forms dialog,
        // or implicitly by opting in to Forms at project creation) — no separate confirmation popup.
        ThemeRequirements requirements =
            ThemeRequirements.LoadFromThemeDirectory(_formsFileService.GetThemeDirectory(themeName));
        ThemeRequirementsDiff diff = requirements.Diff(_projectState.GumProjectSave);

        Dictionary<string, FilePath> sourceDestinations =
            _formsFileService.GetSourceDestinations(themeName, isIncludeDemoScreenGum);

        if (!GetIfShouldSave(sourceDestinations))
        {
            return false;
        }

        // Apply the prerequisite edits to the in-memory project before saving, so the gumx
        // written below already contains the new font generator and standard references.
        diff.Apply(_projectState.GumProjectSave, _skiaShapeStandards);

        SaveFilesToDestination(sourceDestinations);

        AddAllElementsToProject(sourceDestinations);

        // reload standards:
        string fileName = _projectState.GumProjectSave.FullFileName;
        bool wasSaved = _fileCommands.TryAutoSaveProject();
        if (wasSaved)
        {
            _fileCommands.LoadProject(fileName);
        }
        else
        {
            _dialogService.ShowMessage("You must Save, then close/reopen the project.");
        }

        return true;
    }

    private void AddAllElementsToProject(Dictionary<string, FilePath> sourceDestinations)
    {
        foreach (KeyValuePair<string, FilePath> item in sourceDestinations)
        {
            string extension = item.Value.Extension;

            if (extension == "gusx")
            {
                // add screen
                _importLogic.ImportScreen(item.Value, saveProject: false);
            }
            else if (extension == "gucx")
            {
                // add component
                _importLogic.ImportComponent(item.Value, saveProject: false);
            }
            else if (extension == "behx")
            {
                // add behavior
                _importLogic.ImportBehavior(item.Value, saveProject: false);
            }
            // standards are already added
        }
    }

    private void SaveFilesToDestination(Dictionary<string, FilePath> sourceDestinations)
    {
        foreach (KeyValuePair<string, FilePath> kvp in sourceDestinations)
        {
            string sourcePath = kvp.Key;
            FilePath destination = kvp.Value;

            // don't save the project file — overwriting it would wipe existing
            // projects which may have screens or other components referenced.
            if (destination.Extension == "gumx")
            {
                continue;
            }

            string directory = Path.GetDirectoryName(destination.FullPath)!;

            _fileWatchManager.IgnoreNextChangeUntil(directory);
            _fileWatchManager.IgnoreNextChangeUntil(destination.FullPath);
            Directory.CreateDirectory(directory);
            File.Copy(sourcePath, destination.FullPath, overwrite: true);
        }
    }

    private bool GetIfShouldSave(Dictionary<string, FilePath> sourceDestinations)
    {
        List<FilePath> existingFiles = sourceDestinations.Values.Where(item => item.Exists()).ToList();

        bool doStandardsExist = existingFiles.Any(item => item.Extension == "gutx");
        List<FilePath> nonStandardFiles = existingFiles.Where(item => item.Extension != "gutx").ToList();

        // don't block on gumx:
        List<FilePath> nonStandardWhichBlockCopying =
            nonStandardFiles.Where(item => item.Extension != "gumx").ToList();

        bool shouldSave = false;
        if (nonStandardWhichBlockCopying.Count > 0)
        {
            string message = "Cannot add Forms controls because the following file(s) would get overwritten:"
                + "\n\n" + string.Join("\n", nonStandardFiles);
            _dialogService.ShowMessage(message);
        }
        else if (doStandardsExist)
        {
            List<string> filesWhichWouldGetOverwritten = existingFiles
                .Where(item => item.Extension != "gumx")
                .Select(item => item.RelativeTo(_projectState.ProjectDirectory!))
                .ToList();

            List<string> standardFiles = filesWhichWouldGetOverwritten.Where(item => item.EndsWith(".gutx")).ToList();
            List<string> otherFiles = filesWhichWouldGetOverwritten.Except(standardFiles)
                // Be sure to ToList it here to evaluate on the spot
                .ToList();

            RemoveUnmodifiedAndUnusedStandards(standardFiles);

            string message = "";
            if (standardFiles.Any())
            {
                message += "Forms Component styling requires modifications to the Standard Elements " +
                    "in your project." +
                    "\n\nIf you have made any modification to any of the Standard Elements, " +
                    "this will overwrite that styling." +
                    "\n\nThe following components need to be modified:" +
                    "\n\n" + string.Join("\n", standardFiles);
            }

            if (otherFiles.Any())
            {
                message += "\n\nThe following files will also be ovewritten:" +
                    "\n\n" + string.Join("\n", otherFiles);
            }

            if (standardFiles.Any() || otherFiles.Any())
            {
                message += "\n\nProceed?";
                shouldSave = _dialogService.ShowYesNoMessage(message, "Ovewrite files?");
            }
            else
            {
                shouldSave = true;
            }
        }
        else
        {
            // I guess the user completely deleted everything?
            shouldSave = true;
        }

        return shouldSave;
    }

    private void RemoveUnmodifiedAndUnusedStandards(List<string> standardFiles)
    {
        List<string> toRemove = new List<string>();
        foreach (string standardFile in standardFiles)
        {
            FilePath filePath = new FilePath(standardFile);
            string standardElementName = filePath.CaseSensitiveNoPathNoExtension;
            ElementSave? standardElement = ObjectFinder.Self.GetStandardElement(standardElementName);

            if (standardElement != null)
            {
                // See if the states differ:
                StateSave potentiallyModifiedDefault = standardElement.DefaultState;
                StateSave? actualDefault =
                    StandardElementsManager.Self.GetDefaultStateFor(standardElementName)?.Clone();
                if (actualDefault != null)
                {
                    actualDefault.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));

                    // JSON comparison requires we have Newtonsoft, or requires running a newer version
                    // of .NET to have the JSON serailzier present. This isn't currently supported in Gum
                    // tool, so we need to use XML:
                    FileManager.XmlSerialize(potentiallyModifiedDefault, out string potentiallyModifiedSerialized);
                    FileManager.XmlSerialize(actualDefault, out string actualDefaultSerialized);
                    bool differ = potentiallyModifiedSerialized != actualDefaultSerialized;

                    if (!differ)
                    {
                        differ = standardElement.Categories.Count > 0;
                    }
                    if (!differ)
                    {
                        toRemove.Add(standardFile);
                    }

                    if (differ)
                    {
                        // If it differs, we don't care if this isn't used anywhere so let's check
                        if (ObjectFinder.Self.GetElementReferencesToThis(standardElement).Count == 0)
                        {
                            toRemove.Add(standardFile);
                        }
                    }
                }
            }
        }

        standardFiles.RemoveAll(item => toRemove.Contains(item));
    }
}
