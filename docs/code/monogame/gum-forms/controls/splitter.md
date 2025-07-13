# Splitter

## Introduction

The Splitter control can be used to control the size of two controls, usually in a StackPanel. The user can push+drag a Splitter to change control sizes.

## Code Example: Changing ListBox Sizes with a Splitter

The following code creates two ListBoxes with a Splitter inbetween them. The Splitter can be used to adjust the ListBox sizes.

```csharp
var stackPanel = new StackPanel();
stackPanel.Spacing = 1;
stackPanel.AddToRoot();

var listBox = new ListBox();
stackPanel.AddChild(listBox);
for(int i = 0; i < 10; i++)
{
    listBox.Items.Add("List Item " + i);
}

var splitter = new Splitter();
stackPanel.AddChild(splitter);
splitter.Dock(Dock.FillHorizontally);
splitter.Height = 5;

var listBox2 = new ListBox();
stackPanel.AddChild(listBox2);
for (int i = 0; i < 10; i++)
{
    listBox2.Items.Add("List Item " + i);
}

```

<figure><img src="../../../../.gitbook/assets/13_09 55 13.gif" alt=""><figcaption></figcaption></figure>
