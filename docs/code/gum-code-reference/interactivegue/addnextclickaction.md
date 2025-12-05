# AddNextClickAction

## Introduction

AddNextClickAction adds a delegate to be raised the next time the Cursor performs a primary click. This call is not bound to a particular control, and it can be used to handle the user clicks off of the control. This method is used internally to close Menus and ComboBoxes, but it can be used to close custom popups, modals, and context menus.

## Code Example

The following code creates a Button which displays a StackPanel of Buttons. The StackPanel closes on any click, whether on the StackPanel or not, but each button in the StackPanel registers a click.

```csharp
var popupPanel = new StackPanel();
popupPanel.IsVisible = false;
popupPanel.Anchor(Anchor.Center);
Gum.ModalRoot.AddChild(popupPanel);
for(int i = 0; i < 5; i++)
{
    var innerButton = new Button();
    popupPanel.AddChild(innerButton);
    innerButton.Text = "Button " + i;
    innerButton.Click += (_,_) =>
    {
        System.Diagnostics.Debug.WriteLine("Clicked " + innerButton.Text); 
    };
}

var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
button.Text = "Click me to show popup";
button.Y = -200;
button.Click += (not, used) =>
{
    popupPanel.IsVisible = true;
    InteractiveGue.AddNextClickAction(() =>
    {
        popupPanel.IsVisible = false;
    });
};

```

<figure><img src="../../../.gitbook/assets/13_08 07 51.gif" alt=""><figcaption><p>Stack panel closing when clicked off, but buttons still register clicks</p></figcaption></figure>

