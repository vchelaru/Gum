# Control Customization

### Introduction

Gum Forms provide fully functional controls with minimal setup. These controls can be restyled in code, either per-instance, or globally per control type. Customization can be performed in-code or in the Gum tool.

### Customizing an Instance

Individual instances can be customized by modifying the built-in states which are added automatically by the default implementations.

{% hint style="info" %}
Note that all code in this document assumes that Gum Forms has already been initialized.
{% endhint %}

The following code shows how to modify a Button instance.  A default button can be constructed using the following code:

```csharp
var customizedButton = new Button();
this.Root.Children.Add(customizedButton.Visual);
```

Notice that this button has subtle color changes when the cursor hovers over or pushes on it.

<figure><img src="../../.gitbook/assets/26_07 28 29.gif" alt=""><figcaption><p>Button responding to hover and push events</p></figcaption></figure>

These cursor actions result in different states being applied to the button. These states are initialized by default when calling the following code:

```csharp
FormsUtilities.InitializeDefaults();
```

We can customize the state by modifying the values. For example, we can change the color of the background by adding the following code:

```csharp
// ButtonCategory is the category that all Buttons must have
var category = customizedButton.Visual.Categories["ButtonCategory"];

// Highlighted state is applied when the button is hovered over
var highlightedState = category.States.Find(item => item.Name == "Highlighted");
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

<figure><img src="../../.gitbook/assets/26_07 35 48.gif" alt=""><figcaption><p>Button highlighting yellow on hover</p></figcaption></figure>

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

<figure><img src="../../.gitbook/assets/26_07 46 55.gif" alt=""><figcaption><p>Hover state is applied, but not undone</p></figcaption></figure>

The reason that the hover state is not unset is because all variables which are set through states persist until they are undone. Typically if you create states in the Gum tool, the Gum tool forces any state which is set in a category to also be propagated through all other states in the same category. However, when we're setting states in code, we must make sure to apply any states that we care to all other categories.

In this case we can fix the larger text by setting the TextInstance's color and FontScale back to its default:

```csharp
var enabledState = category.States.Find(item => item.Name == "Enabled");
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
    // FontScale expects a float value, so use 2.0f instead of 2
    Value = 1.0f
});

customizedButton.UpdateState();
```

<figure><img src="../../.gitbook/assets/26_07 50 10.gif" alt=""><figcaption><p>Enabled state resetting text color and size</p></figcaption></figure>

### Available Instances

Each default control type is made up of individual instances, most of which are standard types such as ColoredRectangle and TextInstance. The example above assigns properties on two instances:

* ButtonBackground
* TextInstance

Since each control (such as Button, CheckBox, or TextBox) has its own individual instances, we need to know the names and types of these instances to add VariableSaves to our states.

We can find the names of these instances by looking at the source code for the default forms implementations.

These can be found in the Gum repository here:

{% embed url="https://github.com/vchelaru/Gum/tree/master/MonoGameGum/Forms/DefaultVisuals" %}
Forms DefaultVisuals Folder
{% endembed %}

For example, we can look at the DefaultButtonRuntime.cs file and notice that it has a ButtonBackground and TextInstance:

```csharp
var background = new ColoredRectangleRuntime();
// ...
background.Name = "ButtonBackground";

TextInstance = new TextRuntime();
// ...
TextInstance.Name = "TextInstance";
```

By inspecting the code of other controls we can see which instances are available for styling. We can also look at the existing states which are provided by the default implementations. For example the DefaultButtonRuntime adds states for Enabled, Highlighted, Pushed, and Disabled. Note that this list may change in the future as Gum Forms continues to be developed.

Keep in mind that the Name property of the internal instances is important. For example using the code above, the background object is accessed through "ButtonBackground" rather than "background" when creating new states.

### Replacing Styling Globally with Derived Classes

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

    var highlightedState = category.States.Find(item => item.Name == "Highlighted");
    highlightedState.Variables.Clear();
    highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
    {
        Name = "ButtonBackground.Color",
        Value = new Microsoft.Xna.Framework.Color(255,0,191)
    });

    var enabledState = category.States.Find(item => item.Name == "Enabled");
    enabledState.Variables.Clear();
    enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
    {
        Name = "ButtonBackground.Color",
        Value = new Microsoft.Xna.Framework.Color(255, 100, 194),
    });
    
    this.Updatestate();
}
```

Next we need to tell Gum Forms to use our new button as the default Button type. We can do this by replacing the default type associated with `Button` as shown in the following code. This code should go in Game1 right after `FormsUtilities.InitializeDefaults();`, before instantiating any Buttons as shown in the following code:

```csharp
protected override void Initialize()
{
    SystemManagers.Default = new SystemManagers(); 
    SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
    FormsUtilities.InitializeDefaults();

    FrameworkElement.DefaultFormsComponents[typeof(Button)] = 
        typeof(StyledButtonRuntime);
    //...
```

You may want to also remove or comment out any code for customizing the button explicitly below otherwise your pink styling may get overwritten on the particular instance by the styling written earlier in this tutorial.

By instantiating a `Button`, Forms automatically uses your new `StyledButtonRuntime` resulting in the button appearing pink.

```csharp
var customizedButton = new Button();
this.Root.Children.Add(customizedButton.Visual);
```

<figure><img src="../../.gitbook/assets/27_06 45 57.gif" alt=""><figcaption><p>Pink button using styling defined in StyledButtonRuntime</p></figcaption></figure>

### Defining a ButtonRuntime Without Inheritance

The section above shows how to customize a button using inheritance. This approach is beneificial if you would like to modify the styling colors on the existing children of the button. Since Gum visuals are regular Gum objects, you can achieve even more customization by creating a new Button class which inherits from InteractiveGue and adding your own controls. In other words, you can add your own instances to the button (such as additional Text instances) or replace existing instances with your own (such as replacing the background ColoredRectangleRuntime with a NineSlice or Sprite).

For example, we can create a NineSlice that uses the following image:

<figure><img src="../../.gitbook/assets/button_square_gradient.png" alt=""><figcaption><p>Blue NineSlice image</p></figcaption></figure>

Download this file to your game's Content folder, and mark the file as Copy if Newer. Make sure it appears in Visual Studio. For more information on file loading, see the [File Loading](../file-loading.md) tutorial.

<figure><img src="../../.gitbook/assets/27_06 58 31.png" alt=""><figcaption><p>button_square_gradient.png in Visual Studio marked as Copy if newer</p></figcaption></figure>

Now we can create a new Button which inherits from InteractiveGue rather than DefaultButtonRuntime to fully customize the appearance of our button. Note that the only requirement that Buttons have is that they contain a Text object named TextIntance, so we should copy this instance from the DefaultButtonRutime code into our own. Our new Button also has a NineSlice which references the button\_square\_gradient.png from above, and states for Enabled and Pressed.

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
                    Name = "Enabled",
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
                    Name = "Highlighted",
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
                    Name = "Pushed",
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

<figure><img src="../../.gitbook/assets/27_07 13 21.gif" alt=""><figcaption><p>Button using a NineSlice</p></figcaption></figure>

As mentioned above, you are free to add any controls to your custom button including icons, additional Text instances, and additional Sprites for other effects. You can customize your Forms objects to look however you want.

### Available States

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

### Defining a Custom Runtime from Gum

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

<figure><img src="../../.gitbook/assets/image (2) (1).png" alt=""><figcaption><p>Example Button in Gum</p></figcaption></figure>

#### Adding Button States to the Component

All Gum Forms components react to various properties by assigning states. For a full list of states needed, see the DefaultVisuals page linked above.

For Buttons, we can add a ButtonCategory state. You are free to implement as many or as few states as you want. For the full list of states see above in the [Available States](control-customization.md#available-states) section.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Example of a ButtonStateCategory implementing only Normal, Highlighted, and Pushed</p></figcaption></figure>

#### Defining a Custom Runtime for the Forms Control

Once you have created a Component in your project, you need to create a custom runtime class. This custom runtime class associates the Component in your .gumx file to a strongly-typed class. This runtime type also enables the creation of Forms controls by instantiating the runtime object, including when an element from the Gum project is converted to a GraphicalUiElement.

The runtime class does not need much code since most of the work is done in the Gum project. The following shows an example custom runtime for the StandardButton component:

<pre class="language-csharp"><code class="lang-csharp">internal class StandardButtonRuntime : InteractiveGue
{
    public StandardButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base() 
    {
        if(fullInstantiation)
        {
            // no need to do anything here, we are fully instantiated by the Gum object
        }

        // Warning - the StandardButtonRuntime children have not yet been
        // populated from Gum. Therefore, we shouldn't create the children
        // here, even if tryCreateFormsObject is set to true.
        // See AfterFullCreation below

    }

    // The Forms objects should only be created after the 
    // children have been assigned. We can override the AfterFullCreation
<strong>    // method to handle creating the Forms object after the children have 
</strong>    // been created.
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

</code></pre>

This runtime can be associated with the StandardButton component in Gum using the following code in your Game class:

```csharp
ElementSaveExtensions.RegisterGueInstantiationType(
    "Buttons/StandardButton", 
    typeof(StandardButtonRuntime));
```

For more information on using a custom runtime defined in your Gum project, see the [Custom Runtimes](../custom-runtimes.md) tutorial.

#### Associate the Custom Runtime to the Forms Type

Finally, we can associate the Forms control with the runtime. For example, the following code can be used to create a StandardButtonRuntime whenever the code calls `new Button`. Note that this is not a requirement for working with Forms, but it can make testing more convenient.

```csharp
FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(StandardButtonRuntime);
```

Note that in this case, we can only associate one type of component runtime with each type of Forms control. In other words, if you write code that instantiates a new Button using the Button constructor, a StandardButtonRuntime will be created internally.

Of course, you are not required to create buttons this way - you can also create buttons by adding instances of your component in your Gum screen in the tool, or by instantiating the desired runtime.

### Modifying ListBoxItems

ListBoxItems are typically created automatically when items are added to a ListBox instance. We can modify ListBoxItems by creating a runtime object for our ListBoxItem then assigning the ListBox's VisualTemplate.

The easeist way to create a runtime object for ListBoxItem is to copy the existing DefaultListBoxItemRuntime class which can be found here: [https://github.com/vchelaru/Gum/blob/master/MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs](../../../MonoGameGum/Forms/DefaultVisuals/DefaultListBoxItemRuntime.cs)

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

<figure><img src="../../.gitbook/assets/27_06 00 06.gif" alt="" width="164"><figcaption><p>Custom ListBoxItem in a ListBox</p></figcaption></figure>
