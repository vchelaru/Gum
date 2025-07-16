# Input in Forms

## Introduction

Gum supports reading input from the mouse, touch screen, keyboard, and gamepads. Some types of input are automatically enabled such as clicks on Buttons. Other types of input must be enabled through code, such as giving controls focus for tabbing.

This tutorial covers the various ways input can be used to interact with forms objects.

## Automatic Behavior

By default Gum controls work with the mouse and touch screen. The mouse automatically highlights all controls when it moves over the visible area of a control. Furthermore, the following events and actions also happen automatically:

* Button
  * Click event
* CheckBox
  * Click event
  * Checked event
  * Unchecked event
* ComboBox
  * Expand/Collapse
  * Item selection&#x20;
  * SelectionChanged event
* ListBox
  * Item selection
  * SelectionChanged event
  * ItemClicked
  * ItemPushed
  * Scroll on mouse wheel
* Menu
  * Item selection
  * Item expansion
  * Selected (MenuItem)
  * Clicked (MenuItem)
* RadioButton
  * Click event
  * Checked event
  * Unchecked event
* Slider
  * Drag thumb to change value
  * Click on track to change value
  * ValueChanged, ValueChangeCompleted, ValueChangedByUi events
* Splitter
  * Drag to resize controls before and after the Splitter
* TextBox/PasswordBox
  * Enter text with keyboard
  * CTRL+V paste
  * CTRL+C copy (TextBox only)
  * CTRL+X cut (TextBox only)
  * CTRL+A select all
  * Shift+arrow to select
  * Cursor drag to select
  * Caret movement with arrows
  * Delete and backspace to remove letters
  * Double-click to select all
* Window
  * Drag the title bar to move the window
  * Drag the edges and corners to resize the window

## Keyboard and Gamepad Input

Gum supports applying input from the keyboard and gamepads. By default the TextBox and PasswordBox forms types automatically receive input from the keyboard without any additional setup. All other interactive Forms controls can receive input from the keyboard and gamepads, but this input must be enabled.

To enable keyboard input, add the keyboard to the FrameworkElement.KeyboardsForUiControl list as shown in the following code block:

```csharp
FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
```

To enable gamepad input, add the gamepad to the FrameworkElement.GamePadsForUiControl list as shown in the following code block:

```csharp
FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
```

Notice that both KeyboardsForUiControl and GamePadsForUiControl are lists. If you do not want your game to support keyboard input you can keep the KeyboardsForUiControl empty. You can selectively add gamepads to GamePadsForUiControl depending on whether you only want some of the gamepads to control UI. For example, you may only want to read input from gamepads which have joined the game.

{% hint style="info" %}
Most games either support the main keyboard or no keyboard input. KeyboardsForUiControl is a list, allowing for advanced scenarios such as controlling UI using a virtual keyboard. This advanced scenario is not covered in this tutorial.
{% endhint %}

Once keyboard and gamepads are added to their appropriate lists, focused forms controls receive input from these input devices. For example, the following code shows how to set focus on a `Button` which receives input from the keyboard.

```csharp
FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

var button = new Button();
int clickCount = 0;
button.Text = "Click me";
button.Click += (s, e) =>
{
    clickCount++;
    button.Text = $"Clicked {clickCount} times";
};
button.IsFocused = true;
mainPanel.AddChild(button); 
```

<figure><img src="../../../../.gitbook/assets/13_08 31 15.gif" alt=""><figcaption><p>Button clicks through gamepad A button or keyboard space/enter</p></figcaption></figure>

The remainder of this tutorial excludes code to add keyboards and gamepads to their explicit list for brevity.

## IsFocused and Tabbing

If your game includes multiple controls then the user can tab between the controls to pass focus. A forms control must first be given focus before tabbing is possible. Once a control is given focus, the user can tab to the next control using the tab key on the keyboard. Shift+tab tabs backwards (up) through the list of controls. Tabbing with the gamepad can be performed using up and down on the d-pad or analog stick.

The following code shows how to create multiple buttons. Once the first button is focused, the user can tab between the buttons to click each one.

```csharp
// Give some spacing between the buttons:
mainPanel.Spacing = 6;

for(int i = 0; i < 5; i++)
{
    var button = new Button();
    button.Text = "Click me";
    button.Visual.Width = 300;
    button.Click += (s, e) =>
    {
        button.Text = $"Clicked @ {DateTime.Now}";
    };
    if(i == 0)
    {
        button.IsFocused = true;
    }
    mainPanel.AddChild(button); 
}
```

<figure><img src="../../../../.gitbook/assets/13_08 33 43.gif" alt=""><figcaption><p>Button stack tabbing and receiving input</p></figcaption></figure>

Tab order is controlled by the order that controls are added to their parent. If the parent is a StackPanel, then controls will naturally tab top to bottom. Once the last control receives focus, tabbing wraps back around to the first control. Similarly, pressing shift+tab on the first control wraps around to the last control.

Tab navigation stops on every control which implements the `IInputReceiver` interface. Therefore, tabbing skips over forms controls such as Label, Image, Panel, and StackPanel. However, if the control contains children which implement `IInputReceiver`, then those children can be focused through tabbing.

The following code shows that tabbing skips the labels and moves between each TextBox.

```csharp
var label = new Label();
label.Text = "First Name:";
mainPanel.AddChild(label);

var textBox = new TextBox();
textBox.IsFocused = true;
textBox.Placeholder = "";
mainPanel.AddChild(textBox);

var label2 = new Label();
label2.Visual.Y = 10;
label2.Text = "Last Name:";
mainPanel.AddChild(label2);

var textBox2 = new TextBox();
textBox2.Placeholder = "";
mainPanel.AddChild(textBox2);

var label3 = new Label();
label3.Visual.Y = 10;
label3.Text = "Password:";
mainPanel.AddChild(label3);

var passwordBox = new PasswordBox();
passwordBox.AcceptsTab = false;
passwordBox.Placeholder = "";
mainPanel.AddChild(passwordBox);
```

<figure><img src="../../../../.gitbook/assets/13_08 36 31.gif" alt=""><figcaption><p>Tabbing skips over Labels and moves to the next TextBox or PasswordBox</p></figcaption></figure>

Tabbing can be performed to move focus between complex hierarchies. Gum performs a [depth first](https://en.wikipedia.org/wiki/Depth-first_search) search for controls which implement IInputReceiver. For example, the following code creates two groups of `RadioButton` instances, each inside their own `StackPanel`. Tabbing is able to move between the two groups automatically.

```csharp
var panel1 = new StackPanel();
mainPanel.AddChild(panel1);
for (int i = 0; i < 3; i++)
{
    var button = new RadioButton();
    button.Width = 200;
    button.Text = $"Group 1 option {i+1}";
    if(i == 0)
    {
        button.IsFocused = true;
    }
    panel1.AddChild(button); 
}

var panel2 = new StackPanel();
panel2.Y = 10;
mainPanel.AddChild(panel2);
for (int i = 0; i < 3; i++)
{
    var button = new RadioButton();
    button.Width = 200;
    button.Text = $"Group 2 option {i + 1}";
    panel2.AddChild(button);
}
```

<figure><img src="../../../../.gitbook/assets/13_08 37 43.gif" alt=""><figcaption></figcaption></figure>

## Customizing Tab Keys

The keyboard's tab and shift+tab keys are used to move focus between forms controls. This behavior can be customized by adding or removing `KeyCombo` instances from `FrameworkElement.TabKeyCombos` and `FrameworkElement.TabReverseKeyCombos` .&#x20;

For example, the following code adds the ability to tab by pressing the up and down arrows on the keyboard.

```csharp
FrameworkElement.TabKeyCombos.Add(new KeyCombo()
{
    PushedKey = Microsoft.Xna.Framework.Input.Keys.Down
});
FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo()
{
    PushedKey = Microsoft.Xna.Framework.Input.Keys.Up
});
```

`TabKeyCombos` and `TabReverseKeyCombos` are lists and automatically include tab and shift+tab. You can clear these lists if you would like to prevent the tab key from moving focus.

Adding a KeyCombo to either of these lists enables navigation with these keys globally. In some cases this navigation may interfere with regular forms behavior. For example, adding `Keys.Left` and `Keys.Right` for tabbing prevents TextBox caret movement.

Tabbing on a case by case basis can be performed by subscribing to individual control events as shown in the following code. Only the `Button` instances have left/right key tabbing while the `Slider` does not tab with left/right so that it can use the arrow keys to change its value:

```csharp
var slider = new Slider();
mainPanel.AddChild(slider);

var button = new Button();
button.IsFocused = true;
button.KeyDown += HandleTabKeyDown;
mainPanel.AddChild(button);

var button2 = new Button();
button2.KeyDown += HandleTabKeyDown;
mainPanel.AddChild(button2);

void HandleTabKeyDown(object sender, KeyEventArgs args)
{
    if(args.Key == Microsoft.Xna.Framework.Input.Keys.Right)
    {
        ((FrameworkElement)sender).HandleTab(TabDirection.Down);
    }
    else if(args.Key == Microsoft.Xna.Framework.Input.Keys.Left)
    {
        ((FrameworkElement)sender).HandleTab(TabDirection.Up);
    }
}

```

<figure><img src="../../../../.gitbook/assets/16_06 37 17.gif" alt=""><figcaption><p>Tabbing with left/right on Button, but using left/right to change Slider value</p></figcaption></figure>

{% hint style="info" %}
At the time of this writing gamepad tabbing behavior cannot be modified - tabbing automatically uses gamepad up and down on dpad and analog sticks. This may change in future versions of Gum.
{% endhint %}

