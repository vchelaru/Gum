# Bottom-to-Top Stacking

## Introduction

By default a ScrollViewer stacks its top-to-bottom. We can modify stack panel to support bottom-up stacking, such as if used for a chat display or console-style output.

## Creating the Layout

The ScrollViewer's inner pane, obtained through the name "InnerPanelInstance" stacks its children top-to-bottom by default. Furthermore, then panel expands in response to its children.

The following visualization shows the size of a panel as children are added with a red rectangle:

<figure><img src="../../../.gitbook/assets/24_05 58 25.gif" alt=""><figcaption><p>Red rectangle shows the size of the inner panel</p></figcaption></figure>

We want to make the following modifications to our panel:

1. If the inner panel is smaller than the entire ScrollViewer, then the inner panel should fill the entire ScrollViewer vertically
2. All children in the inner panel should be bottom-aligned rather than top-aligned

We will follow a similar pattern to the Bottom-Up Stack example. Specifically we will create another panel that we can place inside the inner panel, and this bottom-up panel will be docked to the bottom.

The following code shows how to achieve this:

```csharp
ScrollViewer scrollViewer;

protected override void Initialize()
{
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.Newest);


    var panel = new StackPanel();
    panel.AddToRoot();
    panel.Anchor(Anchor.Center);

    // wrapped ScrollViewer
    scrollViewer = new ScrollViewer();
    panel.AddChild(scrollViewer);
    scrollViewer.Width = 400;
    var innerPanel = scrollViewer.GetVisual("InnerPanelInstance");

    var bottomAlignedContainer = new ContainerRuntime();
    innerPanel.AddChild(bottomAlignedContainer);
    bottomAlignedContainer.Dock(Dock.Bottom);
    bottomAlignedContainer.Height = 0;
    bottomAlignedContainer.HeightUnits = DimensionUnitType.RelativeToChildren;
    bottomAlignedContainer.ChildrenLayout = ChildrenLayout.TopToBottomStack;

    var button = new Button();
    panel.AddChild(button);
    button.Text = "Add Label";
    button.Click += (sender, args) =>
    {
        var label = new Label();
        bottomAlignedContainer.AddChild(label);
        label.Text = $"Added at " + DateTime.Now.ToString("hh:mm:ss.ff");
        FillAndExpandVertically(innerPanel);
        scrollViewer.ScrollToBottom();
    };
}

void FillAndExpandVertically(GraphicalUiElement visual)
{
    // let it expand...
    visual.HeightUnits = DimensionUnitType.RelativeToChildren;

    // then measure:
    if(visual.GetAbsoluteHeight() > visual.Parent.GetAbsoluteHeight())
    {
        visual.HeightUnits = DimensionUnitType.RelativeToChildren;
    }
    else
    {
        visual.HeightUnits = DimensionUnitType.RelativeToParent;
    }
}
```

<figure><img src="../../../.gitbook/assets/24_06 06 37.gif" alt=""><figcaption><p>Labels added to a bottom-up stack</p></figcaption></figure>
