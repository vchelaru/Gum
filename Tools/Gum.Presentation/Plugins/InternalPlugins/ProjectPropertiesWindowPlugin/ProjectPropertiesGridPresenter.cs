using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// Drives the Project Properties tab's property grid over a <see cref="ProjectPropertiesViewModel"/>,
/// for either head: builds the rows from the view model's members, groups and names them the way
/// the tool always has, and rebuilds on <see cref="ProjectPropertiesViewModel.Reloaded"/>.
/// </summary>
public sealed class ProjectPropertiesGridPresenter
{
    private readonly IDataUiGrid _grid;
    private readonly ProjectPropertiesSection? _section;
    private ProjectPropertiesViewModel? _viewModel;

    /// <summary>
    /// Creates a presenter over <paramref name="grid"/>; with a <paramref name="section"/> it shows only
    /// that page's settings, without one it shows all of them.
    /// </summary>
    public ProjectPropertiesGridPresenter(IDataUiGrid grid, ProjectPropertiesSection? section = null)
    {
        _grid = grid;
        _section = section;
    }

    /// <summary>The view model shown, or null.</summary>
    public ProjectPropertiesViewModel? ViewModel => _viewModel;

    /// <summary>Shows <paramref name="viewModel"/> (or clears the grid for null) and follows its changes.</summary>
    public void Bind(ProjectPropertiesViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.Reloaded -= Rebuild;
        }
        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.Reloaded += Rebuild;
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
            ApplyLayout(_grid.Model, _viewModel, _section);
        }
    }

    /// <summary>
    /// Shapes the auto-populated rows of <paramref name="grid"/>: the guide and single pixel texture
    /// members in their own categories, spaced display names, the file editors and option lists,
    /// and no rows for the view model's bookkeeping members. With a <paramref name="section"/>, only
    /// that section's rows are kept.
    /// </summary>
    public static void ApplyLayout(DataUiGridModel grid, ProjectPropertiesViewModel viewModel, ProjectPropertiesSection? section = null)
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
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.AvailableLanguages));

            if (languages.Count == 0)
            {
                RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.LanguageName));
            }
        }

        InstanceMember? textureFilter = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.TextureFilter));
        if (textureFilter != null)
        {
            textureFilter.CustomOptions = new List<object> { TextureFilter.Point, TextureFilter.Linear };
        }

        if (section != null)
        {
            KeepOnlySection(grid, section.Value);
        }
    }

    // A page shows its own settings; the rest stay on the other pages.
    private static void KeepOnlySection(DataUiGridModel grid, ProjectPropertiesSection section)
    {
        foreach (MemberCategory category in grid.Categories.ToArray())
        {
            foreach (InstanceMember member in category.Members.ToArray())
            {
                if (SectionOf(member.Name) != section)
                {
                    category.Members.Remove(member);
                }
            }
            if (category.Members.Count == 0)
            {
                grid.Categories.Remove(category);
            }
        }
    }

    private static ProjectPropertiesSection? SectionOf(string memberName) => memberName switch
    {
        nameof(ProjectPropertiesViewModel.TextureFilter)
            or nameof(ProjectPropertiesViewModel.RestrictToUnitValues)
            or nameof(ProjectPropertiesViewModel.RestrictFileNamesForAndroid)
            or nameof(ProjectPropertiesViewModel.SinglePixelTextureFile)
            or nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft)
            or nameof(ProjectPropertiesViewModel.SinglePixelTextureTop)
            or nameof(ProjectPropertiesViewModel.SinglePixelTextureRight)
            or nameof(ProjectPropertiesViewModel.SinglePixelTextureBottom) => ProjectPropertiesSection.General,
        nameof(ProjectPropertiesViewModel.CanvasWidth)
            or nameof(ProjectPropertiesViewModel.CanvasHeight)
            or nameof(ProjectPropertiesViewModel.ShowOutlines)
            or nameof(ProjectPropertiesViewModel.ShowCanvasOutline)
            or nameof(ProjectPropertiesViewModel.ShowCheckerBackground) => ProjectPropertiesSection.Canvas,
        nameof(ProjectPropertiesViewModel.ShowLocalization)
            or nameof(ProjectPropertiesViewModel.LocalizationFiles)
            or nameof(ProjectPropertiesViewModel.LanguageName) => ProjectPropertiesSection.Localization,
        _ => null,
    };

    private static void RemoveMember(ICollection<InstanceMember> members, string name)
    {
        InstanceMember? member = members.FirstOrDefault(item => item.Name == name);
        if (member != null)
        {
            members.Remove(member);
        }
    }
}
