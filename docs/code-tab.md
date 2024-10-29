# Code Tab

### Introduction

The Code tab provides generated code for your current Gum objects.

<figure><img src=".gitbook/assets/image (90).png" alt=""><figcaption><p>Code tab displaying generated code for the selected component</p></figcaption></figure>

This tab provides the following functionality:

* Immediate display of generated code for the selected object
* Ability to see generated code for individual instances in an object or for an entire component/screen
* Ability to generate code automatically for a variety of target platforms

If you are working with Gum in a C# environment then the Code tab can help you write Gum code.

### Enabling CodeGen Preview

To enable code generation:

1. Select a Screen, Component, or instance
2. Check the **Is CodeGen Plugin Enabled** checkbox
3. Check the **Show CodeGen Preview** checkbox to display the current selection in the preview window

<figure><img src=".gitbook/assets/image (91).png" alt=""><figcaption><p>Generated code preview displayed in Gum</p></figcaption></figure>

{% hint style="info" %}
The generation of code may make selection slightly slower, especially when viewing complex screens or components. If you are experiencing performance problems, you may consider unchecking the **Show CodeGen Preview** checkbox when performing editing.
{% endhint %}

### Previewing Instances

If you have a single instance selected, the preview window displays the code for creating the instance and assigning its variables. This is especially useful if you are unsure how to reproduce a particular layout in code. For example, the following image shows the generated code for a Text named TextInstance.

<figure><img src=".gitbook/assets/image (92).png" alt=""><figcaption><p>Generated code for a Text named TextInstance</p></figcaption></figure>

The generated code shows all of the assignments necessary to reproduce the current instance's layout. Keep in mind that only explicitly-set variables are displayed. Any default (green background) variables are not assigned in generated code.

### Previewing Entire Screens and Components

If a Screen or Component is selected, then an entire class for the component is displayed in the preview window. This generated code includes:

* `using` statements
* A `partial` class with the suggested name. The name appends the word "Runtime" to the Screen or Component name
* `enum` declaration for all categories
* Properties for each category including switch statements assigning all properties for each state
* A property for each instance in the Screen or Component
* Initialization of all variables including variables on the instances

### Automatic and Manual Code Generation

The Code tab supports the automatic copying of files to disk. By using this feature, C# projects can automatically stay in sync with Gum projects, eliminating the need to write custom Runtime objects.

{% hint style="info" %}
Projects should be backed up or committed to source control before enabling automatic code generation to make it easy to undo changes.
{% endhint %}

to set up automatic code generation:

1. Enable the code generation plugin as shown above. The Show CodeGen Preview checkbox does not need to be checked.
2. Select the desired Output Library, such as MonoGame.
3. Select the Object Instantiaton type
   1. If you are planning on loading the .gumx project, select the FindByName option
   2. If you would like the entire project generated, select the FullyInCode option. This option enables working in Gum to create layouts which will work fully in code without loading a .gumx file. This is especially improtant if you are working on a platform with limited IO access. Generated code can run faster than loading a .gumx file since it does not require file IO, XML parsing, and reflection.
4. Enter the location of the project's .csproj file in the Code Project Root text box. If an absolute path is entered, it will be changed to a relative path so that generatoin works for all users working on a project regardless of where a project is cloned.
5. Enter the project's Root Namespace, such as MyGame.
6. Select the Generation Behavior
   1. Select NeverGenerate for components which should not generate to disk
   2. Select GenerateManually if you would like to generate code only when the Generate Code button is clicked.
   3. Select GenerateAutomaticallyOnPropertyChange to generate code whenever a property changed. This option is useful once you are comfortable with code generation. It results in code being generated automatically as you make edits in Gum.

