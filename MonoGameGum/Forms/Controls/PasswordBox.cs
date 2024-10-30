using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    public class PasswordBox : TextBoxBase
    {
        #region Fields/Properties

#if !UWP
        SecureString securePassword = new SecureString();
        public SecureString SecurePassword
        {
            get { return securePassword; }
        }
        public string Password
        {
            get
            {
                return SecureStringToString(SecurePassword);

            }
            set
            {
                SecurePassword.Clear();
                if (value != null)
                {
                    foreach (var character in value)
                    {
                        SecurePassword.AppendChar(character);
                    }
                }
                CallMethodsInResponseToPasswordChanged();
            }
        }

        String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(value);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
#else
        string password;
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                if(password != value)
                {
                    password = value;

                    CallMethodsInResponseToPasswordChanged();
                }
            }
        }
#endif


        // Update Gum's default to include this first:
        //public char PasswordChar { get; set; } = '●';
        public char PasswordChar { get; set; } = '*';

        public event EventHandler PasswordChanged;

        protected override string DisplayedText
        {
            get
            {
#if UWP
                return new string(PasswordChar, Password?.Length ?? 0);
#else
                return new string(PasswordChar, SecurePassword.Length);
#endif
            }
        }

        protected override string CategoryName => "PasswordBoxCategoryState";


        #endregion

        #region Initialize Methods

        public PasswordBox() : base() { }

        public PasswordBox(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            if (selectionInstance != null)
            {
                selectionInstance.Visible = false;
            }

            UpdateDisplayedCharacters();
        }
        #endregion

        #region Event Handler Methods

        public override void HandleCharEntered(char character)
        {
            // See TextBox on why we don't check IsFocused
            //if (HasFocus)
            {
                if (selectionLength != 0)
                {
                    DeleteSelection();
                }
                // If text is null force it to be an empty string so we can add characters

                if (character == '\b'
                    // I think CTRL Backspace?
                    || character == (char)127
                    // esc
                    || character == (char)27)
                {
                    // do nothing, handled with a backspace above
                    //    HandleBackspace();
                }
                else if (character == '\r' || character == '\n')
                {
                    // no enter supported on passwords, do we send an event?
                }
                else
                {
                    InsertCharacterAtIndex(character, caretIndex);
                    caretIndex++;

                    CallMethodsInResponseToPasswordChanged();
                }

            }
        }

        private void CallMethodsInResponseToPasswordChanged()
        {
            TruncateTextToMaxLength();
            UpdateCaretPositionToCaretIndex();
            OffsetTextToKeepCaretInView();
            UpdateDisplayedCharacters();
            UpdatePlaceholderVisibility();
            PasswordChanged?.Invoke(this, null);
            PushValueToViewModel();
        }

        private void InsertCharacterAtIndex(char character, int caretIndex)
        {
#if UWP
            if(password == null)
            {
                password = "";
            }
            password = this.password.Insert(caretIndex, character.ToString());
#else
            this.SecurePassword.InsertAt(caretIndex, character);

#endif
        }

        public override void HandleBackspace(bool isCtrlDown = false)
        {
            if (caretIndex > 0 || SelectionLength > 0)
            {
                if (selectionLength > 0)
                {
                    DeleteSelection();
                }
                else if (isCtrlDown)
                {
                    for (int i = caretIndex - 1; i > -1; i--)
                    {
#if UWP
                        password = password.Remove(i);
#else
                        SecurePassword.RemoveAt(i);
#endif
                    }

                    caretIndex = 0;
                }
                else
                {
                    var whereToRemoveFrom = caretIndex - 1;
                    // Move the care to the left one before removing from the text. Otherwise, if the
                    // caret is at the end of the word, modifying the word will shift the caret to the left, 
                    // and that could cause it to shift over two times.
                    caretIndex--;
#if UWP
                    password = password.Remove(whereToRemoveFrom);
#else
                    SecurePassword.RemoveAt(whereToRemoveFrom);
#endif
                }
                CallMethodsInResponseToPasswordChanged();
            }
        }

        public void DeleteSelection()
        {
            for (int i = 0; i < SelectionLength; i++)
            {
#if UWP
                password = password.Remove(selectionStart);
#else
                SecurePassword.RemoveAt(selectionStart);
#endif

            }
            CallMethodsInResponseToPasswordChanged();

            CaretIndex = selectionStart;
            SelectionLength = 0;
        }

        protected override void HandleDelete()
        {
#if UWP
            if (caretIndex < (password?.Length ?? 0))
            {
                password = password.Remove(caretIndex);

                CallMethodsInResponseToPasswordChanged();
            }
#else
            if (caretIndex < (SecurePassword?.Length ?? 0))
            {
                SecurePassword.RemoveAt(caretIndex);

                CallMethodsInResponseToPasswordChanged();
            }
#endif
        }

        public void Clear()
        {
#if UWP
            password = null;
#else
            SecurePassword.Clear();
#endif
            CallMethodsInResponseToPasswordChanged();
        }

        protected override void HandlePaste()
        {

            var whatToPaste = Clipboard.ClipboardImplementation.GetText();
            if (!string.IsNullOrEmpty(whatToPaste))
            {
                if (selectionLength != 0)
                {
                    DeleteSelection();
                }
                foreach (var character in whatToPaste)
                {
                    InsertCharacterAtIndex(character, caretIndex);
                    caretIndex++;
                }
                CallMethodsInResponseToPasswordChanged();
            }
        }

        private void UpdateDisplayedCharacters()
        {
#if UWP
            var newText = new string(PasswordChar, password?.Length ?? 0);
#else
            var newText = new string(PasswordChar, SecurePassword.Length);
#endif
            if (this.coreTextObject.RawText != newText)
            {
                textComponent.SetProperty("Text", newText);

                CaretIndex = System.Math.Min(CaretIndex, Password?.Length ?? 0);

            }
        }

        #endregion

        public override void SelectAll()
        {
            if (this.DisplayedText != null)
            {
                this.SelectionStart = 0;
                this.SelectionLength = this.DisplayedText.Length;
            }
        }

        protected override void TruncateTextToMaxLength()
        {
#if UWP
            while(password.Length > MaxLength)
            {
                password = password.Remove(password.Length-1);
            }
#else
            while (SecurePassword.Length > MaxLength)
            {
                SecurePassword.RemoveAt(SecurePassword.Length - 1);
            }
#endif
        }
    }
}
