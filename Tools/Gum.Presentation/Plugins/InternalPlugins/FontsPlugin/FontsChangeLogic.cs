using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.Plugins.FontsPlugin;

/// <summary>
/// What <see cref="FontsChangeLogic.HandlePropertyChanged"/> reports back for the plugin, which owns
/// the cached font character file path.
/// </summary>
public class FontsChangeResult
{
    /// <summary>True when the changed property was <see cref="FontsViewModel.UseFontCharacterFile"/>.</summary>
    public bool FontCharacterFileChanged { get; init; }

    /// <summary>The new font character file path; only meaningful when <see cref="FontCharacterFileChanged"/> is true.</summary>
    public FilePath? FontCharacterFileAbsolute { get; init; }
}

/// <summary>
/// What happens when something on the Fonts tab changes: the view model is written to the project, the
/// baked fonts are made again when a range or the generator changed, and the project is saved and the
/// canvas refreshed. Kept out of the plugin so it can be tested without plugin composition.
/// </summary>
public class FontsChangeLogic
{
    private readonly IFontManager _fontManager;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;
    private readonly IFileCommands _fileCommands;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IPluginManager _pluginManager;

    /// <summary>Creates the logic over the tool services it acts through.</summary>
    public FontsChangeLogic(
        IFontManager fontManager,
        IDialogService dialogService,
        IProjectState projectState,
        IFileCommands fileCommands,
        IWireframeCommands wireframeCommands,
        IGuiCommands guiCommands,
        IPluginManager pluginManager)
    {
        _fontManager = fontManager;
        _dialogService = dialogService;
        _projectState = projectState;
        _fileCommands = fileCommands;
        _wireframeCommands = wireframeCommands;
        _guiCommands = guiCommands;
        _pluginManager = pluginManager;
    }

    /// <summary>Reacts to <paramref name="propertyName"/> changing on <paramref name="viewModel"/>.</summary>
    public async Task<FontsChangeResult> HandlePropertyChanged(FontsViewModel viewModel, string? propertyName)
    {
        // View-only state (the font list, the derived read-only flag) is not project data.
        if (viewModel.IsUpdatingFromModel
            || propertyName is nameof(FontsViewModel.AvailableFonts) or nameof(FontsViewModel.IsFontRangesReadOnly))
        {
            return new FontsChangeResult();
        }

        GumProjectSave? project = _projectState.GumProjectSave;
        List<(string?, string)> entriesBefore = GetEntryKeys(project);

        viewModel.ApplyToModelObjects();

        bool shouldSaveAndRefresh = true;
        bool shouldReloadContent = false;
        bool fontCharacterFileChanged = false;
        FilePath? fontCharacterFileAbsolute = null;

        switch (propertyName)
        {
            case nameof(FontsViewModel.Entries):
                if (TryFixRanges(viewModel))
                {
                    // Setting the fixed range raises Entries again, which comes back through here.
                    return new FontsChangeResult();
                }

                if (GetEntryKeys(project).SequenceEqual(entriesBefore))
                {
                    // A row with no font yet, or an edit that left every saved value as it was.
                    return new FontsChangeResult();
                }

                // An invalid range is saved as typed (the Errors tab reports it) but never baked.
                if (viewModel.Entries.All(entry => BmfcSave.GetIfIsValidRange(entry.Ranges)))
                {
                    await RemakeFonts(project, "re-create it with the new font range values");
                    shouldReloadContent = true;
                }
                break;
            case nameof(FontsViewModel.UseFontCharacterFile):
                fontCharacterFileChanged = true;
                if (viewModel.UseFontCharacterFile)
                {
                    FilePath absolute = new FilePath(_projectState.ProjectDirectory + ".gumfcs");
                    fontCharacterFileAbsolute = absolute;

                    if (System.IO.File.Exists(absolute.FullPath))
                    {
                        viewModel.DefaultEntry.Ranges = BmfcSave.GenerateRangesFromFile(absolute.FullPath);
                    }
                }
                else
                {
                    fontCharacterFileAbsolute = null;
                    viewModel.DefaultEntry.Ranges = BmfcSave.DefaultRanges;
                }
                break;
            case nameof(FontsViewModel.FontGenerator):
                if (await RemakeFonts(project, "re-create it with the new font generator"))
                {
                    shouldReloadContent = true;
                    _guiCommands.RefreshVariables(force: true);
                }
                break;
        }

        if (propertyName != null)
        {
            _pluginManager.ProjectPropertySet(propertyName);
        }

        if (shouldSaveAndRefresh)
        {
            _wireframeCommands.Refresh(forceLayout: true, forceReloadContent: shouldReloadContent);
            _fileCommands.TryAutoSaveProject();
        }

        return new FontsChangeResult
        {
            FontCharacterFileChanged = fontCharacterFileChanged,
            FontCharacterFileAbsolute = fontCharacterFileAbsolute
        };
    }

    /// <summary>Fixes ranges that only have spaces in them; true when any row changed.</summary>
    private static bool TryFixRanges(FontsViewModel viewModel)
    {
        bool changed = false;
        foreach (FontRangeEntryViewModel entry in viewModel.Entries.ToList())
        {
            if (!BmfcSave.GetIfIsValidRange(entry.Ranges))
            {
                string fixedRange = BmfcSave.TryFixRange(entry.Ranges);
                if (fixedRange != entry.Ranges)
                {
                    entry.Ranges = fixedRange;
                    changed = true;
                }
            }
        }
        return changed;
    }

    private static List<(string?, string)> GetEntryKeys(GumProjectSave? project) =>
        project?.FontRangeEntries.Select(entry => (entry.FontFamily, entry.Ranges)).ToList() ?? new List<(string?, string)>();

    /// <summary>Clears the baked fonts and makes the missing ones again; false when the cache could not be cleared.</summary>
    private async Task<bool> RemakeFonts(GumProjectSave? project, string whyClearing)
    {
        try
        {
            _fontManager.DeleteFontCacheFolder();
        }
        catch (System.IO.IOException exception)
        {
            _dialogService.ShowMessage(
                $"Attempted to clear the font cache to {whyClearing} but was unable to do so:\n\n{exception}");
            return false;
        }

        // The tab is only filled from a loaded project.
        if (project != null)
        {
            await _fontManager.CreateAllMissingFontFiles(project);
        }
        return true;
    }
}
