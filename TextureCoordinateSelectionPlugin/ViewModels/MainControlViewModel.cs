using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextureCoordinateSelectionPlugin.ViewModels
{
    public class MainControlViewModel : ViewModel
    {
        public bool IsSnapToGridChecked 
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedSnapToGridValue
        {
            get => Get<int>();
            set => Set(value);
        }

        public List<int> AvailableSnapToGridValues
        {
            get; private set;
        } = new List<int>
        {
            4, 8, 12, 16, 24, 32, 48, 64
        };

        [DependsOn(nameof(IsSnapToGridChecked))]
        public bool IsSnapToGridComboBoxEnabled => IsSnapToGridChecked;

        public MainControlViewModel()
        {
            SelectedSnapToGridValue = 16;
        }
    }
}
