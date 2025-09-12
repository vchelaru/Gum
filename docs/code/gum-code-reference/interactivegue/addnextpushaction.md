# AddNextPushAction

## Introduction

AddNextPushAction adds a delegate to be raised the next time the Cursor performs a primary push. This call is not bound to a particular control, and it can be used to handle when a control is clicked off of the control. This method is used internally to close Menus and ComboBoxes, but it can be used to close custom popups, modals, and context menus.

## Code Example

The following code shows how to hide a button when the cursor is clicked, whether it is on the button or not:

```csharp
var popupPanel = new StackPanel();
popupPanel.IsVisible = false;
popupPanel.Anchor(Anchor.Center);
GumUI.ModalRoot.AddChild(popupPanel);

var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
button.Text = "Click me to show popup";
button.Y = -200;
button.Click += (not, used) =>
{
    popupPanel.IsVisible = true;
    InteractiveGue.AddNextPushAction(() =>
    {
        popupPanel.IsVisible = false;
    });
};
```



