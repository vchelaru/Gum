# Splitter

## Introduction

The Splitter control can be used to control the size of two controls, usually in a StackPanel. The user can push+drag a Splitter to change control sizes.

## Code Example: Changing ListBox Sizes with a Splitter

The following code creates two ListBoxes with a Splitter in between them. The Splitter can be used to adjust the ListBox sizes.

```csharp
// Initialize
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

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA51RPU_DMBD9KydPqVRFaRBLKgZSBK3EgCgSA2GwErc91bEr-8JHq_537NgFBYmheLjPd-_O78AW9q5rWUGmE2PWWVRry4oX5orpMxqxMrwV7HXMUCEhl7gXrGBv3IAlXm8fuBISrkCJd1h-F5LRtFI__XS547UjdrjJsHHdNE_6UWvqJzyrREul_oiU9yH7zefGZhuUTRLRvr3SJkFFgG40mzpXdVl2MYOJT3ycl8GOKnWoFLgXp9MFidZ60qRH5H4r-CKEPLgS0O85hjvtTiKRMKe_x_TPS0_4vh_j9EbX28Sb9BalnGuDe62IS_k5gM0FrjfkNl0ORcrPUymPMsE_dcrPFIodvwADbIY3XgIAAA)

<figure><img src="../../.gitbook/assets/13_09 55 13.gif" alt=""><figcaption></figcaption></figure>

## Code Example: Left/Right Split

The following code creates a horizontal StackPanel with two ListBoxes separated by a Splitter, creating a left/right split layout. The Splitter automatically uses column resizing when placed inside a horizontal StackPanel.

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.Orientation = Orientation.Horizontal;
stackPanel.Width = 500;
stackPanel.Height = 300;
stackPanel.Spacing = 1;
stackPanel.AddToRoot();

var leftListBox = new ListBox();
leftListBox.Width = 200;
leftListBox.Dock(Dock.FillVertically);
stackPanel.AddChild(leftListBox);
for (int i = 0; i < 10; i++)
{
    leftListBox.Items.Add("Left Item " + i);
}

var splitter = new Splitter();
splitter.Dock(Dock.FillVertically);
splitter.Width = 5;
stackPanel.AddChild(splitter);

var rightListBox = new ListBox();
rightListBox.Dock(Dock.FillVertically);
stackPanel.AddChild(rightListBox);
for (int i = 0; i < 10; i++)
{
    rightListBox.Items.Add("Right Item " + i);
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqVSy2rDMBC8B_IPi082KcZp6aVpD33QJhBoSUp78UXYcrxEloK86SMh_17JtR05JdBSXcTOjPYxq22_B-BNyod14V0A6TU_qRCUSMgEbriBvTemoSSWLJ-Y5AKuQPJ3mLeAH4xiuefDR41cEiNU0midKBwrjRtlItF98Yop5UZ7HkVdYsxxkZNhzg6Z-YolKBeGGnaJ6zR9VjOlqGorlrZ5wTOaYkk36qPuvo4qjcO2nZxW9VzmTiVL3_hkJJpnmhW8gsJ7FOKFa8KECfEZ_GjmNkeR-k4mK8mUBh8lAZpa0chclzC092AQxHIbSzDHrT4hXpQ2nR97U4ODBSD2YABoE-6aUcuVQCKumy3V4feO6uAvszRP2hUdGbAR7k3XdnXHXXfpf5rrpvq1u536rr2z6ssd-uv1e7sv0xYY1y4DAAA" target="_blank">Try on XnaFiddle.NET</a>
