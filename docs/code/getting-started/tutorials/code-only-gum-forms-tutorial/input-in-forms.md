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
// Initialize
FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2Qy07DMBBF9_mKwatEVPmAVkGC0KKKTUXKLlLlxoOw4tjVeNzyEP-O41YEdekzZ66v_Z0BiLV_CoOYA1PA2Qi01ayl0V8YqThKgkFqu5EWDVRg8QQNy65PIC8Wrf0bl_dKbd2Lc5z4iuSAJ0f90uCAlstn_Nw7ScqvHL3q2lkml5byWKFBOuoOy0d8k8FM8pg0ltgHZmcvDR7SId1y5uUWPzgOW7Eh9B6ag-wQHMHSMlIrJrE2uuvhtoJ8N4NdAdXdJXqKSAqqm_9r61i6Cx5VNMavun53_a6Nys9ysRDZT_YLwXDrNF4BAAA)

To enable gamepad input, add the gamepad to the FrameworkElement.GamePadsForUiControl list as shown in the following code block:

```csharp
// Initialize
FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACl2QwU7DMAyG730Kk1Mrpj4AU5FGYdNu1TZulabQeGA1TVDibAjEu5OmFUMc_fv_7N_-ygDE1m_CIO6AXcDFKJAhJqnpE6MqztLBIMk00qCGCgxeYM-y65OQF8vW_LbLlVIHu7OWk752csCLdf2TxgENl5tYN1L5tXXPVFvDziZmJ80r5jHGHt2ZOiwf8SSDnoD3CIzTxiAvgdmaOcVDKtKmSS8P-MGx2YrGofcw07BqxdVTa-p6uK0gPy7gWEB1P0-90smC6uYvto2hu-BRRcf4qf9n12-kVT6Zi6XIvrMfW6v54F0BAAA)

Notice that both KeyboardsForUiControl and GamePadsForUiControl are lists. If you do not want your game to support keyboard input you can keep the KeyboardsForUiControl empty. You can selectively add gamepads to GamePadsForUiControl depending on whether you only want some of the gamepads to control UI. For example, you may only want to read input from gamepads which have joined the game.

{% hint style="info" %}
Most games either support the main keyboard or no keyboard input. KeyboardsForUiControl is a list, allowing for advanced scenarios such as controlling UI using a virtual keyboard. This advanced scenario is not covered in this tutorial.
{% endhint %}

Once keyboard and gamepads are added to their appropriate lists, focused forms controls receive input from these input devices. For example, the following code shows how to set focus on a `Button` which receives input from the keyboard.

```csharp
// Initialize
FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

var mainPanel = new StackPanel();
mainPanel.AddToRoot();

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm2RwU7DMAyG73sKK-LQqlPFmalIUNg0cZm2ceslaw1ETRyUJhtQ9d1J0okKWE7x59-_bbmfAbB1t3KK3YA1DucBCBJWcCm-0FN25AYUF7ThhBIKIDzBzvK6jSBJFxX9pPO7ptnrrdY28qXhCk_atI8SFZLNn_DzoLlpuqU2z6LUZI2ORYkfYYfmKGrMH_CFOzmJLzqtfLzh_422nF7xklsoePcFwa2isNTBWavpvNF9DOLUgizUUtRtqZ3_FnDt4SjO9_gRSMXKIACFFZuSI8sKSLo5YArFbUV9ReDf5JdlviCg345XZ0tsoJ_EA1ihsItNhqnR2q9du85ri3i1vyco34RsklGcLthsmH0DCVJb0ekBAAA)

<figure><img src="../../../../.gitbook/assets/13_08 31 15.gif" alt=""><figcaption><p>Button clicks through gamepad A button or keyboard space/enter</p></figcaption></figure>

The remainder of this tutorial excludes code to add keyboards and gamepads to their explicit list for brevity.

## IsFocused and Tabbing

If your game includes multiple controls then the user can tab between the controls to pass focus. A forms control must first be given focus before tabbing is possible. Once a control is given focus, the user can tab to the next control using the tab key on the keyboard. Shift+tab tabs backwards (up) through the list of controls. Tabbing with the gamepad can be performed using up and down on the d-pad or analog stick.

The following code shows how to create multiple buttons. Once the first button is focused, the user can tab between the buttons to click each one.

```csharp
// Initialize
var mainPanel = new StackPanel();
mainPanel.AddToRoot();

// Give some spacing between the buttons:
mainPanel.Spacing = 6;

for(int i = 0; i < 5; i++)
{
    var button = new Button();
    button.Text = "Click me";
    button.Visual.Width = 300;
    button.Click += (s, e) =>
    {
        button.Text = $"Clicked @ {System.DateTime.Now}";
    };
    if(i == 0)
    {
        button.IsFocused = true;
    }
    mainPanel.AddChild(button);
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm1STWvCQBC9-yuG0ENEWYTSHmpT2moVKRQxtr3ksiajDia7stloreS_O_moJWguw7437-282RxbAM4kHWeJ8wDWZNgtAFJkScb0i4w6O2kgkaSmUmEMHijcg29luCkBt90P1JkWL1E01zOtbYmPjExwr83mLcYElRXveFhoaaJ0pM0nDbSyRpcil0fw0ewoRDHEpczi_-arTmM-T-Wl0UyqFV5zKwRbFjTn9bcyJLXiWPeMB2qpjUvKAjHS63N5hDsunU47UMdAAX_FPhaZtVrVy3gtD2Xggq84Mccfyw2BM4gp3ECCgdNs-KI0k7H4psiuufG212vyla7jgZt2AdvgPVV0PcblVTf1XRjBMxz9Q2oxEUNpcU4Jig-9z88j5HWlpctJOSrHu-494Q2HWcqeXvmD_BlUpfHwgzXFkVvpimXkTitvnQAwOCC1YgIAAA)

<figure><img src="../../../../.gitbook/assets/13_08 33 43.gif" alt=""><figcaption><p>Button stack tabbing and receiving input</p></figcaption></figure>

Tab order is controlled by the order that controls are added to their parent. If the parent is a StackPanel, then controls will naturally tab top to bottom. Once the last control receives focus, tabbing wraps back around to the first control. Similarly, pressing shift+tab on the first control wraps around to the last control.

Tab navigation stops on every control which implements the `IInputReceiver` interface. Therefore, tabbing skips over forms controls such as Label, Image, Panel, and StackPanel. However, if the control contains children which implement `IInputReceiver`, then those children can be focused through tabbing.

The following code shows that tabbing skips the labels and moves between each TextBox.

```csharp
// Initialize
var mainPanel = new StackPanel();
mainPanel.AddToRoot();

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACpWSUU_CMBSF3_kVzZ4gMY1ubxAfEJ0hEkNkmpDs5bJeQkPXkrYD1Pjf7epgCw6Db925595zv7WfHUKCsXks8qBPrC7wqhS45JaD4B_o1GALmuTA5RQkCnJLJO7IzEK29kK3N0jlsUyHjCXqRSnr9VhDjjul1w8Cc5SWPuH7QoFmJlb6lY-UtFr5pq5bYYZ6yzOk97iEQtTmclIqyzUELI4rTMqzT_EqTXBvXSkNYq6NJc8uuZ8Gp8uNVlywru-ox1rXeqf21eDk58uPrip07BbOCoPMecrf1ChNBWS4UoKh9vHnMiv_CUzYThPSN24KEHTu6jfXtXyknMBFkOEvyvA8ZvhfmMZ0nxa100TtNFFNMwVj3Dthf8NEddymaqiv7TDiwNRw0GGW4caaBBbOvARhygtsGi7lbvT0BkHnq_MN1JVhokEDAAA)

<figure><img src="../../../../.gitbook/assets/13_08 36 31.gif" alt=""><figcaption><p>Tabbing skips over Labels and moves to the next TextBox or PasswordBox</p></figcaption></figure>

Tabbing can be performed to move focus between complex hierarchies. Gum performs a [depth first](https://en.wikipedia.org/wiki/Depth-first_search) search for controls which implement IInputReceiver. For example, the following code creates two groups of `RadioButton` instances, each inside their own `StackPanel`. Tabbing is able to move between the two groups automatically.

```csharp
// Initialize
var mainPanel = new StackPanel();
mainPanel.AddToRoot();

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACsVRwU7CQBC98xWTxkNJTUPrTfSgKIR4IYAxJr2s7CAT2t1m2YJK-Hdnu41grIk3e5nsmzdv3rzuOwDBeDOqiuASrKnw3AGkyJLI6QMZDbbCQCFITYTCHK5B4Q5mVizWNRB2-5n6asc3Us71VGtb40MjCtxps77PsUBl4wd8f9HCyM1Qm0caaGWNrodCtjBDs6UFxne4FFV-JLcqjfg9ET-FpkK9YpuaGyh5wKllyh1VOsfJHy4arCiXoae75lIbCElZIB7u9blcwQWXKOpmap8p4M8teKms1apZMBWS9G2N1BscyRPiJ5J2xbS01_vemOObZfwsC0ZGVyUkoEtLLLmnKDlkQcOmZchO2ErXvxsLJ0JjzmlRbVCymvvNzeDBF3_Z8VI_5Ewy4SSrtD0r34ufuZu4A37LLv3X7NJjdhDBSXqN_bbrg86h8wnFvL2UIwMAAA)

<figure><img src="../../../../.gitbook/assets/13_08 37 43.gif" alt=""><figcaption><p>Tabbing between RadioButton groups in nested StackPanels</p></figcaption></figure>

## Customizing Tab Keys

The keyboard's tab and shift+tab keys are used to move focus between forms controls. This behavior can be customized by adding or removing `KeyCombo` instances from `FrameworkElement.TabKeyCombos` and `FrameworkElement.TabReverseKeyCombos`.

For example, the following code adds the ability to tab by pressing the up and down arrows on the keyboard.

```csharp
// Initialize
FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
FrameworkElement.TabKeyCombos.Add(new KeyCombo()
{
    PushedKey = Microsoft.Xna.Framework.Input.Keys.Down
});
FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo()
{
    PushedKey = Microsoft.Xna.Framework.Input.Keys.Up
});

var mainPanel = new StackPanel();
mainPanel.AddToRoot();
mainPanel.Spacing = 6;
for (int i = 0; i < 3; i++)
{
    var button = new Button();
    button.Text = $"Button {i + 1}";
    if (i == 0) button.IsFocused = true;
    mainPanel.AddChild(button);
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACrVSTUvDQBC991cMwUNKZVEED2oP2lopIpR-gIdcNsnELk1myu6mVUv-u5M0bRX06F6GfW_mvWHf7joAwdg9lUVwA96WeF4Dhow3OjefKGiw0RYKbWiiCXPoA-EWZl4nqwYIu7cRHWl1n6ZznjL7Bh9ZXeCW7eoxxwLJq2f8iFnb1I3YLsyAyVtuhkJZYYZ2YxJUQ8x0mZ-af1Wa61j4ARcxu0agXuuAhN2IdhGBnEnplpgKIZu_mMSy48yrV9LqKKnGtC4bO6eGvKWIqr8sp7hB6_A_nBfr1vf7c87WOjH0JgrXwmRsITTkwQhwcSvlDq6k9Hon1zqtuPSeqY3qobk0cdT8nlNzfPfScBYFex52BnpwWUVB22cysYK--HQPM2NJLSkdpjJY_5W280f4g6XJ03A_UFtWQafqfAE2d5mXZgIAAA)

`TabKeyCombos` and `TabReverseKeyCombos` are lists and automatically include tab and shift+tab. You can clear these lists if you would like to prevent the tab key from moving focus.

Adding a KeyCombo to either of these lists enables navigation with these keys globally. In some cases this navigation may interfere with regular forms behavior. For example, adding `Keys.Left` and `Keys.Right` for tabbing prevents TextBox caret movement.

Tabbing on a case by case basis can be performed by subscribing to individual control events as shown in the following code. Only the `Button` instances have left/right key tabbing while the `Slider` does not tab with left/right so that it can use the arrow keys to change its value:

```csharp
// Initialize
var mainPanel = new StackPanel();
mainPanel.AddToRoot();

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
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACq2Ry27CMBBF93yFlZWjIi9YtsqC8mhRW6niIXWRjRNPwMUZI9sBtYh_r52kUEG7QbWURL6eO_fEs-8QEk3sQ1VGt8SZCrpBkCid5Ep-glejLTek5BJfOYIiCUHYkZnj-boWaHyX4vGY9YWY66nWrtbHhpew02Y9UlACOvYEH5nmRtixNgs50OiMrk3UI8zAbGUObAgFr9SpOHRKMWBYJQWYb4Z6c5k_WEklaFN6smaVcxpb6329qa2NziaeKK8sCF8R7uF04imGeofkJiGPHIWCOc9a7ffkxnee3PsrundtQq-N0FJc-KjO3iF3xAL6W-gSL4-2fgB9s7SE-1ec4j5F4pcsaBACBUkS8iJzo60uHHtDzo4DZBPcVPVILJvK5cr5BsHdNgmL0vNxx018zI541D9DaTya9FcbSMNPBPeh-YCycAXSMxT_QrTY_OA5RJ1D5wvjsOY0IgMAAA)

<figure><img src="../../../../.gitbook/assets/16_06 37 17.gif" alt=""><figcaption><p>Tabbing with left/right on Button, but using left/right to change Slider value</p></figcaption></figure>

{% hint style="info" %}
Currently, gamepad tabbing behavior cannot be modified - tabbing automatically uses gamepad up and down on dpad and analog sticks. This may change in future versions of Gum.
{% endhint %}


