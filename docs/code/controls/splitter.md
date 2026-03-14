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

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA51QPU_DMBD9KydPqVRFaRBLKgZSBK3EgCgSA2GwErc91bEr-8JHq_537NgFBYmheLjPd-_O78AW9q5rWUGmE2PWWVRry4oX5orpMxqxMrwV7HXMUCEhl7gXrGBv3IAlXm8fuBISrkCJd1h-F5LRtFI__XS547UjdrjJsHHdNE_6UWvqJzyrREul_oiU9yH7zefGZhuUTRLRvr3SJkFFgG40mzpXdVl2MYOJT3ycl8GOKnWoFLgXp9MFidZ60qRH5H4r-CKEPLgS0O85hjvtTiKRMKe_x_TPS0_4vh_j9EbX28Sb9BalnGuDe62IS_k5gM0FrjfkNl0ORcrPUymPMsE_dcrPFIodvwADbIY3XgIAAA)

<figure><img src="../../.gitbook/assets/13_09 55 13.gif" alt=""><figcaption></figcaption></figure>
