namespace WpfDataUi.EventArguments;

public class BeforePropertyChangedArgs : PropertyChangedArgs
{
    object? mOverridingValue;

    public object? OverridingValue
    {
        get { return mOverridingValue; }
        set
        {
            WasManuallySet = true;
            mOverridingValue = value;
        }
    }

    public bool WasManuallySet
    {
        get;
        private set;
    }
}
