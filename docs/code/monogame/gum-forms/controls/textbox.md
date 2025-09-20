# TextBox

The TextBox control allows users to enter a string. It supports highlighting, copy/paste, selection with mouse and keyboard, and CTRL key for performing operations on entire words.

## Code Example: Creating a TextBox

The following code creates two TextBoxes.

```csharp
var panel = new StackPanel();
panel.Spacing = 5;
panel.X = 50;
panel.Y = 50;
panel.AddToRoot();

var textBox = new TextBox();
textBox.Width = 200;
textBox.Placeholder = "Placeholder Text...";
panel.AddChild(textBox);

var textBox2 = new TextBox();
textBox2.Width = 200;
textBox2.Placeholder = "Placeholder Text...";
panel.AddChild(textBox2);
```

<figure><img src="../../../../.gitbook/assets/13_09 57 01.gif" alt=""><figcaption><p>Interacting with TextBoxes</p></figcaption></figure>

## Typing

TextBox supports reading characters from the keyboard. It supports:

* Regular character typing - inserts a character the the caret position (`CaretIndex`)
* Backspace - removes a character at the index before the caret
* Delete - removes a character at the caret index
* Enter (if multi-line) - inserts the newline character (`'\n'`)&#x20;
* Keyboard repeat rate - if a key is held, the repeat rate applies according to the OS settings
* CTRL X, C, and V for cut, copy, and paste

The TextBox respects the OS-level repat rate. For example, the following animation shows the TextBox responding to the Windows Key repeat rate.

<figure><img src="../../../../.gitbook/assets/03_08 07 14.gif" alt=""><figcaption><p>Key repeat rate adjusted in Windows</p></figcaption></figure>

## PreviewTextInput

The `PreviewTextInput` event is raised whenever text is added to a text box. This includes regular typing and also pasting. This method can be used to react to text before it has been added to the TextBox.

The event includes arguments with a `Handled` property. Setting this to true prevents the Text from being added to the `TextBox`. The argument's `Text` property contains the newly-added text. Keep in mind this can be a longer string if the user has pasted text, so you may need to check all letters rather than only the first.

For example, the following code shows how to only allow numbers in a TextBox:

```csharp
var label = new Label();
panel.AddChild(label);

var textBox = new TextBox();
textBox.PreviewTextInput += (sender, args) =>
{
    label.Text = "Handling text " + args.Text;
    if (args.Text.Any(item => !char.IsDigit(item)))
    {
        args.Handled = true;
    }
};
panel.AddChild(textBox);
```

<figure><img src="../../../../.gitbook/assets/13_09 58 12.gif" alt=""><figcaption><p>TextBox only allowing numbers</p></figcaption></figure>

## IsReadOnly

If `IsReadOnly` is set to true, then the user cannot modify a `TextBox`'s `Text`. Setting `IsReadOnly` to true results in the following TextBox behavior:

* Text cannot be changed by typing, pasting, cutting, or deleting text with the keyboard
* Text can be selected with the mouse or with key combinations (shift + arrow key)
* Text can be copied
* The TextBox can receive focus
* The Caret is optionally visible depending on whether `IsCaretVisibleWhenReadOnly` is set to true. By default `IsCaretVisibleWhenReadOnly` is false.

The following code shows how to create a read-only TextBox:

```csharp
var textBox = new TextBox();
textBox.Width = 200;
textBox.IsReadOnly = true;
textBox.Text = "This is read-only text";
panel.AddChild(textBox);
```

<figure><img src="../../../../.gitbook/assets/13_09 59 12.gif" alt=""><figcaption><p>TextBox with IsReadOnly set to true responding to mouse click+drag and double-click</p></figcaption></figure>

## Selection

Selection can be performed programmatically or by the user using the cursor.

The `SelectionLength` property can be used to determine if any text is selected. The following code shows how to output the selected characters:

```csharp
if(textBox.SelectionLength > 0)
{
    var selectedText = textBox.Text.Substring(
        textBox.SelectionStart, 
        textBox.SelectionLength);
    System.Diagnostics.Debug.WriteLine("Selected text: " + selectedText");
}
```

The `SelectionStart` and `SelectionLength` properties can be modified to change the visual selection. For example, the following selects the first 5 letters:

```csharp
textBox.SelectionStart = 0;
textBox.SelectionLength = 5;
```

The entire text can be selected as shown in the following code:

```csharp
textBox.SelectionStart = 0;
textBox.SelectionLength = textBox.Text?.Length ?? 0; // in case text is null
```

Selection can also be performed by the user. Double-clicking the text box selects all text. Also, pressing CTRL+A selects all text.

<figure><img src="../../../../.gitbook/assets/13_10 01 00.gif" alt=""><figcaption><p>Double-click selects all text</p></figcaption></figure>

A push+drag with the mouse selects the text between the start and the current location of the drag.

<figure><img src="../../../../.gitbook/assets/13_10 01 46.gif" alt=""><figcaption><p>Push+drag to select text</p></figcaption></figure>

Selection is performed based on the X position of the cursor, even if the cursor is outside of the bounds of the TextBox.

<figure><img src="../../../../.gitbook/assets/17_21 57 11.gif" alt=""><figcaption><p>Selection by dragging outside of the TextBox bounds</p></figcaption></figure>

Holding down the shift key and pressing the arrow keys adjusts the selection. CTRL+Shift+arrow selects the next or previous word.

<figure><img src="../../../../.gitbook/assets/14_05 58 31.gif" alt=""><figcaption><p>Arrow keys + shift to select</p></figcaption></figure>

## CaretIndex

`CaretIndex` returns the index of the caret where 0 is before the first letter. This value is updated automatically when letters are typed, the caret is moved with arrow/home/end, and when the cursor is clicked and the cursor is moved.

The user can modify the `CaretIndex` using the following actions:

* Clicking in the text box places the caret at the nearest index to the click point
* Typing text moves the caret to the right by one character
* Pasting text moves the caret to the end of the pasted text
* Left arrow and right arrow on the keyboard moves the caret to the left or right by one index
* CTRL + left arrow and CTRL + right arrow move the caret to the left or right by one word
* Home key moves the caret to the beginning of the line
* End key moves the caret to the end of the line

`CaretIndex` can be explicitly set in code to move the caret position.

When `CaretIndex` changes the `CaretIndexChanged` event is raised.

The following code shows how to display the CaretIndex in a label:

```csharp
var panel = new StackPanel();
panel.AddToRoot();

var label = new Label();
panel.AddChild(label);

var textBox = new TextBox();
textBox.TextWrapping = TextWrapping.Wrap;
textBox.Height = 140;
panel.AddChild(textBox);

textBox.CaretIndexChanged += (_, _) =>
{
    UpdateLabelToTextBox(label, textBox);
};

void UpdateLabelToTextBox(Label label, TextBox textBox)
{
    label.Text = "Text box text: " + textBox.Text + 
        " with caret index " + textBox.CaretIndex;
}
```

<figure><img src="../../../../.gitbook/assets/14_06 03 52.gif" alt=""><figcaption><p>CaretIndexChanged is invoked whenever the caret index changes, which updates the Label's text.</p></figcaption></figure>

## TextWrapping

The `TextWrapping` property can be used to set whether the TextBox wraps text. By default this value is set to `TextWrapping.NoWrap` which means the text does not wrap, but instead extends horizontally.

```csharp
var textBox = new TextBox();
textBox.AddToRoot();
```

<figure><img src="../../../../.gitbook/assets/14_06 09 11.gif" alt=""><figcaption><p><code>TextWrapping.NoWrap</code> causes text to scroll</p></figcaption></figure>

If `TextWrapping` is set to `TextWrapping.Wrap`, then text wraps to multiple lines. Note that usually this is combined with a taller text box so that multiple lines display properly.

```csharp
 var wrappedTextBox = new TextBox();
 wrappedTextBox.AddToRoot();
 wrappedTextBox.TextWrapping = TextWrapping.Wrap;
 // If you have set up your TextBox in code, you may need to make it taller:
 wrappedTextBox.Height = 140;
```

<figure><img src="../../../../.gitbook/assets/14_06 11 31.gif" alt=""><figcaption><p><code>TextWrapping.Wrap</code> causes text to wrap</p></figcaption></figure>

`AcceptsReturn` can be set to true to add newlines when the return (enter) key is pressed.

```csharp
var wrappedTextBox = new TextBox();
wrappedTextBox.AddToRoot();
wrappedTextBox.TextWrapping = TextWrapping.Wrap;
// If you have set up your TextBox in code, you may need to make it taller:
wrappedTextBox.Height = 140;
wrappedTextBox.AcceptsReturn = true;
```

<figure><img src="../../../../.gitbook/assets/14_06 12 55.gif" alt=""><figcaption><p>Manually adding newlines by pressing the return (enter) key</p></figcaption></figure>

## Enter Key Behavior

The enter key behavior can be customized for TextBoxes. By default the enter key only applies the Text property to a bound view model. The enter key can also insert multiple lines, but this is usually accompanied with a larger TextBox.

### Enter and Binding

The  `TextBox.Text` property can be bound to a ViewModel's property. By default this property is updated immediately when text changes but the `UpdateSourceTrigger` can be set to `UpdateSourceTrigger.LostFocus`.

In this situation, the enter key also applies binding even if the TextBox has not lost focus.

```csharp
var label = new Label();
label.SetBinding(nameof(label.Text), nameof(TextViewModel.Text));
mainPanel.AddChild(label);

var textBox = new TextBox();
textBox.Width = 500;
var binding = new Binding(nameof(TextViewModel.Text))
{
    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
};

textBox.SetBinding(nameof(textBox.Text), binding);
mainPanel.AddChild(textBox);

```

<figure><img src="../../../../.gitbook/assets/24_07 50 58.gif" alt=""><figcaption><p>Pressing enter applies binding</p></figcaption></figure>

## Tab Key Behavior

The tab key behavior is controlled by the `AccetsTab` property. This value is false by default.

If this value is false, pressing the Tab key does not insert a tab `'\t'` character.&#x20;

If this value is true, pressing the Tab key inserts a tab `'\t'` character.  Note that if this value is true, then tabbing to the next control by pressing tab is disabled. In other words, the TextBox keeps keeps focus after the tab key is pressed.

## Extended Character Sets and Keyboards

The TextBox supports entering characters respecting the current keyboard language settings. This includes typing characters with accents, pasting text, and entering alt codes ([https://www.alt-codes.net/](https://www.alt-codes.net/) ).

<figure><img src="../../../../.gitbook/assets/image (167).png" alt=""><figcaption><p>TextBox displaying the é character</p></figcaption></figure>

Characters must be available in the current font to support being written in TextBoxes. If you would like to support more characters, you can explicitly create a font (.fnt) including the desired characters, or change the default character set in Gum.

For more information on creating fonts, see the [Font](../../../../gum-tool/gum-elements/text/font.md) and [Use Custom Font](../../../../gum-tool/gum-elements/text/use-custom-font.md) pages. For more information on specifying the default character set in Gum, see the [Project Property Font Ranges](../../../../gum-tool/menu/project-properties.md#font-ranges) page.



