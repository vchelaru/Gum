# ListBoxItem

## Introduction

Customized ListBoxItems can be added to ListBox instances. This can be performed by creating a template or by directly adding ListBox instances to a ListBox.&#x20;

## Code Example: Setting ListBox VisualTemplate

```csharp
var listBox = new ListBox();
listBox.AddToRoot();
listBox.Anchor(Anchor.Center);

listBox.VisualTemplate = new Gum.Forms.VisualTemplate(() =>
{
    var listBoxItem = new ListBoxItem();
    var visual = (ListBoxItemVisual)listBoxItem.Visual;

    // Values that are not affected by states can be set:
    visual.TextInstance.FontScale = 2;

    // Values that are affected by states must be set
    // through the states:
    visual.States.Highlighted.Clear();
    visual.States.Highlighted.Apply = () =>
    {
        visual.Background.Color = Color.Orange;
        visual.Background.Visible = true;
    };

    visual.States.Selected.Clear();
    visual.States.Selected.Apply = () =>
    {
        visual.Background.Color = Color.Yellow;
        visual.Background.Visible = true;
    };

    return visual;
});

for(int i = 0; i < 10; i++)
{
    listBox.Items.Add(i);
}
```

<figure><img src="../../../../.gitbook/assets/05_08 48 44.gif" alt=""><figcaption><p>ListBox with modified ListBoxItem highlight and text scale</p></figcaption></figure>
