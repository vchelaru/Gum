using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace WpfDataUi.DataTypes
{
    public class MemberCategory : INotifyPropertyChanged
    {
        #region Properties

        public string Name { get; set; }

        public Visibility Visibility
        {
            get
            {
                if (Members.Count == 0)
                {
                    return System.Windows.Visibility.Collapsed;
                }
                else
                {
                    return System.Windows.Visibility.Visible;

                }
            }
        }

        public bool HideHeader
        {
            get;
            set;
        }

        public int FontSize
        {
            get;
            set;
        }

        public ObservableCollection<InstanceMember> Members
        {
            get;
            private set;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public MemberCategory() 
        {
            InstantiateAll();
        }

        public MemberCategory(string name) 
        {
            InstantiateAll();
            Name = name; 
        }

        void InstantiateAll()
        {
            HideHeader = false;

            FontSize = 12;

            Members = new ObservableCollection<InstanceMember>();

            Members.CollectionChanged += HandleMembersChanged;
        }

        void HandleMembersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Visibility");
        }

        void NotifyPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetAlternatingColors(Brush evenBrush, Brush oddBrush)
        {
            for (int i = 0; i < this.Members.Count; i++)
            {
                if (i % 2 == 0)
                {
                    Members[i].BackgroundColor = evenBrush;
                }
                else
                {
                    Members[i].BackgroundColor = oddBrush;
                }

            }
        }

        public override string ToString()
        {
            return Name + " (" + Members.Count + ")";
        }

        #endregion
    }

}
