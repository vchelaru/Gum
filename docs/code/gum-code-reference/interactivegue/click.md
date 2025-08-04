# Click

## Introduction

A Click event is raised when an InteractiveGue is first pressed, then released. This can occur with a mouse cursor or a touch screen press/release.

The Click event is used by most Gum Forms controls to raise their own Click event, and to update visual state.

## Click Definition

A click occurs when the following happens:

1. The user presses on the InteractiveGue (touch or mouse button not down last frame, is down this frame)
2. The user releases on the same InteractiveGue (touch or mouse button was down last frame, is not down this frame)

{% hint style="info" %}
Some UI elements should react immediately when pressed rather than waiting for the cursor to be released. For this type of behavior, see the Push event. For more information, see the [Push event page](push.md).
{% endhint %}

For simplicity the following code can be used to test click events:

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Gum.Wireframe.Anchor.Center);

// Button's Click event internally uses an InteractiveGue's Click, so
// we can test when clicks are raised by subscribing to a Button's Click
button.Click += (_, _) => 
    button.Text = DateTime.Now.ToString();
```

Clicking (pressing, then releasing) on the button raises the Click event which updates the button's text. Notice that the click is raised on release, and that the cursor can move away from the initial press location so long as it is still within the bounds of the same pressed instance.

<figure><img src="../../../.gitbook/assets/03_13 40 38.gif" alt=""><figcaption><p>Button pressed then released causing a Click event to be raised</p></figcaption></figure>

A click is even raised if the press happens on the button, the cursor is moved off of the button, then moved back on the button before being released.

<figure><img src="../../../.gitbook/assets/03_13 42 15.gif" alt=""><figcaption><p>Click event still raised even if the cursor moves off of the instance temporarily</p></figcaption></figure>

If the moue button is pressed when not hovering over the instance, then a click is not raised even if the user released the mouse over the instance.

<figure><img src="../../../.gitbook/assets/03_13 43 34.gif" alt=""><figcaption><p>Click event is not raised if the user presses while off of the button, then drags on</p></figcaption></figure>

