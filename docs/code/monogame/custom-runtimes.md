# Custom Runtimes

### Introduction

Custom runtimes allow the creation of custom classes which are created when loading your Gum project. The use of custom runtimes is especially important if you are developing UI which should respond to user interactions such as clicks or if you are using Gum Forms.

In the context of Gum, a _runtime_ is a class which handles interaction with a Gum component or screen while your game is running. The term _runtime_ is used to distinguish between an object used at runtime (such as a [TextRuntime](../standard-visuals/textruntime/)), or the Gum element (such as [Text](../../gum-tool/gum-elements/text/)).

Custom runtimes can encapsulate functionality and can be used to build any time of UI including simple UI such as Buttons and Checkboxes, or more complex UI such as ListBoxes or ComboBoxes.

### Custom Runtimes GraphicalUiElement-Inheriting Classes

Custom runtimes are classes which you define which inherit from GraphicalUiElement or InteractiveGue. If your runtime needs custom logic but does not respond to user actions then you should use GraphicalUiElement as your base. If your runtime responds to user actions such as clicks, then you should use InteractiveGue as your base. If you are defining a custom runtime to be used as the Visual for Gum Forms, you should also inherit from InteractiveGue.

This tutorial uses InteractiveGue as a base since responding to clicks is the most common reason for creating a custom runtime, and such an object would be suitable for Gum Forms.

The InteractiveGue class inherits from GraphicalUiElement, but adds additional logic for raising events on common mouse actions. These actions are exposed as public events which can be subscribed internally in your InteractiveGue-inheriting class or externally per-instance. This tutorial shows how to handle events both internally and externally.

### Defining a Custom Runtime

Custom runtimes fall into one of two categories:

1. Runtimes defined fully in-code
2. Runtimes defined in a Gum project

If a custom runtime is defined fully in code, then it must instantiate its own children. Typically these instances are added in a constructor.

Alternatively if a custom runtime is loaded from a Gum project, then it should not define its children since those will come from the loaded Gum project. However, it may need to access those children. It can do so in the `AfterFullCreation` method.

### Example - Custom Runtime Defined in Gum

The following code shows how to create a custom runtime from Gum. Often times custom runtimes are used for Gum object, so using the standard 2-argument constructor and creating Gum objects is recommended as shown in the following code:

```csharp
internal class ClickableButton : InteractiveGue
{
    GraphicalUiElement textInstance;

    public ClickableButton(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base() 
    {
        // no need to do anything here, implement forms object creation in
        // AfterFullCreation
    }
    
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();

        if (FormsControl == null)
        {
            FormsControlAsObject = new Button(this);
        }
    }
    
    public Button FormsControl => FormsControlAsObject as Button;
}
```

Note that this type of control is assumed to be used as a Forms Button, so it includes the instantiation of a Forms button.

### Custom Runtimes for Screens

Custom runtime classes can be created for screens using the same approach as components. Typically custom runtimes for screens inherit from GraphicalUiElement rather than InteractiveGue since the screen itself does not need click events.

The following code is an example of a custom runtime for a screen which contains a text instance named `TextInstance`:

```csharp
class StartScreenRuntime : GraphicalUiElement
{
    public StartScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base()
    { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();

        var exposedVariableInstance = 
            GetGraphicalUiElementByName("TextInstance");
        exposedVariableInstance.SetProperty("Text", "I'm set in code");
    }
}
```

This type is registered using ElementSaveExtensions:

```csharp
ElementSaveExtensions.RegisterGueInstantiation("StartScreen",
    () => new StartScreenRuntime());
```

Calling `ToGraphicalUiElement` on the StartScreen results in an instance of the StartScreenRuntime being created.

### Associating a Gum Component to a Runtime

Once a custom runtime is defined, it needs to be associated with a Gum component. For example, consider a component named StandardButton inside a Buttons folder.

<figure><img src="../../.gitbook/assets/image (8) (1) (1).png" alt=""><figcaption><p>StandardButton in Gum</p></figcaption></figure>

Notice that the name StandardButton does not match the name ClickableButton, and this is often not desirable.

To associate StandardButton with ClickableButton, add the following code **before** calling `ToGraphicalUiElement`:

```csharp
ElementSaveExtensions.RegisterGueInstantiationType(
    "Buttons/StandardButton", 
    typeof(ClickableButton));
```

Now whenever ToGraphicalUiElement is called, all instances of StandardButton will be instantiated as a ClickableButton rather than the default GraphicalUiElement.

### Using Cursor to Enable Events

If using InteractiveGue, the built-in events are only called if DoUiActivityRecursively is called either directly on the component, or on a parent of the component. Usually DoUiActivityRecursively is called in the context of using Forms, so it is recommended to do so using the FormsUtilities object. By using FormsUtilities you can reduce the amount of code needed for interactivity.

We can add FormsUtilities to our code as shown in the following snippet:

```csharp
// Instantiate the Cursor in Initialize()
protected override void Initialize()
{
    FormsUtilities.InitializeDefaults();
    // Remainder of initialization...

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    FormsUtilities.Update(gameTime, CurrentScreen);
    // Remainder of update
}
```

After adding the FormsUtilities calls, all events on any InteractiveGue will automatically be raised so long as the InteractiveGue is part of the hierarchy owned by the CurrentScreen.

### Adding Events Per Instance

Events often need to be customized per instance. For example, a game's MainMenu screen may have buttons for starting the game, going to the options screen, or exiting the game.

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
