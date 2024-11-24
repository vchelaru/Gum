# Gum Forms

### Introduction

Gum Forms provides a collection of standardized, fully functional UI elements. MonoGame Gum includes the following types:

* Button
* CheckBox
* ComboBox
* ListBox
* ListBoxItem (used by ListBox)
* PasswordBox
* RadioButton
* ScrollView
* Slider&#x20;
* TextBox

We can use all of the types above by adding instances of components which map to these controls.

### Adding Forms Instances to a Screen

The previous tutorial showed how to add a Button instance to our screen. We can add other functional controls by drag+dropping instances into the TitleScreen.

<figure><img src="../../../.gitbook/assets/24_12 02 10.gif" alt=""><figcaption><p>Drag+dropping Forms components into the TitleScreen</p></figcaption></figure>

Our forms controls already have some functionality even before we write any code in our game.

<figure><img src="../../../.gitbook/assets/24_12 03 46.gif" alt=""><figcaption><p>Forms controls with built-in functionality</p></figcaption></figure>

### Interacting with Forms Instances

We can interact with any of the Forms instances by using `GetFrameworkElementByName`. For example, the following code can be used to add items to the ListBoxInstance:

```csharp
var listBox = Root.GetFrameworkElementByName<ListBox>("ListBoxInstance");
for(int i = 0; i < 50; i++)
{
    listBox.Items.Add("Item number " + i.ToString());
}
```

<figure><img src="../../../.gitbook/assets/24_12 10 48.gif" alt=""><figcaption><p>ListBox with 50 items</p></figcaption></figure>

Forms types such as Button are associated with Gum components based on their category. For example, the following components can be used to create Button instances.

<figure><img src="../../../.gitbook/assets/image (105).png" alt=""><figcaption><p>Multiple components create Button forms controls</p></figcaption></figure>

Although the prefix "Button" suggests that these controls are Forms Buttons, the name can change and these would still create buttons. At runtime the type of Forms control associated with a component is determined by the state categories defined in the component.

For example, each of these components has a state category named ButtonCategory.

<figure><img src="../../../.gitbook/assets/image (106).png" alt=""><figcaption><p>ButtonClose with a ButtonCategory state category</p></figcaption></figure>

Although we won't cover the details in this tutorial, you can customize the existing components or create new components which will map to the Forms types so long as they have the appropriate category.

### Additional Documentation

Forms component instances can be added and modified just like any other instance, but at runtime these types provide common properties and methods. To learn more about working with Forms in code, see the [Forms documentation](../../gum-forms/controls/).

{% hint style="info" %}
The Forms types and properties are based on the WPF syntax. Developers familiar with WPF may find that many of the same members exist in Gum Forms. However, keep in mind that Gum Forms are still using Gum for the layout engine, so any properties related to position or size follow the Gum rules rather than WPF rules.
{% endhint %}

### Conclusion

This tutorial showed how to create Forms instances in a screen, interact with them in code, and how to work with the different forms types.

The next tutorial covers how to create and destroy screens.
