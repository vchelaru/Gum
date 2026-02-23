namespace WpfDataUi.Controls
{
    public class MultiLineTextBoxDisplay : TextBoxDisplay
    {
        public MultiLineTextBoxDisplay()
        {
            MakeMultiline();
        }

        protected override void ResetToSingleLine()
        {
            // This control is always multiline; do not reset to single-line on InstanceMember change.
        }
    }
}
