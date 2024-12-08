# Strongly Typed Components Using Code Generation

### Introduction

The types used in the previous tutorials fall into two categories:

* Standard runtime types like TextRuntime
* Forms types like Button

Games often need to interact with custom components which do not fall into either of these two categories. This tutorial shows how to create custom classes, which we refer to as _runtime types_ for custom components.

### Creating a Component

For this tutorial we'll create a component which can be used to display score. To do this:

1. Create a new component called ScoreComponent
2. Drag+drop a NineSlice for the background into ScoreComponent
3. Drag+drop a text for the "Score:" label into ScoreComponent
4. Drag+drop a text for the score value into ScoreComponent
5. Rearrange the instances so the two Text instances can be clearly seen as shown in the following image:

<figure><img src="../../../.gitbook/assets/image (107).png" alt=""><figcaption><p>Score component in Gum</p></figcaption></figure>

### Interacting with Custom Components in code

If we add an instance of this component to our screen, we can interact with it as shown in code. First, we need to drag+drop an instance of the ScoreComponent into our Screen.

<figure><img src="../../../.gitbook/assets/image (108).png" alt=""><figcaption><p>ScoreComponentInstance in TitleScreen</p></figcaption></figure>

One way to intearct with this element is to call GetGraphicalUiElementByName. You do not need to do this in your code, this is provided simply as an example of how we might interact with it without strongly typed classes:

```csharp
var scoreComponentInstance = 
    Root.GetGraphicalUiElementByName("ScoreComponentInstance");
var scoreValue = 
    scoreComponentInstance.GetGraphicalUiElementByName("ScoreValue") as TextRuntime;
scoreValue.Text = "999";
```

<figure><img src="../../../.gitbook/assets/image (109).png" alt=""><figcaption><p>Score displaying a value of 999</p></figcaption></figure>

Although this code is functional, it can be difficult to maintain in a larger project. We are relying on values like `"ScoreValue"` to find the text object. If we spell this wrong, or if we change the name of our Text in Gum, this code breaks. Also, this code is quite verbose and it can be difficult to write from memory.

We can use strongly-typed classes to solve these problemsl

### Enabling Code Generation

The Gum tool supports code generation which allows us to interact with Gum components without needing to cast or use string names. We can enable code gen in Gum by checking the check boxes in the Code tab. Also, be sure to switch the Output Library to MonoGame.

<figure><img src="../../../.gitbook/assets/image (110).png" alt=""><figcaption><p>Enabling MonoGame gum</p></figcaption></figure>

This produces a fully-generated class named ScoreComponentRuntime. In this case, the code is using **FullyInCode** instantiation type, which means the generated code shows the code necessary to create this component without loading the gum project. If you would like to use generated code without loading a Gum file, then this approach might be useful. Usually this approach is useful if you would like to avoid file IO or if you cannot read from disk (such as on an embedded device).

Since our project loads from disk, we'll switch to using **FindByName** as our Object Instantiation Type. By switching to this Object Instantiation Type, the generated code is modified to look for instances by name.

The code should look similar to the following:

```csharp
public partial class ScoreComponentRuntime
{
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime ScoreLabel { get; protected set; }
    public TextRuntime ScoreValue { get; protected set; }

    public ScoreComponentRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {


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

### Using Generated Code

We can enable automatic saving of generated code in Gum by specifying the location of our .csproj file. To do this, set Code Project Root to the full path of folder containing your .csproj:

<figure><img src="../../../.gitbook/assets/image (112).png" alt=""><figcaption><p>.csproj location as Code Project Root</p></figcaption></figure>

We can force code generation once by clicking the Generate Code button.

<figure><img src="../../../.gitbook/assets/image (113).png" alt=""><figcaption><p>Generate Code creates the new code files</p></figcaption></figure>

This creates two code files - a generated code file and a custom code file:

* The generated code file contains the same code as is shown in the code generation preview.
* The custom code file can be used to customize the runtime class

<figure><img src="../../../.gitbook/assets/image (114).png" alt=""><figcaption><p>Runtime classes for ScoreComponent</p></figcaption></figure>

This component can be used in code as shown in the following snippet:

```csharp
var scoreComponentInstance = Root.GetGraphicalUiElementByName("ScoreComponentInstance")
    as ScoreComponentRuntime;
scoreComponentInstance.ScoreValue.Text = "999";
```

Note that all screen and components default to a Generation Behavior of GenerateAutomaticallyOnPropertyChange. This means that any changes result in re-generation of the .Generated.cs file.

<figure><img src="../../../.gitbook/assets/image (115).png" alt=""><figcaption></figcaption></figure>

### Screen Runtimes

We can also create runtimes for screens. Once properties have been set up, adding additional runtimes is much easier. In fact, clicking Generate Code button the produces generated code for the screen.

<figure><img src="../../../.gitbook/assets/image (116).png" alt=""><figcaption><p>Generate code button creates runtime classes for the selected Screen</p></figcaption></figure>

<figure><img src="../../../.gitbook/assets/image (117).png" alt=""><figcaption><p>TitleScreenRuntime code in Visual Studio</p></figcaption></figure>

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

### Conclusion

This tutorial shows how to generate types for screens and components which are used automatically when loading a Gum project.

The next tutorial shows how to work with multiple screens.
