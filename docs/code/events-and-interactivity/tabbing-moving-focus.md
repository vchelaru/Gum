# Tabbing (Moving Focus)

## Introduction

Gum supports tabbing focus between controls. Tabbing can be performed with the keyboard or gamepad.

## Keyboard Tabbing

The keyboard can be used to interact with controls. Keyboards can be used to:

* Tab forward and back to pass focus to new controls
* To click controls by pressing enter
* To perform control-specific actions such as changing the value of a slider

### Tab Key Capture (AcceptsTab)

By default, the Tab key is used for navigation. However, certain controls like `TextBox` and `PasswordBox` can be configured to capture the Tab key to insert a tab character instead of moving focus.

When a `TextBox` has its `AcceptsTab` property set to `true`, focus navigation via the Tab key is disabled for that control. You can still move focus using the mouse or other input methods. For more details, see the [TextBox Tab Key Behavior](../controls/textbox.md#tab-key-behavior) section.

To enable keyboard tabbing, add the following code. This code only needs to run once, so add it to your game's Initialize or other code which runs at startup.

```csharp
// Initialize
GumUI.UseKeyboardDefaults();
```

Keep in mind that a control must first be explicitly set to receive keyboard input.

For example, the following code gives a Button focus assuming MyButton is a valid button:

```csharp
// Initialize
MyButton.IsFocused = true;
```

Note that TextBox and PasswordBox automatically have IsFocused set to true when clicked on.

Controls can be skipped when tabbed by setting `GamepadTabbingFocusBehavior`. For example the following code results in a button being skipped if it receives focus from tabbing:

```csharp
// Initialize
MyButton.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
```

{% hint style="info" %}
Despite its name, the GamepadTabbingFocusBehavior property controls tabbing for both gamepad and keyboard tabbing. Future versions of Gum may change this property to more clearly indicate its purpose.
{% endhint %}

### Customizing Tab Key Combos

The keys that trigger tabbing are defined as static lists on `FrameworkElement`: `TabKeyCombos` for moving forward and `TabReverseKeyCombos` for moving backward. By default `TabKeyCombos` contains Tab, and `TabReverseKeyCombos` contains Shift+Tab (both left and right Shift).

Each entry is a `KeyCombo`, with a `PushedKey`, an optional `HeldKey` modifier, and `IsTriggeredOnRepeat` (whether holding `PushedKey` down repeatedly re-triggers the combo, rather than only on the initial press). Both lists are mutable, so you can add, remove, or replace entries. For example, the following code adds the ability to tab forward and backward using the up and down arrow keys in addition to Tab:

```csharp
// Initialize
FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Down, IsTriggeredOnRepeat = true });
FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Up, IsTriggeredOnRepeat = true });
```

{% hint style="warning" %}
Only combos whose `PushedKey` is Tab respect a control's `IsTabNavigationEnabled` (the property backing `AcceptsTab` on `TextBox` and `PasswordBox`, described above). A combo bound to any other key always moves focus, even on a control with `AcceptsTab` set to `true`.
{% endhint %}

For a longer worked example, including tabbing on a per-control basis by handling `KeyDown` instead of changing the global combo lists, see [Customizing Tab Keys](../getting-started/tutorials/code-only-gum-forms-tutorial/input-in-forms.md#customizing-tab-keys) in the code-only Gum Forms tutorial.

`FrameworkElement.ClickCombos` follows the same `KeyCombo` pattern and controls which keys "click" a focused control (Enter and Space by default) rather than move focus.

## Gamepad Tabbing

To enable gamepad support in your game:

1. Be sure to have a gamepad plugged in. Any gamepad that is usable in MonoGame will also work as a gamepad in Gum Forms
2. Add the gamepad to the FrameworkElement.GamePadsForUiControl. You can add multiple gamepads for multiplayer games.
3. Set the initial control to have focus by setting its `IsFocused = true`

For example, the following code enables gamepad control for a game assuming MyButton is a valid button:

```csharp
// Initialize
// The first gamepad:
var gamepad = GumUI.Gamepads[0];
// If this code is run multiple times then the gamepad
// may get added multiple times as well. To be safe, clear
// the list:
FrameworkElement.GamePadsForUiControl.Clear();
FrameworkElement.GamePadsForUiControl.Add(gamepad);
MyButton.IsFocused = true;
```

Gamepad navigation uses the following input for tabbing to the next item:

* DPad down
* DPad right
* Left stick down
* Left stick right

Gamepad navigation uses the following input for tabbing to the previous item:

* DPad up
* DPad left
* Left stick up
* Left stick left

Left and right navigation can be enabled or disabled on a control by setting `IsUsingLeftAndRightGamepadDirectionsForNavigation` to false. For example the following code adds a button which can only be navigated from by pressing up/down on the gamepad:

```csharp
// Initialize
var button = new Button();
myStackPanel.AddChild(button);
button.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
```

`IsUsingLeftAndRightGamepadDirectionsForNavigation` is set to true on all controls except on `Sliders`, which use left/right input for changing the `Slider`'s `Value`.

## Spatial (Any-Angle) Gamepad Navigation

For scattered layouts where controls aren't arranged in a single row, column, or stack, such as a skill tree, radial menu, or free-form HUD, tab order navigation doesn't produce a natural or predictable path between controls. Spatial navigation instead moves focus to whichever focusable control is nearest in the direction the player presses, based on each control's on-screen position.

This section assumes you have already followed the gamepad setup above (adding a gamepad to `GamePadsForUiControl` and giving a control initial focus).

To opt a group of controls into spatial navigation, set `GamepadNavigationMode` on a container that wraps them:

```csharp
// Initialize
Panel skillTreePanel = new();
skillTreePanel.AddToRoot();
skillTreePanel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
```

Every focusable descendant of `skillTreePanel`, including controls nested inside further sub-containers, automatically uses spatial navigation for DPad and left stick input. No changes are needed on the controls themselves. `GamepadNavigationMode` is inherited: a control with the property unset walks up to the nearest ancestor that has it set, so you only need to set it once on the container that groups the scattered controls.

Setting `GamepadNavigationMode` to `TabOrder` on a nested container opts that container back out of an outer `Spatial` zone. For example, a linear list of items inside an otherwise scattered screen can keep tab order navigation for its own items:

```csharp
// Initialize
StackPanel linearList = new();
skillTreePanel.AddChild(linearList);
linearList.GamepadNavigationMode = GamepadNavigationMode.TabOrder;
```

Keyboard Tab navigation always uses tab order, regardless of `GamepadNavigationMode`. Spatial navigation only affects gamepad DPad and left stick input.

### Explicit Direction Overrides

Spatial navigation scores candidates by distance and angle, which can occasionally pick a control other than the one you expect for an unusual layout. To force a specific direction to a specific control, set `SpatialNavigationUp`, `SpatialNavigationDown`, `SpatialNavigationLeft`, or `SpatialNavigationRight` on the control:

```csharp
// Initialize
myButton.SpatialNavigationRight = otherButton;
```

An explicit override is checked independently for each direction currently held, so it also applies on a diagonal DPad press (for example, an override on `SpatialNavigationRight` still applies even while Up is held at the same time). If more than one held direction has an override set, `SpatialNavigationRight` takes priority, followed by `SpatialNavigationLeft`, `SpatialNavigationDown`, then `SpatialNavigationUp`. Left stick input is snapped to its nearest cardinal direction and checks that single direction's override the same way.

#### Blocking a Direction Entirely

Assigning a control to itself blocks that direction: the press is consumed and focus stays put, instead of falling back to automatic scoring. This is useful when a neighboring control should only be reachable by an explicit action (for example, pressing a confirm button) rather than by drifting into it with spatial navigation.

```csharp
// Initialize
myButton.SpatialNavigationRight = myButton;
```

Leaving the property unset is not the same as blocking the direction. With no override assigned, that direction still falls back to automatic distance/angle scoring and can still move focus to a nearby control.

## Modal Tab Trapping

When you show an element modally by adding it to `GumService.Default.ModalRoot`, tab focus is confined to the controls inside that modal element. Tabbing forward past the last focusable control wraps back to the first, and Shift+Tabbing before the first control wraps to the last. Focus never escapes to the controls behind the modal under the regular `Root`.

This is the keyboard and gamepad counterpart to the input blocking already described for modal popups — see [ModalRoot and PopupRoot](../gum-code-reference/gumservice-gumui/modalroot-and-popuproot.md). A modal captures all interaction until it is dismissed: mouse input is routed only to the top-most modal element, and tab focus is trapped within it.

No extra setup is required. As soon as the modal element is removed from `ModalRoot`, tabbing returns to the controls under `Root`.

When several elements are stacked in `ModalRoot` (for example, one modal opening another), tab focus stays within the top-most modal — the last one added. Focus does not fall through to the modals behind it, consistent with mouse input being routed only to the top-most modal. For this to work, give each modal a single root element (a container, component, or screen) so that its controls are grouped together under `ModalRoot`.

## Getting Focused Item (CurrentInputReceiver)

The static `InteractiveGue.CurrentInputReceiver` returns the current item that has focus. This can be used to diagnose problems.

The following code shows how to display which button has focus with a label:

```csharp
// Class scope
Label label;

protected override void Initialize()
{
    GumUI.Initialize(this);

    // Enables tabbing with the keyboard
    GumUI.UseKeyboardDefaults();

    StackPanel stackPanel = new();
    stackPanel.AddToRoot();
    stackPanel.Anchor(Anchor.Center);
    stackPanel.Spacing = 6;

    for(int i = 0; i < 5; i++)
    {
        Button button = new();
        stackPanel.AddChild(button);
        button.Text = $"Button {i + 1}";
        button.Name = button.Text;
        if(i == 0)
        {
            button.IsFocused = true;
        }
    }

    label = new ();
    stackPanel.AddChild(label);

    base.Initialize();
}


protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    label.Text = 
        $"Focused Control: {InteractiveGue.CurrentInputReceiver?.ToString() ?? "null"}";

    base.Update(gameTime);
}
```

<figure><img src="../../.gitbook/assets/17_05 01 49.gif" alt=""><figcaption><p><code>CurrentInputReceiver</code> displayed on a <code>Label</code></p></figcaption></figure>

