using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using ImportFromGumxPlugin;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsPlugin.Services;

/// <inheritdoc/>
/// <remarks>
/// The theme is imported through <see cref="IGumxImportService"/>, the same pipeline as Import from
/// .gumx. What stays here is specific to Forms themes: the theme's prerequisites, which Standards a
/// theme may touch, the optional demo screen, and the font license files.
/// </remarks>
public class FormsThemeImporter : IFormsThemeImporter
{
    private const string DemoScreenName = "DemoScreenGum";

    private readonly IFormsFileService _formsFileService;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;
    private readonly IFileWatchIgnoreList _fileWatch;
    private readonly ISkiaShapeStandardsLogic _skiaShapeStandards;
    private readonly IFontManager _fontManager;
    private readonly IImportVersionGate _versionGate;
    private readonly IImportFromGumxDialog _importDialog;
    private readonly IGumxSourceService _sourceService;
    private readonly IGumxDependencyResolver _dependencyResolver;
    private readonly IGumxImportService _importService;

    public FormsThemeImporter(
        IFormsFileService formsFileService,
        IDialogService dialogService,
        IProjectState projectState,
        IFileWatchIgnoreList fileWatch,
        ISkiaShapeStandardsLogic skiaShapeStandards,
        IFontManager fontManager,
        IImportVersionGate versionGate,
        IImportFromGumxDialog importDialog,
        IGumxSourceService sourceService,
        IGumxDependencyResolver dependencyResolver,
        IGumxImportService importService)
    {
        _formsFileService = formsFileService;
        _dialogService = dialogService;
        _projectState = projectState;
        _fileWatch = fileWatch;
        _skiaShapeStandards = skiaShapeStandards;
        _fontManager = fontManager;
        _versionGate = versionGate;
        _importDialog = importDialog;
        _sourceService = sourceService;
        _dependencyResolver = dependencyResolver;
        _importService = importService;
    }

    /// <inheritdoc/>
    public async Task<bool> ImportThemeAsync(string themeName, bool isIncludeDemoScreenGum)
    {
        // Prerequisites have already been surfaced to the user (inline in the Add Forms dialog,
        // or implicitly by opting in to Forms at project creation) — no separate confirmation popup.
        if (_projectState.GumProjectSave is not { } project)
        {
            return false;
        }

        string themeDirectory = _formsFileService.GetThemeDirectory(themeName);
        ThemeRequirements requirements = ThemeRequirements.LoadFromThemeDirectory(themeDirectory);
        if (requirements.ProjectVersion is { } themeVersion)
        {
            string? rejection = _versionGate.GetRejection(themeVersion, project.Version, "The Forms theme");
            if (rejection != null)
            {
                _dialogService.ShowMessage(rejection);
                return false;
            }
        }

        ThemeRequirementsDiff diff = requirements.Diff(project);

        string themeProjectPath = _formsFileService.GetFormsGumxPath(themeName);
        GumProjectSave? theme = await _sourceService.LoadProjectAsync(themeProjectPath);
        if (theme == null)
        {
            _dialogService.ShowMessage($"The {themeName} theme could not be loaded from {themeProjectPath}.");
            return false;
        }

        bool isDefaultTheme = string.Equals(themeName, _formsFileService.DefaultThemeName, StringComparison.OrdinalIgnoreCase);
        ImportSelections selections = BuildSelections(theme, isDefaultTheme, isIncludeDemoScreenGum);

        ImportDecision decision = GetImportDecision(theme, selections, themeName, isDefaultTheme, isIncludeDemoScreenGum);
        if (decision == ImportDecision.PickWhatToImport)
        {
            return await PickWhatToImportAsync(themeProjectPath, themeName, isDefaultTheme, isIncludeDemoScreenGum, diff, project);
        }
        if (decision == ImportDecision.Cancel)
        {
            return false;
        }

        // The prerequisites apply before the elements are imported, since they can need what the
        // prerequisites add (a Skia shape Standard, for example). A theme's Standards are replaced
        // whole but never their animations: Add Forms has never touched those.
        ImportResult result = await _importService.ImportAsync(
            selections,
            theme,
            _sourceService.GetSourceBase(themeProjectPath),
            destinationSubfolder: string.Empty,
            beforeImport: () => ApplyRequirements(diff, project),
            replaceStandardAnimations: false);

        if (result.ConflictingElements.Count > 0)
        {
            _dialogService.ShowMessage(
                "Add Forms stopped because these changed in your project while it was working:\n\n" +
                string.Join("\n", result.ConflictingElements));
            return false;
        }

        CopyFontLicenseFiles(themeDirectory);

        if (result.SkippedElements.Count > 0)
        {
            _dialogService.ShowMessage(
                "These were not added because files they use could not be found:\n\n" +
                string.Join("\n", result.SkippedElements));
        }
        if (result.NeedsManualSave)
        {
            _dialogService.ShowMessage("You must Save, then close/reopen the project.");
        }

        return true;
    }

    /// <summary>
    /// Everything the theme adds: all its components (in an order that is safe to import), behaviors,
    /// the demo screen when asked for, and its Standards for the default theme only. A non-default
    /// theme never touches the project's Standards: its look must live in its own Components, or
    /// importing one theme would stomp another.
    /// </summary>
    private ImportSelections BuildSelections(GumProjectSave theme, bool isDefaultTheme, bool isIncludeDemoScreenGum)
    {
        return new ImportSelections
        {
            // Direct rather than transitive components, so behaviors are registered first.
            DirectComponents = _dependencyResolver.SortLeavesFirst(theme.Components, theme).ToList(),
            DirectScreens = isIncludeDemoScreenGum ? theme.Screens.Where(IsDemoScreen).ToList() : new List<ScreenSave>(),
            Behaviors = theme.Behaviors.ToList(),
            Standards = isDefaultTheme ? theme.StandardElements.ToList() : new List<StandardElementSave>(),
        };
    }

    private static bool IsDemoScreen(ScreenSave screen) =>
        screen.Name == DemoScreenName || screen.Name.EndsWith("/" + DemoScreenName, StringComparison.Ordinal);

    /// <summary>
    /// Opens the Import from .gumx dialog on the theme's own project so the user picks items one by
    /// one (and sees its Skip/Overwrite prompt). The theme's prerequisites apply only if that
    /// import goes through.
    /// </summary>
    private Task<bool> PickWhatToImportAsync(
        string themeProjectPath,
        string themeName,
        bool isDefaultTheme,
        bool isIncludeDemoScreenGum,
        ThemeRequirementsDiff diff,
        GumProjectSave project)
    {
        FixedImportSource source = new FixedImportSource(
            themeProjectPath,
            $"Add Forms: {themeName}",
            IncludeStandards: isDefaultTheme,
            BeforeImport: () => ApplyRequirements(diff, project),
            PreselectScreens: isIncludeDemoScreenGum);
        return _importDialog.ShowAsync(source);
    }

    // Apply the prerequisite edits to the in-memory project before saving, so the gumx written
    // afterwards already contains the new font generator and standard references.
    private void ApplyRequirements(ThemeRequirementsDiff diff, GumProjectSave project)
    {
        diff.Apply(project, _skiaShapeStandards);

        // Fonts already cached were generated without the theme's characters. Clearing the cache
        // lets the reload that follows regenerate them with the widened ranges - the same thing the
        // Project Properties window does when FontRanges changes.
        if (diff.FontRangesChange != null)
        {
            TryClearFontCache();
        }
    }

    private void TryClearFontCache()
    {
        try
        {
            _fontManager.DeleteFontCacheFolder();
        }
        catch (IOException exception)
        {
            _dialogService.ShowMessage(
                "The project's font ranges were widened for this theme, but the font cache could not be cleared " +
                $"to regenerate its fonts. Delete the FontCache folder and reopen the project:\n\n{exception.Message}");
        }
    }

    /// <summary>
    /// Copies the license text beside each bundled font (for example <c>Fonts/OFL.txt</c>) into the
    /// project. Nothing references them, so the import does not carry them. A file the project
    /// already has is left alone.
    /// </summary>
    private void CopyFontLicenseFiles(string themeDirectory)
    {
        string? projectDirectory = _projectState.ProjectDirectory;
        string themeFontsDirectory = Path.Combine(themeDirectory, "Fonts");
        if (projectDirectory == null || !Directory.Exists(themeFontsDirectory))
        {
            return;
        }

        foreach (string licenseFile in Directory.GetFiles(themeFontsDirectory, "*.txt"))
        {
            string destination = Path.Combine(projectDirectory, "Fonts", Path.GetFileName(licenseFile));
            if (File.Exists(destination))
            {
                continue;
            }

            _fileWatch.IgnoreNextChangeUntil(new FilePath(Path.GetDirectoryName(destination)!));
            _fileWatch.IgnoreNextChangeUntil(new FilePath(destination));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(licenseFile, destination);
        }
    }

    private enum ImportDecision
    {
        Proceed,
        Cancel,
        PickWhatToImport,
    }

    private ImportDecision GetImportDecision(
        GumProjectSave theme,
        ImportSelections selections,
        string themeName,
        bool isDefaultTheme,
        bool isIncludeDemoScreenGum)
    {
        List<string> changedInProject = GetItemsChangedInProject(theme, selections);
        if (changedInProject.Count > 0)
        {
            string message = "These already exist in your project with different content:"
                + "\n\n" + string.Join("\n", changedInProject);
            MessageDialogResult answer = _dialogService.ShowMessage(message, "Add Forms",
                new MessageDialogStyle { AffirmativeText = "Pick what to import", NegativeText = "Cancel" });
            return answer == MessageDialogResult.Affirmative ? ImportDecision.PickWhatToImport : ImportDecision.Cancel;
        }

        return isDefaultTheme && !ConfirmStandardsOverwrite(themeName, isIncludeDemoScreenGum)
            ? ImportDecision.Cancel
            : ImportDecision.Proceed;
    }

    // The selected components, screens and behaviors the project already has with other content.
    private List<string> GetItemsChangedInProject(GumProjectSave theme, ImportSelections selections)
    {
        ImportDestinationStatus status = _importService.GetDestinationStatus(theme, string.Empty);
        List<string> changed = new List<string>();
        changed.AddRange(selections.DirectComponents
            .Where(component => status.Differing.Contains(new ImportItemKey(ElementItemType.Component, component.Name)))
            .Select(component => "Components/" + component.Name));
        changed.AddRange(selections.DirectScreens
            .Where(screen => status.Differing.Contains(new ImportItemKey(ElementItemType.Screen, screen.Name)))
            .Select(screen => "Screens/" + screen.Name));
        changed.AddRange(selections.Behaviors
            .Where(behavior => status.Differing.Contains(new ImportItemKey(ElementItemType.Behavior, behavior.Name)))
            .Select(behavior => "Behaviors/" + behavior.Name));
        return changed;
    }

    // The default theme replaces the project's Standards. Those the user modified and that something
    // uses are worth a confirmation; unmodified or unused ones are not.
    private bool ConfirmStandardsOverwrite(string themeName, bool isIncludeDemoScreenGum)
    {
        List<string> standardFiles = _formsFileService.GetSourceDestinations(themeName, isIncludeDemoScreenGum)
            .Where(kvp => FormsFileService.IsStandardExtension(kvp.Value.Extension)
                && kvp.Value.Exists()
                && !AreFilesIdentical(kvp.Key, kvp.Value.FullPath))
            .Select(kvp => kvp.Value.RelativeTo(_projectState.ProjectDirectory!))
            .ToList();

        RemoveUnmodifiedAndUnusedStandards(standardFiles);
        if (!standardFiles.Any())
        {
            return true;
        }

        string message = "Forms Component styling requires modifications to the Standard Elements " +
            "in your project." +
            "\n\nIf you have made any modification to any of the Standard Elements, " +
            "this will overwrite that styling." +
            "\n\nThe following components need to be modified:" +
            "\n\n" + string.Join("\n", standardFiles) +
            "\n\nProceed?";
        return _dialogService.ShowYesNoMessage(message, "Overwrite files?");
    }

    private static bool AreFilesIdentical(string sourcePath, string destinationPath)
    {
        // The source is always a real on-disk theme file in production, but some test doubles use
        // placeholder source keys with no backing file - treat that as "not identical" (preserves
        // the original always-block behavior) rather than throwing.
        if (!File.Exists(sourcePath))
        {
            return false;
        }

        FileInfo sourceInfo = new FileInfo(sourcePath);
        FileInfo destinationInfo = new FileInfo(destinationPath);
        if (sourceInfo.Length != destinationInfo.Length)
        {
            return false;
        }

        return File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(destinationPath));
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
                StateSave? potentiallyModifiedDefault = standardElement.DefaultState;
                StateSave? actualDefault =
                    StandardElementsManager.Self.GetDefaultStateFor(standardElementName)?.Clone();
                if (actualDefault != null)
                {
                    actualDefault.Variables.Sort((a, b) => GumProjectSave.CompareNames(a.Name, b.Name));

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
