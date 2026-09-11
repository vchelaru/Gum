using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gum.DataTypes;
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// Drives the Project Properties tab's property grid over a <see cref="ProjectPropertiesViewModel"/>,
/// for either head: builds the rows from the view model's members, groups and names them the way
/// the tool always has, rebuilds on <see cref="ProjectPropertiesViewModel.Reloaded"/>, and keeps the
/// font ranges read-only while a font character file is in use.
/// </summary>
public sealed class ProjectPropertiesGridPresenter
{
    private readonly IDataUiGrid _grid;
    private ProjectPropertiesViewModel? _viewModel;

    /// <summary>Creates a presenter over <paramref name="grid"/>.</summary>
    public ProjectPropertiesGridPresenter(IDataUiGrid grid)
    {
        _grid = grid;
    }

    /// <summary>The view model shown, or null.</summary>
    public ProjectPropertiesViewModel? ViewModel => _viewModel;

    /// <summary>Shows <paramref name="viewModel"/> (or clears the grid for null) and follows its changes.</summary>
    public void Bind(ProjectPropertiesViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.Reloaded -= Rebuild;
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }
        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.Reloaded += Rebuild;
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }
        Rebuild();
    }

    /// <summary>Rebuilds the rows from the view model's current members.</summary>
    public void Rebuild()
    {
        // Clearing first makes the grid rebuild its rows even when the view model is the same object.
        _grid.Instance = null;
        if (_viewModel != null)
        {
            _grid.Instance = _viewModel;
            ApplyLayout(_grid.Model, _viewModel);
            RefreshFontRangeEditability();
        }
    }

    /// <summary>
    /// Shapes the auto-populated rows of <paramref name="grid"/>: the guide, single pixel texture
    /// and font generation members in their own categories, spaced display names, the file editors
    /// and option lists, and no rows for the view model's bookkeeping members.
    /// </summary>
    public static void ApplyLayout(DataUiGridModel grid, ProjectPropertiesViewModel viewModel)
    {
        foreach (InstanceMember member in grid.Categories.SelectMany(category => category.Members).ToArray())
        {
            member.SupportsMakeDefault = false;
        }

        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowOutlines), "Guides");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowCanvasOutline), "Guides");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowCheckerBackground), "Guides");

        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureFile), "Single Pixel Texture");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft), "Single Pixel Texture");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureTop), "Single Pixel Texture");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureRight), "Single Pixel Texture");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureBottom), "Single Pixel Texture");

        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontRanges), "Font Generation");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.UseFontCharacterFile), "Font Generation");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontSpacingHorizontal), "Font Generation");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontSpacingVertical), "Font Generation");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs), "Font Generation");
        grid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontGenerator), "Font Generation");

        IReadOnlyList<string> languages = viewModel.AvailableLanguages;
        foreach (MemberCategory category in grid.Categories)
        {
            foreach (InstanceMember member in category.Members)
            {
                member.DisplayName = StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);

                if (member.Name == nameof(ProjectPropertiesViewModel.LocalizationFiles))
                {
                    member.PreferredDisplayer = typeof(StandardDisplayers.MultiFile);
                    member.PropertiesToSetOnDisplayer["Filter"] = "Localization Files|*.csv;*.resx|All Files|*.*";
                }
                else if (member.Name == nameof(ProjectPropertiesViewModel.LanguageName))
                {
                    member.DisplayName = "Language";
                    if (languages.Count > 0)
                    {
                        member.CustomOptions = languages.Cast<object>().ToList();
                    }
                }
                else if (member.Name == nameof(ProjectPropertiesViewModel.SinglePixelTextureFile))
                {
                    member.PreferredDisplayer = typeof(StandardDisplayers.FileSelection);
                }
            }

            // Members that are view state or bookkeeping, not settings, get no row.
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.IsUpdatingFromModel));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.LanguageIndex));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.IsFontRangesReadOnly));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.AvailableLanguages));

            if (languages.Count == 0)
            {
                RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.LanguageName));
            }
        }

        // Named after the spacing pass, which would otherwise split "Auto-Size" at its capital.
        InstanceMember? useFontCharacterFile = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.UseFontCharacterFile));
        if (useFontCharacterFile != null)
        {
            useFontCharacterFile.DisplayName = useFontCharacterFile.DisplayName + " (.gumfcs)";
        }

        InstanceMember? fontGenerator = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontGenerator));
        if (fontGenerator != null)
        {
            fontGenerator.CustomOptions = new List<object> { FontGeneratorType.BmFont, FontGeneratorType.KernSmith };
        }

        InstanceMember? autoSize = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs));
        if (autoSize != null)
        {
            autoSize.DisplayName = "Auto-Size Font Outputs";
            autoSize.DetailText = "Fewer PNGs, but font generation can be much slower";
        }

        InstanceMember? textureFilter = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.TextureFilter));
        if (textureFilter != null)
        {
            textureFilter.CustomOptions = new List<object> { TextureFilter.Point, TextureFilter.Linear };
        }
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectPropertiesViewModel.IsFontRangesReadOnly) or nameof(ProjectPropertiesViewModel.FontRanges))
        {
            RefreshFontRangeEditability();
        }
    }

    private void RefreshFontRangeEditability()
    {
        if (_viewModel != null && _grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontRanges)) is { } member)
        {
            member.IsReadOnly = _viewModel.IsFontRangesReadOnly;
        }
        _grid.Refresh();
    }

    private static void RemoveMember(ICollection<InstanceMember> members, string name)
    {
        InstanceMember? member = members.FirstOrDefault(item => item.Name == name);
        if (member != null)
        {
            members.Remove(member);
        }
    }
}
