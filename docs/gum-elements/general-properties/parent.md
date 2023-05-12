# Parent

## Introduction

Parenting allows UI elements to be positioned and sized according to other UI elements. Parenting hierarchies can go many levels deep and the parent/child relationship can be visualized by the white line connecting the parent to the child when the child is selected.

## Example - Drag+Drop in the Tree View

To change the parent/child relationship in the tree view:

1. Select a child
2. Drag+drop the child onto the desired parent

<figure><img src="../../.gitbook/assets/11_20 21 41.gif" alt=""><figcaption></figcaption></figure>

The child can be detached from its parent by drag+dropping it onto the Component.

<figure><img src="../../.gitbook/assets/11_20 22 36.gif" alt=""><figcaption></figcaption></figure>

Drag+dropping onto a parent may set the Parent property to an instance inside of the parent's Component type sets its Default Child Container value. For more information see the [Default Child Container](../component/default-child-container.md) page.

## Example - Using the Dropdown

To set a parent/child relationship:

1. Select the child
2. Change the Parent property to the desired parent:

<figure><img src="../../.gitbook/assets/11_20 20 04.gif" alt=""><figcaption></figcaption></figure>
