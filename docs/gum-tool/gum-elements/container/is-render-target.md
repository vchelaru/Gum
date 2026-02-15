# Is Render Target

## Introduction

`Is Render Target` controls whether all instances contained in this container render directly to the screen (if false), or if they first render to their own dedicated target before rendering to the screen (if true).

`Is Render Target` defaults to false (unchecked).

`Is Render Target` enables a number of graphical effects including:

* Container [Alpha](../general-properties/alpha.md) (transparency)
* Container [Blend](../general-properties/blend.md)
* Alpha-only [Blend](../general-properties/blend.md#alpha-only-blends) modes on instances contained in the Container

{% hint style="warning" %}
As of February 2025 the `Is Render Target` variable is considered experimental. You may experience issues when using this as it is being developed. Please report any problems you find through GitHub or on Discord.
{% endhint %}

## Is Render Target Clips Children

Containers with Is Render Target set to true automatically clip their children. This behavior is the same as setting [Clips Children](../general-properties/clips-children.md) to true. This happens because render targets internally create a texture which matches their size. Therefore, any items which are placed outside of the bounds of a render target container are not rendered.

<figure><img src="../../../.gitbook/assets/22_05 49 40.gif" alt=""><figcaption><p>Is Render Target set to true clips children</p></figcaption></figure>

## Using Is Render Target for Special Effects

Once a container is rendered to a render target, additional special effects can be applied at runtime. These include:

* Scaling
* Rotating
* Rendering portions using texture coordinates

For more information see the [SpriteRuntime RenderTargetTextureSource](../../../code/standard-visuals/spriteruntime/rendertargettexturesource.md) documentation.

