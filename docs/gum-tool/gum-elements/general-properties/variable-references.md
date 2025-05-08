# Variable References

## Introduction

`Variable References` allows any variable on an instance or component to reference other variables. These other variables can be on the same instance or component, a different instance, or even variables from a different component.

<figure><img src="../../../.gitbook/assets/image (158).png" alt=""><figcaption><p>Component setting its Height relative to its Width</p></figcaption></figure>

One common use of `Variable References` is to create a centralized style component which can be referenced throughout a Gum project.

Variables which are assigned through `Variable References` cannot be directly set on the instance - the value obtained through the reference overwrites any custom value. For example, the reference `Height = Width` results in Height being read-only and depending on Width. Note that this is using a shorthand variable assignment which is discussed in later sections.

<figure><img src="../../../.gitbook/assets/01_07 20 13.png" alt=""><figcaption><p>Height is assigned to Width, so it is read only</p></figcaption></figure>

{% hint style="warning" %}
Variable References result in the following changes

* Dynamic updating of variables whenever the _right side_ changes
* Evaluation of the variable reference in tool, explicitly setting the _left side_

As of February 2025 no runtimes support dynamic variable assignments; however any changes made in the will propgate those changes in the tool and they will appear in the games.

This limitation may change in future versions of Gum. If you need dynamic variable refernces please file an issue on GitHub or make a request in Discord.
{% endhint %}

{% hint style="info" %}
Variable references mimic the C# syntax, but provide only a subset of C# functionality. Future versions of Gum may expand supported syntax. If your project requires additional functionality, please post an issue on GitHub, or make a request in the Discord server.
{% endhint %}

## Variable Reference Syntax

`Variable References` can contain multiple lines. Each line is a separate variable reference. Each variable reference uses the following syntax:

```csharp
{Variable} = {Components or Screens}/{ComponentOrScreenName}.{Instance}.{Variable}
```

For example, to assign a Component's X value to a different component's X value, the following syntax could be used:

```csharp
X = Components/OtherComponent.X
```

### Variable Assignment Reference

X references the X value in OtherComponent. Note that X could be the X value on any component or any instance inside of a component:

```
X = Components/OtherComponent.X
```

Spaces are optional around the equals sign, but spaces are not allowed in variable names. The following lines are okay:

```csharp
Y=Components/OtherComponent.Y
Width = Components/OtherComponent.Width
```

However, the following is not allowed:

```csharp
// Spaces are not allowed, so Gum would comment this reference:
X Units = Components/OtherComponent.X Units
```

{% hint style="info" %}
Previous versions of Gum included spaces in some variable names. The public release of Gum in February 2025 has removed all spaces to simplify the syntax.

For more information, see the [Removal of Variable Spaces](../../breaking-changes/removal-of-variable-spaces.md) page.
{% endhint %}

Lines can be commented out to disable the reference. If Gum encounters a scripting error, it will automatically comment out lines as well so that you can make corrections:

```csharp
// This is considered a comment so it will not run
// Neither will this:
// X = Components/OtherComponent.Y
```

An Instance can reference its own Component's X value by using the qualified name:

```csharp
X = Components/ComponentContainingThisInstance.X
```

An instance or component can reference the variable of another instance in the same component by using the name of the instance. The name of the containing component or screen is not required if the instances are both in the same component or screen:

```csharp
Width = OtherInstanceInSameComponent.Width
```

Components and instances can reference variables that are contained within instances of other components. In this case the name of the referenced instance is appended to the qualified name of the Screen or Component:

```csharp
Red = Components/StyleComponent.PrimaryColorRectangle.Red
```

Elements and Screens inside subfolders can be referenced. The subfolder path is included with forward slashes:

```csharp
XUnits = Components/ComponentFolder/ComponentInFolder.XUnits
```

The _right side_ can be a variable in a Screen, although this isn't too common in practice:

```csharp
Green = Screens/MainMenuGum.ColoredRectangleInstance.Green
```

Similarly, variables on Standards can also be referenced. This is also quite rare, since this modifying the Standard element also has side effects on every other instance that is of the same type:

```csharp
YUnits = Standards/Text.YUnits
```

Although it's common for variables to reference the same variable on a different object (such as X being set to another object's X value), this is not a requirement. For example, the following lines are valid:

```csharp
Y = OtherInstance.X
Width = Components/OtherComponent.Height
```

Instances can reference their own variables, but these must be qualified. For example, ColoredRectangleInstance can assign its Width to equal its own Height. Note that Gum will extend shorthand code as shown in a later section:

```csharp
Width = ColoredRectangleInstance.Height
```

Variables can be assigned to constant values, essentially locking the value:

```csharp
X = 100
Text = "Hello"
Visible = true
```

Math operations can be used in variable assignments. This includes add, subtract, multiply, divide, and parenthesis to control order of operations:

```csharp
// This evaluates to 17
Y = (1 + 2) * 7 - 4
```

Math operations can reference both constant values (1, 2, 3) or other variables:

```csharp
Width = (OtherInstance.Height * 3) + 10
```

{% hint style="warning" %}
Gum treats the prefixes `Components/`, `Screens/`, and `Standards/` as special prefixes and does not consider this to be a division operator. Therefore, you should not name variables Components, Screens, or Standards as these are reserved words. Other variable references can freely use the forward slash character to create division.

For example, the following two lines of code show the difference between a simple variable reference assignment and a division operation:

```csharp
// This is a direct assignment setting X to 
// the X value on Components/OtherComponent:
X = Components/OtherComponent.X
// This is a division operation setting X to
// the result of dividing the CustomVariable by OtherComponent.X
X = CustomVariable/OtherComponent.X
```
{% endhint %}

Numeric variable types can be mixed. For example, Red is typically an `int` value (whole number), while X is a `float` (supports decimals). Gum automatically casts the variable appropriately:

```csharp
X = ColoredRectangleInstance.Red
Green = ColoredRectangleInstance.Y
```

Gum automatically casts any value to a `string`  (text). For example, a Text's Text variable could be assigned to its own Y value:

```csharp
Text=TextInstance.Y
```

Strings can be _concatenated_ (combined) using the + operator:

```csharp
Text="My Position is " + TextInstance.X + ", " + TextInstance.Y
```

Gum cannot convert unrelated types, including different Units. For example, the following would be commented out by Gum:

```csharp
//WidthUnits = TextInstance.HeightUnits
```

### Referencing Custom Variables

Variable references can include custom variables, both on the left and right side. Custom variables can be combined with variable references to create flexible layouts. For more information on using custom variables, see the [Add Variables page](../../variables/add-variable.md).

### Unqualified and Shorthand Assignments

As mentioned above, fully qualified assignments are allowed in any context. For example, a component can reference its own qualified variables:

```csharp
X = Components/SameComponent.Y
```

Components do not need to qualify their own variables. They can reference them without any qualification. Therefore, the following variable reference is equivalent to the fully-qualified reference above:

```csharp
X = Y
```

Similarly, the following two variable references are equivalent assuming ContainedInstance is part of the component with the reference:

```csharp
Y = Components/SameComponent.InstanceInComponent.Y
// is equivalent to:
Y = InstanceInComponent.Y
```

Instances must qualify their own variables as shown in the following code:

```csharp
X = SameInstance.Y
```

Gum will automatically qualify assignments when an instance is selected. In other words, `X = Y`  gets qualified to `X = SameInstance.Y` if SameInstance is the owner of the variable. This automatic qualification makes it easy for an instance to reference its own values. The following animation shows how the `Y` and `Height` values become qualified to the instance after tabbing out of the Variable Reference text box.

<figure><img src="../../../.gitbook/assets/01_11 35 26.gif" alt=""><figcaption><p>Tabbing automatically qualifies variables to the selected instance</p></figcaption></figure>

Complex assignments are automatically qualified as well.

<figure><img src="../../../.gitbook/assets/01_11 41 15.gif" alt=""><figcaption><p>Y, Width, and Height are automatically qualified to the current instance</p></figcaption></figure>

The left side of an assignment can be omitted if referencing the same variable on another instance or component. For example, by typing `OtherInstance.YUnits` , Gum automatically expands the reference to `YUnits = OtherInstance.YUnits` .

<figure><img src="../../../.gitbook/assets/01_11 39 05.gif" alt=""><figcaption><p>OtherInstance.YUnits is automatically prefixed with the text YUnits=</p></figcaption></figure>

Note that this only works when assigning one variable directly to another variable. Complex assignments will not be prefixed.

### Color Expansion

Assigning color values is a common part of styling, so to help with this situation, Gum also expands the "Color" variable into all three components when the Variable References text box loses focus. For example, the following text can be used to assign all three values at once:

```csharp
Components/Styles.PrimaryColor.Color
```

When the Variable References box loses focus, this is expanded to the following assignments:

```csharp
Red = Components/Styles.PrimaryColor.Red
Green = Components/Styles.PrimaryColor.Green
Blue = Components/Styles.PrimaryColor.Blue
```

<figure><img src="../../../.gitbook/assets/07_08 19 47.gif" alt=""><figcaption><p>Assigning Color expands the variables automatically</p></figcaption></figure>

## Variable References in the Property Grid

As shown above, Variable References can be used to assign one variable to another. If a variable is referenced, then the variable cannot be manually assigned. The Variable Reference takes priority. For example, if an object references the Red, Green, and Blue variables, then those values cannot be manually set on the object. The values appear disabled and text indicates why they are read-only.

<figure><img src="../../../.gitbook/assets/image (20).png" alt=""><figcaption><p>Left-side variables become read-only</p></figcaption></figure>

## Obtaining a Qualified Variable Name

Typing a variable name can be tedious, especially when referencing a variable in a different Screen or Component. Qualified variable names can be obtained by right-clicking on the variable name in Gum and selecting the **Copy Qualified Variable Name** option. This can then be pasted in the Variable References box of any other object.

<figure><img src="../../../.gitbook/assets/07_08 35 17 (1).gif" alt=""><figcaption><p>Right-click to obtain the qualified name of a variable</p></figcaption></figure>

## Example - Creating Color Styles

The following example creates a Styles component which contains a color value which is referenced by objects in a MainMenu Screen.

Any component can serve as a centralized location for styling, but we use the name **Styles** by convention.

The Styles component can contain as many objects as are needed to style your project. Additional objects can be added to help indicate how things are used visually. For example, we include a Text object to indicate the red color is the **Primary Color**.

<figure><img src="../../../.gitbook/assets/image (17) (1).png" alt=""><figcaption></figcaption></figure>

The color value can be referenced by any other object including objects in different screens or components.

To add a variable reference:

1. Select the object which should have a variable reference
2. Click inside the **Variable References** text box
3. Type the variable reference. The format of the variable reference is \
   \
   `{VariableName} = {Components or Screens}/{ComponentOrScreenName}.{InstanceName}.{InstanceVariable}`\
   \
   For example, to reference the Red variable in the Styles component, the syntax is\
   \
   `Red = Components/Styles.PrimaryColor.Red`\


Since color values have three components (Red, Green, and Blue), then all three components must be referenced. In this example, the background can reference the three colors with the following assignment text:

```
Red = Components/Styles.PrimaryColor.Red
Green = Components/Styles.PrimaryColor.Green
Blue = Components/Styles.PrimaryColor.Blue
```

<figure><img src="../../../.gitbook/assets/image (18).png" alt=""><figcaption><p>Background instance referencing the Styles.PrimaryColor color values</p></figcaption></figure>

The types of the objects that contain the **Variable References** or which are being referenced do not matter. For example, a Text object could have its color values depend on the color values defined by a ColoredRectangle in the Styles component.

<figure><img src="../../../.gitbook/assets/image (19).png" alt=""><figcaption><p>TextInstance also referencing the PrimaryColor color values</p></figcaption></figure>

Once Variable References are set, the referenced instances (instances in Styles) can be changed and the changes will immediately propagate throughout the entire project.

<figure><img src="../../../.gitbook/assets/StyleUpdate.gif" alt=""><figcaption><p>Changing the source color values updates all objects referencing the Style.PrimaryColor values</p></figcaption></figure>
