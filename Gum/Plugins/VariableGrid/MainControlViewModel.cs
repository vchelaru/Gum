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
        Visibility hasErrors;
        public Visibility HasErrors
        {
            get { return hasErrors; }
            set
            {
                base.SetProperty(ref hasErrors, value);
            }
        }

        string errorInformation;
        public string ErrorInformation
        {
            get { return errorInformation; }
            set
            {
                base.SetProperty(ref errorInformation, value);
            }
        }

        Visibility showBehaviorUi = Visibility.Collapsed;
        public Visibility ShowBehaviorUi
        {
            get { return showBehaviorUi; }
            set { base.SetProperty(ref showBehaviorUi, value); }
        }

        public ObservableCollection<VariableSave> BehaviorVariables
        {
            get;
            private set;
        } = new ObservableCollection<VariableSave>();

        Visibility showVariableGrid = Visibility.Collapsed;
        public Visibility ShowVariableGrid
        {
            get { return showVariableGrid; }
            set { base.SetProperty(ref showVariableGrid, value); }
        }

        VariableSave selectedBehaviorVariable;
        public VariableSave SelectedBehaviorVariable
        {
            get { return selectedBehaviorVariable; }
            set { base.SetProperty(ref selectedBehaviorVariable, value); }
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
