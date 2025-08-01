# Strongly Typed Components Using Code Generation

{% hint style="danger" %}
This tutorial series represents the old way to add a .gumx project to your MonoGame project. This tutorial was  retired in April 2025, replaced by the new [Gum Project Forms Tutorial](../gum-project-forms-tutorial/).

This tutorial is still syntactically valid but it is not recommended as of the April 2025 release:

[https://github.com/vchelaru/Gum/releases/tag/Release\_April\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_April_27_2025)
{% endhint %}

## Introduction

The types used in the previous tutorials fall into two categories:

* Standard runtime types like TextRuntime
* Forms types like Button

Games often need to interact with custom components which do not fall into either of these two categories. This tutorial shows how to create custom classes, which we refer to as _runtime types_ for custom components.

{% hint style="info" %}
This tutorial does not require any of the instances from the previous tutorial. It assumes that you still have a Gum project and that you have set up your Game class to include the necessary Initialize, Draw, and Update calls.

If you would like a simpler starting point, feel free to delete all content in your TitleScreen in Gum, and feel free to delete all code aside from the bare minimum for your project.

For a full example of what your Game code might look like, see the start of the [Gum Forms](../../gum-forms/#introduction) tutorial.
{% endhint %}

## Creating a Component

For this tutorial we'll create a component which can be used to display score. To do this:

1. Create a new component called ScoreComponent
2. Drag+drop a NineSlice for the background into ScoreComponent
3. Drag+drop a text for the "Score:" label into ScoreComponent
4. Drag+drop a text for the score value into ScoreComponent
5. Rearrange the instances so the two Text instances can be clearly seen as shown in the following image:

<figure><img src="../../../../.gitbook/assets/image (107).png" alt=""><figcaption><p>Score component in Gum</p></figcaption></figure>

The exact layout of your component does not need to match the layout displayed in the image above. If you are new to Gum and would like to learn more about creating components, see the [Components tutorial](../../../../gum-tool/tutorials-and-examples/intro-tutorials/components.md).

The NineSlice in the image above may look different than your default NineSlice. You can modify its appearance by changing its texture coordinates:

1. Select NineSliceInstance
2. Click the Texture Coordinates Tab
3. Check the Snap to Grid option
4. Set the grid snapping value to 8
5. Drag the Texture Coordinates box to the desired region

<figure><img src="../../../../.gitbook/assets/09_05 00 58.gif" alt=""><figcaption><p>Dragging the Texture Coordinates rectangle</p></figcaption></figure>

For more information on working with texture coordinates, see the NineSlice [Texture Left](../../../../gum-tool/gum-elements/nineslice/texture-left.md) and [Texture Top](../../../../gum-tool/gum-elements/nineslice/texture-top.md) pages.

## Interacting with Custom Components in code

If we add an instance of this component to our screen, we can interact with it as shown in code. First, we need to drag+drop an instance of the ScoreComponent into our Screen.

<figure><img src="../../../../.gitbook/assets/image (108).png" alt=""><figcaption><p>ScoreComponentInstance in TitleScreen</p></figcaption></figure>

One way to interact with this element is to call GetGraphicalUiElementByName. You do not need to do this in your code, this is provided simply as an example of how we might interact with it without strongly typed classes:

```csharp
var scoreComponentInstance = 
    screenRuntime.GetGraphicalUiElementByName("ScoreComponentInstance");
var scoreValue = 
    scoreComponentInstance.GetGraphicalUiElementByName("ScoreValue") as TextRuntime;
scoreValue.Text = "999";
```

<figure><img src="../../../../.gitbook/assets/image (109).png" alt=""><figcaption><p>Score displaying a value of 999</p></figcaption></figure>

Although this code is functional, it can be difficult to maintain in a larger project. We are relying on values like `"ScoreValue"` to find the text object. If we spell this wrong, or if we change the name of our Text in Gum, this code breaks. Also, this code is quite verbose and it can be difficult to write from memory.

We can use strongly-typed classes to solve these problems.

## Enabling Code Generation

The Gum tool supports code generation which allows us to interact with Gum components without needing to cast or use string names. We can enable code gen in Gum by checking the check boxes in the Code tab. Also, be sure to switch the Output Library to MonoGame.

<figure><img src="../../../../.gitbook/assets/image (110).png" alt=""><figcaption><p>Enabling MonoGame gum</p></figcaption></figure>

This produces a fully-generated class named ScoreComponentRuntime. In this case, the code is using **FullyInCode** instantiation type, which means the generated code shows the code necessary to create this component without loading the gum project. If you would like to use generated code without loading a Gum file, then this approach might be useful. Usually this approach is useful if you would like to avoid file IO or if you cannot read from disk (such as on an embedded device).

Since our project loads from disk, we'll switch to using **FindByName** as our Object Instantiation Type. By switching to this Object Instantiation Type, the generated code is modified to look for instances by name.

<figure><img src="../../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Set Object Instantiation Type to FindByName since we are loading our project from file at runtime</p></figcaption></figure>

The code should look similar to the following block:

```csharp
public partial class ScoreComponentRuntime
{
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime ScoreLabel { get; protected set; }
    public TextRuntime ScoreValue { get; protected set; }

    public ScoreComponentRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("ScoreComponent");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }
    }
    public override void AfterFullCreation()
    {
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        ScoreLabel = this.GetGraphicalUiElementByName("ScoreLabel") as TextRuntime;
        ScoreValue = this.GetGraphicalUiElementByName("ScoreValue") as TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}

```

## Using Generated Code

We can enable automatic saving of generated code in Gum by specifying the location of our .csproj file. To do this, set Code Project Root to the full path of folder containing your .csproj:

<figure><img src="../../../../.gitbook/assets/image (112).png" alt=""><figcaption><p>.csproj location as Code Project Root</p></figcaption></figure>

We can force code generation once by clicking the Generate Code button.

<figure><img src="../../../../.gitbook/assets/image (113).png" alt=""><figcaption><p>Generate Code creates the new code files</p></figcaption></figure>

This creates two code files - a generated code file and a custom code file:

* The generated code file contains the same code as is shown in the code generation preview.
* The custom code file can be used to customize the runtime class

See below for a more detailed discussion about generated and custom code.

<figure><img src="../../../../.gitbook/assets/image (114).png" alt=""><figcaption><p>Runtime classes for ScoreComponent</p></figcaption></figure>

This component can be used in code as shown in the following snippet:

```csharp
var scoreComponentInstance = screenRuntime.GetGraphicalUiElementByName("ScoreComponentInstance")
    as ScoreComponentRuntime;
scoreComponentInstance.ScoreValue.Text = "999";
```

Note that all screen and components default to a Generation Behavior of GenerateAutomaticallyOnPropertyChange. This means that any changes automatically result in re-generation of the .Generated.cs file.

<figure><img src="../../../../.gitbook/assets/image (115).png" alt=""><figcaption></figcaption></figure>

## Generated and Custom Code

As mentioned above, Gum code generation creates two files. These files are named using the following conventions:

* {YourScreenOrComponentName}Runtime.cs
* {YourScreenOrComponentName}Runtime.Generated.cs

The two files are each declared as `partial` which allows a single class to be defined across multiple files. This allows you to write additional code without needing to worry about it being overwritten by Gum when a generation happens. For more information on partial classes, see the partial class documentation: [https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods)

### Generated Code

As the name suggests, the Generated code (ending in .Generated.cs) contains the code generated by Gum. Generated code includes the following:

* A class name matching the name of your Screen or Component with the word Runtime appended
* Inheritance if Inheritance Location is set to In Generated Code
* Properties for all instances&#x20;
* Properties for exposed variables
* Properties for setting the current state, one per category
* Enumerations for each category type
* Initialization code for your instances
  * If using Object Instantiation Type of FindByName, then the items are assigned by searching by name
  * If using Object Instantiation Type of FullyInCode, then the items are instantiated on the spot and properties are assigned matching the variables assigned in Gum
* RegisterRuntimeType method which associates this Runtime class with its matching Gum Screen or Component

Generated code is re-generated whenever a change is made in Gum or whenever you press the **Generate Code** button.

{% hint style="danger" %}
Generated code is completely rewritten whenever a change is made or when pressing the **Generate Code** button. If you make any modifications to generated code, those will be lost the next time code is generated.

Feel free to make changes if you would like to quickly test changes, but any permanent changes should be made in Gum or in custom code.

If you have encountered a situation where you would like to change how generated code behaves, please file an issue on GitHub or contact the maintainers on Discord.
{% endhint %}

Since Gum can fully generate all generated code, you can choose whether you would like to include it in version control. By including generated code in version control, your project compiles on any new machine without needing to open Gum to perform any generation. However, if you make frequent changes to your Gum project, then you will also have a lot of generated code changes in your source control history.

{% hint style="info" %}
If you would like to exclude generated code from git, you can open your .gitignore file and add the following line:

```ignore
*.Generated.cs
```
{% endhint %}

### Custom Code

Custom code exists to allow for modifications to the generated code. Many components do not require any additional changes.

Any type of code can be written in your custom code file. Here are a few examples of the type of code you might add in custom code:

* Creating internal event handlers such as playing a sound when a button is clicked
* Exposing properties which are not generated, such as an `int` value for a health bar which in turn modifies an internal Text's Text property
* Dynamically creating instances according to game state, such as adding items to a ListBox for an inventory screen
* Updating according to a state in your game, such as querying a game options service and setting volume sliders. Similarly, events can be added to UI such as when a Slider's Value property changes and pushing the new Value to the game options service.

By default your custom code file includes a single method named `CustomInitialize` which allows you to perform initialization when the component is first created.

```csharp
partial void CustomInitialize()
{

}
```

As mentioned above, if you do not need any customization for your control, you can leave this blank. In fact, the `CustomInitialize` method is declared as partial which means you can even delete this method completely and your code will still compile and run without it.

## Using Statements in Generated Code

Gum generates using statements in the generated code file. These using statements can be modified in the Code tab. Initially Gum includes a best-guess of the type of using statements needed in your project.

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Using statements in Gum result in generated code using statements</p></figcaption></figure>

At times your generated code may include types which are not handled by using statements. If necessary you may need to add additional project-wide using statments as your Screens or Components grow.

Notice that initially Gum may be generating additonal statements which are not needed; however, as your project grows in complexity these using statements may be needed - especially if you are using  an **Object Instantiation Type** of **FullyInCode**.

## Screen Runtimes

We can also create runtimes for screens. Once properties have been set up, adding additional runtimes is much easier. Just click the Generate Code button!

<figure><img src="../../../../.gitbook/assets/image (116).png" alt=""><figcaption><p>Generate code button creates runtime classes for the selected Screen</p></figcaption></figure>

<figure><img src="../../../../.gitbook/assets/image (117).png" alt=""><figcaption><p>TitleScreenRuntime code in Visual Studio</p></figcaption></figure>

Once we have a runtime class for our Screen, we can delete code from Game1 and write it in the CustomInitialize method as shown in the following snippet:

```csharp
partial class TitleScreenRuntime : Gum.Wireframe.GraphicalUiElement
{
    partial void CustomInitialize()
    {
        ScoreComponentInstance.ScoreValue.Text = "999";
    }
}
```

## Conclusion

This tutorial shows how to generate types for screens and components which are used automatically when loading a Gum project.

The next tutorial shows how to work with multiple screens.
