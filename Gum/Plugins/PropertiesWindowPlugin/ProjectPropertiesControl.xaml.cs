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

namespace Gum.Gui.Controls
{
    /// <summary>
    /// Interaction logic for ProjectPropertiesControl.xaml
    /// </summary>
    public partial class ProjectPropertiesControl : UserControl
    {
        public event EventHandler PropertyChanged;
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
                    if (DataGrid.Instance != null)
                    {
                        ((ProjectPropertiesViewModel)DataGrid.Instance).PropertyChanged -= HandleViewModelPropertyChanged;
                    }

                    DataGrid.Instance = value;

                    if(value != null)
                    {
                        value.PropertyChanged += HandleViewModelPropertyChanged;
                    }

                    UpdateToInstance();
                }
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, null);
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
