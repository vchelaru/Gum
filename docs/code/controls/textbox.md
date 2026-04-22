# TextBox

The TextBox control allows users to enter a string. It supports highlighting, copy/paste, selection with mouse and keyboard, and CTRL key for performing operations on entire words.

## Code Example: Creating a TextBox

The following code creates two TextBoxes.

```csharp
// Initialize
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

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACqWOsQrCMBiE90LfIWRqQUIJuFgc1EHcii2okCU0wQZjUmqqRfHdTWotFXVy_O7-_-5uvgcAXJ2W9RFOgKlqPmoVoYQRVIortzI80wqUVHEJpkDxC0gNzQ-JE4IwJqq1UFrSXKi9PRn32tZR1OPuHWeMZXqttWlTiHI1hjdmrpuuKHtS63cO2ghmCuvjyCW91ETSnBdaMl5Zj8AhuxSEEIHD5kUhJAu6949-_HsA_r4A_zkBhzH0vfsDBYYAXJEBAAA)

<figure><img src="../../.gitbook/assets/13_09 57 01.gif" alt=""><figcaption><p>Interacting with TextBoxes</p></figcaption></figure>

## Typing

TextBox supports reading characters from the keyboard. It supports:

* Regular character typing - inserts a character the the caret position (`CaretIndex`)
* Backspace - removes a character at the index before the caret
* Delete - removes a character at the caret index
* Enter (if multi-line) - inserts the newline character (`'\n'`)
* Keyboard repeat rate - if a key is held, the repeat rate applies according to the OS settings
* CTRL X, C, and V for cut, copy, and paste

The TextBox respects the OS-level repat rate. For example, the following animation shows the TextBox responding to the Windows Key repeat rate.

<figure><img src="../../.gitbook/assets/03_08 07 14.gif" alt=""><figcaption><p>Key repeat rate adjusted in Windows</p></figcaption></figure>

## PreviewTextInput

The `PreviewTextInput` event is raised whenever text is added to a text box. This includes regular typing and also pasting. This method can be used to react to text before it has been added to the TextBox.

The event includes arguments with a `Handled` property. Setting this to true prevents the Text from being added to the `TextBox`. The argument's `Text` property contains the newly-added text. Keep in mind this can be a longer string if the user has pasted text, so you may need to check all letters rather than only the first.

For example, the following code shows how to only allow numbers in a TextBox:

```csharp
// Initialize
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

[Try on XnaFiddle.NEt](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA2WRy07DMBBFf8V4FUuRFRWxSZRFCwgidVGRLkCEhYlNapFOgu30QdV_x3bcCIRXnjtzz_XjhAv9MGxxatQgYjxoCY3G6Ssuj9qILV1K-MJvMZYgjWSt_BY4xTumUM9AtChHIPaoNKz-XDkhIlkFvkXLntUWZkduJu3ZVclUvvwt55yvu6euM57iQlr2PoUs3f4X307fbmTLIz90cRhxMIvuEDzrsfKu0KErJXZS7F2rgH4wqBqSZLbIUaQFcKFixFSjCcqdfn1fwakCZJePoc5m4d4ze2TAW3dHxw5awHmIn85Gu_xA0aTRORwjad_3koKu6g1TtNB3spHGtwghozPku-UBPlVwewr3ZwF_ruD8_2nCnUmGzz_-FlCJ6QEAAA)

<figure><img src="../../.gitbook/assets/13_09 58 12.gif" alt=""><figcaption><p>TextBox only allowing numbers</p></figcaption></figure>

## IsReadOnly

If `IsReadOnly` is set to true, then the user cannot modify a `TextBox`'s `Text`. Setting `IsReadOnly` to true results in the following TextBox behavior:

* Text cannot be changed by typing, pasting, cutting, or deleting text with the keyboard
* Text can be selected with the mouse or with key combinations (shift + arrow key)
* Text can be copied
* The TextBox can receive focus
* The Caret is optionally visible depending on whether `IsCaretVisibleWhenReadOnly` is set to true. By default `IsCaretVisibleWhenReadOnly` is false.

The following code shows how to create a read-only TextBox:

```csharp
// Initialize
var textBox = new TextBox();
textBox.Width = 200;
textBox.IsReadOnly = true;
textBox.Text = "This is read-only text";
panel.AddChild(textBox);
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAClWOMQvCMBSEd6H_4ZGpBS1FcLE4qIN0UmpBhSzBBBqMibSpVsX_bl6sUuEtd9-9457BAIBk9ao5kynYqhFD70gtrWRKPoSzyZVVcGFaKJiBFjfYWnY8bdAIo5Rqj-K9g5PkJw__cs55YXJjrP-gGiutaO3CtF1p8VGedyTeSW5Lx8cJNn3drM4F42ut7g7h5h7DFudSUpSyBneVi44MZjFCSX_RspSKh91rlJJg8HoD3z5OohEBAAA)

<figure><img src="../../.gitbook/assets/13_09 59 12.gif" alt=""><figcaption><p>TextBox with IsReadOnly set to true responding to mouse click+drag and double-click</p></figcaption></figure>

## MaxLength

The `MaxLength` property can be used to restrict the number of characters that can be entered into a `TextBox`. This limit applies to both user typing and pasting. If a user attempts to type more characters than allowed, the extra characters are ignored. If a user pastes a string that would exceed the limit, the string is truncated to fit.

The following code creates a `TextBox` with a `MaxLength` of 10:

```csharp
// Initialize
var textBox = new TextBox();
textBox.AddToRoot();
textBox.MaxLength = 10;
// only 10 characters show:
textBox.Text = "abcdefghijklmnopqrs";
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA1WMsQrCMBRFfyW8SaGU2LHFQRcRdJGCg3WI5rV92iaavGhR_HcrddDxnsO5T1j6RWghZRcwguDJVB7SHfQw3pLD0qkWYR8BGWJSDT0QUrgpJxg7nttOTIXBu8iHNRpnhfmaeKZ1bjfW8h9dq26FpuK6LyfyR3wuelYEKZNEHY4ay6qm07lpjb1cnR9EBq83c7Gb4LUAAAA)

## Selection

Selection can be performed programmatically or by the user using the cursor.

The `SelectionLength` property can be used to determine if any text is selected. The following code shows how to output the selected characters:

```csharp
// Update
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
// Initialize
textBox.SelectionStart = 0;
textBox.SelectionLength = 5;
```

The entire text can be selected as shown in the following code:

```csharp
// Initialize
textBox.SelectionStart = 0;
textBox.SelectionLength = textBox.Text?.Length ?? 0; // in case text is null
```

Selection can also be performed by the user. Double-clicking the text box selects all text. Also, pressing CTRL+A selects all text.

<figure><img src="../../.gitbook/assets/13_10 01 00.gif" alt=""><figcaption><p>Double-click selects all text</p></figcaption></figure>

A push+drag with the mouse selects the text between the start and the current location of the drag.

<figure><img src="../../.gitbook/assets/13_10 01 46.gif" alt=""><figcaption><p>Push+drag to select text</p></figcaption></figure>

Selection is performed based on the X position of the cursor, even if the cursor is outside of the bounds of the TextBox.

<figure><img src="../../.gitbook/assets/17_21 57 11.gif" alt=""><figcaption><p>Selection by dragging outside of the TextBox bounds</p></figcaption></figure>

Holding down the shift key and pressing the arrow keys adjusts the selection. CTRL+Shift+arrow selects the next or previous word.

<figure><img src="../../.gitbook/assets/14_05 58 31.gif" alt=""><figcaption><p>Arrow keys + shift to select</p></figcaption></figure>

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
// Initialize
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

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm1Q30vDMBB-H-x_OPLU0VIm6MukgvZBBz6IVnQQGNkS2mBNSs1ccex_N7k0XYfm6e77cXdfDtMJAFl-3e8-yQJMuxMJIlJJI1ktf4SFyTdroWFK1JCBEnt4MWz78eSAaHZNFVLpuyWv5kO7Om9vOS_0s9YGHVS5kTXbDCMfXT2aZvV5JWseoejkMaIzd7rrXYXv0NczqcPeWtY0UpVWNm5TV4ykD0KWlbGii8v53829yu8Olpy1wiwVF11eMVUKDnEG0TqB9QyyG6oOVIF9rw1nRmCoQocrMUoSIri5R59LS_6_A1v_T0kIO_iHZchjbhuFEiw2vXBhAYiDx4ti8D73LLuXpoKtywXSBTt3nAK7e8l0cvwFIohkSDQCAAA)

<figure><img src="../../.gitbook/assets/14_06 03 52.gif" alt=""><figcaption><p>CaretIndexChanged is invoked whenever the caret index changes, which updates the Label's text.</p></figcaption></figure>

## Placeholder

The `Placeholder` property sets the placeholder text which appears when there is no text in the TextBox.

The placeholder text automatically disappears when the user has entered text.

The following code shows how to set `Placeholder` :

```csharp
// Initialize
TextBox textBox = new();
textBox.AddToRoot();
textBox.Anchor(Gum.Wireframe.Anchor.Center);
textBox.Width = 250;
textBox.Placeholder = "Enter Name...";
```

[Try on XnaFiddle.Net](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUcrMyyzJTMzJrEpVslIKSa0occqvUCiB0rYKeanlGprWMXlQET3HlJSQ_KD8_BJU0bzkjPwiDaCxeuGZRalpRYm5qVBBPedUoLIiZNXhmSklGUCzjUwNkEQDchKTUzPyc1JSi4ByMaUGBkZGriCtCn4g0_T0IELWSrUAF_V5g8IAAAA)

Placeholder can be removed by assigning an empty string:

```csharp
// Initialize
textBox.Placeholder = string.Empty;
```

## TextWrapping

The `TextWrapping` property can be used to set whether the TextBox wraps text. By default this value is set to `TextWrapping.NoWrap` which means the text does not wrap, but instead extends horizontally.

```csharp
// Initialize
var textBox = new TextBox();
textBox.AddToRoot();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvm5VJQUPIsdi_NVbJSKCkqTdUBi2TmZZZkJuZkVqUChZXKEosUSlIrSpzyKxRsFfJSyxVCIDwNTeuYPKiMnmNKSkh-UH5-CVBUiZerFgAyeZ9_XAAAAA)

<figure><img src="../../.gitbook/assets/14_06 09 11.gif" alt=""><figcaption><p><code>TextWrapping.NoWrap</code> causes text to scroll</p></figcaption></figure>

If `TextWrapping` is set to `TextWrapping.Wrap`, then text wraps to multiple lines. Note that usually this is combined with a taller text box so that multiple lines display properly.

```csharp
// Initialize
var wrappedTextBox = new TextBox();
wrappedTextBox.AddToRoot();
wrappedTextBox.TextWrapping = TextWrapping.Wrap;
 // If you have set up your TextBox in code, you may need to make it taller:
 wrappedTextBox.Height = 140;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm1OwQqCQBS8C_7D4KlArKCT0qEu5TWCLl2W9qVLuivbU7Po31uloKjTm5k3b-bdfQ8I0su6LoMYbGsKB0VpxUoU6kZODhph0VpRVSR3dOWVuWIBTS1ebDRODvrbEC2l3JmtMfxv2c99LymduahPGvXAXUwmSE_oTI1cNIQLMeqq5_bdCqVxNJLCwVWKzr1EEmwcPhMUg0VRkI1_6jekspxd8Ww-TQLfezwBuTkzGgcBAAA)

<figure><img src="../../.gitbook/assets/14_06 11 31.gif" alt=""><figcaption><p><code>TextWrapping.Wrap</code> causes text to wrap</p></figcaption></figure>

## Enter Key Behavior

The behavior of the Enter key depends on the `AcceptsReturn` property and whether the TextBox is bound to a data source.

### Multi-Line Entry (AcceptsReturn)

`AcceptsReturn` can be set to true to add newlines when the return (enter) key is pressed.

```csharp
// Initialize
var wrappedTextBox = new TextBox();
wrappedTextBox.AddToRoot();
wrappedTextBox.TextWrapping = TextWrapping.Wrap;
// If you have set up your TextBox in code, you may need to make it taller:
wrappedTextBox.Height = 140;
wrappedTextBox.AcceptsReturn = true;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm1QywrCMBC8F_yHoScF8QGeWjzoRb0WwYuX0KxtsCYl3dQX_ruJKCj2tDuzszvD3nsREG-alTvFCdg6Gr4YpRUrUakbeTpuhcXZiromuaULL80Fc2g64436g3SvfwWjhZRbkxnDXcNQd4FSuvCnvuEoNH5jPMbmgKtxKEVLaIjh6oDtxxVKIzeShi_VSVx9JJJg4_sjQTFYVBXZ5M9-Taoo2RtPZ5OO5HlONTcZsbPai8JX0rgXPZ5dWeYvLQEAAA)

<figure><img src="../../.gitbook/assets/14_06 12 55.gif" alt=""><figcaption><p>Manually adding newlines by pressing the return (enter) key</p></figcaption></figure>

### Enter and Binding

The `TextBox.Text` property can be bound to a ViewModel's property. By default this property is updated immediately when text changes but the `UpdateSourceTrigger` can be set to `UpdateSourceTrigger.LostFocus`.

In this situation, the enter key also applies binding even if the TextBox has not lost focus.

```csharp
// Initialize
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

<figure><img src="../../.gitbook/assets/24_07 50 58.gif" alt=""><figcaption><p>Pressing enter applies binding</p></figcaption></figure>

## Tab Key Behavior

The tab key behavior is controlled by the `AcceptsTab` property. This value is false by default.

If this value is false, pressing the Tab key does not insert a tab `'\t'` character.

If this value is true, pressing the Tab key inserts a tab `'\t'` character. Note that if this value is true, then tabbing to the next control by pressing tab is disabled. In other words, the TextBox keeps focus after the tab key is pressed. For more information on how focus navigation works, see the [Tabbing (Moving Focus)](../events-and-interactivity/tabbing-moving-focus.md) page.

## Extended Character Sets and Keyboards

The TextBox supports entering characters respecting the current keyboard language settings. This includes typing characters with accents, pasting text, and entering alt codes ([https://www.alt-codes.net/](https://www.alt-codes.net/) ).

<figure><img src="../../.gitbook/assets/image (167).png" alt=""><figcaption><p>TextBox displaying the é character</p></figcaption></figure>

Characters must be available in the current font to support being written in TextBoxes. If you would like to support more characters, you can explicitly create a font (.fnt) including the desired characters, or change the default character set in Gum.

For more information on creating fonts, see the [Font](../../gum-tool/gum-elements/text/font.md) and [Use Custom Font](../../gum-tool/gum-elements/text/use-custom-font.md) pages. For more information on specifying the default character set in Gum, see the [Project Property Font Ranges](../../gum-tool/project-properties.md#font-ranges) page.
