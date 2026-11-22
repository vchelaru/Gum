# Blocking Input

## Introduction

`IsInputBlocked` stops all input to a visual and everything under it while something plays out, such as an animated screen transition. Blocked controls ignore the cursor, keyboard and gamepad, but animations, the deferred queue and the rest of `GumUI.Update` keep running.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Blocking and Unblocking

`IsInputBlocked` is a property on every visual. Setting it to true on a screen's visual blocks the whole screen, and setting it back to false restores input:

```csharp
// Initialize
var screen = new MainMenuScreen();
screen.AddToRoot();

// when the transition starts
screen.Visual.IsInputBlocked = true;

// when the transition ends
screen.Visual.IsInputBlocked = false;
```

`GumUI.Root.IsInputBlocked = true;` blocks everything added to the root. Popups and modals are not under `Root`, so block `GumUI.PopupRoot` and `GumUI.ModalRoot` as well if they should stop too.

The cursor passes through a blocked visual as if it were not there, so a control behind it that is not blocked still receives the cursor.

## What Blocking Keeps

The control that has focus keeps it, and keeps its focused look, while blocked and after. `IsInputBlocked` never changes `IsFocused` or `IsEnabled`, so controls do not show their disabled look.

When a visual becomes blocked, a control under it that is hovered or pushed returns to its normal look. A press that starts while blocked, or is held when the block starts, does not click anything. A key or gamepad button held when the focused control is unblocked does nothing until it is released, so a held Tab or DPad does not start repeating.

## Your Game's Own Input

`IsInputBlocked` affects only Gum controls. `GumUI.Cursor` and `GumUI.Keyboard` keep reporting the real device state, so game code that reads input directly can check the same property:

```csharp
// Update
if (!screen.Visual.IsInputBlocked && GumUI.Keyboard.KeyPushed(Gum.Forms.Input.Keys.Escape))
{
    // open the pause menu
}
```
