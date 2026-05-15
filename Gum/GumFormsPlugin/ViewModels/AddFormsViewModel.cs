using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Plugins.ImportPlugin.Manager;
using ToolsUtilities;
using GumFormsPlugin.Services;
using Gum.Managers;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Logic.FileWatch;

namespace GumFormsPlugin.ViewModels;

public class AddFormsViewModel : DialogViewModel
{
    #region Fields/Properties

    private readonly FormsFileService _formsFileService;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;
    private readonly IFileWatchManager _fileWatchManager;

    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    public IReadOnlyList<string> AvailableThemes
    {
        get => Get<IReadOnlyList<string>>() ?? Array.Empty<string>();
        private set => Set(value);
    }

    public string? SelectedTheme
    {
        get => Get<string?>();
        set => Set(value);
    }

    public bool HasMultipleThemes => AvailableThemes.Count > 1;

    #endregion

    public AddFormsViewModel(FormsFileService formsFileService,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IImportLogic importLogic,
        IProjectState projectState,
        IFileWatchManager fileWatchManager)
    {
        _formsFileService = formsFileService;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _importLogic = importLogic;
        _projectState = projectState;
        _fileWatchManager = fileWatchManager;

        AvailableThemes = _formsFileService.GetAvailableThemes();
        SelectedTheme = AvailableThemes.FirstOrDefault(t =>
                            string.Equals(t, FormsFileService.DefaultThemeName, System.StringComparison.OrdinalIgnoreCase))
                        ?? AvailableThemes.FirstOrDefault();
    }

    public override void OnAffirmative()
    {
        var theme = SelectedTheme ?? FormsFileService.DefaultThemeName;

        // Check the theme's prerequisites against the user's project before
        // touching any files. If the user declines the required project-level
        // edits we abort without writing anything.
        var requirements = ThemeRequirements.LoadFromThemeDirectory(_formsFileService.GetThemeDirectory(theme));
        var diff = requirements.Diff(_projectState.GumProjectSave);
        if (diff.HasGumxChanges && !ConfirmPrerequisites(theme, diff))
        {
            base.OnAffirmative();
            return;
        }

        var sourceDestinations = _formsFileService.GetSourceDestinations(theme, IsIncludeDemoScreenGum);
        bool canSaveFiles = GetIfShouldSave(sourceDestinations);

        if (canSaveFiles)
        {
            // Apply the prerequisite edits to the in-memory project before saving,
            // so the gumx written below already contains the new font generator
            // and standard references.
            diff.Apply(_projectState.GumProjectSave);

            SaveFilesToDestination(sourceDestinations);

            // now add all components and screens to the project
            AddAllElementsToProject(sourceDestinations);

            // reload standards:
            var fileName = _projectState.GumProjectSave.FullFileName;
            bool wasSaved = _fileCommands.TryAutoSaveProject();
            if (wasSaved)
            {
                _fileCommands.LoadProject(fileName);

                if (diff.HasGumxChanges && diff.RuntimePackages.Count > 0)
                {
                    _dialogService.ShowMessage(BuildRuntimePackagesMessage(theme, diff.RuntimePackages));
                }
            }
            else
            {
                _dialogService.ShowMessage("You must Save, then close/reopen the project.");
            }
        }
        base.OnAffirmative();
    }

    private bool ConfirmPrerequisites(string theme, ThemeRequirementsDiff diff)
    {
        var lines = new List<string>
        {
            $"The {theme} theme requires the following project-level changes:",
            string.Empty,
        };
        lines.AddRange(diff.DescribeGumxChanges().Select(c => "  • " + c));

        if (diff.RuntimePackages.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Your game project will also need these runtime packages added manually:");
            foreach (var pkg in diff.RuntimePackages)
            {
                lines.Add("  • " + pkg);
            }
        }

        lines.Add(string.Empty);
        lines.Add("Apply these project changes and proceed with the import?");

        return _dialogService.ShowYesNoMessage(string.Join("\n", lines), $"Apply {theme} prerequisites?");
    }

    private static string BuildRuntimePackagesMessage(string theme, IReadOnlyList<string> packages)
    {
        var lines = new List<string>
        {
            $"{theme} imported successfully.",
            string.Empty,
            "Don't forget to add these runtime packages to your game project:",
        };
        foreach (var pkg in packages)
        {
            lines.Add("  • " + pkg);
        }
        return string.Join("\n", lines);
    }


    private void AddAllElementsToProject(Dictionary<string, FilePath> sourceDestinations)
    {
        foreach (var item in sourceDestinations)
        {
            var extension = item.Value.Extension;

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
        foreach (var kvp in sourceDestinations)
        {
            var sourcePath = kvp.Key;
            var destination = kvp.Value;

            var extension = destination.Extension;

            // don't save the project file — overwriting it would wipe existing
            // projects which may have screens or other components referenced.
            if (extension == "gumx") continue;


            var directory = Path.GetDirectoryName(destination.FullPath)!;

            _fileWatchManager.IgnoreNextChangeUntil(directory);
            _fileWatchManager.IgnoreNextChangeUntil(destination.FullPath);
            Directory.CreateDirectory(directory);
            File.Copy(sourcePath, destination.FullPath, overwrite: true);
        }
    }


    private bool GetIfShouldSave(Dictionary<string, FilePath> sourceDestinations)
    {
        var existingFiles = sourceDestinations.Values.Where(item => item.Exists());

        var doStandardsExist = existingFiles.Any(item => item.Extension == "gutx");
        var nonStandardFiles = existingFiles.Where(item => item.Extension != "gutx").ToList();

        var nonStandardWhichBlockCopying = nonStandardFiles.Where(item =>
        {
            // don't block on gumx:
            if (item.Extension == "gumx")
            {
                return false;
            }
            return true;
        }).ToList();


        var shouldSave = false;
        if (nonStandardWhichBlockCopying.Count > 0)
        {
            var message = "Cannot add Forms controls because the following file(s) would get overwritten:"
                + "\n\n" + string.Join("\n", nonStandardFiles);
            _dialogService.ShowMessage(message);
        }
        else if (doStandardsExist)
        {
            var filesWhichWouldGetOverwritten = existingFiles
                .Where(item =>
                {
                    return item.Extension != "gumx";
                })
                .Select(item => item.RelativeTo(_projectState.ProjectDirectory!))
                .ToList();

            var standardFiles = filesWhichWouldGetOverwritten.Where(item => item.EndsWith(".gutx")).ToList();
            var otherFiles = filesWhichWouldGetOverwritten.Except(standardFiles)
                // Be sure to ToList it here to evaluate on the spot
                .ToList();

            RemoveUnmodifiedAndUnusedStandards(standardFiles);



            string message = "";
            if(standardFiles.Any())
            {
                message += "Forms Component styling requires modifications to the Standard Elements " +
                    "in your project." +
                    "\n\nIf you have made any modification to any of the Standard Elements, " +
                    "this will overwrite that styling." +
                    "\n\nThe following components need to be modified:" +
                    "\n\n" + string.Join("\n", standardFiles);
            }

            if(otherFiles.Any())
            {
                message += "\n\nThe following files will also be ovewritten:" +
                    "\n\n" + string.Join("\n", otherFiles);
            }

            if(standardFiles.Any() || otherFiles.Any())
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
        foreach (var standardFile in standardFiles)
        {
            FilePath filePath = new FilePath(standardFile);
            var standardElementName = filePath.CaseSensitiveNoPathNoExtension;
            var standardElement = ObjectFinder.Self.GetStandardElement(standardElementName);

            if (standardElement != null)
            {
                // See if the states differ:
                var potentiallyModifiedDefault = standardElement.DefaultState;
                var actualDefault = StandardElementsManager.Self.GetDefaultStateFor(standardElementName)?.Clone();
                if(actualDefault != null)
                {
                    actualDefault.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));

                    // JSON comparison requires we have Newtonsoft, or requires running a newer version
                    // of .NET to have the JSON serailzier present. This isn't currently supported in Gum
                    // tool, so we need to use XML:
                    //var differ = JsonConvert.SerializeObject(potentiallyModifiedDefault) != JsonConvert.SerializeObject(actualDefault);
                    FileManager.XmlSerialize(potentiallyModifiedDefault, out string potentiallyModifiedSerialized);
                    FileManager.XmlSerialize(actualDefault, out string actualDefaultSerialized);
                    var differ = potentiallyModifiedSerialized != actualDefaultSerialized;

                    if (!differ)
                    {
                        differ = standardElement.Categories.Count > 0;
                    }
                    if(!differ)
                    {
                        toRemove.Add(standardFile);
                    }

                    if(differ)
                    {
                        // If it differs, we don't care if this isn't used anywhere so let's check
                        var referencesToThis = ObjectFinder.Self.GetElementReferencesToThis(standardElement);

                        if(referencesToThis.Count == 0)
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
