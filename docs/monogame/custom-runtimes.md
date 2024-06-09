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
    TextRuntime textInstance;

    public ClickableButton() : base() 
    {
        this.Click += HandleClick;

        // All of the children (such as TextInstance) have not yet
        // been created, so we can't assign the textInstance yet.
        // See AfterFullCreation
    }

    int NumberOfClicks;

    public override void AfterFullCreation()
    {
        // The GraphicalUiElement is fully created at this point so it
        // should have access to all children such as TextInstance
        textInstance = (TextRuntime)GetGraphicalUiElementByName("TextInstance");
    }

    public void HandleClick(object sender, EventArgs args)
    {
        NumberOfClicks++;

        textInstance.Text = NumberOfClicks + " times";
    }
}
```

Notice that this assumes that the component has a TextRuntime, so your associated component must have a Text instance named TextInstance.

### Associating a Gum Component to a Runtime

Once a custom runtime is defined, it needs to be associated with a Gum component. For example, consider a component named StandardButton inside a Buttons folder.

<figure><img src="../.gitbook/assets/image (8).png" alt=""><figcaption><p>StandardButton in Gum</p></figcaption></figure>

Notice that the name StandardButton does not match the name ClickableButton, and this is often not desirable. The only thing that matters in this case is that StandardButton must have a TextInstance, as suggested by the code above.

To associate StandardButton with ClickableButton, add the following code **before** calling `ToGraphicalUiElement`:

```csharp
ElementSaveExtensions.RegisterGueInstantiationType(
    "Buttons/StandardButton", 
    typeof(ClickableButton));
```

Now whenever ToGraphicalUiElement is called, all instances of StandardButton will be instantiated as a ClickableButton rather than the default GraphicalUiElement.

### Using Cursor to Enable Events

If using InteractiveGue, the built-in events are only called if DoUiActivityRecursively is called either directly on the component, or on a parent of the component. Typically, DoUiActivityRecursively is called on the root-most object (usually a GraphicalUiElement representing a Screen).

This method requires a Cursor implementation. Therefore, the following simplified code shows the full requirements to create a Cursor, update the Cursor every frame, and call DoUiActivityRecursively. Note that this code assumes a Screen GraphicalUiElement named CurrentScreen:

```csharp
// Define the Cursor at class scope:
Cursor cursor;

// Instantiate the Cursor in Initialize()
protected override void Initialize()
{
    cursor = new Cursor();

    // Remainder of initialization...

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    cursor.Activity(gameTime.TotalGameTime.TotalSeconds)
    CurrentScreen.DoUiActivityRecursively(cursor);
    // Remainder of update
}
```

After adding the Cursor and DoUiActivityRecursively code, all events on any InteractiveGue will automatically be raised so long as the InteractiveGue is part of the hierarchy owned by the CurrentScreen.

### Adding Events Per Instance

Events often need to be customized per instance. For example, a game's MainMenu screen may have buttons for starting the game, going to the options screen, or exiting the game.&#x20;

The following code shows how to add events to runtime instances. This code assumes that each runtime instance is using a Runtime that inherits from InteractiveGue.

```csharp
// Run the following code after calling ToGraphicalUiElement
var startGameButton = (InteractiveGue)CurrentScreen.GetGraphicalUiElementByName("StartButton");
startGameButton.Click += HandleStartGameClicked;

var optionsGameButton = (InteractiveGue)CurrentScreen.GetGraphicalUiElementByName("OptionsButton");
optionsGameButton.Click += HandleOptionsClicked;

var exitGameButton = (InteractiveGue)CurrentScreen.GetGraphicalUiElementByName("ExitGameButton");
exitGameButton.Click += HandleExitGameClicked;
```
