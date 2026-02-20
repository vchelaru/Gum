using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services;
using Gum.Services.Fonts;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Gui.Controls;

/// <summary>
/// Interaction logic for ProjectPropertiesControl.xaml
/// </summary>
public partial class ProjectPropertiesControl : UserControl
{
    public event EventHandler CloseClicked;

    public ProjectPropertiesViewModel ViewModel
    {
        get
        {
            return (ProjectPropertiesViewModel)DataGrid.Instance;
        }
        internal set
        {
            if(value != DataGrid.Instance)
            {
                DataGrid.Instance = value;

                UpdateToInstance();
            }
        }
    }

    public ProjectPropertiesControl()
    {
        InitializeComponent();
    }

    private void CancelButtonClicked(object? sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke(this, null);
    }

    private void UpdateToInstance()
    {
        // Move all colors into their own category:
        var allMembers = DataGrid.Categories.SelectMany(item => item.Members).ToArray();

        foreach(var member in allMembers)
        {
            member.SupportsMakeDefault = false;
        }

        DataGrid.MoveMemberToCategory(nameof(ViewModel.ShowOutlines), "Guides");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.ShowCanvasOutline), "Guides");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.ShowCheckerBackground), "Guides");


        DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureFile), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureLeft), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureTop), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureRight), "Single Pixel Texture");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureBottom), "Single Pixel Texture");

        DataGrid.MoveMemberToCategory(nameof(ViewModel.FontRanges), "Font Generation");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.UseFontCharacterFile), "Font Generation");
        var useFontCharacterFileMember = DataGrid.GetInstanceMember(nameof(ViewModel.UseFontCharacterFile));
        if (useFontCharacterFileMember != null)
        {
            useFontCharacterFileMember.DisplayName = useFontCharacterFileMember.DisplayName + " (.gumfcs)";
        }
        DataGrid.MoveMemberToCategory(nameof(ViewModel.FontSpacingHorizontal), "Font Generation");
        DataGrid.MoveMemberToCategory(nameof(ViewModel.FontSpacingVertical), "Font Generation");

        DataGrid.MoveMemberToCategory(nameof(ViewModel.AutoSizeFontOutputs), "Font Generation");
        var autoSizeMember = DataGrid.GetInstanceMember(nameof(ViewModel.AutoSizeFontOutputs));
        if(autoSizeMember != null)
        {
            autoSizeMember.DisplayName = "Auto-Size Font Outputs";
            autoSizeMember.DetailText = "Fewer PNGs, but font generation can be much slower";
            
        }

        var textureFilterMember = DataGrid.GetInstanceMember(nameof(ViewModel.TextureFilter));
        if(textureFilterMember != null)
        {
            textureFilterMember.CustomOptions = new List<object>()
            {
                TextureFilter.Point,
                TextureFilter.Linear
            };
        }

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

                if(member.Name == nameof(ViewModel.LocalizationFile))
                {
                    member.PreferredDisplayer = typeof(FileSelectionDisplay);
                }
                else if(member.Name == nameof(ViewModel.SinglePixelTextureFile))
                {
                    member.PreferredDisplayer = typeof(FileSelectionDisplay);
                }
            }

            var isUpdatingMember = category.Members.FirstOrDefault(item => item.Name == nameof(ViewModel.IsUpdatingFromModel));
            if(isUpdatingMember != null)
            {
                category.Members.Remove(isUpdatingMember);
            }
        }

        bool IsColor(InstanceMember member) => member.PropertyType.Name == "Microsoft.Xna.Framework.Color" || member.PropertyType.Name == "Color";
    }
}
