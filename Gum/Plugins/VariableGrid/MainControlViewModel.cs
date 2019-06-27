using Gum.DataTypes.Variables;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Gum.Plugins.VariableGrid
{
    public class MainControlViewModel : ViewModel
    {
        public Visibility HasErrors
        {
            get { return Get<Visibility>(); }
            set { Set(value); }
        }

        public string ErrorInformation
        {
            get { return Get<string>(); }
            set { Set(value); }
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
            get { return Get<Visibility>(); }
            set { Set(value); }
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
