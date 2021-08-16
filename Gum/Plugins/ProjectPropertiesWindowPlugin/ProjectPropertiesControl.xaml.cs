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

        private void UpdateToInstance()
        {
            foreach (var category in DataGrid.Categories)
            {
                category.HideHeader = true;
                foreach (var member in category.Members)
                {
                    member.DisplayName =
                            ToolsUtilities.StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);

                    switch(member.PropertyType.Name)
                    {
                        case "Microsoft.Xna.Framework.Color":
                        case "Color":
                            member.PreferredDisplayer = typeof(Gum.Controls.DataUi.ColorDisplay);
                            break;
                    }

                    if(member.Name == nameof(ViewModel.LocalizationFile))
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
        }

        public ProjectPropertiesControl()
        {
            InitializeComponent();
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, null);
        }

        
    }
}
