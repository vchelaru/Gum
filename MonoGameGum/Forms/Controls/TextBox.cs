using Gum.Wireframe;
using System;
using System.Reflection;


#if FRB
using FlatRedBall.Forms.Clipboard;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
using Gum.Clipboard;
namespace Gum.Forms.Controls;

#endif

public class TextBox : TextBoxBase
{
    #region Fields/Properties

    protected override string DisplayedText => Text;

    public string Text
    {
        get => coreTextObject.RawText;
        set
        {
            if (value != Text)
            {
                if (value?.Length > MaxLength)
                {
                    value = value.Substring(0, MaxLength.Value);
                }

                // go through the component instead of the core text object to force a layout refresh if necessary
                // Calling SetProperty.
                // This bypasses the Text change event so we need to explicitly handle text changing.
                textComponent.SetProperty("Text", value);

                OnTextChanged(value);
            }
        }
    }

    /// <summary>
    /// The maximum letters to display. This can be used to 
    /// create an effect where the text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => coreTextObject.MaxLettersToShow;
        set
        {
            if (value != MaxLettersToShow)
            {
                coreTextObject.MaxLettersToShow = value;
            }
        }
    }

    /// <summary>
    /// The maximum number of lines to display. This can be used to 
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => coreTextObject.MaxNumberOfLines;
        set
        {
            if (value != MaxNumberOfLines)
            {
                coreTextObject.MaxNumberOfLines = value;
            }
        }
    }

    protected override string CategoryName => "TextBoxCategoryState";



    #endregion

    #region Events

    public event EventHandler TextChanged;

    #endregion

    #region Initialize Methods

    public TextBox() : base() { }

    public TextBox(InteractiveGue visual) : base(visual) { }


    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        if (selectionInstance != null)
        {
            selectionInstance.Visible = false;
        }
    }

    #endregion

    #region Event Handler Methods

    /// <summary>
    /// Will update the Text based on what was pressed, if existing text was already selected, or enter pressed.  Also updates the caret position.  
    /// Does not handle control keys like(LEFT, RIGHT, UP, DOWN, HOME, END, BACKSPACE, DELETE, CTRL+C, CTRL+X, CTRL+V, CTRL+A)
    /// control key situations are handled by TextBoxBase.HandleKeyDown
    /// Key information is gathered from Keyboard.cs in the Activity method, then passed during TextBoxBase.DoKeyboardAction
    /// </summary>
    /// <param name="character">The ascii code character that we need to perform an action on</param>
    public override void HandleCharEntered(char character)
    {
        // not sure why we require this to be focused. It blocks things like the
        // OnScreenKeyboard from adding characters
        //if(IsFocused)
        if(!IsReadOnly)
        {
            if (selectionLength != 0)
            {
                DeleteSelection();
            }

            // If text is null force it to be an empty string so we can add characters
            string textAfterAdd = Text ?? "";
            string? newlyAddedText = null;
            var addedCharacter = false;

            // We handle these actions in TextBoxBase.HandleKeyDown based on Keyboard.GetState()
            if (character == '\b'           // BACKSPACE key
                || character == (char)127   // DEL key
                || character == (char)27)   // ESC key (Not handled anywhere, unsure why we are ignoring it?)
            {
                //    HandleBackspace();    // handled by TextBoxBase
            }
            else if (character == '\r'      // \r is triggerd by CTRL+ENTER on windows
                || character == '\n')
            {
                if (AcceptsReturn)
                {
                    newlyAddedText = "\n";
                    textAfterAdd = textAfterAdd.Insert(caretIndex, newlyAddedText);
                    addedCharacter = true;
                }
                else
                {
                    var textBinding = PropertyRegistry.GetBindingExpression(nameof(Text));
                    textBinding?.UpdateSource();
                }
            }
            else
            {
                newlyAddedText = "" + character;
                textAfterAdd = textAfterAdd.Insert(caretIndex, newlyAddedText);
                // could be limited by MaxLength:
                addedCharacter = true;
            }

            // Vic asks - should RaisePreviewTextInput be called before truncating max length? 
            textAfterAdd = TruncateTextToMaxLength(textAfterAdd);
            var caretIndexBefore = caretIndex;

            var wasHandledByEvent = false;
            if (addedCharacter)
            {
                var args = RaisePreviewTextInput(newlyAddedText);
                wasHandledByEvent = args.Handled;
                if (!wasHandledByEvent)
                {
                    // set caretIndex before assigning Text so that the events are
                    // raised with the new caretIndex value
                    caretIndex = System.Math.Min(caretIndex + 1, textAfterAdd.Length);
                    Text = textAfterAdd;
                }
            }

            if (!wasHandledByEvent)
            {
                UpdateCaretPositionFromCaretIndex();
                OffsetTextToKeepCaretInView();
            }

            if(caretIndex != caretIndexBefore)
            {
                RaiseCaretIndexChanged();
            }
        }
    }

    public override void HandleBackspace(bool isCtrlDown = false)
    {
        //if (IsFocused && caretIndex > 0 && Text != null)
        if ((caretIndex > 0 || selectionLength > 0) && Text != null)
        {
            if (selectionLength > 0)
            {
                DeleteSelection();
            }
            else if (isCtrlDown)
            {
                var indexBeforeNullable = GetCtrlBeforeTarget(caretIndex);

                var indexToDeleteTo = indexBeforeNullable ?? 0;

                this.Text = Text.Remove(indexToDeleteTo, caretIndex - indexToDeleteTo);

                caretIndex = indexToDeleteTo;
            }
            else
            {
                var whereToRemoveFrom = caretIndex - 1;
                // Move the care to the left one before removing from the text. Otherwise, if the
                // caret is at the end of the word, modifying the word will shift the caret to the left, 
                // and that could cause it to shift over two times.
                caretIndex--;
                this.Text = this.Text.Remove(whereToRemoveFrom, 1);
            }
        }
    }

    protected override void HandleDelete()
    {
        if (selectionLength > 0)
        {
            DeleteSelection();
        }
        else if (caretIndex < (Text?.Length ?? 0))
        {
            this.Text = this.Text.Remove(caretIndex, 1);
        }
    }

    protected override void HandleCopy()
    {
        if (selectionLength != 0)
        {
            var whatToCopy = DisplayedText.Substring(
                selectionStart, selectionLength);
            ClipboardImplementation.PushStringToClipboard(
                whatToCopy);
        }
    }

    protected override void HandleCut()
    {
        if (selectionLength != 0)
        {
            var whatToCopy = DisplayedText.Substring(
                selectionStart, selectionLength);
            ClipboardImplementation.PushStringToClipboard(
                whatToCopy);

            DeleteSelection();
        }
    }

    protected override void HandlePaste()
    {
        var whatToPaste = Clipboard.ClipboardImplementation.GetText();

        if (!string.IsNullOrEmpty(whatToPaste))
        {
            var args = RaisePreviewTextInput(whatToPaste);

            if(args.Handled == false)
            {
                if (selectionLength != 0)
                {
                    DeleteSelection();
                }
                foreach (var character in whatToPaste)
                {
                    this.Text = this.Text.Insert(caretIndex, "" + character);
                    caretIndex++;
                }

                TruncateTextToMaxLength();
                UpdateCaretPositionFromCaretIndex();
                OffsetTextToKeepCaretInView();
            }
        }
    }

    public void DeleteSelection()
    {
        var lengthToRemove = selectionLength;
        if (selectionStart + lengthToRemove > Text.Length)
        {
            lengthToRemove = Text.Length - selectionStart;
        }
        this.Text = Text.Remove(selectionStart, lengthToRemove);
        CaretIndex = selectionStart;
        SelectionLength = 0;
    }

    #endregion

    public override void SelectAll()
    {
        if (this.Text != null)
        {
            this.SelectionStart = 0;
            this.SelectionLength = this.Text.Length;
        }
    }

    #region Truncation

    protected override void TruncateTextToMaxLength()
    {
        Text = TruncateTextToMaxLength(Text);
    }

    private string TruncateTextToMaxLength(string text)
    {
        if (text?.Length > MaxLength)
        {
            text = text.Substring(0, MaxLength.Value);
        }

        return text;
    }

    #endregion

    protected override void OnTextChanged(string value)
    {

        CaretIndex = System.Math.Min(CaretIndex, value?.Length ?? 0);

        TextChanged?.Invoke(this, EventArgs.Empty);

        UpdatePlaceholderVisibility();

        PushValueToViewModel(nameof(Text));
    }
}
