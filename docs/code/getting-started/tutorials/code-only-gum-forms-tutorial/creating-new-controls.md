# Creating New Controls

## Introduction

Gum provides a number of common controls for creating UI. These can be combined to create new custom controls for encapsulating behavior or for reusability. This document shows how to create a new custom control.

## Creating a New Class

Our new control will be used to ask the user a question and let them enter a response in a TextBox. This can be used for operations such as entering a name for a character. We'll name this class `TextInputDialog` .

To create this class:

1. Select a folder where you would like to add this class. This tutorial will add it to a Components folder, but you are free to place this file wherever you'd like.
2. Add a new class named `TextInputDialog`.
3. Create the empty stub of a class as shown in the following code block.

```csharp
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using Gum.Forms.DefaultVisuals;

namespace MonoGameAndGum.Components;

// By inheriting from Panel we get a
// Visual object automatically instead
// of having to set one manually.
public class TextInputDialog : Panel
{
    Label prompt;

    public string PromptText
    {
        get => prompt.Text;
        set => prompt.Text = value;
    }

    public TextInputDialog()
    {
        // panels all size themselves according to their children.
        // let's give this extra size so it has a border.
        // we want 12 pixels on each side, so that's 12+12 = 24
        this.Width = 24;
        this.Height = 24;

        // Let's give this a background:
        var background = new NineSliceRuntime();
        this.AddChild(background);
        background.Dock(Gum.Wireframe.Dock.Fill);
        // We can use the built-in styling for the background:
        background.Texture = Styling.ActiveStyle.SpriteSheet;
        background.ApplyState(Styling.ActiveStyle.NineSlice.Panel);

        // An innerPanel stacks our children.
        // This innerPanel is sized according to
        // its children, and "this" will be 24 pixels
        // larger, creating a border.
        var innerPanel = new StackPanel();
        innerPanel.Spacing = 10;
        // Center this so the parent adds borders to all sides
        innerPanel.Anchor(Gum.Wireframe.Anchor.Center);
        this.AddChild(innerPanel);

        // Our label. We want to use a class
        // member so we can access this later if needed.
        prompt = new Label();
        innerPanel.AddChild(prompt);
        prompt.Text = "Enter text:";

        var textBox = new TextBox();
        innerPanel.AddChild(textBox);
        // Make the text box fill the available width:
        textBox.Width = 0;
        textBox.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        var buttonPanel = new StackPanel();
        innerPanel.AddChild(buttonPanel);
        buttonPanel.Spacing = 4;
        buttonPanel.Orientation = Orientation.Horizontal;
        
        var okButton = new Button();
        buttonPanel.AddChild(okButton);
        okButton.Text = "OK";

        var cancelButton = new Button();
        buttonPanel.AddChild(cancelButton);
        cancelButton.Text = "Cancel";
    }
}
```

Before we look at the individual parts of the code, let's add an instance to our game so we can see what it looks like. Add the following code to your Initialize method:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    GumUi.Initialize(this, DefaultVisualsVersion.V3);

<strong>    var dialog = new TextInputDialog();
</strong><strong>    dialog.AddToRoot();
</strong><strong>    dialog.Anchor(Gum.Wireframe.Anchor.Center);
</strong>}
</code></pre>
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACp1V32_aMBB-56-w-hSkyWq3PhV1EoWVVlvXCmi7V5McwcLYkeNAf4j_fXdJSExIxTpeSO777u7L3fmcpVLH7M5oMxIrGGWrXifLTfjIr41dpQcGPjDaWaMqxPPmowyGYOUa7YeOQ5iLTLknmWaidv8I50_fqgwytCY1c8f_aMGvLSbbGLs8AvORFclChpipk2QzJUMWKpGmbAov7lYnmRtKoUzMLtiD0KA67x2Gv19iBool1qwSh55kKr1TZyndQw5RkBwsvOgXg2OX30tXToRehaUHGLtka6EyKDjbvUwNhUG3kcktZMqfZeQWGOXreW8fuAEZL1yJVNBaWDYT4TK2JtMRoho27LfUMMGMMM60kysIuo1Y_SgaLKSKgtrVo9RGPjThMqBePksLc-pBbuLXUql2D_rIzAIqmbhXhZXl_dDJNdAb8ElipYPJAsC1eveTRL1OnHAQtLlXH8bz3nYbhZBag82RshAYKVzmBr8ENQ31iJC6f8nOTlsJfR0ujG2UoDDyAWgH9sPa1lF8ncWslPryqfxAWhWn8PBI--N28oNUMIdvFyeNgpDxyryU2abF27F8pZP_XYWlms3TQ6g43gXjUUuXIo2KNhROTF8TwD2Ag6hTaTTBZOJjUIJ6OzUPwmItm2OdOWf0p9pZz3Xt649pbfU6f95OuLcSNaFAo5HkvfEbY-UbrkuhGorN8ioPUMotXnypfvxK687L4-1MVY_vfzZbGwodgvqffL6nx_XNVd5BbsTcxNh2to2dSxfEGW5a-i8X7W49D2GN5_ROaBHjdMbV0s452WoClnB6fLylHVrbdjfG_prOUwVd9l7FKr-6NWFAR7HbqxawNQ5CBxEza7BWRsDWRkbsFkcRd7F8g4NlnAvjHoEifmH7t9kTWJpovNS8QlJ3ouIOqs_d3t6vuQWPejM1Y2NcG_aZDTQTKfiiS-hIGR6TiDYulXiKp5TF5UN7TUp2RWpkb4ePKBhasfmH_Hut5gMFwgYDo6gSxuq5MhuwV3j7epoKzXn8ptLceKBz2_kLv9x1XD8JAAA" target="_blank">Try on XnaFiddle.NET</a>

The TextInputDialog should appear in the center of the screen automatically.

<figure><img src="../../../../.gitbook/assets/20_07 16 41.png" alt=""><figcaption><p>TextInputDialog in the center of the screen</p></figcaption></figure>

The next section discusses some important parts about the code.

## Panel Inheritance

New controls can inherit from any of the standard Gum Forms controls, but the most common classes for inheritance are:

* Panel - Use Panel if you would like to have a default panel that sizes itself according to its children
* StackPanel - Use StackPanel if you would like a default panel that sizes itself according to its children and that stacks its children
* Window - Use Window if you would like a control that can be moved and resized by the user
* FrameworkElement - Use FrameworkElement if you would like to completely customize every aspect of your control. Inheriting from FrameworkElement is considered a more advanced approach since it requires a deeper understanding of Visual objects.

We use Panel since it provides a good mix of built-in functionality and flexibility.

## Margin

Our control uses a 12-pixel margin around the controls. This is accomplished by setting the position of the control and by anchoring `innerPanel` to `Center`.

We can see change our code to see how it might differ if we hadn't added these values. For example, we can completely remove our margin by deleting the `Width` and `Height` assignments as shown in the following code block:

```diff
public TextInputDialog()
{
-    // panels all size themselves according to their children.
-    // let's give this extra size so it has a border.
-    // we want 12 pixels on each side, so that's 12+12 = 24
-    this.Width = 24;
-    this.Height = 24;
    ...
```

Notice that this removes our margins around the content.

<figure><img src="../../../../.gitbook/assets/20_07 21 27.png" alt=""><figcaption><p>TextInputDialog without a margin</p></figcaption></figure>

If the margins are added back, we can also see why it is important to center our innerPanel. If we remove the `Anchor` call, we see that all 24 pixels of margin are added to the right and bottom rather than around each side.

```diff
public TextInputDialog()
{
    // panels all size themselves according to their children.
    // let's give this extra size so it has a border.
    // we want 12 pixels on each side, so that's 12+12 = 24
    this.Width = 24;
    this.Height = 24;

    // Let's give this a background:
    var background = new NineSliceRuntime();
    this.AddChild(background);
    background.Dock(Gum.Wireframe.Dock.Fill);
    // We can use the built-in styling for the background:
    background.Texture = Styling.ActiveStyle.SpriteSheet;
    background.ApplyState(Styling.ActiveStyle.NineSlice.Panel);

    // An innerPanel stacks our children.
    // This innerPanel is sized according to
    // its children, and "this" will be 24 pixels
    // larger, creating a border.
    var innerPanel = new StackPanel();
    innerPanel.Spacing = 10;
    // Center this so the parent adds borders to all sides
-   innerPanel.Anchor(Gum.Wireframe.Anchor.Center);
    this.AddChild(innerPanel);
    ...
```

<figure><img src="../../../../.gitbook/assets/20_07 23 57.png" alt=""><figcaption><p>innerPanel without setting its Anchor to Center</p></figcaption></figure>

It's worth noting that we use `Anchor` and not `Dock` because we do not want to change `innerPanel's` width and height behavior.

## NineSliceRuntime Background

Our `TextInputDialog` includes a `background` using a NineSliceRuntime. `background` is added directly to the `TextInputDialog` rather than to the `innerPanel` so that it covers the entire bounds of the `TextInputDialog`, effectively making it 12 pixels bigger than `innerPanel`.

Notice that `background` is styled by setting its `Texture` property and calling `ApplyState`. Gum provides default styling through its `Styling` class. If you would like to use a different Texture2D, you can assign it here rather than using the built-in styling. We use `Styling` for convenience in this tutorial. Similarly, we call `ApplyState` which sets the background's texture coordinates. If we leave this code out, then `background` uses the entire sprite sheet, resulting in a strange appearance.

```diff
 // Let's give this a background:
 var background = new NineSliceRuntime();
 this.AddChild(background);
 background.Dock(Gum.Wireframe.Dock.Fill);
 // We can use the built-in styling for the background:
 background.Texture = Styling.ActiveStyle.SpriteSheet;
-background.ApplyState(Styling.ActiveStyle.NineSlice.Panel);
```

<figure><img src="../../../../.gitbook/assets/20_07 33 32.png" alt=""><figcaption><p>The background uses the entire sprite sheet if the ApplyState method is not called</p></figcaption></figure>

For more information on working with NineSliceRuntime, see the [NineSliceRuntime page](../../../standard-visuals/ninesliceruntime.md).

## Prompt Label

`TextInputDialog` includes a `Label` named `prompt`. This is defined at class scope so that the prompt can be assigned per-instance.

For example, we could change our code in Game1 to set the prompt text as shown in the following code block:

```csharp
protected override void Initialize()
{
    GumUi.Initialize(this, DefaultVisualsVersion.V3);

    var dialog = new TextInputDialog();
    dialog.AddToRoot();
    dialog.Anchor(Gum.Wireframe.Anchor.Center);
    dialog.PromptText = "Enter character name:";
}
```
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACp1VTW_bMAy951cIPTnAILRbTw06IE3WtNi6Fk3a7qrajC1EkQxaTvqB_PdRtmMrjousyyUW-Ug-kRSZZ1LH7MZoMxFLmOTLQS8vRPTJLw0usz0BHxlt0aha41nzSQ5jQLki-b7hGOYiV_ZRZrlozD_S88dvdQQZosnM3PI_WvBLpGBrg4sDaj5BkSYypEi9NH9WMmShElnGZvBir3Wa27EUysTsjN0JDar33mP0-yWeQbEUzTK1ZOlElXVm0YW7K1TOSaEsrdwvBsvOv1em3AEGtS7b07FzthIqhxKz2YnUYhj0W5FsIjP-JCObkJevp4NdxRXIOLGVplatBLJnES5iNLmOSKthzX5LDVOKCPe5tnIJQb_laxhFo0SqKGhMPUgj5GMTLgJXyyeJMHc1KET8UirVbeEumSMQk6l9VZRZPgytXIE7AZ-mKC1MEwDbaT1MU_U6tcJC0GVeX4wXte23EiG1Biw0VSLIU7goBH4KGhjxEaGr_jk7Oe4EDHWYGGyloBTyEWgL-GFuGy8-z7JXKn5FV35ArfZTWnig3XY7-uFYMEuns6NWQpzwwrxU0Wbl6VC8ysi_Vympe_N4X1U-7xLxoKXNCOaSNhZWzF5ToDlAjagzabRTOxG_ByVcbWfmTiDlst3WubVGf6qcTV83tn6bNlKv8qfdgFuUxIkIGk0g78SvDMo3GpdCtRibxUXhoKJbHnyqvv-a69bKw21FdY1vf7ZLGwodgvqfeL6lh_XFddxRIaTYDrHpbVoz1y2IE5q07r8atNvxPIYVvdMboUVM3RnXQ7vA5MspoNO7z4drN0Mb2XZj7I7pIlTQZ--1r-rWnQED9xT7g3oAo7EQWoiYWQGijICtjIzYNbUizWL5BnvDuCDGPYDz-IXtbrNHQNfRtNS8RLrqROUOat7dztxvsCXO1WZm7o2xXbrPTKDKpllmzYAIE4GCcoBMkwc3KuouERn4N638HcjdQxq5Me3qMqOnzeLqozuRFboGtaJ3qw8wGKNY_0P8nf7gIwUCg5FRLn0G9VyZNeAFrWyPU8m58N9mWgj3eG56fwGevqS7dAkAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/20_07 38 58.png" alt=""><figcaption></figcaption></figure>

This tutorial limits the exposed variables to changing the prompt text, but a control like this may include additional exposed properties and events including:

* Setting the `textBox.Placeholder` property
* Event for when OK is clicked
* Event for when Cancel is clicked

## Sizing (RelativeToParent and RelativeToChildren)

The sizing of our `TextInputDialog` and its contained controls uses a mixture of parent and child dependencies. For example, `TextInputDialog` uses a `WidthUnits` of `RelativeToChildren` (the default for Panel), but its background uses a `WidthUnits` of `RelativeToParent` (as set by `Dock.Fill`).

This section provides a deeper dive into how sizing works on this control.

To understand how this works, we will begin at the top-level object - the `TextInputDialog` itself. As mentioned above, `Panels` size themselves according to their children. In this case `TextInputDialog` has two children:

* `background`
* `innerPanel`

The relationship with `background` creates an apparent infinite loop of dependencies. `TextInputDialog` depends on its children to determine its own size, but `background` depends on its parent to determine its own size. To resolve this circular dependency, `TextInputDialog` ignores `background` when determining its own size, and `background` determines its size only after `TextInputDialog` has determined its own size. In other words, the order of determining size is as follows:

1. `innerPanel` determines its own size according to its children
2. `TextInputDialog` determines its own size according to `innerPanel` , adding 24 pixels to both width and height
3. `background` determines its own size according to `TextInputDialog`

Therefore, `innerPanel` is ultimately the control that decides the size of the entire `TextInputDialog` and the `background`.

## TextBox Sizing

The contents of `innerPanel` have a similar sizing relationship to the contents of `TextInputDialog`. Since `innerPanel` is a `StackPanel`, it is (by default) sized according to its children. It has three children:

* `prompt`
* `textBox`
* `buttonPanel`

Similarly to the previous section, only prompt and `buttonPanel` set their own widths. `textBox` has its width set by its parent since its `WidthUnits` is assigned to `RelativeToParent`.

Therefore, the order of determining width for innerPanel is as as follows:

1. `prompt` and `buttonPanel` each determine their widths according to their own text/children
2. `innerPanel` sets its width according to the largest of either `prompt` or `buttonPanel`
3. `textBox` sizes itself according to `innerPanel`

If we change our prompt, notice that the textBox adjusts appropriately:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    GumUi.Initialize(this, DefaultVisualsVersion.V3);

    var dialog = new TextInputDialog();
    dialog.AddToRoot();
    dialog.Anchor(Gum.Wireframe.Anchor.Center);

<strong>    dialog.PromptText = "This is a much longer prompt, so the dialog is wider:";
</strong>}
</code></pre>
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACp1V30_bMBB-719h7SmVkAUbT1RMKu0oaGMgWmCvJrkmVl07cpyWH-r_vrskTdw0qGMVEsndd3df7s6f80zqmN0YbSZiCZN8OejlhQkf-aWxy2zPwEdGO2tU7fGi-SSHMVi5Qvt-4BjmIlfuUWa5aMI_8vPHb3UFGVqTmbnjf7TglxaLrY1dHHDziRVpIkOs1EvzZyVDFiqRZWwGL-5ap7kbS6FMzM7YndCgeu89hr9f4hkUS61Zpg4jyVRFZ85SubvCRUkKZxlFvxgcO_9ehXICDGpftudj52wlVA4lZrNTqcUw6LcquURm_ElGLsEsX08Hu44rkHHiKk_tWgnLnkW4iK3JdYReDWv2W2qYYkW4z7WTSwj6rVzDKBolUkVBE-pBGiMfm3AR0CyfpIU5zaAw8UupVHcEfWRuAZlM3avCzvJh6OQK6A34NLXSwTQBcJ3RwzRVr1MnHARd4fWH8WK2_VYjpNZgC0_VCMwULgqD34IGhnxESNM_ZyfHnYChDhNjWy0ojXwE2oH9sLdNFp9nuSsVv2IrP6BW5ykjPNDuun35QSyYw7ezL62GkPHCvFTVZuXboXpVkP9dpaXezeN9V3m8S8SDli5DGDVtLJyYvaaAOoCLqDNpNLnJxO9BCZrtzNwJi71sr3XunNGfGmez102sv6aN1Zv8aTfg1krkhASNRpD3xq-MlW8ol0K1GJvFRZGgolu--FT9_DXXbZSH25rqGd_-bI82FDoE9T_1_EgP65vruqPCiLUJseltWppLF8QJKi39r4R2K89jWOE5vRFaxLidcS3aBSZfTsGSnx4frklDG9v2xtiV6aJU0Gfvda7qqzsLBnQU-4NagK1xEDqImFmBtTICtjIyYte4iqjF8g32xLggxj0AZTxiu7fZI1jaaLzUvEbSdKLyDmrO3Y7uN9gSR7OZmXtjXJfvMwpUxTSXGQ1xhswZ_gm2zMOEKaNpJKWKHLHMoG7BljHC1tgeS1JSb5HIwO9EVe9Abx_SiGSc5jbDo8_i6qG70RW6BrWqd7sPMBhbsf6H-jv7w0cKhA1GRlF7jdVzZdZgL_BK9ziVnIv8baaFcY_npvcX24DXMJQJAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/20_08 01 24.png" alt=""><figcaption><p>Wider prompt makes the TextInputDialog wider</p></figcaption></figure>

## Conclusion

As this document shows, creating new controls can require an advanced understanding of layouts. If you aren't familiar, it can be helpful to read through the various [properties available on Gum elements](../../../../gum-tool/gum-elements/). Similarly, it can be good to work through the [Gum tool intro tutorials](../../../../gum-tool/tutorials-and-examples/intro-tutorials/) to understand the basics of Gum layout. As always, the best way to learn is to try things out and make mistakes, so don't be afraid to experiment.
