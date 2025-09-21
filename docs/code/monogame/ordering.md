# Ordering

## Introduction

The visual and logical ordering of Gum instances is controlled by the order of a child in the parent's Children list. this order can be modified at runtime to re-order children. The order of children affects:

* Draw order - children earlier in the list are drawn behind children later in the list
* Cursor interaction order - children later in the list have first priority to respond to cursor events
* Stacking order - stacked children are drawn with the first children appearing top-most or left-most depending on the stacking order
* Grid order - children in a grid occupy cells with the first child occupying the first grid

## Order With Regular Layout

Children are drawn in order that they are added to their parent, with the last child appearing on top. The following code adds buttons in a loop. Notice that the last button appears on top:

```csharp
for(int i = 0; i < 5; i++)
{
    var button = new Button();
    button.AddToRoot();
    button.X = i * 30;
    button.Y = i * 20;
    button.Text = "Button " + (i + 1);
}
```

<figure><img src="../../.gitbook/assets/17_19 47 37.gif" alt=""><figcaption><p>Overlapping buttons - the topmost receives clicks</p></figcaption></figure>

Similarly, children can be added to a control such as a window. The following code adds buttons to a Window rather than to the root:

```csharp
var window = new Window();
window.AddToRoot();
window.Width = 300;
window.Height = 300;
window.Anchor(Anchor.Center);

for (int i = 0; i < 5; i++)
{
    var button = new Button();
    window.AddChild(button);
    button.X = i * 30;
    button.Y = i * 20;
    button.Text = "Button " + (i + 1);
}
```

<figure><img src="../../.gitbook/assets/17_19 50 13.png" alt=""><figcaption><p>Overlapping buttons in a Window</p></figcaption></figure>

## Changing Order at Runtime

A child can be reordered at runtime, resulting in the visual ordering changing. This can be useful if your game includes floating windows which can be brought to the foreground. The following code creates&#x20;

```csharp
List<Window> windows = new();

protected override void Initialize()
{
    ...
    for (int i = 0; i < 4; i++)
    {
        CreateWindow(100 + i * 50, 100 + i*50);
    }
}

void CreateWindow(int x, int y)
{
    Window window = new Window();
    window.Width = 200;
    window.Height = 200;
    window.X = x;
    window.Y = y;
    window.AddToRoot();
    window.Visual.PushPreview += (_,_) => BringToFront(window);

    var button = new Button();
    window.AddChild(button);
    button.Anchor(Anchor.Center);
    button.Text = "Click Me";
}

private void BringToFront(Window window)
{
    var root = GumUi.Root;
    var currentIndex = root.Children.IndexOf(window.Visual);
    root.Children.Move(currentIndex, root.Children.Count-1);
}

```

<figure><img src="../../.gitbook/assets/17_19 56 22.gif" alt=""><figcaption><p>Windows being brought to font when pushed</p></figcaption></figure>

