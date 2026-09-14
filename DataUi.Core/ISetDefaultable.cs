namespace WpfDataUi
{
    /// <summary>
    /// A displayer that must clear its in-progress edit state when its member is reset to default,
    /// so a later focus loss does not write the old text back.
    /// </summary>
    public interface ISetDefaultable
    {
        /// <summary>Called after "Make Default" has reset the member.</summary>
        void SetToDefault();
    }
}
