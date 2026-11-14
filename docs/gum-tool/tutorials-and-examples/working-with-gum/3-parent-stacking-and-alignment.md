# 3 - Parent, Stacking, and Alignment

### Introduction

Although it is technically possible to align UI manually by setting X and Y values, this type of layout makes it more difficult to maintain UI or create UI that is responsive to its parents.

Gum provides parent/children relationships which can be combined with layout values to easily center, stack, dock instances.

A parent is an instance that contains other instances, called its children. When a parent moves, its children move with it. For more, see [Parent](../../gum-elements/general-properties/parent.md).

### StackPanel Control

The key to creating layouts in Gum is to add your controls to containers. Containers can be used to create stacks, grids, add margins, and move children together. The StackPanel control is one of the most common controls since it automatically stacks its children, vertically by default or horizontally when its `Orientation` is changed.

This page starts with an empty MainScreen.

1. Select an instance in MainScreen, press `Delete`, and click **Yes** in the **Delete?** dialog. Repeat for each instance that was added on the previous pages.
2. Drag **StackPanel** from **Components** > **Controls** onto the Editor tab.

A StackPanelInstance now appears in MainScreen.

<figure><img src="../../../.gitbook/assets/13_10_43_53.png" alt=""><figcaption><p>StackPanel in MainScreen</p></figcaption></figure>

To add buttons to the StackPanel:

1. Drag **ButtonStandard** from **Components** > **Controls** onto **StackPanelInstance** in the Project tab.
2. Select the new button, then press `CTRL+C` and `CTRL+V` to add another button. Repeat until the stack of buttons extends past the bottom of the StackPanelInstance's outline.

<figure><img src="../../../.gitbook/assets/13_10_45_30.gif" alt=""><figcaption><p>ButtonStandard instances added to a StackPanel</p></figcaption></figure>

The buttons stack top to bottom inside StackPanelInstance.

<figure><img src="../../../.gitbook/assets/13_10_58_39.png" alt=""><figcaption></figcaption></figure>

### Alignment and Dock

The **Alignment** tab positions the selected instance. Its **Anchor** buttons change position only, and its **Dock** buttons change position and size.

To center the StackPanel:

1. Select **StackPanelInstance** and click the **Alignment** tab.
2. In the **Anchor** group, click the button in the middle of the grid.

The StackPanelInstance is now centered in MainScreen.

<figure><img src="../../../.gitbook/assets/image (91).png" alt=""><figcaption><p>Centering the StackPanel</p></figcaption></figure>

By default the StackPanelInstance has an absolute width and height of 150, regardless of what it contains. This presents two problems. First, the contents of the stack panel are not perfectly center aligned - buttons are left-aligned and also spill out of the bottom. Second, buttons which spill out of the bottom of the StackPanel will not receive events.

To see the event problem:

1. Click the **Preview** button.
2. Move the mouse over the buttons.

The buttons inside the StackPanelInstance react to the mouse, while the buttons outside it do not.

<figure><img src="../../../.gitbook/assets/13_11_16_10.gif" alt=""><figcaption><p>Button events in Gum</p></figcaption></figure>

Both problems can be solved by having the StackPanelInstance resize itself according to its children rather than using an absolute 150x150 size.

1. Select **StackPanelInstance** and click the **Alignment** tab.
2. In the **Dock** group, click the **Size to Children** button.

The StackPanelInstance now resizes to fit its buttons.

<figure><img src="../../../.gitbook/assets/13_11_18_48.png" alt=""><figcaption><p>Size to Children button in the Alignment tab</p></figcaption></figure>

### Spacing and Children Size

Now that the StackPanelInstance depends on its children's size, we can modify either the children or the spacing between the children and the stack panel updates automatically.

1. Select **StackPanelInstance** and click the **Variables** tab.
2. Change the `Spacing` variable.

The distance between each button changes.

<figure><img src="../../../.gitbook/assets/13_11_23_39.gif" alt=""><figcaption><p>Spacing adjusts the distance between each button</p></figcaption></figure>

Similarly we can adjust the size of buttons by changing their height, or adding text that wraps, and the stack adjusts dynamically, staying centered.

<figure><img src="../../../.gitbook/assets/13_11_25_40.gif" alt=""><figcaption></figcaption></figure>

Individual button spacing can be adjusted by changing its Y value. The total spacing is the total of spacing and Y.

<figure><img src="../../../.gitbook/assets/13_11_27_05.png" alt=""><figcaption></figcaption></figure>

