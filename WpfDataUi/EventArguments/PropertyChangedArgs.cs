using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfDataUi.EventArguments
{
    public class PropertyChangedArgs : EventArgs
    {
        public object Owner { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }
}
