using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services.Fonts;
using RenderingLibrary.Graphics.Fonts;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Gui.Controls;

/// <summary>
/// The WPF Project Properties tab: a property grid over the <see cref="ProjectPropertiesViewModel"/>
/// the shared plugin hands to the tab manager (TabViewRegistry makes it this control's DataContext).
/// The grid builds its rows from the view model's members, so it rebuilds on
/// <see cref="ProjectPropertiesViewModel.Reloaded"/> and hides the view-only members.
/// </summary>
public partial class ProjectPropertiesControl : UserControl
{
    private ProjectPropertiesViewModel? _viewModel;

    public ProjectPropertiesControl()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => Bind(e.NewValue as ProjectPropertiesViewModel);
    }

    private void Bind(ProjectPropertiesViewModel? viewModel)
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

    private void Rebuild()
    {
        // Clearing first makes the grid rebuild its rows even when the view model is the same object.
        DataGrid.Instance = null;
        if (_viewModel != null)
        {
            DataGrid.Instance = _viewModel;
            UpdateToInstance(_viewModel);
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
        if (_viewModel != null && DataGrid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontRanges)) is { } member)
        {
            member.IsReadOnly = _viewModel.IsFontRangesReadOnly;
        }
        DataGrid.Refresh();
    }

    private void CancelButtonClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel?.RequestClose();
    }

    private void UpdateToInstance(ProjectPropertiesViewModel viewModel)
    {
        // Move all colors into their own category:
        var allMembers = DataGrid.Categories.SelectMany(item => item.Members).ToArray();

        foreach(var member in allMembers)
        {
            member.SupportsMakeDefault = false;
        }

        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowOutlines), "Guides");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowCanvasOutline), "Guides");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.ShowCheckerBackground), "Guides");

        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureFile), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureTop), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureRight), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.SinglePixelTextureBottom), "Single Pixel Texture");

        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontRanges), "Font Generation");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.UseFontCharacterFile), "Font Generation");
        var useFontCharacterFileMember = DataGrid.GetInstanceMember(nameof(ProjectPropertiesViewModel.UseFontCharacterFile));
        if (useFontCharacterFileMember != null)
        {
            useFontCharacterFileMember.DisplayName = useFontCharacterFileMember.DisplayName + " (.gumfcs)";
        }
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontSpacingHorizontal), "Font Generation");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontSpacingVertical), "Font Generation");

        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs), "Font Generation");
        DataGrid.MoveMemberToCategory(nameof(ProjectPropertiesViewModel.FontGenerator), "Font Generation");

        var fontGeneratorMember = DataGrid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontGenerator));
        if (fontGeneratorMember != null)
        {
            fontGeneratorMember.CustomOptions = new List<object>()
            {
                FontGeneratorType.BmFont,
                FontGeneratorType.KernSmith
            };
        }

        var autoSizeMember = DataGrid.GetInstanceMember(nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs));
        if(autoSizeMember != null)
        {
            autoSizeMember.DisplayName = "Auto-Size Font Outputs";
            autoSizeMember.DetailText = "Fewer PNGs, but font generation can be much slower";
        }

        var textureFilterMember = DataGrid.GetInstanceMember(nameof(ProjectPropertiesViewModel.TextureFilter));
        if(textureFilterMember != null)
        {
            textureFilterMember.CustomOptions = new List<object>()
            {
                TextureFilter.Point,
                TextureFilter.Linear
            };
        }

        IReadOnlyList<string> languages = viewModel.AvailableLanguages;
        foreach (var category in DataGrid.Categories)
        {
            foreach (var member in category.Members)
            {
                member.DisplayName =
                        ToolsUtilities.StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);

                if(IsColor(member))
                {
                    member.PreferredDisplayer = typeof(Gum.Controls.DataUi.ColorDisplay);
                }

                if(member.Name == nameof(ProjectPropertiesViewModel.LocalizationFiles))
                {
                    member.PreferredDisplayer = typeof(MultiFileDisplay);
                    member.PropertiesToSetOnDisplayer["Filter"] = "Localization Files|*.csv;*.resx|All Files|*.*";
                }
                else if(member.Name == nameof(ProjectPropertiesViewModel.LanguageName))
                {
                    member.DisplayName = "Language";
                    if(languages.Count > 0)
                        member.CustomOptions = languages.Cast<object>().ToList();
                }
                else if(member.Name == nameof(ProjectPropertiesViewModel.SinglePixelTextureFile))
                {
                    member.PreferredDisplayer = typeof(FileSelectionDisplay);
                }
            }

            // Members that are view state or bookkeeping, not settings, get no row.
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.IsUpdatingFromModel));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.LanguageIndex));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.IsFontRangesReadOnly));
            RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.AvailableLanguages));

            if(languages.Count == 0)
            {
                RemoveMember(category.Members, nameof(ProjectPropertiesViewModel.LanguageName));
            }
        }

        RefreshFontRangeEditability();

        bool IsColor(InstanceMember member) => member.PropertyType.Name == "Microsoft.Xna.Framework.Color" || member.PropertyType.Name == "Color";
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
