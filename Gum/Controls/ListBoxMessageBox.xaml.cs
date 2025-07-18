using Gum;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Gum.Commands;
using Gum.Services;

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
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged("CanProceed");

                ItemSelected?.Invoke(this, null);
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

        bool IsDialog = true;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler ItemSelected;

        #endregion

        public ListBoxMessageBox()
        {
            InitializeComponent();

            Items = new ObservableCollection<object>();

            this.ListBox.DataContext = this;
            this.MessageLabel.DataContext = this;
            this.OkButton.DataContext = this;

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            GuiCommands guiCommands = Locator.GetRequiredService<GuiCommands>();
            guiCommands.MoveToCursor(this);

            ListBox.Focus();
        }

        public void HideCancelNoDialog()
        {
            IsDialog = false;
            CancelButton.Visibility = Visibility.Hidden;
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
            if(IsDialog)
            {
                this.DialogResult = true;
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(SelectedItem != null)
            {
                OkButton_Click(null, null);
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                OkButton_Click(this, null);
                e.Handled = true;
            }
            else if(e.Key == Key.Escape)
            {
                CancelButton_Click(this, null);
            }
        }
    }
}
