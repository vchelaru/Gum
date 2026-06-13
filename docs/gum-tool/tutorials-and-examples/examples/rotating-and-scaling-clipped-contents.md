# Rotating and Scaling Clipped Contents

## Introduction

The [Clips Children](../../gum-elements/general-properties/clips-children.md) and [Is Render Target](../../gum-elements/container/is-render-target.md) properties clip their children to an axis-aligned rectangle. Because the clip region stays aligned to the screen, rotating a container that clips its children leaves the clip region unrotated, so the result looks broken.

To rotate (or scale) a container _and_ keep its contents clipped, you render the container to a render target and display that render target through a `Sprite`. The `Sprite` can then be rotated, scaled, positioned, and stacked like any other Gum object, and the clipping rotates along with it.

<figure><img src="../../../.gitbook/assets/13_07 43 45.gif" alt=""><figcaption><p>Clipping doesn't support rotation</p></figcaption></figure>

## The Two-Object Pattern

This effect uses two objects:

1. A **Container** with **Is Render Target** set to `true`. This container holds the content you want to clip, and it clips its children automatically.
2. A **Sprite** with its **Render Target Texture Source** set to the container above. The `Sprite` displays the container's render target and can be freely rotated and scaled.

<figure><img src="../../../.gitbook/assets/13_07 45 56.gif" alt=""><figcaption><p>Sprite rotating while referencing a render target container</p></figcaption></figure>

The source container (which has Use Render Terget set to true) can even be made invisible:

<figure><img src="../../../.gitbook/assets/13_07 47 54.gif" alt=""><figcaption><p>Invisible container still used as a source render target</p></figcaption></figure>

## Step by Step

1. Create a **Container** and add the children you want to clip and rotate.
2. Select the **Container** and set its **Is Render Target** value to `true`. Its children are now clipped to the container's bounds.
3. Create a **Sprite**.
4. Select the **Sprite** and set its **Render Target Texture Source** value to the container created in step 1. The dropdown lists all containers in the current element that have **Is Render Target** set to `true`.
5. Set the **Sprite**'s [Rotation](../../gum-elements/general-properties/rotation.md) (and optionally its `Width` and `Height`) to rotate and scale the clipped contents.

<figure><img src="../../../.gitbook/assets/13_07 49 25.gif" alt=""><figcaption><p>Sprite with a Render Target Texture Source</p></figcaption></figure>

## Notes

* A container with **Is Render Target** set to `true` performs its layout and rendering even when its `Visible` value is `false`. Set the source container's `Visible` to `false` to hide the original while still displaying it through the `Sprite`.
* Children inside the render target container are not interactive while displayed through the `Sprite`, because input is handled by the original (invisible) container rather than the rotated `Sprite`.

## See Also

* [Clips Children](../../gum-elements/general-properties/clips-children.md)
* [Rotation](../../gum-elements/general-properties/rotation.md)
* [Is Render Target](../../gum-elements/container/is-render-target.md)
* [RenderTargetTextureSource](../../../code/standard-visuals/spriteruntime/rendertargettexturesource.md) — the same pattern in code
