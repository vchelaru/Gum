using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for ListBoxMessageBox.xaml
    /// </summary>
    public partial class ListBoxMessageBox : INotifyPropertyChanged
    {
        #region Fields

        string mMessage;
        object mSelectedItem;

        bool mRequiresSelection;

        #endregion

        #region Properties

        public string Message
        {
            get { return mMessage;}
            set
            {
                mMessage = value;
                OnPropertyChanged("Message");
            }
        }

        public ObservableCollection<object> Items
        {
            get;
            private set;
        }
                      
        public object SelectedItem
        {
            get { return mSelectedItem; }
            set 
            { 
                mSelectedItem = value;
                OnPropertyChanged("SelectedItem");
                OnPropertyChanged("CanProceed");
            }
        }

        public bool RequiresSelection
        {
            get { return mRequiresSelection; }
            set 
            { 
                mRequiresSelection = value;
                OnPropertyChanged("CanProceed");
            }
        }

        public bool CanProceed
        {
            get
            {
                return RequiresSelection == false || SelectedItem != null;
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public ListBoxMessageBox()
        {
            InitializeComponent();

            Items = new ObservableCollection<object>();

            this.ListBox.DataContext = this;
            this.MessageLabel.DataContext = this;
            this.OkButton.DataContext = this;
        }

        void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
