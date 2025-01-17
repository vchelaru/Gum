# Visible

## Introduction

The `Visible` variable controls whether an object and its children appear.

## Example

Setting Visible to false hides the selected instance.

![Visible property controls whether an instance is hidden or shown](<../../../.gitbook/assets/16_19 40 08.gif>)

## Parent/Child Visibility

Setting a parent's `Visible` variable to false also hides all children. Note that this does not explicitly set the Visible property to false for all children, but a child's effective visibility depends on its parent.

![A parent's Visible value controls whether children are visible](<../../../.gitbook/assets/16_19 45 06.gif>)

## Visibility and Stacking

If an instance is part of a parent which stacks its children (has a `Children Layout` of `Left to Right Stack` or `Top to Bottom Stack`), then it will no longer be considered when stacking siblings if it is invisible. In other words, making an item invisible removes it from the stack.

<figure><img src="../../../.gitbook/assets/16_19 48 47.gif" alt=""><figcaption><p>Invisible siblings are not considered in stacking</p></figcaption></figure>

If the stack contains children which use a `Width Units` of `Ratio`, then hiding any of the siblings results in the children with ratio width adjusting to occupy the extra space.

<figure><img src="../../../.gitbook/assets/16_19 52 56.gif" alt=""><figcaption><p>Invisible siblings are not considered when calculating used space for <code>Width Units</code> of <code>Ratio</code>.</p></figcaption></figure>

## Selecting Invisible Objects

An invisible object can be selected by clicking on it in the Project tab or in the Editor tab.

<figure><img src="../../../.gitbook/assets/16_20 04 41.gif" alt=""><figcaption><p>Invisible objects can be selected</p></figcaption></figure>

Visible items are given preferential selection even if they are ordered behind invisible items.

<figure><img src="../../../.gitbook/assets/16_20 05 57.gif" alt=""><figcaption><p>The visible ColoredRectangle is selected before the invisible container</p></figcaption></figure>
