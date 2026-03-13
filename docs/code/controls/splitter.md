# Splitter

## Introduction

The Splitter control can be used to control the size of two controls, usually in a StackPanel. The user can push+drag a Splitter to change control sizes.

## Code Example: Changing ListBox Sizes with a Splitter

The following code creates two ListBoxes with a Splitter inbetween them. The Splitter can be used to adjust the ListBox sizes.

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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACpVQTU8CMRC896-Y9FSyZiObeBE9oEYh8WDEYy_NboEJZUrawQ8I_90WuxJIjNpDp_PmzZvp2xYAYhwe1ktxCezX5iwBSMioLW5MRMWr9hBYN4snTcbCNZB5g8k3IHsDRYd6NVnpBmkWef3jwrBtX9yzc7zvUJR0LQa-ce9Z9PErO1WMjbdztK3M7FSeOi-RGDC2ng9iuIJ-imXZU7RVBPFkejVmswxJRSqRRkACQAkoAZPWrtsmrCwyG9_9Mac_7tPx9_X8ru5cs5Dpqu7R2pHzuHHE2tqPI9rI4GzOcdLFqRn1_9yosx3wVz_q3w0Rxa74BA785tMbAgAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../.gitbook/assets/13_09 55 13.gif" alt=""><figcaption></figcaption></figure>
