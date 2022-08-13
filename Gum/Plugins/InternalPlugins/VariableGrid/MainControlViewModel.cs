using Gum.DataTypes.Variables;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Gum.Plugins.VariableGrid
{
    public class MainControlViewModel : ViewModel
    {
        public Visibility HasStateInformation
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public Visibility HasErrors
        {
            get => Get<Visibility>();
            set => Set(value); 
        }

        public string StateInformation
        {
            get => Get<string>();
            set => Set(value);
        }

        public Brush StateBackground
        {
            get => Get<Brush>();
            set => Set(value);
        }

        public string ErrorInformation
        {
            get => Get<string>();
            set => Set(value); 
        }

        public Visibility ShowBehaviorUi
        {
            get { return Get<Visibility>(); }
            set { Set(value); }
        }

        public ObservableCollection<VariableSave> BehaviorVariables
        {
            get;
            private set;
        } = new ObservableCollection<VariableSave>();

        public Visibility ShowVariableGrid
        {
            //get => Visibility.Hidden;
            get => Get<Visibility>();
            set => Set(value);
        }

        public VariableSave SelectedBehaviorVariable
        {
            get { return Get<VariableSave>(); }
            set { Set(value); }
        }

        public VariableSave EffectiveSelectedBehaviorVariable
        {
            get
            {
                if(ShowBehaviorUi == Visibility.Visible)
                {
                    return SelectedBehaviorVariable;
                } 
                else
                {
                    return null;
                }
            }
        }
    }
}
