# ModalRoot and PopupRoot

### Introduction

The static ModalRoot and PopupRoot properties provide an InteractiveGue which serve as the root for any element which should appear on top of other elements. These properties have the following characteristics:

* Automatically created by `FormsUtilities.InitializeDefaults`
* Automatically resized to fit the entire screen, including if the `GraphicalUiElement.CanvasHeight` and `GraphicalUiElement.CanvasWidth` change.
* Both Remains on top of all other elements for its given layer. ModalRoot appears on top of PopupRoot.

These properties can be used in custom code to place elements (such as custom popup and toast elements) above all other elements.

{% hint style="info" %}
ModalRoot and PopupRoot are used internally by Gum Forms. For example PopupRoot is used to display the ListBox which appears when expanding a ComboBox.
{% endhint %}

### Modal Popups

Popups which are placed on the ModalRoot element are considered _modal_ - they block the input of all other controls. This is useful if you would like to create a popup which must be clicked before any other elements receive input. If multiple elements are added to ModalRoot, only the last item (and its children) receive input events. This allows one popup to show another popup.

By contrast, elements added to the PopupRoot are not modal - other elements can receive input.

{% hint style="info" %}
The ComboBox control normally places its expanded form ListBox on PopupRoot; however if the ComboBox is on a ModalRoot then its ListBox is also added to ModalRoot.
{% endhint %}

### Example - Adding a Popup

The following code can be used to display a popup to either ModalRoot or PopupRoot depending on the `isModal` value.

```csharp
// assuming popupButton is valid:
popupButton.Click += (_,_) =>
{
    ShowPopup(isModal:true);
};
// Define ShowPopup
private void ShowPopup(string text, bool isModal)
{
    
private void ShowPopup(string text, bool isModal)
{
    var container = isModal ? 
        FrameworkElement.ModalRoot : FrameworkElement.PopupRoot;

    var button = new Button();
    button.Text = "Close Me";
    button.Width = 200;
    button.Height = 200;
    var buttonVisual = button.Visual;
    buttonVisual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
    buttonVisual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
    buttonVisual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
    buttonVisual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
    container.Children.Add(button.Visual);
    button.Click += (_, _) =>
    {
        buttonVisual.RemoveFromManagers();
        buttonVisual.Parent = null;
    };
}


```

<figure><img src="../../../../../.gitbook/assets/31_06 02 52.gif" alt=""><figcaption><p>Modal popup button blocks all other UI when it is shown</p></figcaption></figure>

### Example - Adding a Popup from Gum Element

Popups can also be created if your game is loading a Gum project. Since the GraphicalUiElement will be added to either ModalRoot or PopupRoot, it should not also be added to managers.

```csharp
// Don't add to managers because it will be contained in a container
// which has already been added to managers
bool addToManagers = false;
var popupComponent = gumProject.Components.First(item => item.Name == "MyPopup")
    .ToGraphicalUiElement(SystemManagers.Default, addToManagers);

popupComponent.Parent = FrameworkElement.ModalRoot;

// later, the popup can be removed:
popupComponent.RemoveFromManagers();
popupComponent.Parent = null;
```

If you are going to add a Screen to a ModalRoot, then the Screen must have a renderable contained object so that it can have its Parent assigned. You can do this by creating a Screen runtime which inherits from ContainerBase, or you can optionally add an InvisibleRenderable as shown in the following code:

```csharp
// Don't add to managers because it will be contained in a container
// which has already been added to managers
bool addToManagers = false;
var popupScreen = gumProject.Screens.First(item => item.Name == "MyScreen")
    .ToGraphicalUiElement(SystemManagers.Default, addToManagers);
// Give the Screen a ContainedObject so that it can have its parent assigned
popupScreen.SetContainedObject (new InvisibleRenderable());
popupScreen.Parent = FrameworkElement.ModalRoot;

// later, the popup can be removed:
popupScreen.RemoveFromManagers();
popupScreen.Parent = null;
```
