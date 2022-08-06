using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gum.Plugins.PropertiesWindowPlugin;
using ToolsUtilities;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Gui.Controls
{
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
                return DataGrid.Instance as ProjectPropertiesViewModel;
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

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, null);
        }

        private void UpdateToInstance()
        {
            // Move all colors into their own category:
            var allMembers = DataGrid.Categories.SelectMany(item => item.Members).ToArray();

            var colorCategory = new MemberCategory("Colors");

            foreach(var member in allMembers)
            {
                if(IsColor(member))
                {
                    DataGrid.MoveMemberToCategory(member.Name, colorCategory.Name);
                }
            }

            DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureFile), "Single Pixel Texture");

            DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureLeft), "Single Pixel Texture");
            DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureTop), "Single Pixel Texture");
            DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureRight), "Single Pixel Texture");
            DataGrid.MoveMemberToCategory(nameof(ViewModel.SinglePixelTextureBottom), "Single Pixel Texture");


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
}
