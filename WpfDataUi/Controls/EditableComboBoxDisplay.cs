using System;
using System.Collections.Generic;
using System.Text;
using WpfDataUi.Controls;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// This is added for convenience for systems which cannot set values on their displayers
    /// </summary>
    public class EditableComboBoxDisplay : ComboBoxDisplay
    {
        public EditableComboBoxDisplay()
        {
            IsEditable = true;
        }
    }
}
