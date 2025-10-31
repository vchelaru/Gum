# Custom NineSlice

## Introduction

Although Gum includes a standard  NineSlice element, the Gum layout system can be used to create a custom NineSlice component. Such a component could be used if additional flexibility beyond what is provided by the standard NineSlice is needed.

## Creating the Component

As implied by the name, the NineSlice element is composed of nine Sprites. First we'll create the component:

1. Open Gum
2. Open or create a new Gum project
3. Right-click on the **Components** folder
4. Select **Add Component**
5. Name the Component "CustomNineSlice"

![Component named CustomNineSlice](<../../../.gitbook/assets/CustomNineSlice1 (1).PNG>)

## Adding Corner Sprites

Next, we'll add corner Sprite instances to our CustomNineSlice. We'll be using the alignment tab to position Sprites. The alignment tab provides a quick way to place objects, but the same could be achieved using the following variables individually:

* [Width Units](../../gum-elements/general-properties/width-units.md)
* [X Origin](../../gum-elements/general-properties/x-origin.md)
* [X Units](../../gum-elements/general-properties/x-units.md)
* [Height Units](../../gum-elements/general-properties/height-units.md)
* [Y Origin](../../gum-elements/general-properties/y-origin.md)
* [Y Units](../../gum-elements/general-properties/y-units.md)

To create the corner Sprites:

1. Drag+drop a **Sprite** element onto the **CustomNineSlice** component

![Drag+drop a Sprite onto CustomNineSlice](../../../.gitbook/assets/DragDropSprite.png)

2. Click the **Alignment** tab
3.  Anchor the newly-created Sprite to the top-left of its container&#x20;

    <figure><img src="../../../.gitbook/assets/AnchorTopLeft (1).png" alt=""><figcaption><p>Anchor the Sprite to the top-left of its container</p></figcaption></figure>
4. Repeat the steps above three more times, creating one **Sprite** for each of the four corners, anchoring each one to their respective corner

![Four sprites in CustomNineSlice](<../../../.gitbook/assets/FourCornerSprites (1).PNG>)

Notice if we resize our CustomNineSlice component, each of the four Sprites remains in its respective corner.

![Resized CustomNineSlice keeping its Sprites in the corner](../../../.gitbook/assets/CustomNineSliceResized.PNG)

## Adding Edge Sprites

Next we'll add the four Sprites which will sit on the edges of our component:

1. Drag+drop a **Sprite** element onto the **CustomNineSlice** component
2. Click on the **Alignment** tab
3.  Dock the newly-created **Sprite** to the top of its container. Docking sets the width of the sprite to match the width of the component. We'll address this in the next step.\


    ![Click the top dock tab](../../../.gitbook/assets/DockTop.png)
4.  To accommodate for the corner Sprites, we need to adjust the width of the top Sprite. Set the newly-created Sprite's `Width` to `-128`. Since the Sprite uses a `Width Units` of `Relative to Parent`, setting the value to -128 makes the Sprite 128 units smaller than its parent. We picked 128 because each of the corner sprites is 64.\


    ![Top Sprite in CustomNineSlice](<../../../.gitbook/assets/TopStretched (1).PNG>)
5. Repeat the above steps, but instead setting the dock to create sprites on the left, right, and bottom. adjust width and height values as necessary.

## Adding the Center Sprite

Finall we'll add the center Sprite:

1. Drag+drop a Sprite element onto the CustomNineSlice component
2. Click on the alignment tab
3. Dock the newly-created Sprite to the center of its container.
4. Set both the newly created Sprite's Width and Height to -128

Now the Sprites stretch and adjust whenever the CustomNineSlice is resized. ![](<../../../.gitbook/assets/CustomNineSliceResize (1).gif>)

## Assigning values on CustomNineSlice

Unlike the regular NineSlice, changing the texture values requires a considerable amount of variable assignment. To change the CustomNineSlice to use 9 separate textures, the following values must be set:

* Each of the Sprite instances must have its SourceFile value set
* The edge Sprites will have to have their Width and Height values modified to account for the possible resizing of the corner sprites
* The center Sprite will have to have both its Width and Height values modified

If using a sprite sheet, then all of the work above will need to be done plus the texture coordinate values will need to be modified.
