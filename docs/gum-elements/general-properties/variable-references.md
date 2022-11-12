# Variable References

## Introduction

**Variable References** allows any variable on an instance to reference a variable on another instance. The most common use of **Variable References** is to create a centralized style component which can be referenced throughout a Gum project.

Variables which are assigned through **Variable References** cannot be directly set on the instance - the value obtained through the reference overwrites any custom value.

## Example - Creating Color Styles

The following example creates a Styles component which contains a color value which is referenced by objects in a MainMenu Screen.

Any component can serve as a centralized location for styling, but we use the name **Styles** by convention.

The Styles component can contain as many objects as are needed to style your project. Additional objects can be added to help indicate how things are used visually. For example, we include a Text object to indicate the red color is the **Primary Color**.

![](<StylesComponent.png>)

The color value can be referenced by any other object including objects in different screens or components. 

To add a varaible reference

1. Select the object which should have a variable reference
1. Click the **+** button under the **Variable References** ListBox
1. Enter the variable reference. The format of the variable reference is `{VariableName} = {Components or Screens}/{ComponentOrScreenName}.{InstanceName}.{InstanceVariable}`. For example, to reference the Red variable in the Styles component, the syntax is `Red = Components/Styles.PrimaryColor.Red`. Note that all three color values need to be referenced to match colors.

For example, the MainMenu screen contains a Background ColoredRectangle which references the PrimaryColor values.

![](<BackgroundWithStyle.png>)

The types of the objects that contain the **Variable References** or which are being referenced do not matter. For example, a Text object could have its color values depend on the color values defined by a ColoredRectangle in the Styles component.

![](<TextWithStyle.png>)

Once Variable References are set, the referenced instances (instances in Styles) can be changed and the changes will immediately propagate throughout the entire project.

![](<StyleUpdate.gif>)