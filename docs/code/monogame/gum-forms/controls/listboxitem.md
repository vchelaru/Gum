# ListBoxItem

### Introduction

The ListBoxItem class is used by the ListBox control for each instance added to the ListBox.Items property.

Gum Forms includes a default ListBoxItem implementation which includes a single label.

### Code Example - Adding ListBoxItems

ListBoxItems can be implicitly instantiated by adding any type of object to a ListBox. The following code creates 20 ListBoxItems, each displaying an integer.

```csharp
var listBox = new ListBox();
this.Root.Children.Add(listBox.Visual);
listBox.X = 50;
listBox.Y = 50;
listBox.Width = 400;
listBox.Height = 200;

for(int i = 0; i < 20; i++)
{
    listBox.Items.Add(i);
}
```

<figure><img src="../../../../.gitbook/assets/09_09 13 40.gif" alt=""><figcaption><p>ListBoxItems created by adding ints to an Items</p></figcaption></figure>

### ListBoxItems use ToString

By default each item in a ListBox creates a new ListBoxItem. The ListBoxItem calls ToString on the item. Some types, such as `int` have ToString methods which display expressive values.

Other types simply display their type when added. For example, the following code adds ListBoxItems which displays the type:

```csharp
for (int i = 0; i < 20; i++)
{
    var cancellationToken = CancellationToken.None;
    listBox.Items.Add(cancellationToken);
}
```

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>An object's ToString is displayed by default ListBoxItems</p></figcaption></figure>

This also includes adding Gum objects (such as Sprite). If a Sprite is added, the Sprite is not displayed, but rather the type of the Sprite is displayed, as shown in the following code:

```csharp
for (int i = 0; i < 20; i++)
{
    var sprite = new SpriteRuntime();
    // sprite.ToString() returns the Sprite's Name
    sprite.Name = "Test Sprite";
    listBox.Items.Add(sprite);
}
```

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Sprites (and all Gum objects) return their name when ToString is called</p></figcaption></figure>

