# TextBox

The TextBox control allows users to enter a string. It supports highlighting, copy/paste, selection with mouse and keyboard, and CTRL key for performing operations on entire words.

### Code Example: Creating a TextBox

The following code creates a TextBox.

```csharp
var textBox = new TextBox();
this.Root.Children.Add(textBox.Visual);
textBox.X = 50;
textBox.Y = 50;
textBox.Width = 200;
textBox.Height = 34;
textBox.Placeholder = "Placeholder Text...";

var textBox2 = new TextBox();
this.Root.Children.Add(textBox2.Visual);
textBox2.X = 50;
textBox2.Y = 90;
textBox2.Width = 200;
textBox2.Height = 34;
textBox2.Placeholder = "Placeholder Text...";
```

<figure><img src="../../../.gitbook/assets/24_07 22 19.gif" alt=""><figcaption><p>Interacting with TextBoxes</p></figcaption></figure>

### Selection

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

The `SelectionStart` and `SelectionLength` proerties can be modified to change the visual selection. For example, the following selects the first 5 letters:

```csharp
textBox.SelectionStart = 0;
textBox.SelectionLength = 5;
```

The entire text can be selected as shown in the following code:

```csharp
textBox.SelectionStart = 0;
textBox.SelectionLength = textBox.Text?.Length ?? 0; // in case text is null
```
