using System;

namespace WpfDataUi.EventArguments;

public class PropertyChangedArgs : EventArgs
{
    public object Owner { get; set; }
    public string PropertyName { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
}
