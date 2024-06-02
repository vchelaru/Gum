# Custom Runtimes

### Introduction

Custom runtimes allow the creation of custom classes which are created when loading your Gum project. The use of custom runtimes is especially important if you are developing UI which should respond to user interactions such as clicks.

In the context of Gum, a _runtime_ is a class which handles interaction with a Gum component or screen while your game is running. The term _runtime_ is used to distinguish between an object used at runtime (such as a [TextRuntime](runtime-objects-graphicaluielement-deriving/textruntime.md)), or the Gum element (such as [Text](../gum-elements/text/)).

Custom runtimes can encapsulate functionality and can be used to build any time of UI including simple UI such as Buttons and Checkboxes, or more complex UI such as ListBoxes or ComboBoxes.

### Custom Runtimes GraphicalUiElement-Inheriting Classes

Custom runtimes are classes which you define which inherit from GraphicalUiElement or InteractiveGue. If your runtime needs custom logic but does not respond to user actions then you should use GraphicalUiElement as your base. If your runtime responds to user actions such as clicks, then you should use InteractiveGue as your base.

This tutorial uses InteractiveGue as a base since responding to clicks is the most common reason for creating a custom runtime.

The InteractiveGue class inherits from GraphicalUiElement, but adds additional logic for raising events on common mouse actions. These actions are exposed as public events which can be subscribed internally in your InteractiveGue-inheriting class or externally per-instance. This tutorial shows how to handle events both internally and externally.

### Defining a Custom Runtime

The only requirement to create a runtime class is to inherit from GraphicalUiElement or InteractiveGue. As mentioned earlier, we will be using InteractiveGue since it gives us access to cursor events.

The following is an example of a basic button runtime named ClickableButton:

```csharp
internal class ClickableButton : InteractiveGue
{
    GraphicalUiElement textInstance;

    public ClickableButton() : base() 
    {
        this.Click += HandleClick;
    }

    int NumberOfClicks;

    public void HandleClick(object sender, EventArgs args)
    {
        if(textInstance == null)
        {
            textInstance = GetGraphicalUiElementByName("TextInstance");
        }

        NumberOfClicks++;

        textInstance.SetProperty("Text", "Clicked " + NumberOfClicks + " times");
    }
}
```

Notice that this assumes that the component has a TextRuntime, so your associated component must have a Text instance named TextInstance.



...under construction...
