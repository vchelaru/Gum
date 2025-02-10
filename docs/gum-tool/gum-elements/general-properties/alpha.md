# Alpha

## Introduction

`Alpha` controls an instance's transparency. A fully opaque instance has an `Alpha` of 255. A fully transparent instance has an `Alpha` of 0.

<figure><img src="../../../.gitbook/assets/image (172).png" alt=""><figcaption><p>Sprites with Alpha of 255, 200, 150, 100, 50, and 0</p></figcaption></figure>

An object's transparency is a combination of its `Alpha`, [Blend](blend.md), and its [Source File](../sprite/source-file.md). Skia elements may also have transparent portions due to their shape (such as [ColoredCircle](../skia-standard-elements/coloredcircle.md) and [RoundedRectangle](../skia-standard-elements/roundedrectangle/)) as well as [dropshadows](../skia-standard-elements/general-properties/has-dropshadow.md).
