# ListBox Items

## Introduction

ListBoxes are one of the more complex controls in Gum Forms. This document discusses how to work with and customize ListBox items.

{% hint style="info" %}
This document uses the ListBox control for consistency but most of the same code applies to ComboBox. This document mentions where ComboBox differs from ListBox.
{% endhint %}

## Default ToString Implementation

By default whenever an instance is added to the ListBox Items property, the ListBox creates a ListBoxItem internally to display the object. Notice that the following code adds integers to a ListBox's Items property which internally creates UI elements to represent the integers.

```csharp
var listBox = new ListBox();
for (int i = 0; i < 10; i++)
{
    listBox.Items.Add(i);
}
mainPanel.AddChild(listBox);
```

<figure><img src="../../../../.gitbook/assets/image (1).png" alt=""><figcaption><p>ListBox displaying integers</p></figcaption></figure>

{% tabs %}
{% tab title="Full Code" %}
{% hint style="info" %}
The following code produces similar results, although explicitly creating ListBoxItems is less common:

```csharp
var listBox = new ListBox();
for (int i = 0; i < 10; i++)
{
    var listBoxItem = new ListBoxItem();
    listBoxItem.UpdateToObject(i);
    listBox.Items.Add(listBoxItem);
}
mainPanel.AddChild(listBox);
```
{% endhint %}
{% endtab %}

{% tab title="Diff" %}
{% hint style="info" %}
The following code produces similar results, although explicitly creating ListBoxItems is less common:

<pre class="language-diff"><code class="lang-diff">var listBox = new ListBox();
for (int i = 0; i &#x3C; 10; i++)
{
<strong>+   var listBoxItem = new ListBoxItem();
</strong>+   listBoxItem.UpdateToObject(i);
+   listBox.Items.Add(listBoxItem);
}
mainPanel.AddChild(listBox);
</code></pre>
{% endhint %}
{% endtab %}
{% endtabs %}

By default the ListBoxItem displays the ToString of whatever is added to the Items property. In the case of integers, the string representation of the integer is displayed. However, if we display an object, such as information about a weapon, the default display is not very useful.

For example, consider the following code:

```csharp
//Define WeaponDefinition
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

// Later, create the ListBox:
var listBox = new ListBox();
listBox.Width = 400;
listBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
listBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
mainPanel.AddChild(listBox);
```

In this case ToString is called on WeaponDefinition, but this doesn't provide useful information to the user:

<figure><img src="../../../../.gitbook/assets/05_11 46 14.png" alt=""><figcaption><p>ToString displays the class name</p></figcaption></figure>

We can change the displayed string by modifying the ToString method in WeaponDefinition.

{% tabs %}
{% tab title="Full Code" %}
```csharp
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
    public override string ToString() => 
        $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}
```
{% endtab %}

{% tab title="Diff" %}
```diff
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
+   public override string ToString() => 
+       $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/05_11 51 53.png" alt=""><figcaption></figcaption></figure>

By implementing a custom ToString method on the WeaponDefinition class we can customize how it is displayed in the ListBox. While this is handy, it does limit us because we may want to modify ToString for other purposes such as customizing information in the debugger.

## FrameworkElementTemplate

We can customize the way WeaponDefinition is displayed without making any changes to WeaponDefinition itself. Instead we can create a new class that is responsible for converting a WeaponDefinition instance into a string to be displayed by each ListBoxItem.

To do this, we need to perform the following steps:

1. Create a new class that inherits from ListBoxItem. The purpose of this class is to convert our WeaponDefinition into a string.
2. Associate this class with our ListBox by assigning the ListBox's FrameworkElementTemplate property

The following code has been modified to create and use a WeaponDefinitionListBoxItem:

{% tabs %}
{% tab title="First Tab" %}
```csharp
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
}

public class WeaponDefinitionListBoxItem : ListBoxItem
{
    public override void UpdateToObject(object objectInstance)
    {
        if(objectInstance is WeaponDefinition definition)
        {
            var textToDisplay =
                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
            base.UpdateToObject(textToDisplay);
        }
        else
        {
            base.UpdateToObject($"Error updating to {objectInstance}");
        }
    }
}

// Later, create the ListBox
var listBox = new ListBox();
listBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
//...
```
{% endtab %}

{% tab title="Diff" %}
```diff
class WeaponDefinition
{
    public string Name { get; set; }
    public int DamageDealt { get; set; }
    public int RequiredLevel { get; set; }
    public int Price { get; set; }
-   public override string ToString() => 
-       $"{Name} {DamageDealt}dmg requires lvl{RequiredLevel}";
}

+public class WeaponDefinitionListBoxItem : ListBoxItem
+{
+    public override void UpdateToObject(object objectInstance)
+    {
+        if(objectInstance is WeaponDefinition definition)
+        {
+            var textToDisplay =
+                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
+            base.UpdateToObject(textToDisplay);
+        }
+        else
+        {
+            base.UpdateToObject($"Error updating to {objectInstance}");
+        }
+    }
+}

// Later, create the ListBox
var listBox = new ListBox();
+listBox.FrameworkElementTemplate =
+    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
//...
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/05_11 51 53 (1).png" alt=""><figcaption><p>WeaponDefinitionListBoxItem displaying custom text</p></figcaption></figure>

This code example creates a ListBoxItem named WeaponDefinitionListBoxItem. As the name suggests, it is specifically created to display WeaponDefinition instances. Of course, you could create a more generalized version of this class which might handle a variety of different types of items.

## Customizing ComboBox Text

As mentioned above, the ComboBox control is similar to ListBox. For example, we can change the type from ListBox to ComboBox to get nearly identical behavior as shown in the following code block:

```csharp
var comboBox = new ComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
comboBox.Width = 400;
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Dagger",
    DamageDealt = 5,
    RequiredLevel = 1,
    Price = 10
});
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Sword",
    DamageDealt = 10,
    RequiredLevel = 2,
    Price = 20
});
comboBox.Items.Add(new WeaponDefinition
{
    Name = "Axe",
    DamageDealt = 15,
    RequiredLevel = 3,
    Price = 30
});
mainPanel.AddChild(comboBox);
```

This code compiles and works mostly the way we want it, but not perfectly. Notice that WeaponDefinition's ToString is still called to display the main text on the combo box.

<figure><img src="../../../../.gitbook/assets/05_12 30 17.gif" alt=""><figcaption><p>ComboBox displaying WeaponDefinition ToString</p></figcaption></figure>

Fortunately we can create a class that is derived from ComboBox which overrides the UpdateToObject method just like we did earlier for ListBoxItem.

```csharp
public class WeaponDefinitionComboBox : ComboBox
{
    public override void UpdateToObject(object objectInstance)
    {
        if (objectInstance is WeaponDefinition definition)
        {
            var textToDisplay =
                $"{definition.Name} {definition.DamageDealt}dmg requires lvl{definition.RequiredLevel}";
            base.UpdateToObject(textToDisplay);
        }
        else
        {
            base.UpdateToObject($"Error updating to {objectInstance}");
        }
    }
}
```

Now we can use this new ComboBox-deriving class:

{% tabs %}
{% tab title="Full Code" %}
```csharp
var comboBox = new WeaponDefinitionComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
// ...
```
{% endtab %}

{% tab title="Diff" %}
```diff
-var comboBox = new ComboBox();
+var comboBox = new WeaponDefinitionComboBox();
comboBox.FrameworkElementTemplate =
    new FrameworkElementTemplate(typeof(WeaponDefinitionListBoxItem));
// ...
```
{% endtab %}
{% endtabs %}



<figure><img src="../../../../.gitbook/assets/05_12 39 59.gif" alt=""><figcaption></figcaption></figure>

## SelectedObject

Once items are added to a ListBox, the selection can be accessed through the SelectedObject property. For example, we could handle equipping the selected weapon using code similar to the following block:

```csharp
var equipButton = new Button();
equipButton.Click += (_, _) =>
{
    var weapon = listBox.SelectedObject as WeaponDefinition;
    if(weapon != null)
    {
        EquipWeapon(weapon);
    }
};
mainPanel.AddChild(equipButton);
```

Keep in mind that the ListBoxItem is responsible for converting what was added to the ListBox Items property into a string, but the Items and SelectedObject property are still referencing the WeaponDefinition instances.

## Conclusion

This document shows how to work with ListBox and ComboBox Items. A future tutorial discusses how to further customize the appearance of ListBoxItems.

The next tutorial covers how to use various input hardware including the mouse, keyboard, and gamepad with Gum UI.
