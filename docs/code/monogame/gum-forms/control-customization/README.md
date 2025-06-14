# Control Customization In Code

## Introduction

Gum Forms provide fully functional controls with minimal setup. These controls can be restyled in code, either per-instance, or globally per control type. Customization can be performed in-code or in the Gum tool.

## States vs Direct Property Assignments

To customize a component, either the instances can be modified directly, such as directly changing a Text's color), or a variable can be modified through a state, such as changing a background color when the button is highlighted.

Whether you use a direct property assignment or whether you use a state depends on whether the variable should always be applied or whether it should only be applied depending on the actions the user has taken and other UI related properties like enabled/disabled.

This document covers both approaches.

{% hint style="info" %}
Note that all code in this document assumes that Gum Forms has already been initialized. For more information on setting up a code-only Gum Forms project, see&#x20;
{% endhint %}

## Directly Setting an Instance's Values

Values can be directly assigned on items within an instance. If the variable being assigned does not change in response to user interaction or in response to the control being disabled, the variable can be directly assigned instead of using states.

For example we can adjust the font color on a Button directly.

The following code shows how to modify the text color on a Button.

```csharp
var customizedButton = new Button();
customizedButton.AddToRoot();
var text = customizedButton.Visual
    .GetGraphicalUiElementByName("TextInstance") as TextRuntime;
text.Color = Color.Purple;
```

<figure><img src="../../../../.gitbook/assets/07_05 34 17.gif" alt=""><figcaption><p>A button with purple text</p></figcaption></figure>

We can see the type of objects in a control by inspecting the `Visual.Children` collection in a debugger, or by printing out the children to text output. For example, we can see that a default Button has the following items:

* ButtonBackground of type ColoredRectangleRuntime
* TextInstance of type TextRuntime
* FocusedIndicator of type RectangleRuntime

<figure><img src="../../../../.gitbook/assets/07_05 37 33.png" alt=""><figcaption><p>Button children</p></figcaption></figure>

Components are ultimately made out of the following base runtimes:

* [CircleRuntime](../../../gum-code-reference/circleruntime.md)
* [ColoredRectangleRuntime](../../../gum-code-reference/coloredrectangleruntime.md)
* [ContainerRuntime](../../../gum-code-reference/containerruntime.md)
* [NineSliceRuntime](../../../gum-code-reference/ninesliceruntime.md)
* [RectangleRuntime](../../../gum-code-reference/rectangleruntime.md)
* [SpriteRuntime](../../../gum-code-reference/spriteruntime/)
* [TextRuntime](../../../gum-code-reference/textruntime/)

For information on working with the individual components, click the links in the list above.

## Identifying State vs Direct Assignment Variables

Some variables are assigned in states on default controls. We can identify which variables are assigned in states by inspecting the category for the control type. We can add the control's `Visual.Categories` property to the debugger or output its information to text output to see this information. For example, a default Button has the following category:

* ButtonCategory
  * Enabled
  * Focused
  * Highlighted
  * HighlightedFocused
  * Pushed
  * Disabled
  * DisabledFocused

<figure><img src="../../../../.gitbook/assets/07_05 49 37.png" alt=""><figcaption><p>Default states in ButtonCategory</p></figcaption></figure>

These states modify some of the existing variables which are listed if we expand the states in the watch window. For example, if we expand Enabled, we see that the state modifies:

* ButtonBackground.Color
* FocusedIndicator.Visible

<figure><img src="../../../../.gitbook/assets/07_05 51 44.png" alt=""><figcaption><p>Variables modified by the Enabled state</p></figcaption></figure>

Since these variables are modified by our button's states, that means these variables are changed in response to button interactions.

Keep in mind that these variables are assigned only when a button is interacted with. Therefore, if we explicitly change the ButtonBackground's color when we initialize the button, this color **will apply until we hover or click on the button,** at which point our changes are overwritten by the application of the button's states.

We can see this by assigning the button background. The change shows initially but then the button reverts to the values assigned in its states when the mouse moves over the button:

```csharp
var customizedButton = new Button();
customizedButton.AddToRoot();
var background = customizedButton.Visual
    .GetGraphicalUiElementByName("ButtonBackground") as ColoredRectangleRuntime;
background.Color = Color.Orange;
```

<figure><img src="../../../../.gitbook/assets/07_06 00 50.gif" alt=""><figcaption><p>Button is initially orange, but then changes back to default colors when highlighted</p></figcaption></figure>

To apply changes to variables which are modified by state, we have two options:

1. We can modify the state so that the color is assigned whenever the state is applied
2. We can clear the variables in the states so that the states do not overwrite our values

## Customizing an Instance's States

Individual instances can be customized by modifying the built-in states which are added automatically by the default implementations.

First we'll begin with a default button as shown in the following code block:

```csharp
var customizedButton = new Button();
customizedButton.AddToRoot();
```

Notice that this button has subtle color changes when the cursor hovers over or pushes on it.

<figure><img src="../../../../.gitbook/assets/26_07 28 29.gif" alt=""><figcaption><p>Button responding to hover and push events</p></figcaption></figure>

We can customize the state by modifying the values. For example, we can change the color of the background by adding the following code:

```csharp
// ButtonCategory is the category that all Buttons must have
var category = customizedButton.Visual.Categories["ButtonCategory"];

// Highlighted state is applied when the button is hovered over
var highlightedState = category.States.Find(item => 
    item.Name == FrameworkElement.HighlightedState);
// remove all old styling:
highlightedState.Variables.Clear();
// Add the new color:
highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "ButtonBackground.Color",
    Value = Microsoft.Xna.Framework.Color.Yellow
});

// We can forcefully refresh the appearance by calling UpdateState, which is
// important if we modify the default "Enabled" state:
customizedButton.UpdateState();
```

Now the button highlights yellow instead of a lighter blue.

<figure><img src="../../../../.gitbook/assets/26_07 35 48.gif" alt=""><figcaption><p>Button highlighting yellow on hover</p></figcaption></figure>

{% hint style="info" %}
Each Forms instance creates its own Categories dictionary, so modifying the Categories on one instance does not affect the Categories on another instance.
{% endhint %}

Any property on the button or its children can be modified through states. For example, we can also change the text color and size as shown in the following code. You may need to make the button bigger so it can contain the larger text.

```csharp
// make the button bigger to hold the larger text:
customizedButton.Width = 200;
customizedButton.Height = 50;

highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.Color",
    Value = Microsoft.Xna.Framework.Color.Black
});

highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.FontScale",
    // FontScale expects a float value, so use 2.0f instead of 2
    Value = 2.0f
});

// We can forcefully refresh the appearance by calling UpdateState, which is
// important if we modify the default "Enabled" state:
customizedButton.UpdateState();
```

The button text now becomes black and is twice as big when highlighted but notice that the text changes are not undone when the cursor moves off of the button (when the Highlighted state is unset).

<figure><img src="../../../../.gitbook/assets/26_07 46 55.gif" alt=""><figcaption><p>Hover state is applied, but not undone</p></figcaption></figure>

The reason that the hover state is not unset is because all variables which are set through states persist until they are undone. Typically if you create states in the Gum tool, the Gum tool forces any state which is set in a category to also be propagated through all other states in the same category. However, when we're setting states in code, we must make sure to apply any states that we care to all other categories.

In this case we can fix the larger text by setting the TextInstance's color and FontScale back to its default:

```csharp
var enabledState = category.States.Find(item => 
    item.Name == FrameworkElement.EnabledState);
enabledState.Variables.Clear();
enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "ButtonBackground.Color",
    Value = new Microsoft.Xna.Framework.Color(0, 0, 128),
});

enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.Color",
    Value = Microsoft.Xna.Framework.Color.White
});

enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.FontScale",
    // FontScale expects a float value, so use 1.0f instead of 1
    Value = 1.0f
});

customizedButton.UpdateState();
```

<figure><img src="../../../../.gitbook/assets/26_07 50 10.gif" alt=""><figcaption><p>rEnabled state resetting text color and size</p></figcaption></figure>

## Removing Variables from States

We can also remove variables from states so that the states do not overwrite our explicitly set color. For example, if we want buttons to always be orange, we can clear the states and set the color as shown in the following code:

```csharp
var customizedButton = new Button();
customizedButton.AddToRoot();

var category = customizedButton.Visual.Categories["ButtonCategory"];
foreach(var state in category.States)
{
    state.Variables.Clear();
}

var background = customizedButton.Visual
    .GetGraphicalUiElementByName("ButtonBackground") as ColoredRectangleRuntime;
background.Color = Color.Orange;
```

<figure><img src="../../../../.gitbook/assets/09_20 16 07.gif" alt=""><figcaption><p>States removed from the ButtonCategory results in no changes when a Button is Highlighted and Pressed</p></figcaption></figure>

## Accessing Children Instances

Some Forms components contain other Forms components. For example, the ListBox component contains a scroll bar named VerticalScrollBar, which itself contains a button named UpButtonInstance. These children can be obtained by calling GetGraphicalUiElementByName. This method searches for items recursively, so it can be called at the top level object. For example, the following code can be used to obtain a reference to the UpButtonInstance's Visual.

```csharp
var listBox = new ListBox();
listBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
var scrollBarVisual = listBox.Visual.GetGraphicalUiElementByName("VerticalScrollBar");
var upButtonVisual = scrollBarVisual.GetGraphicalUiElementByName("UpButtonInstance");
// modify upButtonVisual directly, or modify its states
```

## Removing and Replacing Instances

As mentioned above, all Forms components are made of individual objects which are referenced through the `Visual.Children` property. For example, a default Button includes the following children:

* ButtonBackground of type ColoredRectangleRuntime
* TextInstance of type TextRuntime
* FocusedIndicator of type RectangleRuntime

Forms components have almost no requirements for their children unless the children are critical for the function of the component. For example, a Button can function without any of its children since the Button itself is a container which has size and which can be clicked. Therefore, we are free to remove elements from the button to achieve more flexibility in our styling.

For example, we can remove the background from a Button so it only displays its Text instance. Notice that it is still fully functional as shown by the click handler.

```csharp
var label = new Label();
label.AddToRoot();

var button = new Button();
var background = button.Visual.GetGraphicalUiElementByName("ButtonBackground");
button.Visual.Children.Remove(background);
button.AddToRoot();
button.Y = 20;
button.Click += (_, _) =>
{
    label.Text = "Clicked at " + DateTime.Now;
};
```

<figure><img src="../../../../.gitbook/assets/09_06 21 11.gif" alt=""><figcaption><p>Button with no background</p></figcaption></figure>

In this case the Button still has its default states which are attempting to set the ButtonBackground's color in response to hover and click. If a state references a missing instance then the component ignores the state value.

We are also free to add additional objects to any component by modifying its Visual. For example, we could use a NineSliceRuntime, SpriteRuntime, or even a regular RectangleRuntime for the button's background.

The following code shows how to replace the background with a RectangleRuntime so that the button has an outline instead of a filled background.

```csharp
using Gum.DataTypes;

var label = new Label();
label.AddToRoot();

var button = new Button();
var background = button.Visual.GetGraphicalUiElementByName("ButtonBackground");
button.Visual.Children.Remove(background);

// replace the background with a RectangleRuntime
var rectangle = new RectangleRuntime();
rectangle.WidthUnits = DimensionUnitType.RelativeToParent;
rectangle.Width = 0;
rectangle.HeightUnits = DimensionUnitType.RelativeToParent;
rectangle.Height = 0;
// By using the same name, we can take advantage of the existing 
// states. 
rectangle.Name = "ButtonBackground";
button.Visual.Children.Insert(0, rectangle);

// do this so it immediately updates the new background
// to the proper color
button.UpdateState();

button.AddToRoot();
button.Y = 20;
button.Click += (_, _) =>
{
    label.Text = "Clicked at " + DateTime.Now;
};
```

<figure><img src="../../../../.gitbook/assets/09_06 31 37.gif" alt=""><figcaption><p>Button with a RectangleRuntime</p></figcaption></figure>

In this example we are creating a RectangleRuntime as a replacement for the existing background, but keep in mind Gum controls can contain any children. You are free to add and remove controls to style your component as needed.

### Required Children

Some Forms components require having children of a certain type to function properly. For example, a TextBox must have a TextInstance child to be able to display its text. These children should not be removed from the the control's Visual.Children so that the control can remain functional. The following lists the children which should not be removed:

* Button
  * TextInstance of type TextRuntime - optional, but needed if the Text property is used
* CheckBox
  * TextInstance of type TextRuntime - optional, but needed if the Text property is used
* ComboBox
  * ListBoxInstance of type InteractiveGue with a Forms control of ListBox
  * TextInstance of type TextRuntime
* Label
  * TextInstance of type TextRuntime
* ListBox
  * VerticalScrollBarInstance of type InteractiveGue
  * InnerPanelInstance of type InteractiveGue
  * ClipContainerInstance of type InteractiveGue
* ListBoxItem
  * TextInstance of type TextRuntime - optional, is used to display text if it exists
* MenuItem
  * TextInstance of type TextRuntime - optional, is used to display text if it exists
* PasswordBox
  * TextInstance of type TextRuntime
  * CaretInstance of type InteractiveGue
  * SelectionInstance of type InteractiveGue
  * PlaceholderTextInstance of type TextRuntime - optional, is used to display placeholder text if it exists
* RadioButton
  * TextInstance of type TextRuntime - optional, is used to display text if it exists
* ScrollBar
  * UpButtonInstance of type InteractiveGue
  * DownButtonInstance of type InteractiveGue
  * ThumbInstance of type InteractiveGue
  * TrackInstance of type InteractiveGue
* ScrollViewer
  * VerticalScrollBarInstance of type InteractiveGue
  * InnerPanelInstance of type InteractiveGue
  * ClipContainerInstance of type InteractiveGue
* Slider
  * ThumbInstance of type InteractiveGue
  * TrackInstance of type InteractiveGue
* TextBox
  * TextInstance of type TextRuntime
  * CaretInstance of type InteractiveGue
  * SelectionInstance of type InteractiveGue
  * PlaceholderTextInstance of type TextRuntime - optional, is used to display placeholder text if it exists



## Replacing Styling Globally with Derived Classes

The example above shows how to replace styling on a single Button instance. Instead of changing each instance manually, we can create a derived class which defines its own custom styling.

The first step is to create a new class which inherits from the default Forms control. These can all be found in the DefaultVisuals page linked above.

For example, we can create a derived class which inherits from the base DefaultButtonRuntime as shown in the following code:

```csharp
using MonoGameGum.Forms.DefaultVisuals;

namespace GumFormsSample
{
    class StyledButtonRuntime : DefaultButtonRuntime
    {
        public StyledButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
            base(fullInstantiation, tryCreateFormsObject)
        {
            if(fullInstantiation)
            {
                // add styling here:
            }
        }
    }
}
```

Notice that we must implement a constructor with the same parameters as the base class - this is important because Gum calls this constructor for us when we create a Button instance and the parameter list must match.

Now we can implement our own styling inside the `if(fullInstantiation)` block. The code to implement styling here is the same as above except this object is the visual, so the category exists on `this`. For example, we can make the background pink when highlighted and enabled as shown in the following code:

```csharp
if(fullInstantiation)
{
    var category = this.Categories["ButtonCategory"];

    var highlightedState = category.States.Find(item => 
        item.Name == FrameworkElement.HighlightedState);
    highlightedState.Variables.Clear();
    highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
    {
        Name = "ButtonBackground.Color",
        Value = new Microsoft.Xna.Framework.Color(255,0,191)
    });

    var enabledState = category.States.Find(item => 
        item.Name == FrameworkElement.EnabledState);
    enabledState.Variables.Clear();
    enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
    {
        Name = "ButtonBackground.Color",
        Value = new Microsoft.Xna.Framework.Color(255, 100, 194),
    });
    
    this.Updatestate();
}
```

Next we need to tell Gum Forms to use our new button as the default Button type. We can do this by replacing the default type associated with `Button` as shown in the following code. This code should go in Game1 right after `Gum.Initialize(this);`, before instantiating any Buttons as shown in the following code:

```csharp
protected override void Initialize()
{
    Gum.Initialize(this);

    FrameworkElement.DefaultFormsComponents[typeof(Button)] = 
        typeof(StyledButtonRuntime);
    //...
```

You may want to also remove or comment out any code for customizing the button explicitly below otherwise your pink styling may get overwritten on the particular instance by the styling written earlier in this tutorial.

By instantiating a `Button`, Forms automatically uses your new `StyledButtonRuntime` resulting in the button appearing pink.

```csharp
var customizedButton = new Button();
customizedButton.AddToRoot();
```

<figure><img src="../../../../.gitbook/assets/27_06 45 57.gif" alt=""><figcaption><p>Pink button using styling defined in StyledButtonRuntime</p></figcaption></figure>

## Defining a Custom Element (Without Inheritance)

This section shows how to create an element without inheriting from the default control type. This approach provides ultimate styling flexibility. Since Gum visuals are regular Gum objects, you can achieve even more customization by creating a new class which inherits from InteractiveGue and adding your own visuals. For example, you can add your own instances to a Button (such as additional Text instances) or replace existing instances with your own (such as replacing the background ColoredRectangleRuntime with a NineSlice or Sprite).

For example, we can create a NineSlice that uses the following image:

<figure><img src="../../../../.gitbook/assets/button_square_gradient.png" alt=""><figcaption><p>Blue NineSlice image</p></figcaption></figure>

Download this file to your game's Content folder, and mark the file as Copy if Newer. Make sure it appears in Visual Studio. For more information on file loading, see the [File Loading](../../file-loading.md) tutorial.

<figure><img src="../../../../.gitbook/assets/27_06 58 31.png" alt=""><figcaption><p>button_square_gradient.png in Visual Studio marked as Copy if newer</p></figcaption></figure>

Now we can create a new Button which inherits from InteractiveGue rather than DefaultButtonRuntime to fully customize its appearance. Note that the only requirement that Buttons have is that they contain a Text object named TextIntance, so we should copy this instance from the DefaultButtonRutime code into our own. Our new Button also has a NineSlice which references the button\_square\_gradient.png from above, and states for Enabled and Pressed.

In general when creating your own Forms control, it can be helpful to reference the existing Default implementation for the control you are creating.

```csharp
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace GumFormsSample
{
    internal class FullyCustomizedButton : InteractiveGue
    {
        public TextRuntime TextInstance { get; private set; }
        public FullyCustomizedButton(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.Width = 128;
                this.Height = 32;

                var background = new NineSliceRuntime();
                background.Width = 0;
                background.Height = 0;
                background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                background.Name = "ButtonBackground";
                // This depends on the current RelativeDirectory. Typically the RelativeDirectory
                // is set to Content since that's where the Gum project lives. You may need to adjust
                // your SourceFileName to account for the relative directory.
                background.SourceFileName = "button_square_gradient.png";
                this.Children.Add(background);

                // TextInstance is copied as-is from DefaultButtonRuntime.cs
                TextInstance = new TextRuntime();
                TextInstance.X = 0;
                TextInstance.Y = 0;
                TextInstance.Width = 0;
                TextInstance.Height = 0;
                TextInstance.Name = "TextInstance";
                TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
                this.Children.Add(TextInstance);


                var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                buttonCategory.Name = "ButtonCategory";
                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.EnabledState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Microsoft.Xna.Framework.Color(255, 255, 255),
                        }
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.HighlightedState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Microsoft.Xna.Framework.Color(230, 230, 230),
                        }
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = FrameworkElement.PushedState,
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Microsoft.Xna.Framework.Color(128, 128, 128),
                        }
                    }
                });

                this.AddCategory(buttonCategory);
            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new Button(this);
            }
        }
        public Button FormsControl => FormsControlAsObject as Button;

    }
}

```

Of course we also need to tell Forms to use our new class as the default button type:

```csharp
FrameworkElement.DefaultFormsComponents[typeof(Button)] =
    typeof(FullyCustomizedButton);
```

Now we can run our game and see the button in action:

<figure><img src="../../../../.gitbook/assets/27_07 13 21.gif" alt=""><figcaption><p>Button using a NineSlice</p></figcaption></figure>

As mentioned above, you are free to add any controls to your custom button including icons, additional Text instances, and additional Sprites for other effects. You can customize your Forms objects to look however you want.

## Available States

Most controls in Forms share the same common states. The exception is components which can be toggled on/off such as CheckBox. For all other controls the following states exist:

* Enabled
* Disabled
* Highlighted
* Pushed
* Focused
* HighlightedFocused
* DisabledFocused

CheckBoxes append the words On, Off, and Indeterminate to the states. For example, a CheckBox can support states including:

* EnabledOn
* EnabledOff
* EnabledIndeterminate
* DisabledOn
* DisabledOff
* DisabledIndetermate
* ... and so on.

These states can be accessed through the FrameworkElement const strings, such as:

```csharp
var highlightedState = FrameworkElement.HighlightedState;
```

## Defining a Custom Runtime from Gum

Buttons can be defined fully in the Gum tool. This approach allows you to preview your visuals in the editor, and allows editing of the styles without writing any code.

Conceptually the steps are as follows:

1. Define a component for your forms type in the Gum tool
2. Add the states needed for the forms type that you are working with in the proper category
3. Define a custom runtime for the Forms control in your Visual Studio project
4. Associate the custom runtime to the forms type using the DefaultFormsComponents dictionary

This section walks you through how to create a custom Button component, and how to use this in your project. Once you understand how to create a Button component, other Forms controls can be created similarly.

#### Defining a Button Component in Gum

The first step is to define a Button component in Gum. This component can be named anything you want. For example, you may name it **Button** or **StandardButton**. You can also create components for specific purposes such as CloseButton which would be a button that closes menus.

Components defined in Gum can contain almost anything you want; however, Buttons should usually contain a TextInstance so that the Text property can assign the string on an internal Text object.

The following image shows the a component named StandardButton which contains a ColoredRectangle, a Rectangle, and a Text instance. For other controls, see the DefaultVisuals page linked above.

<figure><img src="../../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Example Button in Gum</p></figcaption></figure>

#### Adding Button States to the Component

All Gum Forms components react to various properties by assigning states. For a full list of states needed, see the DefaultVisuals page linked above.

For Buttons, we can add a ButtonCategory state. You are free to implement as many or as few states as you want. For the full list of states see above in the [Available States](./#available-states) section.

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Example of a ButtonStateCategory implementing only Normal, Highlighted, and Pushed</p></figcaption></figure>

#### Defining a Custom Runtime for the Forms Control

Once you have created a Component in your project, you need to tell your game to use this for Buttons. To do this, add the following code in your Game class:

```csharp
ElementSaveExtensions.RegisterGueInstantiationType(
    "Buttons/StandardButton", 
    typeof(MonoGameGum.Forms.DefaultFromFileVisuals.DefaultFromFileButtonRuntime));
```

#### Associate the Custom Runtime to the Forms Type

Finally, we can associate the Forms control with the runtime. For example, the following code can be used to create a StandardButtonRuntime whenever the code calls `new Button`. Note that this is not a requirement for working with Forms, but it can make testing more convenient.

```csharp
FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(StandardButtonRuntime);
```

Note that in this case, we can only associate one type of component runtime with each type of Forms control. In other words, if you write code that instantiates a new Button using the Button constructor, a StandardButtonRuntime will be created internally.

Of course, you are not required to create buttons this way - you can also create buttons by adding instances of your component in your Gum screen in the tool, or by instantiating the desired runtime.

## Modifying ListBoxItems

ListBoxItems are typically created automatically when items are added to a ListBox instance. We can modify ListBoxItems by creating a runtime object for our ListBoxItem then assigning the ListBox's VisualTemplate.

The easiest way to create a runtime object for ListBoxItem is to copy the existing DefaultListBoxItemRuntime class which can be found here: [https://github.com/vchelaru/Gum/blob/master/MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs](../../../../../MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs)

For an example of a fully customized ListBoxItem, see this example: [https://github.com/vchelaru/Gum/blob/master/Samples/GumFormsSample/GumFormsSampleCommon/CustomRuntimes/CustomListBoxItemRuntime.cs](../../../../../Samples/GumFormsSample/GumFormsSampleCommon/CustomRuntimes/CustomListBoxItemRuntime.cs)

You may want to rename the class when creating your own version. For example, you may want to name yours `CustomListBoxItemRuntime`.

Notice that the DefaultListBoxItemRuntime creates visual states beginning on this line: [https://github.com/vchelaru/Gum/blob/cc88486578636cdb46f0c3333233e54f54a75eba/MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs#L68](https://github.com/vchelaru/Gum/blob/cc88486578636cdb46f0c3333233e54f54a75eba/MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs#L68)

Once you have created your custom ListBoxItem runtime implementation, you can use it on a list box by assigning the VisualTemplate. Be sure to assign it before adding items to your ListBox, as shown in the following code:

```csharp
var listBox = new ListBox();
Root.Children.Add(listBox.Visual);
listBox.X = 0;
listBox.Y = 100;
listBox.Width = 200;
listBox.Height = 200;

// assign the template before adding new list items
listBox.VisualTemplate = 
    new MonoGameGum.Forms.VisualTemplate(() => 
        // do not create a forms object because this template will be
        // automatically added to a ListBoxItem by the ListBox:
        new CustomListBoxItemRuntime(tryCreateFormsObject:false));

for (int i = 0; i < 20; i++)
{
    listBox.Items.Add($"Custom ListBoxItem [{i}]");
}

```

{% hint style="info" %}
Notice that the code above passes a false value to the `tryCreateFormsObject` parameter. This prevents the CustomListBoxItemRuntime from creating its own Forms object that would override the default styling.
{% endhint %}

<figure><img src="../../../../.gitbook/assets/27_06 00 06.gif" alt="" width="164"><figcaption><p>Custom ListBoxItem in a ListBox</p></figcaption></figure>
