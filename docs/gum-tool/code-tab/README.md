# Code Tab

## Introduction

The Code tab provides generated code for your current Gum objects. The code tab is available if a Screen or Component is selected.

If you have not yet set up code generation, you may see buttons to help you with code generation.

<figure><img src="../../.gitbook/assets/09_06 34 44.png" alt=""><figcaption></figcaption></figure>

If you have already set up code generation, or if your Gum project does not have an associated .csproj file then you will see options for code generation.

<figure><img src="../../.gitbook/assets/09_06 29 20.png" alt=""><figcaption><p>Code tab displaying generated code for the selected component</p></figcaption></figure>

This tab provides the following functionality:

* Immediate display of generated code for the selected object
* Ability to see generated code for individual instances in an object or for an entire component/screen
* Ability to generate code automatically for a variety of target platforms

If you are working with Gum in a C# environment then the Code tab can help you write Gum code.

## Viewing Generated Code

The Code tab automatically displays the selected Screen, Component, or instance. If you are using code generation just for previwing code, you should select the Manually Setu Code Generation button. This enables the modification of properties without generating any code.

## Previewing Instances

If you have a single instance selected, the preview window displays the code for creating the instance and assigning its variables. This is especially useful if you are unsure how to reproduce a particular layout in code. For example, the following image shows the generated code for a Text named TextInstance.

<figure><img src="../../.gitbook/assets/09_06 39 27.png" alt=""><figcaption><p>Generated code for a Text named TextInstance</p></figcaption></figure>

The generated code shows all of the assignments necessary to reproduce the current instance's layout. Keep in mind that only explicitly-set variables are displayed. Any default variables (variables which appear with a green background in the Variables tab) are not assigned in generated code.

## Previewing Entire Screens and Components

If a Screen or Component is selected, then an entire class for the component is displayed in the preview window. This generated code includes:

* `using` statements
* A `partial` class with the suggested name. The name may append the word "Runtime" to the Screen or Component name depending on the target platform.
* Inheritance depending on the current platform and base type.
* `enum` declaration for all categories
* Properties for each category including switch statements assigning all properties for each state
* A property for each instance in the Screen or Component
* Initialization of all variables including variables on the instances if using full code generation

## Automatic Saving of Generated Code

The Code tab supports the automatic copying of files to disk. By using this feature, C# projects can automatically stay in sync with Gum projects, eliminating the need to write custom Runtime objects.

{% hint style="info" %}
Projects should be backed up or committed to source control before enabling automatic code generation to make it easy to undo changes.
{% endhint %}

To set up code generation, either click the **Auto Setup Code Generation** button, or modify the values in the Code tab for code generation.

* For MonoGame/KNI/FNA projects, see the [tutorial on setting up code generation](../../code/getting-started/tutorials/gum-project-forms-tutorial/gum-screens.md).
* For FlatRedBall projects, code generation is automatically handled by the FlatRedBall Editor.

See the sections below for information about each code generation option.

### Code Project Root

The location of the folder containing the .csproj file. This path is used to determine where to generate the code files. Gum generates two folders at this location:

* Components
* Screens

If you would like Gum to place these folders in a subfolder rather than at the same location as your .csproj file, then you can specify a subfolder here.

If an absolute path is entered, it is saved to a relative path so that generation works for all users working on a project regardless of where a project is cloned even though it appears absolute in Gum. For example: `C:\Users\Owner\Documents\GitHub\Gum\Samples\MonoGameGumCodeGeneration\`

Since the path is saved as relative to your .gumx location, this path will break if you move your Gum project to a new location. Be sure to update this if you are moving your .gumx.

### Output Library

Select the desired Output Library, such as **MonoGame + Forms**. This should match the type of project you are developing.

* **MonoGame + Forms** is the recommended code generation if your project is using MonoGame. This code generation generates code with classes containing properties which inherit from FrameworkElement such as Button and Textbox wherever possible. If a non-forms instance (such as a Sprite or Text instance) is added to a screen or component, then code will _fall back_ to generating non-forms properties (such as SpriteRuntime or TextRuntime).
* **MonoGame (no forms, deprecated)** generates code without creating forms controls. Use this if your game does not use Forms, or if your game predates Forms support in MonoGame.
* **SkiaSharp** generates code for runtimes which use SkiaSharp for graphics, such as WPF, .NET Maui, and Silk.NET

Additional libraries may be added in the future. If your project needs support for code generation and you are using a library that is not supported, please contact the Gum team on Discord or GitHub.

### Object Instantiation Type

This option controls how much code is generated by Gum. The following options are available:

#### Reference Loaded Gum Project

This generates minimal code for access to objects. Specifically this generates:

* Instantiation of Screens and Components using a strongly-typed class
* Access to instances through strongly typed property names
* Setting of states through enums

This approach allows for the customization of Gum files without requiring full code regeneration. Games which use this type of code generation can still support modding, so long as the modified files do not remove instances or change their names. This type of code generation still requires the loading of the Gum project (.gumx and associated files).

#### Fully in Code

This option enables working in Gum to create layouts which will work fully in code without loading a .gumx file. This is especially important if you are working on a platform with limited IO access. Generated code can run faster than loading a .gumx file since it does not require file IO, XML parsing, and reflection.

For more details see the [Runtime Generation Details](runtime-generation-details.md) page.

### Project-wide Using Statements

Add the following using statement at the end of the Project-wide Using Statement box so that references to standard runtime types are found.

```csharp
using MonoGameGum.GueDeriving;
```

If you plan on creating Screens, you should also add using statements for your component runtimes

```csharp
using {YourProjectNamespace}.Components;
```

### Root Namespace

Enter the project's Root Namespace, such as `MyGame`. Gum prefixes the generated code namespace with this entered namespace.

### Append Folder to Namespace

This option controls whether the folder names of screens and components should be added to a namespace.

For example, consider a component named ButtonStandard inside the Components/Controls folder. If this option is checked (recommended), then the class is generated with the `Contrlos` folder included in the namespace:

```csharp
namespace MyProject.Components.Controls;
```

If this option is unchecked, then folders are not included in the namespace, so the generated code would include the following namespace:

```csharp
namespace MyProject.Components;
```

### Default Screen Base

Enter the type that you would like all screen runtimes to inherit from. If you're not sure what to enter, then use `Gum.Wireframe.BindableGue`. If your game uses a custom class that you have written for all screens, then use the name of that class. You can switch to custom classes later as your project grows.

Note that Gum may choose to override this base class in certain conditions including:

* If your project is using Forms
* If your screen inherits from another screen

### Generation Behavior

This controls when Gum generates code for a component.

* NeverGenerate - generated code is not saved for this component
* GenerateManually - generated code is only saved for this element if the **Generate Code** button is clicked, or if it is a requirement for another component or screen which is generated.
* GenerateAutomaticallyOnPropertyChanged - this is generated whenever a change is made to this element in Gum.
