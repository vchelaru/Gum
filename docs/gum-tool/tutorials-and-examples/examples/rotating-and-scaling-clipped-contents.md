# Rotating and Scaling Clipped Contents

## Introduction

The [Clips Children](../../gum-elements/general-properties/clips-children.md) and [Is Render Target](../../gum-elements/container/is-render-target.md) properties clip their children to an axis-aligned rectangle. Because the clip region stays aligned to the screen, rotating a container that clips its children leaves the clip region unrotated, so the result looks broken.

To rotate (or scale) a container *and* keep its contents clipped, you render the container to a render target and display that render target through a `Sprite`. The `Sprite` can then be rotated, scaled, positioned, and stacked like any other Gum object, and the clipping rotates along with it.

{% hint style="warning" %}
**Image placeholder:** Animated gif showing a container with `Clips Children` set to `true` being rotated, with the clip region staying axis-aligned (the broken result).
{% endhint %}

## The Two-Object Pattern

This effect uses two objects:

1. A **Container** with **Is Render Target** set to `true`. This container holds the content you want to clip, and it clips its children automatically.
2. A **Sprite** with its **Render Target Texture Source** set to the container above. The `Sprite` displays the container's render target and can be freely rotated and scaled.

{% hint style="warning" %}
**Image placeholder:** Screenshot of the project tree showing the render target Container and the Sprite that references it.
{% endhint %}

## Step by Step

1. Create a **Container** and add the children you want to clip and rotate.
2. Select the **Container** and set its **Is Render Target** value to `true`. Its children are now clipped to the container's bounds.
3. Create a **Sprite**.
4. Select the **Sprite** and set its **Render Target Texture Source** value to the container created in step 1. The dropdown lists all containers in the current element that have **Is Render Target** set to `true`.
5. Set the **Sprite**'s [Rotation](../../gum-elements/general-properties/rotation.md) (and optionally its `Width` and `Height`) to rotate and scale the clipped contents.

{% hint style="warning" %}
**Image placeholder:** Animated gif showing the **Render Target Texture Source** dropdown being assigned and the resulting Sprite being rotated, with the clipped contents rotating correctly.
{% endhint %}

## Notes

* A container with **Is Render Target** set to `true` performs its layout and rendering even when its `Visible` value is `false`. Set the source container's `Visible` to `false` to hide the original while still displaying it through the `Sprite`.
* Children inside the render target container are not interactive while displayed through the `Sprite`, because input is handled by the original (invisible) container rather than the rotated `Sprite`.

## See Also

* [Clips Children](../../gum-elements/general-properties/clips-children.md)
* [Rotation](../../gum-elements/general-properties/rotation.md)
* [Is Render Target](../../gum-elements/container/is-render-target.md)
* [RenderTargetTextureSource](../../../code/standard-visuals/spriteruntime/rendertargettexturesource.md) — the same pattern in code
