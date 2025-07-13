using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Services.Dialogs;

public partial class GetUserStringDialogView : UserControl
{
    public GetUserStringDialogView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ValueTextBox.Focus();
            if (DataContext is GetUserStringDialogBaseViewModel vm)
            {               
                if (!string.IsNullOrEmpty(vm.Value))
                {
                    ValueTextBox.CaretIndex = vm.Value!.Length;
                }
                
                if (vm.PreSelect)
                {
                    ValueTextBox.SelectAll();
                }
                
                vm.Validate();
            }
        };
    }
}