// These placeholder classes mirror the public properties of the real Forms controls
// in MonoGameGum. Gum.ProjectServices cannot reference MonoGameGum directly (it
// depends on MonoGame), so code generation uses reflection against these types to
// determine whether an exposed variable already exists on the base Forms class.
//
// When a match is found the generated code skips emitting the property so it does
// not hide the inherited member.
//
// If a Forms control gains a new public property that users commonly expose,
// add it here so code generation knows about it.
//
// Eventually the Forms controls may move to GumCommon (or a shared abstraction
// layer), which would let us reflect against the real types and remove these
// placeholders. That is a large architectural change and not expected soon.

namespace Gum.Forms.Controls
{

    public class Button
    {
        public virtual string Text { get; set; } = string.Empty;
    }

    public class CheckBox
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ComboBox
    {
        public string Text { get; set; } = string.Empty;
    }

    public class Label
    {
        public string Text { get; set; } = string.Empty;
    }

    public class MenuItem
    {
        public virtual string Header { get; set; } = string.Empty;
    }

    public class PasswordBox
    {
        public string Password { get; set; } = string.Empty;
        public char PasswordChar { get; set; }
    }

    public class RadioButton
    {
        public string Text { get; set; } = string.Empty;
    }

    public class TextBox
    {
        public virtual string Text { get; set; } = string.Empty;
        public virtual string Placeholder { get; set; } = string.Empty;
        public virtual int? MaxLettersToShow { get; set; }
        public virtual int? MaxNumberOfLines { get; set; }
    }

}
