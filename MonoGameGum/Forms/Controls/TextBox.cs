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

/// <summary>
/// A single-line or multi-line text input control that supports selection, clipboard operations,
/// caret navigation, and optional localization. For a masked variant (e.g. password entry),
/// see <see cref="PasswordBox"/>.
/// </summary>
public class TextBox : TextBoxBase
{
    #region Fields/Properties

    /// <summary>
    /// Returns the text currently shown in the text box. In <see cref="TextBox"/> this is
    /// identical to <see cref="Text"/>. Subclasses such as <see cref="PasswordBox"/> override
    /// this to return a masked representation (e.g. bullet characters) while keeping the
    /// actual value in <see cref="Text"/>.
    /// </summary>
    protected override string? DisplayedText => Text;

    /// <summary>
    /// Applies text returned from the native on-screen keyboard dialog. Uses
    /// <see cref="SetTextNoTranslate"/> so user-entered input is not passed through the
    /// localization service.
    /// </summary>
    protected override void SetTextFromNativeKeyboardInput(string value)
    {
        SetTextNoTranslate(value);
    }

    /// <summary>
    /// Gets and sets the displayed Text. If the text exceeds MaxLength, it will be truncated.
    /// Setting this property applies localization if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization (for example, for user-entered text), use <see cref="SetTextNoTranslate"/>.
    /// Internal typing, paste, and delete operations automatically bypass localization.
    /// </summary>
    public virtual string? Text
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
    /// Sets the text box text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for programmatically setting text that should not be localized, such as user-entered input.
    /// Internal typing, paste, and delete operations use this method automatically.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
        if (value != Text)
        {
            if (value?.Length > MaxLength)
            {
                value = value.Substring(0, MaxLength.Value);
            }

            textComponent.SetProperty("TextNoTranslate", value);

            OnTextChanged(value);
        }
    }

    /// <summary>
    /// The maximum number of characters to display visually. Characters beyond this count
    /// are hidden but remain in the <see cref="Text"/> string. This is a display-only
    /// property useful for typewriter-style effects where text prints out letter-by-letter.
    /// It does <em>not</em> restrict how many characters the user can type.
    /// To limit input length, use <see cref="TextBoxBase.MaxLength"/> instead.
    /// </summary>
    public virtual int? MaxLettersToShow
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
    public virtual int? MaxNumberOfLines
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

    /// <inheritdoc/>
    protected override string CategoryName => "TextBoxCategoryState";

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the Text property changes.
    /// </summary>
    public event EventHandler TextChanged;

    #endregion

    #region Initialize Methods

    /// <summary>
    /// Creates a new TextBox instance using the default visual.
    /// </summary>
    public TextBox() : base() { }

    /// <summary>
    /// Creates a new TextBox instance using the specified visual.
    /// </summary>
    /// <param name="visual"></param>
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
                    // Use SetTextNoTranslate because this is user-typed input
                    SetTextNoTranslate(textAfterAdd);
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

    /// <summary>
    /// Performs logic to handle backspace. This includes deleting individual letters, deleting selection, or deleting the previous word if ctrl is down.
    /// </summary>
    /// <param name="isCtrlDown">Whether the ctrl key is held. If true, the entire word is deleted rather than a single character.</param>
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

                // Use SetTextNoTranslate because this is user-initiated editing
                SetTextNoTranslate(Text.Remove(indexToDeleteTo, caretIndex - indexToDeleteTo));

                caretIndex = indexToDeleteTo;
            }
            else
            {
                var whereToRemoveFrom = caretIndex - 1;
                // Move the care to the left one before removing from the text. Otherwise, if the
                // caret is at the end of the word, modifying the word will shift the caret to the left, 
                // and that could cause it to shift over two times.
                caretIndex--;
                // Use SetTextNoTranslate because this is user-initiated editing
                SetTextNoTranslate(this.Text.Remove(whereToRemoveFrom, 1));
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
            // Use SetTextNoTranslate because this is user-initiated editing
            SetTextNoTranslate(this.Text.Remove(caretIndex, 1));
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
        var whatToPaste = Clipboard.ClipboardImplementation.GetText(HandlePaste);
        //////////////////////Early Out////////////////////
        if (string.IsNullOrEmpty(whatToPaste)) return;
        ///////////////////End Early Out///////////////////

        var args = RaisePreviewTextInput(whatToPaste);

        if(args.Handled == false)
        {
            whatToPaste = whatToPaste.Replace("\r\n", "\n");

            if (selectionLength != 0)
            {
                DeleteSelection();
            }
            foreach (var character in whatToPaste)
            {
                // Use SetTextNoTranslate because this is user-pasted input
                SetTextNoTranslate(this.Text.Insert(caretIndex, "" + character));
                caretIndex++;
            }

            TruncateTextToMaxLength();
            UpdateCaretPositionFromCaretIndex();
            OffsetTextToKeepCaretInView();
        }
    }

    /// <summary>
    /// Deletes the selected text, updates the caret position, and sets the SelectionLength to 0.
    /// </summary>
    public void DeleteSelection()
    {
        var lengthToRemove = selectionLength;
        if (selectionStart + lengthToRemove > Text.Length)
        {
            lengthToRemove = Text.Length - selectionStart;
        }
        // Use SetTextNoTranslate because this is user-initiated editing
        SetTextNoTranslate(Text.Remove(selectionStart, lengthToRemove));
        CaretIndex = selectionStart;
        SelectionLength = 0;
    }

    #endregion

    /// <summary>
    /// Sets the SelectionStart to 0 and the SelectionLength to the length of the text, if any.
    /// </summary>
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

    protected override void OnTextChanged(string? value)
    {

        CaretIndex = System.Math.Min(CaretIndex, value?.Length ?? 0);

        TextChanged?.Invoke(this, EventArgs.Empty);

        UpdatePlaceholderVisibility();

        PushValueToViewModel(nameof(Text));
    }
}
